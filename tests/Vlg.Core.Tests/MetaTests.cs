using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Vlg.Core.Bench;
using Vlg.Core.Config;
using Vlg.Core.Meta;
using Vlg.Core.Run;
using Vlg.Core.Sim;
using Xunit;

namespace Vlg.Core.Tests;

public class MetaTests
{
    static string MetaJson => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "meta.json"));
    static MetaConfig Meta => MetaConfig.Parse(MetaJson);
    static MetaUpgradeDef Up(MetaConfig c, string id) => c.Upgrades.Single(u => u.Id == id);

    [Fact]
    public void Shipped_meta_json_parses_with_slot_and_stat_upgrades()
    {
        var c = Meta;
        Assert.Equal(new[] { "active_slot", "usable_slot", "damage", "max_hp", "pickup", "start_credits" }, c.Upgrades.Select(u => u.Id));
        Assert.Equal(3f, c.LoopReward(1));
        Assert.Equal(7f, c.LoopReward(3));
        Assert.ThrowsAny<Exception>(() => MetaConfig.Parse(MetaJson.Replace("\"creditValue\": 1,", "\"creditValue\": 1, \"bogus\": 1,")));
        Assert.Throws<FormatException>(() => MetaConfig.Parse(MetaJson.Replace("\"kind\": \"activeSlots\", \"perRank\": 1", "\"kind\": \"activeSlots\", \"perRank\": 2")));
    }

    [Fact]
    public void Buying_ranks_costs_more_each_rank_and_stops_at_max()
    {
        var c = Meta;
        var shop = new MetaShop(c, new MetaSave { MetaCredits = 1000 });
        var slot = Up(c, "active_slot");
        Assert.Equal(40, shop.NextCost(slot));
        Assert.True(shop.Buy(slot));
        Assert.Equal(80, shop.NextCost(slot));
        Assert.True(shop.Buy(slot));
        Assert.True(shop.Buy(slot));
        Assert.Equal(-1, shop.NextCost(slot));
        Assert.False(shop.Buy(slot));
        Assert.Equal(1000f - 40 - 80 - 120, shop.Save.MetaCredits);

        var poor = new MetaShop(c, new MetaSave { MetaCredits = 5 });
        Assert.False(poor.Buy(Up(c, "damage")));
        Assert.Equal(0, poor.Save.Rank("damage"));
    }

    [Fact]
    public void Bonuses_sum_ranks_times_per_rank()
    {
        var c = Meta;
        var save = new MetaSave();
        save.Ranks["usable_slot"] = 2;
        save.Ranks["damage"] = 4;
        save.Ranks["max_hp"] = 10;
        save.Ranks["pickup"] = 99; // clamped to max rank 5
        save.Ranks["start_credits"] = 1;
        var b = new MetaShop(c, save).Bonuses();
        Assert.Equal((0, 2), (b.ActiveSlots, b.UsableSlots));
        Assert.Equal(0.04f, b.Damage, 4);
        Assert.Equal(0.3f, b.MaxHp, 4);
        Assert.Equal(0.25f, b.Pickup, 4);
        Assert.Equal(10f, b.StartCredits);
    }

    [Fact]
    public void Save_round_trips_and_old_or_odd_files_still_load()
    {
        var s = new MetaSave { MetaCredits = 12.5f, Loadout = new[] { "vinyl", "slap" }, Ultimate = "encore", BestLoop = 3 };
        s.Ranks["damage"] = 2;
        var back = MetaSave.Parse(s.ToJson());
        Assert.Equal((12.5f, "encore", 3, 2), (back.MetaCredits, back.Ultimate, back.BestLoop, back.Rank("damage")));
        Assert.Equal(new[] { "vinyl", "slap" }, back.Loadout);

        var odd = MetaSave.Parse("{ \"metaCredits\": 5, \"futureThing\": true }");
        Assert.Equal(5f, odd.MetaCredits);
        Assert.Empty(odd.Ranks);
        Assert.Equal(0f, MetaSave.Parse("").MetaCredits);

        odd.AddRun(3f, 4);
        Assert.Equal((8f, 4, 1), (odd.MetaCredits, odd.BestLoop, odd.Runs));
    }

    static RunConfig Quiet => TestConfig.Run with { Stages = new[] { new StageDef { Kind = StageKind.Mobs, DurationSec = 1000 } } };

    [Fact]
    public void Meta_bonuses_shape_the_run()
    {
        var b = new MetaBonuses { ActiveSlots = 1, UsableSlots = 3, Damage = 0.1f, MaxHp = 0.3f, Pickup = 0.2f, StartCredits = 20 };
        var sim = new RunSim(Quiet, TestConfig.Hero, TestConfig.Items, 1, null, null, null, Meta, b);
        Assert.Equal((3, 5), (sim.Inventory.ActiveSlots, sim.Inventory.UsableSlots));
        Assert.Equal(sim.Hero.MaxHp * 1.3f, sim.HeroMaxHp, 3);
        Assert.Equal(sim.HeroMaxHp, sim.HeroHp);
        Assert.Equal(1.1f, sim.DamageMultiplier, 4);
        Assert.Equal(20f, sim.Inventory.Credits);
    }

    [Fact]
    public void Kills_drop_meta_orbs_and_completed_loops_pay_meta_credits()
    {
        var meta = Meta with { CreditDropChance = 1f };
        var c = Quiet with { Stages = new[] { new StageDef { Kind = StageKind.Mobs, DurationSec = 1000 } } };
        var sim = new RunSim(c, TestConfig.Hero, TestConfig.Items, 1, null, null, null, meta);
        var rng = new XorShift32(2);
        for (int i = 0; i < 3; i++) sim.Enemies.Spawn(new Vector2(1 + i * 0.2f, 0), 0f, ref rng);
        for (int i = 0; i < 20; i++) sim.Step(0.05f, Vector2.Zero);
        Assert.Equal(3f, sim.MetaCreditsEarned, 3);

        sim.DevSkipStage(); // single-stage loop: wraps to loop 2 → loop 1 completed
        Assert.Equal(3f + meta.LoopReward(1), sim.MetaCreditsEarned, 3);
        sim.DevSkipStage();
        Assert.Equal(3f + meta.LoopReward(1) + meta.LoopReward(2), sim.MetaCreditsEarned, 3);
    }

    [Fact]
    public void No_meta_config_means_no_meta_credits()
    {
        var sim = new RunSim(Quiet, TestConfig.Hero, TestConfig.Items, 1);
        sim.DevSkipStage();
        Assert.Equal(0f, sim.MetaCreditsEarned);
    }

    [Fact]
    public void Run_args_skip_the_hub()
    {
        Assert.False(LaunchOptions.Parse(Array.Empty<string>()).WantsRun);
        Assert.True(LaunchOptions.Parse(new[] { "--run" }).WantsRun);
        Assert.True(LaunchOptions.Parse(new[] { "--skill-level", "3" }).WantsRun);
        Assert.True(LaunchOptions.Parse(new[] { "--stage", "4" }).WantsRun);
        Assert.False(LaunchOptions.Parse(new[] { "--seed", "3", "--preset", "medium" }).WantsRun);
    }
}
