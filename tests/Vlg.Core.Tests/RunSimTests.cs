using System;
using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Run;
using Vlg.Core.Sim;
using Xunit;

namespace Vlg.Core.Tests;

public class RunSimTests
{
    const float Dt = 0.05f;

    static StageDef Quiet(float sec) => new() { Kind = StageKind.Mobs, DurationSec = sec, SpawnPerSec = 0, MaxAlive = 0 };

    /// <summary>Shipped config with no spawning, so tests place enemies themselves.</summary>
    static RunConfig QuietConfig(WalkerSettings? walker = null, params StageDef[] stages)
    {
        var c = TestConfig.Run;
        return c with
        {
            Walker = walker ?? c.Walker,
            Stages = stages.Length > 0 ? stages : new[] { Quiet(1000) },
        };
    }

    static void Place(RunSim sim, Vector2 p, float hp)
    {
        var rng = new XorShift32(9);
        sim.Enemies.Spawn(p, hp, ref rng);
    }

    [Fact]
    public void Hero_moves_at_hero_speed_and_long_input_is_normalized()
    {
        var sim = new RunSim(TestConfig.Run, TestConfig.Hero, 1);
        for (int i = 0; i < 20; i++) sim.Step(Dt, new Vector2(1, 0));
        Assert.Equal(sim.Hero.Speed, sim.HeroPosition.X, 3);

        var sim2 = new RunSim(TestConfig.Run, TestConfig.Hero, 1);
        sim2.Step(Dt, new Vector2(3, 4));
        Assert.Equal(sim2.Hero.Speed * Dt, sim2.HeroPosition.Length(), 4);
        Assert.Equal(0.6f, sim2.HeroPosition.X / sim2.HeroPosition.Length(), 4);
    }

    [Fact]
    public void Touching_walkers_deal_contact_damage_per_second()
    {
        var c = QuietConfig(TestConfig.Run.Walker with { Hp = 1e6f, ContactDps = 40 });
        var sim = new RunSim(c, TestConfig.Hero, 1);
        Place(sim, new Vector2(c.Hero.Radius + c.Walker.Radius, 0), 1e6f);
        Place(sim, new Vector2(0, 10), 1e6f); // not touching

        sim.Step(Dt, Vector2.Zero);

        Assert.Equal(sim.Hero.MaxHp - 40 * Dt, sim.HeroHp, 3);
        Assert.True(sim.HeroHurt);
    }

    [Fact]
    public void Contact_damage_grows_with_the_loop()
    {
        var c = QuietConfig(TestConfig.Run.Walker with { Hp = 1e6f, ContactDps = 40 });
        var sim = new RunSim(c, TestConfig.Hero, 1);
        sim.DevSkipStage(); // single-stage loop: skip wraps to loop 2
        Assert.Equal(2, sim.Stages.Loop);
        Place(sim, new Vector2(c.Hero.Radius + c.Walker.Radius, 0), 1e6f);

        sim.Step(Dt, Vector2.Zero);

        Assert.True(c.LoopScaling.Damage(2) > 1f);
        Assert.Equal(sim.Hero.MaxHp - 40 * Dt * c.LoopScaling.Damage(2), sim.HeroHp, 3);
    }

    [Fact]
    public void Death_wins_over_a_level_up_from_the_same_step()
    {
        var c = QuietConfig(TestConfig.Run.Walker with { Hp = 1e6f, ContactDps = 1e5f });
        var sim = new RunSim(c, TestConfig.Hero, 1);
        Place(sim, new Vector2(1, 0), 1e6f);
        sim.Gems.Spawn(Vector2.Zero, c.Leveling.XpBase); // collected this step

        sim.Step(Dt, Vector2.Zero);

        Assert.Equal(1, sim.Progress.PendingLevelUps);
        Assert.Equal(RunPhase.Dead, sim.Phase);
    }

    [Fact]
    public void Level_up_is_answered_before_a_chest()
    {
        var c = QuietConfig(null, new StageDef { Kind = StageKind.Chest }, Quiet(10));
        var sim = new RunSim(c, TestConfig.Hero, 1);
        Assert.Equal(RunPhase.Chest, sim.Phase);

        sim.DevGainLevel();
        Assert.Equal(RunPhase.LevelUp, sim.Phase);
        sim.ChooseLevelUp(0);
        Assert.Equal(RunPhase.Chest, sim.Phase);
    }

    [Fact]
    public void Zero_hp_ends_the_run_and_freezes_it()
    {
        var c = QuietConfig(TestConfig.Run.Walker with { Hp = 1e6f, ContactDps = 1e5f });
        var sim = new RunSim(c, TestConfig.Hero, 1);
        Place(sim, new Vector2(1, 0), 1e6f);

        sim.Step(Dt, Vector2.Zero);
        Assert.Equal(RunPhase.Dead, sim.Phase);
        float time = sim.Time;
        var pos = sim.HeroPosition;

        sim.Step(Dt, new Vector2(1, 0));
        Assert.Equal(time, sim.Time);
        Assert.Equal(pos, sim.HeroPosition);
        Assert.Throws<InvalidOperationException>(sim.ContinueStage);
        Assert.Throws<InvalidOperationException>(() => sim.ChooseLevelUp(0));
    }

    [Fact]
    public void Kill_drops_xp_gem_and_level_up_pauses_until_a_pick()
    {
        var c = QuietConfig(TestConfig.Run.Walker with { Hp = 1, Xp = 5 });
        var sim = new RunSim(c, TestConfig.Hero, 1);
        Place(sim, new Vector2(3, 0), 1);

        for (int i = 0; i < 60 && sim.Phase == RunPhase.Playing; i++) sim.Step(Dt, Vector2.Zero);

        Assert.Equal(1, sim.Kills);
        Assert.Equal(RunPhase.LevelUp, sim.Phase);
        Assert.Equal(2, sim.Progress.Level);
        float time = sim.Time;
        sim.Step(Dt, new Vector2(1, 0));
        Assert.Equal(time, sim.Time);

        var options = sim.LevelUpOptions();
        Assert.Equal(3, options.Count);
        Assert.All(options, o => Assert.Equal(LevelUpKind.Unlock, o.Kind));
        sim.ChooseLevelUp(0);
        Assert.Equal(RunPhase.Playing, sim.Phase);
        Assert.Equal(1, sim.Progress.SkillLevel(1));
    }

    [Fact]
    public void Chest_stage_pauses_until_continue()
    {
        var c = QuietConfig(null, Quiet(0.5f), new StageDef { Kind = StageKind.Chest }, Quiet(0.5f));
        var sim = new RunSim(c, TestConfig.Hero, 1);

        for (int i = 0; i < 40 && sim.Phase == RunPhase.Playing; i++) sim.Step(Dt, Vector2.Zero);
        Assert.Equal(RunPhase.Chest, sim.Phase);
        float time = sim.Time;
        sim.Step(Dt, Vector2.Zero);
        Assert.Equal(time, sim.Time);

        sim.ContinueStage();
        Assert.Equal(RunPhase.Playing, sim.Phase);
        Assert.Equal(2, sim.Stages.Index);
    }

    [Fact]
    public void Shop_stage_fully_heals_and_continue_starts_the_next_loop()
    {
        var c = QuietConfig(TestConfig.Run.Walker with { Hp = 1e6f, ContactDps = 10 }, Quiet(1f), new StageDef { Kind = StageKind.Shop });
        var sim = new RunSim(c, TestConfig.Hero, 1);
        Place(sim, new Vector2(1, 0), 1e6f);

        float lowest = sim.HeroHp;
        for (int i = 0; i < 40 && sim.Phase == RunPhase.Playing; i++)
        {
            sim.Step(Dt, Vector2.Zero);
            lowest = MathF.Min(lowest, sim.HeroHp);
        }

        Assert.True(lowest < sim.Hero.MaxHp);
        Assert.Equal(RunPhase.Shop, sim.Phase);
        Assert.Equal(sim.Hero.MaxHp, sim.HeroHp);
        sim.ContinueStage();
        Assert.Equal((2, 0), (sim.Stages.Loop, sim.Stages.Index));
    }

    [Fact]
    public void Answers_outside_their_phase_throw()
    {
        var sim = new RunSim(TestConfig.Run, TestConfig.Hero, 1);
        Assert.Throws<InvalidOperationException>(sim.ContinueStage);
        Assert.Throws<InvalidOperationException>(() => sim.ChooseLevelUp(0));
    }

    [Fact]
    public void Dev_shortcuts_skip_a_stage_and_add_a_level()
    {
        var sim = new RunSim(TestConfig.Run, TestConfig.Hero, 1);
        sim.DevSkipStage();
        Assert.Equal(StageKind.Horde, sim.Stages.Current.Kind);
        sim.DevGainLevel();
        Assert.Equal(RunPhase.LevelUp, sim.Phase);
    }

    [Fact]
    public void Drop_start_sends_a_shockwave_ring_from_the_hero()
    {
        var sim = new RunSim(QuietConfig(), TestConfig.Hero, 1);
        var b = sim.Hero.Beat;
        int steps = (int)(sim.Beat.BeatSec * sim.Beat.CycleBeats / Dt) + 10;
        bool seen = false;
        for (int i = 0; i < steps && !seen; i++)
        {
            sim.Step(Dt, Vector2.Zero);
            if (!sim.Beat.DropStarted) continue;
            seen = true;
            int k = Array.IndexOf(sim.Rings.Source, RunSim.DropShockSource, 0, sim.Rings.Count);
            Assert.True(k >= 0);
            Assert.Equal((b.ShockRadius, b.ShockSpeed, b.ShockKnockback), (sim.Rings.MaxRadius[k], sim.Rings.Shot(k).Speed, sim.Rings.Shot(k).Knockback));
        }
        Assert.True(seen);
        Assert.Equal(1, sim.Beat.Drops);
    }

    [Fact]
    public void Loadout_override_sets_slot_order_and_slot_zero_starts_unlocked()
    {
        var sim = new RunSim(TestConfig.Run, TestConfig.Hero, 1, null, new[] { "vinyl", "distortion" });

        Assert.Equal(new[] { "vinyl", "distortion" }, Array.ConvertAll(sim.Loadout, s => s.Id));
        Assert.Equal((1, 0), (sim.Progress.SkillLevel(0), sim.Progress.SkillLevel(1)));
        Assert.Throws<FormatException>(() => new RunSim(TestConfig.Run, TestConfig.Hero, 1, null, new[] { "bogus" }));
    }

    [Fact]
    public void Aim_follows_the_camera_yaw()
    {
        var sim = new RunSim(TestConfig.Run, TestConfig.Hero, 1);
        sim.SetView(MathF.PI / 2f, RunSim.DefaultAspect);
        Assert.Equal(CameraMath.Forward(MathF.PI / 2f), sim.Aim);
    }

    /// <summary>Scripted play through the shipped config: circles, takes the last option, continues chest/shop.</summary>
    static RunSim Play(float seconds, uint seed)
    {
        var sim = new RunSim(TestConfig.Run, TestConfig.Hero, seed);
        int steps = (int)(seconds / Dt);
        for (int i = 0; i < steps && sim.Phase != RunPhase.Dead; i++)
        {
            switch (sim.Phase)
            {
                case RunPhase.LevelUp: sim.ChooseLevelUp(sim.LevelUpOptions().Count - 1); break;
                case RunPhase.Chest or RunPhase.Shop: sim.ContinueStage(); break;
            }
            float a = i * Dt * 0.4f;
            sim.Step(Dt, new Vector2(MathF.Cos(a), MathF.Sin(a)));
        }
        return sim;
    }

    [Fact]
    public void Scripted_play_kills_levels_and_unlocks_skills()
    {
        var sim = Play(180f, 1);

        Assert.True(sim.Kills > 50, $"kills {sim.Kills}");
        Assert.True(sim.Progress.Level >= 5, $"level {sim.Progress.Level}");
        Assert.True(sim.Progress.SkillLevel(3) >= 1, "last option each time unlocks the last loadout skill first");
        Assert.True(sim.Time > 60f);
    }

    [Fact]
    public void Same_seed_same_run()
    {
        var a = Play(60f, 3);
        var b = Play(60f, 3);

        Assert.Equal(a.Kills, b.Kills);
        Assert.Equal(a.HeroPosition, b.HeroPosition);
        Assert.Equal(a.HeroHp, b.HeroHp);
        Assert.Equal(a.Enemies.Count, b.Enemies.Count);
    }
}
