using System.Collections.Generic;
using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Run;
using Vlg.Core.Sim;
using Xunit;

namespace Vlg.Core.Tests;

public class SkillSystemTests
{
    const float HeroRadius = 0.5f, EnemyRadius = 0.45f;
    /// <summary>120 BPM: beat 0.5 s, half-beat 0.25 s. Steps of 1/16 s land exactly on boundaries.</summary>
    const float Dt = 0.0625f;

    static BeatSettings Beat(int dropBars = 0, float dropDamage = 0f, float dropArea = 0f) => new()
    {
        Bpm = 120, CycleBars = 1, BuildBars = 0, DropBars = dropBars, DropDamage = dropDamage, DropArea = dropArea,
        ShockDamage = 0, ShockRadius = 1, ShockSpeed = 1, ShockKnockback = 0,
    };

    static SkillDef Skill(SkillKind kind, SkillStats stats, SkillStats perLevel = default) => new()
    {
        Id = kind.ToString(), Name = kind.ToString(), Kind = kind, Tags = new[] { SkillTag.Area }, Base = stats, PerLevel = perLevel,
    };

    static readonly SkillDef Notes = Skill(SkillKind.Projectile,
        new SkillStats { Damage = 6, Cooldown = 2, Count = 1, Speed = 20, Range = 10 }, new SkillStats { Count = 0.5f });
    static readonly SkillDef Ring = Skill(SkillKind.Ring, new SkillStats { Damage = 5, Cooldown = 2, Radius = 6, Speed = 12, Knockback = 6 });
    static readonly SkillDef Speaker = Skill(SkillKind.Speaker,
        new SkillStats { Damage = 4, Cooldown = 2, Count = 1, Duration = 4, Radius = 2, Range = 12 });
    static readonly SkillDef Aura = Skill(SkillKind.Aura, new SkillStats { Damage = 3, Cooldown = 1, Radius = 2.5f, Vuln = 0.5f });
    static readonly SkillDef Beam = Skill(SkillKind.Beam, new SkillStats { Damage = 10, Cooldown = 2, Range = 10, Width = 1 });
    static readonly SkillDef Orbit = Skill(SkillKind.Orbit,
        new SkillStats { Damage = 5, Cooldown = 0.5f, Count = 3, Speed = 2, Range = 3, Radius = 0.6f });

    sealed class World
    {
        public readonly SkillSystem Skills;
        public readonly HeroProgress Progress;
        public readonly BeatClock Clock;
        public readonly EnemySwarm Enemies = new(64);
        public readonly SpatialGrid Grid = new(32, 2f, 64);
        public readonly ProjectilePool Projectiles = new(256);
        public readonly RingPool Rings = new(16, 64);
        public readonly SummonPool Summons = new(32);
        public readonly BeamEvents Beams = new(8);
        public readonly PointBuffer Hits = new(256);
        public readonly SkillWorld Ctx;
        XorShift32 _rng = new(5);

        public World(BeatSettings beat, int divisor, params SkillDef[] defs)
        {
            Clock = new BeatClock(beat);
            Ctx = new SkillWorld(Enemies, Grid, Projectiles, Rings, Summons, Beams, Hits, HeroRadius, EnemyRadius);
            Skills = new SkillSystem(defs, Ctx, Clock, 1, 7);
            Progress = new HeroProgress(new LevelingSettings
            {
                XpBase = 5, XpPerLevel = 0, LevelDivisor = divisor, MaxSkillLevel = 10, MaxedBonusDamage = 0.02f,
            }, defs.Length);
        }

        public World(params SkillDef[] defs) : this(Beat(), 3, defs) { }

        public int Enemy(Vector2 p, float hp = 100f)
        {
            Enemies.Spawn(p, hp, ref _rng);
            return Enemies.Count - 1;
        }

        public void Update(float dt = Dt)
        {
            Hits.Clear();
            Beams.Clear();
            Clock.Advance(dt);
            Grid.Build(Enemies.Position, Enemies.Count, Vector2.Zero);
            Skills.Update(dt, Progress, Progress.DamageMultiplier);
        }

        public void Raise(int slot, int times)
        {
            for (int i = 0; i < times; i++)
            {
                Progress.DevGainLevel();
                var options = Progress.Options();
                int pick = -1;
                for (int k = 0; k < options.Count; k++)
                    if (options[k].Slot == slot) pick = k;
                Progress.Choose(pick);
            }
        }
    }

    [Fact]
    public void Projectile_fires_at_the_nearest_enemy_with_its_damage_range_and_source()
    {
        var w = new World(Notes);
        w.Enemy(new Vector2(0, 8));
        w.Enemy(new Vector2(5, 0));

        w.Update();

        Assert.Equal(1, w.Projectiles.Count);
        Assert.Equal(new Vector2(20, 0), w.Projectiles.Velocity[0]);
        Assert.Equal(new Vector2(HeroRadius, 0), w.Projectiles.Position[0]);
        Assert.Equal(6f, w.Projectiles.Damage[0]);
        Assert.Equal(10f / 20f, w.Projectiles.Life[0]);
        Assert.Equal(1, w.Projectiles.Source[0]);
        Assert.True(w.Skills.FiredThisStep[0]);
    }

    [Fact]
    public void Projectile_without_a_target_in_range_waits_and_fires_when_one_arrives()
    {
        var w = new World(Notes);
        w.Enemy(new Vector2(0, 11));
        for (int i = 0; i < 8; i++) w.Update();
        Assert.Equal(0, w.Projectiles.Count);

        w.Enemies.Position[0] = new Vector2(0, 9);
        // Ready since the start: fires on the next half-beat tick (at most 4 steps of 1/16 s).
        for (int i = 0; i < 4 && w.Projectiles.Count == 0; i++) w.Update();
        Assert.Equal(1, w.Projectiles.Count);
    }

    [Fact]
    public void Projectile_level_three_fires_two_at_the_two_nearest()
    {
        var w = new World(Beat(), 1, Notes);
        w.Raise(0, 2);
        Assert.Equal(3, w.Progress.SkillLevel(0));
        w.Enemy(new Vector2(-4, 0));
        w.Enemy(new Vector2(9, 0));
        w.Enemy(new Vector2(0, 3));

        w.Update();

        Assert.Equal(2, w.Projectiles.Count);
        Assert.Equal(new Vector2(0, 20), w.Projectiles.Velocity[0]);
        Assert.Equal(new Vector2(-20, 0), w.Projectiles.Velocity[1]);
    }

    [Fact]
    public void Cooldown_in_beats_paces_casts_on_the_beat()
    {
        var w = new World(Ring);
        int casts = 0;
        for (int i = 0; i < 160; i++) // 10 s at 120 BPM
        {
            w.Update();
            if (!w.Skills.FiredThisStep[0]) continue;
            casts++;
            Assert.True(w.Clock.HalfTick);
        }

        // Every 2 beats = 1 s: first step, then t = 1, 2, ..., 10.
        Assert.Equal(11, casts);
    }

    [Fact]
    public void Ready_skill_waits_for_the_next_half_beat()
    {
        var fast = Skill(SkillKind.Ring, new SkillStats { Damage = 1, Cooldown = 0.1f, Radius = 1, Speed = 5 });
        var w = new World(fast);
        int casts = 0, ticks = 0;
        for (int i = 0; i < 32; i++)
        {
            w.Update();
            if (w.Clock.HalfTick) ticks++;
            if (!w.Skills.FiredThisStep[0]) continue;
            casts++;
            Assert.True(w.Clock.HalfTick);
        }

        // Ready every 0.1 beats but capped to one cast per 8th note.
        Assert.Equal(ticks, casts);
        Assert.True(casts < 32);
    }

    [Fact]
    public void Drop_scales_damage_and_area()
    {
        var w = new World(Beat(dropBars: 1, dropDamage: 0.5f, dropArea: 0.2f), 1, Notes, Aura);
        w.Raise(1, 1);
        w.Enemy(new Vector2(3, 0));

        w.Update();

        Assert.True(w.Clock.InDrop);
        Assert.Equal(6f * 1.5f, w.Projectiles.Damage[0], 4);
        Assert.Equal(2.5f * 1.2f, w.Skills.AuraRadius[1], 4);
    }

    [Fact]
    public void Ring_skill_spawns_a_ring_at_the_hero_with_its_stats()
    {
        var w = new World(Ring);
        w.Ctx.Hero = new Vector2(2, 3);

        w.Update();

        Assert.Equal(1, w.Rings.Count);
        Assert.Equal(new Vector2(2, 3), w.Rings.Center[0]);
        Assert.Equal((6f, 12f, 5f, 6f), (w.Rings.MaxRadius[0], w.Rings.Speed[0], w.Rings.Damage[0], w.Rings.Knockback[0]));
    }

    [Fact]
    public void Speaker_lands_on_the_crowd_pulses_on_beats_and_expires()
    {
        var w = new World(Speaker);
        var crowd = new Vector2(6, 0);
        for (int i = 0; i < 5; i++) w.Enemy(crowd + new Vector2(0.3f * i, 0));
        w.Enemy(new Vector2(-6, 0));

        w.Update();
        Assert.Equal(1, w.Summons.Count);
        Assert.True(Vector2.Distance(w.Summons.Position[0], crowd) < 1.5f);

        // Pulses only on beat ticks: 4 beats of life = 2 s = 32 steps.
        int pulses = 1; // the placement step was beat 0
        Assert.True(w.Skills.PulsedThisStep[0]);
        for (int i = 0; i < 40 && w.Summons.Count > 0; i++)
        {
            w.Update();
            if (w.Skills.PulsedThisStep[0])
            {
                pulses++;
                Assert.True(w.Clock.BeatTick);
            }
        }
        Assert.InRange(pulses, 4, 5);
        Assert.Equal(100f - 4f * pulses, w.Enemies.Health[0], 3);
        Assert.Equal(100f, w.Enemies.Health[5]);
    }

    [Fact]
    public void Speaker_at_the_cap_waits_and_none_without_enemies_in_range()
    {
        var w = new World(Speaker);
        for (int i = 0; i < 8; i++) w.Update();
        Assert.Equal(0, w.Summons.Count);

        w.Enemy(new Vector2(4, 0), 1e6f);
        for (int i = 0; i < 24; i++) w.Update(); // cooldown 2 beats < life 4 beats, count 1
        Assert.Equal(1, w.Summons.Count);
    }

    [Fact]
    public void Aura_hits_enemies_in_radius_and_makes_them_take_more_damage()
    {
        var w = new World(Aura);
        int inside = w.Enemy(new Vector2(2.9f, 0)); // 2.9 <= 2.5 + 0.45
        int outside = w.Enemy(new Vector2(0, 3.0f));

        w.Update();

        Assert.Equal(97f, w.Enemies.Health[inside], 3);
        Assert.Equal(100f, w.Enemies.Health[outside]);
        Assert.True(w.Enemies.VulnTimer[inside] > 0f);
        w.Enemies.Damage(inside, 10f, 0);
        Assert.Equal(97f - 15f, w.Enemies.Health[inside], 3);
    }

    [Fact]
    public void Beam_hits_the_line_along_the_aim_only()
    {
        var w = new World(Beam);
        w.Ctx.Aim = new Vector2(1, 0);
        int onLine = w.Enemy(new Vector2(6, 0.8f));  // within width / 2 + enemy radius = 0.95
        int far = w.Enemy(new Vector2(11.5f, 0));    // past length + enemy radius
        int side = w.Enemy(new Vector2(5, 1.2f));
        int behind = w.Enemy(new Vector2(-3, 0));

        w.Update();

        Assert.Equal(90f, w.Enemies.Health[onLine], 3);
        Assert.Equal(new[] { 100f, 100f, 100f }, new[] { w.Enemies.Health[far], w.Enemies.Health[side], w.Enemies.Health[behind] });
        Assert.Equal(1, w.Beams.Count);
        Assert.Equal((new Vector2(1, 0), 10f, 1f), (w.Beams.Direction[0], w.Beams.Length[0], w.Beams.Width[0]));
    }

    [Fact]
    public void Orbit_objects_circle_at_range_and_hit_only_near_one()
    {
        var w = new World(Orbit);
        int nearBlade = w.Enemy(new Vector2(0, 3)); // object 0 starts at angle 0 → (sin 0, cos 0) * 3
        int center = w.Enemy(Vector2.Zero);

        w.Update(0f);
        Assert.Equal(3, w.Skills.BladeCount);
        for (int b = 0; b < 3; b++) Assert.Equal(3f, w.Skills.BladePositions[b].Length(), 3);
        Assert.Equal(95f, w.Enemies.Health[nearBlade]);
        Assert.Equal(100f, w.Enemies.Health[center]);
    }

    [Fact]
    public void Locked_skills_do_nothing()
    {
        var w = new World(Notes, Ring);
        w.Update(); // no enemies: notes can't fire, ring is locked

        Assert.Equal(0, w.Projectiles.Count);
        Assert.Equal(0, w.Rings.Count);
        Assert.False(w.Skills.FiredThisStep[1]);
    }

    [Fact]
    public void Damage_bonus_multiplies_skill_damage()
    {
        var w = new World(Beat(), 10, Notes);
        w.Progress.DevGainLevel();
        Assert.Equal(LevelUpKind.Bonus, w.Progress.Options()[0].Kind);
        w.Progress.Choose(0);
        w.Enemy(new Vector2(3, 0));

        w.Update();

        Assert.Equal(6f * 1.02f, w.Projectiles.Damage[0], 4);
    }

    [Fact]
    public void Dead_enemies_are_not_targeted()
    {
        var w = new World(Notes);
        w.Enemy(new Vector2(2, 0));
        w.Enemy(new Vector2(0, 6));
        w.Enemies.Health[0] = 0f;

        w.Update();

        Assert.Equal(1, w.Projectiles.Count);
        Assert.Equal(new Vector2(0, 20), w.Projectiles.Velocity[0]);
    }

    [Fact]
    public void Same_seed_same_speaker_placement()
    {
        var spots = new List<Vector2>();
        for (int run = 0; run < 2; run++)
        {
            var w = new World(Speaker);
            for (int i = 0; i < 12; i++) w.Enemy(new Vector2(i - 6, i % 3));
            w.Update();
            spots.Add(w.Summons.Position[0]);
        }
        Assert.Equal(spots[0], spots[1]);
    }
}
