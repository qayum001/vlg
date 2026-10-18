using System;
using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Run;
using Vlg.Core.Sim;
using Xunit;
using W = Vlg.Core.Tests.SkillTestWorld;

namespace Vlg.Core.Tests;

/// <summary>Branch-only behaviors (flags and kinds) through SkillSystem. Skills sit at level 5 with branch 0 picked.</summary>
public class SkillBranchMechanicsTests
{
    static W Make(SkillKind kind, SkillStats st, params SkillFlag[] flags) => Make(W.Beat(), kind, st, flags);

    static W Make(BeatSettings beat, SkillKind kind, SkillStats st, params SkillFlag[] flags)
    {
        var w = new W(beat, 3, W.WithBranch(W.Skill(kind, st), default, null, flags));
        w.BranchAll();
        return w;
    }

    static void Run(W w, int steps)
    {
        for (int i = 0; i < steps; i++) w.Full();
    }

    static readonly SkillStats Speaker1 = new() { Damage = 4, Cooldown = 2, Count = 1, Duration = 8, Radius = 2, Range = 12 };
    /// <summary>Ring placement puts speaker 0 at (0, 4) on the first cast.</summary>
    static readonly Vector2 FirstSpot = new(0, 4);

    [Fact]
    public void Twin_adds_a_faster_half_damage_shot()
    {
        var w = Make(SkillKind.Projectile, new SkillStats { Damage = 6, Cooldown = 2, Count = 1, Speed = 20, Range = 10 }, SkillFlag.Twin);
        w.Enemy(new Vector2(3, 0));
        w.Update();

        Assert.Equal(2, w.Projectiles.Count);
        Assert.Equal((20f, 30f), (w.Projectiles.Velocity[0].Length(), w.Projectiles.Velocity[1].Length()));
        Assert.Equal((6f, 3f), (w.Projectiles.Damage[0], w.Projectiles.Damage[1]));
    }

    [Fact]
    public void Projectile_pierce_stat_reaches_the_shots()
    {
        var w = new W(W.Skill(SkillKind.Projectile, new SkillStats { Damage = 6, Cooldown = 4, Count = 1, Speed = 20, Range = 12, Pierce = 2 }));
        int a = w.Enemy(new Vector2(3, 0)), b = w.Enemy(new Vector2(5, 0)), c = w.Enemy(new Vector2(7, 0)), d = w.Enemy(new Vector2(9, 0));
        Run(w, 12);
        Assert.Equal(new[] { 94f, 94f, 94f, 100f }, new[] { w.Enemies.Health[a], w.Enemies.Health[b], w.Enemies.Health[c], w.Enemies.Health[d] });
    }

    [Fact]
    public void Boomerang_hits_out_and_back()
    {
        var w = Make(SkillKind.Boomerang, new SkillStats { Damage = 5, Cooldown = 4, Count = 1, Speed = 10, Range = 6, Radius = 0.5f });
        int e = w.Enemy(new Vector2(3, 0));
        Run(w, 24);
        Assert.Equal(90f, w.Enemies.Health[e]);
        Assert.Equal(0, w.Projectiles.Count);
    }

    [Fact]
    public void Bells_fall_on_the_crowd_after_half_a_beat_and_stun()
    {
        var w = Make(SkillKind.BellDrop, new SkillStats { Damage = 10, Cooldown = 4, Count = 1, Radius = 2, Range = 12, Stun = 1 });
        int e = w.Enemy(new Vector2(5, 0));
        w.Update();
        Assert.Equal(1, w.Strikes.Count);
        Assert.Equal(0.25f, w.Strikes.Delay[0], 4);

        Run(w, 5);
        Assert.Equal(90f, w.Enemies.Health[e]);
        Assert.Equal(1f, w.Enemies.StunTimer[e]);
    }

    [Fact]
    public void Quake_cracks_hurt_on_beats_and_aftershock_erupts_every_period_casts()
    {
        var w = Make(SkillKind.Quake, new SkillStats { Damage = 2, Cooldown = 2, Radius = 3, Duration = 4, Period = 2 }, SkillFlag.Aftershock);
        int e = w.Enemy(new Vector2(1, 0));
        Run(w, 16); // casts at t = 0 and 1 s; beats at 0, 0.5, 1

        Assert.Equal(2, w.Zones.Count);
        // Zone 1: 3 beats x 2, zone 2: 1 beat x 2, second cast erupts for 2 x 4.
        Assert.Equal(100f - 6f - 2f - 8f, w.Enemies.Health[e], 3);
    }

    [Fact]
    public void Wall_of_sound_places_three_speakers_around_the_hero()
    {
        var w = Make(SkillKind.Speaker, Speaker1 with { Count = 3 }, SkillFlag.RingPlacement);
        w.Update();
        Assert.Equal(3, w.Summons.Count);
        for (int i = 0; i < 3; i++) Assert.Equal(4f, w.Summons.Position[i].Length(), 3);
        Assert.Equal(FirstSpot.Y, w.Summons.Position[0].Y, 3);
    }

    [Fact]
    public void Big_pulse_every_period_pulses_hits_three_times_harder()
    {
        var w = Make(SkillKind.Speaker, Speaker1 with { Period = 2 }, SkillFlag.RingPlacement, SkillFlag.BigPulse);
        int e = w.Enemy(FirstSpot + new Vector2(0, 0.5f));
        Run(w, 8); // pulses at beats 0 and 1
        Assert.Equal(100f - 4f - 12f, w.Enemies.Health[e], 3);
    }

    [Fact]
    public void Link_hits_enemies_on_the_line_between_speakers()
    {
        var w = Make(SkillKind.Speaker, Speaker1 with { Count = 3 }, SkillFlag.RingPlacement, SkillFlag.Link);
        w.Update();
        var mid = (w.Summons.Position[0] + w.Summons.Position[1]) * 0.5f;
        int e = w.Enemy(mid);
        int off = w.Enemy(mid * 0.2f); // near the hero, far from every speaker and line
        for (int i = 0; i < 8; i++) w.Update(); // next pulse at beat 1

        Assert.Equal(96f, w.Enemies.Health[e], 3);
        Assert.Equal(100f, w.Enemies.Health[off]);
    }

    [Fact]
    public void Crowd_scaling_crush_and_pull_on_pulse()
    {
        var crowd = Make(SkillKind.Speaker, Speaker1, SkillFlag.RingPlacement, SkillFlag.CrowdScaling);
        for (int i = 0; i < 5; i++) crowd.Enemy(FirstSpot + new Vector2(0.3f * i - 0.6f, 1.5f));
        crowd.Update();
        Assert.Equal(100f - 4f * 1.5f, crowd.Enemies.Health[0], 3);

        var crush = Make(SkillKind.Speaker, Speaker1 with { Pull = 4 }, SkillFlag.RingPlacement, SkillFlag.Crush);
        int center = crush.Enemy(FirstSpot);
        int edge = crush.Enemy(FirstSpot + new Vector2(0, 1.8f));
        crush.Update();
        Assert.True(crush.Enemies.Health[center] <= 0f);
        Assert.Equal(96f, crush.Enemies.Health[edge], 3);
        Assert.Equal(new Vector2(0, -4), crush.Enemies.Push[edge]);
    }

    [Fact]
    public void Blowout_explodes_when_the_speaker_expires()
    {
        var w = Make(SkillKind.Speaker, Speaker1 with { Duration = 1 }, SkillFlag.RingPlacement, SkillFlag.Blowout);
        int e = w.Enemy(FirstSpot + new Vector2(0, 3)); // outside the pulse, inside x2 radius
        for (int i = 0; i < 10; i++) w.Update();
        Assert.Equal(100f - 24f, w.Enemies.Health[e], 3);
    }

    [Fact]
    public void Fuzz_trail_drops_puddles_as_the_hero_moves()
    {
        var w = Make(SkillKind.Aura, new SkillStats { Damage = 1, Cooldown = 1, Radius = 1, Vuln = 0.1f, Duration = 4 }, SkillFlag.Trail);
        w.Update();
        Assert.Equal(1, w.Zones.Count);
        w.Update(); // half-beats only: standing still or between ticks adds nothing
        w.Ctx.Hero = new Vector2(2, 0);
        for (int i = 0; i < 3; i++) w.Update();
        w.Ctx.Hero = new Vector2(4, 0);
        for (int i = 0; i < 4; i++) w.Update();

        Assert.Equal(3, w.Zones.Count);
        Assert.Equal(new Vector2(4, 0), w.Zones.Center[2]);
    }

    [Fact]
    public void Death_burst_explodes_deaths_inside_the_aura()
    {
        var w = Make(SkillKind.Aura, new SkillStats { Damage = 2, Cooldown = 1, Radius = 3 }, SkillFlag.DeathBurst);
        int e = w.Enemy(new Vector2(1.5f, 0));
        w.Update();
        var deaths = new PointBuffer(4);
        deaths.Add(new Vector2(1, 0));
        deaths.Add(new Vector2(20, 0)); // outside the aura

        w.Skills.OnDeaths(deaths);

        Assert.Equal(100f - 2f - 4f, w.Enemies.Health[e], 3);
        Assert.Equal(1, w.Flashes.Count);
    }

    [Fact]
    public void Clipping_doubles_the_aura_in_the_drop()
    {
        var plain = Make(W.Beat(dropBars: 1), SkillKind.Aura, new SkillStats { Damage = 2, Cooldown = 1, Radius = 3 });
        var clip = Make(W.Beat(dropBars: 1), SkillKind.Aura, new SkillStats { Damage = 2, Cooldown = 1, Radius = 3 }, SkillFlag.DropDouble);
        plain.Update();
        clip.Update();
        Assert.Equal(3f, plain.Skills.AuraRadius[0]);
        Assert.Equal(6f, clip.Skills.AuraRadius[0]);
    }

    [Fact]
    public void Beam_count_end_burst_and_split()
    {
        var beam = new SkillStats { Damage = 10, Cooldown = 2, Range = 10, Width = 1, Count = 2 };
        var two = Make(SkillKind.Beam, beam);
        two.Ctx.Aim = new Vector2(1, 0);
        two.Update();
        Assert.Equal(2, two.Beams.Count);
        Assert.Equal(-1f, Vector2.Dot(two.Beams.Direction[0], two.Beams.Direction[1]), 3);

        var pop = Make(SkillKind.Beam, beam with { Count = 1 }, SkillFlag.EndBurst);
        pop.Ctx.Aim = new Vector2(1, 0);
        int e = pop.Enemy(new Vector2(10, 1.8f));
        pop.Update();
        Assert.Equal(90f, pop.Enemies.Health[e]);

        var split = Make(SkillKind.Beam, beam with { Count = 1 }, SkillFlag.Split);
        split.Update();
        Assert.Equal(4, split.Beams.Count);
    }

    [Fact]
    public void Ramp_beam_hits_harder_while_it_keeps_hitting()
    {
        var w = Make(SkillKind.Beam, new SkillStats { Damage = 10, Cooldown = 0.5f, Range = 10, Width = 1, Count = 1 }, SkillFlag.Ramp);
        w.Ctx.Aim = new Vector2(1, 0);
        int e = w.Enemy(new Vector2(5, 0));
        for (int i = 0; i < 8; i++) w.Update(); // casts at t = 0, 0.25, 0.5

        Assert.Equal(100f - 10f - 12.5f - 15f, w.Enemies.Health[e], 3);
    }

    [Fact]
    public void Sweep_covers_the_arc_in_front_once()
    {
        var w = Make(SkillKind.Sweep, new SkillStats { Damage = 5, Cooldown = 4, Range = 8, Width = 1, Arc = 180, Duration = 2, Count = 1 });
        w.Ctx.Facing = new Vector2(0, 1);
        int front = w.Enemy(new Vector2(0, 4)), left = w.Enemy(new Vector2(-4, 0)), right = w.Enemy(new Vector2(4, 0)), behind = w.Enemy(new Vector2(0, -4));
        int beams = 0;
        for (int i = 0; i < 20; i++)
        {
            w.Update();
            beams += w.Beams.Count;
        }

        Assert.Equal(new[] { 95f, 95f, 95f, 100f },
            new[] { w.Enemies.Health[front], w.Enemies.Health[left], w.Enemies.Health[right], w.Enemies.Health[behind] });
        Assert.Equal(16, beams); // one beam per frame for the 1 s sweep
    }

    [Fact]
    public void Double_deck_burn_scratch_and_spinback()
    {
        var orbit = new SkillStats { Damage = 5, Cooldown = 0.5f, Count = 3, Speed = 2, Range = 3, Radius = 0.6f };

        var deck = Make(SkillKind.Orbit, orbit, SkillFlag.DoubleDeck);
        deck.Update();
        Assert.Equal(6, deck.Skills.BladeCount);
        Assert.Equal(4.5f, deck.Skills.BladePositions[3].Length(), 3);

        var wax = Make(SkillKind.Orbit, orbit with { Burn = 4, BurnSec = 2 });
        int lit = wax.Enemy(new Vector2(0, 3)); // object 0 at angle 0
        wax.Update(0f);
        Assert.Equal((4f, 2f), (wax.Enemies.Burn[lit], wax.Enemies.BurnTimer[lit]));

        var scratch = Make(SkillKind.Orbit, orbit with { Period = 2 }, SkillFlag.Scratch);
        int e = scratch.Enemy(new Vector2(0, 5));
        for (int i = 0; i < 8; i++) scratch.Update(); // beats 0 and 1: the second scratches
        Assert.Equal(90f, scratch.Enemies.Health[e], 3);
        Assert.Equal(new Vector2(0, -8), scratch.Enemies.Push[e]);

        var spin = Make(W.Beat(dropBars: 1), SkillKind.Orbit, orbit, SkillFlag.Spinback);
        for (int i = 0; i < 4; i++) spin.Update();
        Assert.True(spin.Skills.BladePositions[0].Length() > 5f);
    }

    [Fact]
    public void Drone_follows_the_hero_and_its_bolts_explode()
    {
        var w = Make(SkillKind.Drone, new SkillStats { Damage = 5, Cooldown = 1, Count = 1, Speed = 20, Range = 10, Blast = 1.5f }, SkillFlag.ExplodeOnHit);
        int a = w.Enemy(new Vector2(0.1f, 6)), b = w.Enemy(new Vector2(1.8f, 6));
        w.Update();
        Assert.Equal(1, w.Skills.DroneCount);
        Assert.Equal(1.6f, w.Skills.DronePositions[0].Length(), 3);
        Assert.Equal(1, w.Projectiles.Count);

        for (int i = 0; i < 6; i++) w.Full();
        Assert.Equal((95f, 97f), (w.Enemies.Health[a], w.Enemies.Health[b]));
    }

    [Fact]
    public void Shield_charges_blocks_once_with_a_blast_then_recharges()
    {
        var w = Make(SkillKind.Shield, new SkillStats { Damage = 10, Cooldown = 4, Radius = 2, Knockback = 5 });
        int e = w.Enemy(new Vector2(1, 0));
        w.Update();
        Assert.True(w.Skills.ShieldUp[0]);

        Assert.True(w.Skills.TryBlock());
        Assert.Equal(90f, w.Enemies.Health[e]);
        Assert.Equal(new Vector2(5, 0), w.Enemies.Push[e]);
        Assert.False(w.Skills.TryBlock());

        int steps = 0;
        while (!w.Skills.ShieldUp[0] && steps < 64)
        {
            w.Update();
            steps++;
        }
        Assert.InRange(steps, 32, 36); // 4 beats = 2 s = 32 steps, then the next half-beat
    }

    [Fact]
    public void Sidechain_boosts_other_skills_while_the_shield_is_up()
    {
        var shield = W.WithBranch(W.Skill(SkillKind.Shield, new SkillStats { Damage = 1, Cooldown = 4, Radius = 1 }), default, null, SkillFlag.Sidechain);
        var notes = W.Skill(SkillKind.Projectile, new SkillStats { Damage = 6, Cooldown = 2, Count = 1, Speed = 20, Range = 10 });
        var w = new W(W.Beat(), 3, shield, notes);
        w.BranchAll();
        w.Update(); // charges the shield
        w.Enemy(new Vector2(3, 0));
        for (int i = 0; i < 4 && w.Projectiles.Count == 0; i++) w.Update();

        Assert.Equal(6f * 1.15f, w.Projectiles.Damage[0], 4);
    }

    [Fact]
    public void Noise_gate_in_a_run_blocks_contact_damage_for_a_moment()
    {
        var c = TestConfig.Run with
        {
            Stages = new[] { new StageDef { Kind = StageKind.Mobs, DurationSec = 1000 } },
            Walker = TestConfig.Run.Walker with { ContactDps = 100 },
        };
        var sim = new RunSim(c, TestConfig.Hero, TestConfig.Items, 1, null, new[] { "distortion" });
        sim.Progress.DevSetAllSkills(5);
        sim.ChooseLevelUp(2); // Noise Gate
        sim.Step(0.05f, Vector2.Zero);
        Assert.True(sim.Skills.ShieldUp[0]);
        var rng = new XorShift32(2);
        sim.Enemies.Spawn(new Vector2(c.Hero.Radius + c.Walker.Radius, 0), 1e6f, ref rng);

        sim.Step(0.05f, Vector2.Zero);
        Assert.True(sim.Skills.BlockedThisStep);
        Assert.Equal(sim.HeroMaxHp, sim.HeroHp);

        for (int i = 0; i < 14; i++) sim.Step(0.05f, Vector2.Zero);
        Assert.True(sim.HeroHp < sim.HeroMaxHp);
    }
}
