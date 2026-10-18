using System;
using System.Linq;
using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Run;
using Vlg.Core.Sim;
using Xunit;

namespace Vlg.Core.Tests;

/// <summary>Items in a run: credits, passives, actives, usables, hex, poison, shop phase.</summary>
public class RunItemTests
{
    const float Dt = 0.05f;

    static RunConfig Quiet => TestConfig.Run with { Stages = new[] { new StageDef { Kind = StageKind.Mobs, DurationSec = 1000 } } };

    static RunSim Sim(RunConfig? config = null, ItemsConfig? items = null, params string[] loadout) =>
        new(config ?? Quiet, TestConfig.Hero, items ?? TestConfig.Items, 1, null, loadout.Length > 0 ? loadout : new[] { "cowbell" });

    static int Place(RunSim sim, Vector2 p, float hp = 1e5f)
    {
        var rng = new XorShift32(8);
        sim.Enemies.Spawn(p, hp, ref rng);
        return sim.Enemies.Count - 1;
    }

    [Fact]
    public void Kills_drop_coins_that_become_credits_with_the_gold_tooth_bonus()
    {
        var sim = Sim(items: TestConfig.Items with { CreditDropChance = 1f });
        sim.DevGrant("gold_tooth");
        for (int i = 0; i < 4; i++) Place(sim, new Vector2(20 + i * 0.3f, 0), 0f); // beyond the pickup radius
        sim.Step(Dt, Vector2.Zero);
        Assert.Equal(4, sim.Coins.Count);

        for (int i = 0; i < 60; i++) sim.Step(Dt, new Vector2(1, 0));
        Assert.Equal(4f * 1.2f, sim.Inventory.Credits, 3);
    }

    [Fact]
    public void Move_speed_regen_and_armor_passives()
    {
        var sim = Sim();
        float baseSpeed = sim.HeroSpeed;
        sim.DevGrant("knockoff_sneakers");
        Assert.Equal(baseSpeed * 1.08f, sim.HeroSpeed, 4);

        var hurt = Sim(TestConfig.Run with { Stages = Quiet.Stages, Walker = Quiet.Walker with { ContactDps = 100, Hp = 1e6f } });
        Place(hurt, new Vector2(hurt.Config.Hero.Radius + hurt.Config.Walker.Radius, 0));
        hurt.Step(Dt, Vector2.Zero);
        float plain = hurt.HeroMaxHp - hurt.HeroHp;

        var armored = Sim(TestConfig.Run with { Stages = Quiet.Stages, Walker = Quiet.Walker with { ContactDps = 100, Hp = 1e6f } });
        armored.DevGrant("knights_pauldron");
        armored.DevGrant("herbal_tea");
        Place(armored, new Vector2(armored.Config.Hero.Radius + armored.Config.Walker.Radius, 0));
        armored.Step(Dt, Vector2.Zero);
        Assert.Equal(plain * 0.92f - 0.5f * Dt, armored.HeroMaxHp - armored.HeroHp, 3);
    }

    [Fact]
    public void Life_steal_heals_from_damage_dealt()
    {
        var sim = Sim(TestConfig.Run with { Stages = Quiet.Stages, Walker = Quiet.Walker with { ContactDps = 100 } });
        sim.DevGrant("vampire_tooth");
        int toucher = Place(sim, new Vector2(sim.Config.Hero.Radius + sim.Config.Walker.Radius, 0));
        for (int i = 0; i < 5; i++) sim.Step(Dt, Vector2.Zero);
        sim.Enemies.Health[toucher] = 0f; // stop the contact damage
        sim.Step(Dt, Vector2.Zero);
        float before = sim.HeroHp;
        Assert.True(before < sim.HeroMaxHp);

        Place(sim, new Vector2(12, 0)); // in cowbell range, too far to reach the hero in time
        for (int i = 0; i < 50; i++) sim.Step(Dt, Vector2.Zero); // cowbell every 2 beats, 9 per note
        Assert.True(sim.HeroHp > before);
    }

    [Fact]
    public void Clockwork_heart_speeds_skill_and_active_recharge()
    {
        var sim = Sim();
        sim.DevGrant("clockwork_heart");
        sim.Step(Dt, Vector2.Zero);
        Assert.Equal(1f / 0.92f, sim.Skills.RateMultiplier, 4);
        Assert.Equal(1f / 0.92f, sim.Actives.RateMultiplier, 4);
    }

    [Fact]
    public void Owned_active_items_fire_on_their_own()
    {
        var sim = Sim();
        int e = Place(sim, new Vector2(4, 0));
        sim.DevGrant("tesla_backpack");
        for (int i = 0; i < 5; i++) sim.Step(Dt, Vector2.Zero);
        int tesla = Array.IndexOf(sim.OwnedActives.Items, sim.Items.IndexOf("tesla_backpack"));
        Assert.Equal(SkillKind.Chain, sim.Actives.Form(tesla)!.Kind);
        Assert.Null(sim.Actives.Form(Array.IndexOf(sim.OwnedActives.Items, sim.Items.IndexOf("toy_drone"))));
        Assert.True(sim.Enemies.Health[e] <= 1e5f - 14f);
    }

    [Fact]
    public void Tesla_chain_jumps_between_close_enemies()
    {
        var sim = Sim(loadout: "distortion");
        sim.DevGrant("tesla_backpack");
        int a = Place(sim, new Vector2(6, 0)), b = Place(sim, new Vector2(9, 0)), c = Place(sim, new Vector2(30, 0));
        sim.Step(Dt, Vector2.Zero);
        Assert.Equal(new[] { 1e5f - 14f, 1e5f - 14f, 1e5f }, new[] { sim.Enemies.Health[a], sim.Enemies.Health[b], sim.Enemies.Health[c] });
    }

    [Fact]
    public void Socketed_modifier_reaches_the_skill_and_hex_marks_hit_enemies()
    {
        var sim = Sim(loadout: "low_end");
        sim.Progress.DevSetAllSkills(1);
        sim.DevGrant("bell_shard");
        sim.DevGrant("hex_needle");
        sim.Socket(0, 0);
        Assert.False(sim.CanSocket(sim.Inventory.Stash[0], 0)); // 1 socket at level 1
        sim.Step(Dt, Vector2.Zero);
        Assert.Equal(7f * 1.25f, sim.Skills.Form(0)!.Stats.Radius, 3);

        sim.Unsocket(0, 0);
        sim.Socket(sim.Inventory.Stash.ToList().IndexOf(sim.Items.IndexOf("hex_needle")), 0);
        int e = Place(sim, new Vector2(2, 0));
        for (int i = 0; i < 80 && sim.Enemies.VulnTimer[e] <= 0f; i++) sim.Step(Dt, Vector2.Zero); // next ring within a bar
        Assert.True(sim.Enemies.VulnTimer[e] > 0f);
        Assert.Equal(0.1f, sim.Enemies.Vuln[e], 4);
    }

    [Fact]
    public void Plague_flask_kills_leave_poison_puddles()
    {
        var sim = Sim(loadout: "low_end");
        sim.DevGrant("plague_flask");
        sim.Socket(0, 0);
        Place(sim, new Vector2(2, 0), 1f);
        for (int i = 0; i < 30 && sim.Kills == 0; i++) sim.Step(Dt, Vector2.Zero);
        Assert.Equal(1, sim.Kills);
        Assert.Contains(Enumerable.Range(0, sim.Zones.Count), i => sim.Zones.SourceOf(i) == RunSim.PoisonSource);
    }

    [Fact]
    public void Branch_that_drops_a_tag_moves_its_modifier_to_the_stash()
    {
        var sim = Sim(loadout: "low_end");
        sim.DevGrant("hex_needle"); // fits area: Low End yes, Earthquake (zone) no
        sim.Socket(0, 0);
        sim.Progress.DevSetAllSkills(5);
        sim.ChooseLevelUp(2); // Earthquake
        Assert.Empty(sim.Inventory.Sockets(0));
        Assert.Single(sim.Inventory.Stash);
    }

    [Fact]
    public void Usables_queue_into_the_step_and_respect_their_cooldown()
    {
        var sim = Sim();
        sim.DevGrant("energy_drink");
        Assert.False(sim.UseItem(1));
        Assert.True(sim.UseItem(0));
        sim.Step(Dt, Vector2.Zero);
        Assert.Equal(sim.Items.IndexOf("energy_drink"), sim.Usables.UsedItem);
        Assert.True(sim.Usables.RushActive);
        Assert.Equal(1.4f, sim.Skills.RateMultiplier, 4);
        Assert.Equal(sim.Hero.Speed * 1.3f, sim.HeroSpeed, 3);
        Assert.False(sim.UseItem(0));

        for (int i = 0; i < 70; i++) sim.Step(Dt, Vector2.Zero); // 3 s rush, then crash
        Assert.False(sim.Usables.RushActive);
        Assert.Equal(sim.Hero.Speed * 0.8f, sim.HeroSpeed, 3);
    }

    [Fact]
    public void Smoke_makes_enemies_chase_the_throw_point()
    {
        var sim = Sim(TestConfig.Run with { Stages = Quiet.Stages, Walker = Quiet.Walker with { ContactDps = 0 } });
        sim.DevGrant("smoke_bomb");
        int e = Place(sim, new Vector2(10, 0));
        sim.UseItem(0);
        sim.Step(Dt, Vector2.Zero);
        for (int i = 0; i < 20; i++) sim.Step(Dt, new Vector2(-1, 0)); // hero walks away to -x
        Assert.True(sim.Usables.SmokeActive);
        Assert.True(sim.Enemies.Position[e].X > 0f, "walker heads for the smoke at the origin, not past it");
    }

    [Fact]
    public void Holy_water_burns_ahead_and_singularity_pulls_then_pops()
    {
        var sim = Sim(loadout: "distortion");
        sim.DevGrant("holy_water");
        sim.DevGrant("pocket_singularity");
        sim.SetView(0f, RunSim.DefaultAspect); // aim (0, -1)
        sim.UseItem(0);
        sim.Step(Dt, Vector2.Zero);
        int pool = Enumerable.Range(0, sim.Zones.Count).Single(i => sim.Zones.SourceOf(i) == RunSim.UsableSource);
        Assert.Equal(new Vector2(0, -6), sim.Zones.Center[pool]);

        int e = Place(sim, new Vector2(10, -5));
        sim.UseItem(1);
        sim.Step(Dt, Vector2.Zero);
        Assert.True(sim.Usables.SingularityActive);
        sim.Step(Dt, Vector2.Zero);
        Assert.True(sim.Enemies.Push[e].X < 0f);
        float hp = sim.Enemies.Health[e];
        for (int i = 0; i < 70 && !sim.Usables.PoppedThisStep; i++) sim.Step(Dt, Vector2.Zero);
        Assert.True(sim.Usables.PoppedThisStep);
        Assert.True(sim.Enemies.Health[e] < hp);
    }

    [Fact]
    public void Dev_jump_opens_the_shop_or_a_chest_directly()
    {
        var sim = new RunSim(TestConfig.Run, TestConfig.Hero, TestConfig.Items, 1);
        sim.DevJumpToStage(4);
        Assert.Equal(RunPhase.Shop, sim.Phase);
        Assert.Equal(sim.Items.Shop.Size, sim.Shop.Offers.Count);
        var chest = new RunSim(TestConfig.Run, TestConfig.Hero, TestConfig.Items, 1);
        chest.DevJumpToStage(2);
        Assert.Equal((RunPhase.Chest, 3), (chest.Phase, chest.ChestOffer.Count));
    }

    [Fact]
    public void Shop_stage_opens_offers_and_buying_needs_credits()
    {
        var c = Quiet with { Stages = new[] { new StageDef { Kind = StageKind.Mobs, DurationSec = 0.1f }, new StageDef { Kind = StageKind.Shop } } };
        var sim = Sim(c);
        for (int i = 0; i < 5 && sim.Phase == RunPhase.Playing; i++) sim.Step(Dt, Vector2.Zero);
        Assert.Equal(RunPhase.Shop, sim.Phase);
        Assert.Equal(sim.Items.Shop.Size, sim.Shop.Offers.Count);
        Assert.False(sim.Buy(0));
        sim.DevAddCredits(1000);
        int item = sim.Shop.Offers[0].Item;
        Assert.True(sim.Buy(0));
        Assert.True(sim.Inventory.Owns(item));
        Assert.True(sim.Reroll());
        sim.ContinueStage();
        Assert.Equal(RunPhase.Playing, sim.Phase);
    }
}
