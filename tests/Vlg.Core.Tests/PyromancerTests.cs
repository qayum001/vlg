using System;
using System.Linq;
using System.Numerics;
using Vlg.Core.Bench;
using Vlg.Core.Config;
using Vlg.Core.Meta;
using Vlg.Core.Run;
using Vlg.Core.Sim;
using Xunit;

namespace Vlg.Core.Tests;

public class PyromancerTests
{
    const float Dt = 0.05f;
    static RunConfig Quiet => TestConfig.Run with { Stages = new[] { new StageDef { Kind = StageKind.Mobs, DurationSec = 1000 } } };

    static RunSim Sim(string ultimate = "inferno", int rank = 1)
    {
        var sim = new RunSim(Quiet, TestConfig.Pyromancer, TestConfig.Items, 1, null, new[] { "meteor" }, ultimate);
        sim.Progress.DevSetUltimate(rank);
        sim.Step(Dt, Vector2.Zero);
        return sim;
    }

    static void Enemy(RunSim sim, Vector2 position, float hp = 10000)
    {
        var rng = new XorShift32(7);
        sim.Enemies.Spawn(position, hp, ref rng);
    }

    [Fact]
    public void Hero_has_six_skills_three_ultimates_and_complete_branch_trees()
    {
        var hero = TestConfig.Pyromancer;
        Assert.Equal("Pyromancer", hero.Name);
        Assert.False(hero.Rhythmic);
        Assert.True(hero.DevelopmentAvailable);
        Assert.Equal(new[] { "magic_bolts", "fireball", "flame_aura", "flame_orbit", "flame_nova", "meteor" }, hero.Skills.Select(s => s.Id));
        Assert.Equal(4, hero.LoadoutSkills().Length);
        Assert.Equal(3, hero.Ultimates.Length);
        foreach (var skill in hero.Skills)
        {
            Assert.Equal(2, skill.Branches.Length);
            foreach (var first in skill.Branches) Assert.Equal(2, first.Branches.Length);
            var original = skill.Resolve(10, new[] { -1, -1 }, Quiet.Leveling.BranchLevels);
            for (int a = 0; a < 2; a++)
            for (int b = 0; b < 2; b++)
            {
                var form = skill.Resolve(10, new[] { a, b }, Quiet.Leveling.BranchLevels);
                Assert.NotEqual(original.Name, form.Name);
                Assert.Contains(Enum.GetValues<Stat>(), stat => form.Stats[stat] != original.Stats[stat]);
            }
        }
        Assert.All(hero.Ultimates, u => Assert.Equal(3, u.Levels.Length));
        Assert.Equal(0.25f, hero.HealingPerKill);
    }

    [Fact]
    public void Hero_choice_round_trips_and_switches_clear_foreign_loadouts()
    {
        var save = new MetaSave { Loadout = new[] { "vinyl" }, Ultimate = "encore", MetaCredits = 30 };
        Assert.Equal("lowkey", MetaSave.Parse("{}").HeroId);
        save.SelectHero("pyromancer");
        Assert.Null(save.Loadout);
        Assert.Null(save.Ultimate);
        save.Loadout = TestConfig.Pyromancer.Loadout;
        save.Ultimate = "phoenix";
        save.SelectHero("pyromancer");
        var back = MetaSave.Parse(save.ToJson());
        Assert.Equal("pyromancer", back.HeroId);
        Assert.Equal(save.Loadout, back.Loadout);
        Assert.Equal("phoenix", back.Ultimate);
        Assert.Equal(30, back.MetaCredits);
        Assert.Throws<ArgumentException>(() => save.SelectHero("bogus"));
        var unknown = MetaSave.Parse("{\"heroId\":\"gone\",\"loadout\":[\"missing\"],\"ultimate\":\"missing\"}");
        Assert.Equal("lowkey", unknown.HeroId);
        Assert.Null(unknown.Loadout);
    }

    [Fact]
    public void Hero_launch_arg_selects_run_and_rejects_unknown_ids()
    {
        var options = LaunchOptions.Parse(new[] { "--hero", "pyromancer" });
        Assert.Equal("pyromancer", options.Hero);
        Assert.True(options.WantsRun);
        Assert.Throws<ArgumentException>(() => LaunchOptions.Parse(new[] { "--hero", "../other" }));
    }

    [Fact]
    public void Fireball_explodes_and_pyromancer_skills_use_seconds()
    {
        var hero = TestConfig.Pyromancer;
        var fireball = hero.Skills.Single(s => s.Id == "fireball").Resolve(1, new[] { -1, -1 }, Quiet.Leveling.BranchLevels);
        Assert.True(fireball.Has(SkillFlag.ExplodeOnHit));
        Assert.True(fireball.Stats.Blast > 0);
        var sim = new RunSim(Quiet, hero, TestConfig.Items, 1);
        Assert.Equal(1f, sim.Skills.UnitSec);
        var lowkey = new RunSim(Quiet, TestConfig.Hero, TestConfig.Items, 1);
        Assert.Equal(lowkey.Beat.BeatSec, lowkey.Skills.UnitSec);
    }

    [Theory]
    [InlineData("inferno")]
    [InlineData("phoenix")]
    public void Fire_blasts_damage_nearby_enemies_and_require_charge(string id)
    {
        var sim = Sim(id);
        Enemy(sim, new Vector2(5, 0));
        Enemy(sim, new Vector2(20, 0));
        Assert.False(sim.PressUltimate());
        sim.DevFillUltimate();
        Assert.True(sim.PressUltimate());
        sim.Step(Dt, Vector2.Zero);
        Assert.True(sim.Enemies.Health[0] <= 10000 - sim.Ultimate.Stats.Damage);
        Assert.Equal(10000, sim.Enemies.Health[1]);
        Assert.False(sim.PressUltimate());
        if (id == "inferno") Assert.True(sim.Zones.Count > 0);
    }

    [Fact]
    public void Meteor_shower_waits_for_targets_then_spawns_repeated_delayed_strikes()
    {
        var sim = Sim("meteor_shower");
        sim.DevFillUltimate();
        Assert.True(sim.PressUltimate());
        sim.Step(Dt, Vector2.Zero);
        Assert.True(sim.Ultimate.Ready);
        Enemy(sim, new Vector2(7, 0));
        sim.PressUltimate();
        sim.Step(Dt, Vector2.Zero);
        Assert.Equal(UltimatePhase.MeteorShower, sim.Ultimate.Phase);
        Assert.True(sim.Strikes.Count > 0);
        Assert.Equal(10000, sim.Enemies.Health[0]);
        for (int i = 0; i < 90; i++) sim.Step(Dt, Vector2.Zero);
        Assert.True(sim.Enemies.Health[0] < 10000 - sim.Ultimate.Stats.Damage);
        Assert.Equal(UltimatePhase.Idle, sim.Ultimate.Phase);
    }

    [Fact]
    public void Phoenix_heals_once_and_ember_heart_heals_on_kills()
    {
        var sim = Sim("phoenix");
        Enemy(sim, Vector2.Zero, 1000000);
        for (int i = 0; i < 80; i++) sim.Step(Dt, Vector2.Zero);
        float hurt = sim.HeroHp;
        Assert.True(hurt < sim.HeroMaxHp);
        sim.Enemies.Health[0] = 0;
        sim.Step(Dt, Vector2.Zero);
        Assert.Equal(MathF.Min(sim.HeroMaxHp, hurt + 0.25f), sim.HeroHp, 3);
        sim.DevFillUltimate();
        sim.PressUltimate();
        sim.Step(Dt, Vector2.Zero);
        Assert.Equal(sim.HeroMaxHp, sim.HeroHp);
        Assert.Equal(sim.Ultimate.Stats.Heal, sim.Ultimate.HealOnCast);
        sim.Step(Dt, Vector2.Zero);
        Assert.Equal(0f, sim.Ultimate.HealOnCast);
    }

    [Fact]
    public void Ultimate_ranks_scale_and_pause_freezes_the_effect()
    {
        var rank1 = Sim("meteor_shower", 1);
        var rank3 = Sim("meteor_shower", 3);
        Assert.True(rank3.Ultimate.Stats.Damage > rank1.Ultimate.Stats.Damage);
        Assert.True(rank3.Ultimate.Stats.Amount > rank1.Ultimate.Stats.Amount);
        Enemy(rank3, new Vector2(7, 0));
        rank3.DevFillUltimate();
        rank3.PressUltimate();
        rank3.Step(Dt, Vector2.Zero);
        float left = rank3.Ultimate.PhaseLeft;
        rank3.DevGainLevel();
        rank3.Step(Dt, Vector2.Zero);
        Assert.Equal(left, rank3.Ultimate.PhaseLeft);
        Assert.False(rank3.PressUltimate());
    }
}
