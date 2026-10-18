using System;
using System.Linq;
using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Run;
using Vlg.Core.Sim;
using Xunit;

namespace Vlg.Core.Tests;

public class UltimateTests
{
    const float Dt = 0.05f;

    static RunConfig Quiet => TestConfig.Run with { Stages = new[] { new StageDef { Kind = StageKind.Mobs, DurationSec = 1000 } } };

    static RunSim Sim(string ultimate, int level = 1)
    {
        var sim = new RunSim(Quiet, TestConfig.Hero, TestConfig.Items, 1, null, new[] { "cowbell" }, ultimate);
        sim.Progress.DevSetUltimate(level);
        sim.Step(Dt, Vector2.Zero); // sync the level into the system
        return sim;
    }

    static int Place(RunSim sim, Vector2 p, float hp = 1e5f)
    {
        var rng = new XorShift32(8);
        sim.Enemies.Spawn(p, hp, ref rng);
        return sim.Enemies.Count - 1;
    }

    [Fact]
    public void Shipped_lowkey_has_three_ultimates_with_three_levels()
    {
        var h = TestConfig.Hero;
        Assert.Equal(new[] { "sub_quake", "encore", "bass_cannon" }, h.Ultimates.Select(u => u.Id));
        Assert.All(h.Ultimates, u => Assert.Equal(3, u.Levels.Length));
        Assert.Equal("sub_quake", h.UltimateById().Id);
        Assert.Throws<FormatException>(() => h.UltimateById("bogus"));
    }

    [Fact]
    public void Ultimate_with_a_missing_level_throws()
    {
        string json = TestConfig.HeroJson.Replace(",\n        { \"charge\": 60, \"damage\": 120, \"radius\": 40, \"stun\": 3, \"tick\": 10, \"duration\": 16 }", "");
        Assert.NotEqual(TestConfig.HeroJson, json);
        Assert.Contains("sub_quake", Assert.Throws<FormatException>(() => HeroDef.Parse(json, TestConfig.Run)).Message);
    }

    [Fact]
    public void Ultimate_card_appears_at_hero_levels_10_20_30()
    {
        var l = TestConfig.Run.Leveling with { LevelDivisor = 100 }; // skills capped: only bonus or ultimate cards
        var p = new HeroProgress(l, 1, null, hasUltimate: true);
        for (int level = 2; level <= 31; level++)
        {
            p.DevGainLevel();
            var options = p.Options();
            bool ult = options.Any(o => o.Kind == LevelUpKind.Ultimate);
            Assert.Equal(level is 10 or 20 or 30, ult);
            p.Choose(ult ? options.ToList().FindIndex(o => o.Kind == LevelUpKind.Ultimate) : 0);
        }
        Assert.Equal(3, p.UltimateLevel);
    }

    [Fact]
    public void Locked_ultimate_neither_charges_nor_fires()
    {
        var sim = Sim("sub_quake", 0);
        for (int i = 0; i < 100; i++) sim.Step(Dt, Vector2.Zero);
        Assert.Equal(0f, sim.Ultimate.Charge);
        Assert.False(sim.PressUltimate());
    }

    [Fact]
    public void Sub_quake_charges_by_seconds_then_hits_and_stuns_the_screen()
    {
        var sim = Sim("sub_quake");
        Assert.False(sim.PressUltimate());
        int steps = 0;
        while (!sim.Ultimate.Ready && steps < 2000)
        {
            sim.Step(Dt, Vector2.Zero);
            steps++;
        }
        Assert.InRange(steps * Dt, 59f, 61f);

        int near = Place(sim, new Vector2(10, 0)), far = Place(sim, new Vector2(60, 0));
        Assert.True(sim.PressUltimate());
        sim.Step(Dt, Vector2.Zero);

        var st = sim.Ultimate.Stats;
        Assert.True(sim.Ultimate.FiredThisStep);
        Assert.True(sim.Enemies.Health[near] <= 1e5f - st.Damage + 1f);
        Assert.True(sim.Enemies.StunTimer[near] > 0f);
        Assert.Equal(1e5f, sim.Enemies.Health[far]);
        Assert.InRange(sim.Ultimate.Charge, 0f, Dt + 1e-4f); // recharging from this step on
    }

    [Fact]
    public void Sub_quake_level_3_leaves_shaking_ground()
    {
        var sim = Sim("sub_quake", 3);
        sim.DevFillUltimate();
        sim.PressUltimate();
        sim.Step(Dt, Vector2.Zero);
        Assert.Equal(1, sim.Zones.Count);
    }

    [Fact]
    public void Encore_forces_the_drop_speeds_skills_and_heals_per_kill_at_level_3()
    {
        var sim = Sim("encore", 3);
        Assert.Equal(ChargeKind.Kills, sim.Ultimate.Def.Charge);
        sim.DevFillUltimate();
        Assert.True(sim.PressUltimate());
        sim.Step(Dt, Vector2.Zero);
        Assert.Equal(UltimatePhase.Encore, sim.Ultimate.Phase);
        Assert.True(sim.Beat.InDrop);
        sim.Step(Dt, Vector2.Zero);
        Assert.Equal(1f + sim.Ultimate.Stats.Speed, sim.Skills.RateMultiplier);

        Place(sim, new Vector2(sim.Config.Hero.Radius + sim.Config.Walker.Radius, 0)); // keeps hurting the hero
        for (int i = 0; i < 10; i++) sim.Step(Dt, Vector2.Zero);
        float hurt = sim.HeroHp;
        Assert.True(hurt < sim.HeroMaxHp);
        for (int i = 0; i < 5; i++) Place(sim, new Vector2(30 + i, 0), 0f); // removed as kills this step
        sim.Step(Dt, Vector2.Zero);
        Assert.True(sim.HeroHp > hurt);
        for (int i = 0; i < sim.Enemies.Count; i++) sim.Enemies.Health[i] = 0f; // stop the contact damage

        for (int i = 0; i < 1000 && sim.Ultimate.Phase == UltimatePhase.Encore; i++) sim.Step(Dt, Vector2.Zero);
        Assert.Equal(UltimatePhase.Idle, sim.Ultimate.Phase);
        Assert.False(sim.Beat.ForceDrop);
        sim.Step(Dt, Vector2.Zero);
        Assert.Equal(1f, sim.Skills.RateMultiplier);
    }

    [Fact]
    public void Kill_charge_fills_from_kills_while_idle()
    {
        var sim = Sim("encore");
        for (int i = 0; i < 7; i++) Place(sim, new Vector2(30 + i, 0), 0f);
        sim.Step(Dt, Vector2.Zero);
        Assert.Equal(7f, sim.Ultimate.Charge);
    }

    static bool Alive(RunSim sim, uint id) => Array.IndexOf(sim.Enemies.Id, id, 0, sim.Enemies.Count) >= 0;

    [Fact]
    public void Bass_cannon_charges_on_drops_winds_up_a_bar_then_kills_along_the_aim()
    {
        var sim = Sim("bass_cannon");
        int drops = 0;
        while (!sim.Ultimate.Ready && drops < 10)
        {
            sim.Step(Dt, Vector2.Zero);
            if (sim.Beat.DropStarted) drops++;
        }
        Assert.Equal(3, drops);

        sim.SetView(0f, RunSim.DefaultAspect); // aim (0, -1)
        uint onLine = sim.Enemies.Id[Place(sim, new Vector2(0.5f, -20))];
        uint off = sim.Enemies.Id[Place(sim, new Vector2(8, -20))];
        Assert.True(sim.PressUltimate());
        sim.Step(Dt, Vector2.Zero);
        Assert.Equal(UltimatePhase.Windup, sim.Ultimate.Phase);

        int steps = 0;
        while (sim.Ultimate.Phase == UltimatePhase.Windup && steps < 100)
        {
            sim.Step(Dt, Vector2.Zero);
            steps++;
        }
        float windup = UltimateSystem.WindupBeats * sim.Beat.BeatSec;
        Assert.InRange(steps * Dt, windup - 0.1f, windup + 0.1f);
        Assert.True(Alive(sim, onLine));
        sim.Step(Dt, Vector2.Zero);

        Assert.False(Alive(sim, onLine));
        Assert.True(Alive(sim, off));
    }

    [Fact]
    public void Bass_cannon_level_3_leaves_a_burning_line()
    {
        var sim = Sim("bass_cannon", 3);
        sim.DevFillUltimate();
        sim.PressUltimate();
        sim.Step(Dt, Vector2.Zero);
        for (int i = 0; i < 60; i++) sim.Step(Dt, Vector2.Zero); // windup ~37 steps, then firing
        Assert.Equal(UltimatePhase.Firing, sim.Ultimate.Phase);
        Assert.True(sim.Zones.Count > 5);
    }
}
