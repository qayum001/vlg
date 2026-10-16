using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Run;
using Vlg.Core.Sim;
using Xunit;

namespace Vlg.Core.Tests;

public class SkillSystemTests
{
    const float HeroRadius = 0.5f, EnemyRadius = 0.45f;

    static readonly SkillDef Bolt = new()
    {
        Id = "bolt", Name = "Bolt", Kind = SkillKind.Bolt,
        Base = new SkillStats { Damage = 6, Cooldown = 1, Count = 1, Speed = 20, Range = 10 },
        PerLevel = new SkillStats { Count = 0.5f },
    };

    static readonly SkillDef Nova = new()
    {
        Id = "nova", Name = "Nova", Kind = SkillKind.Nova,
        Base = new SkillStats { Damage = 5, Cooldown = 1, Count = 4, Speed = 10, Range = 5 },
        PerLevel = default,
    };

    static readonly SkillDef Aura = new()
    {
        Id = "aura", Name = "Aura", Kind = SkillKind.Aura,
        Base = new SkillStats { Damage = 3, Cooldown = 0.5f, Radius = 2.5f },
        PerLevel = default,
    };

    static readonly SkillDef Orbit = new()
    {
        Id = "orbit", Name = "Orbit", Kind = SkillKind.Orbit,
        Base = new SkillStats { Damage = 5, Cooldown = 0.25f, Count = 3, Speed = 2, Range = 3, Radius = 0.6f },
        PerLevel = default,
    };

    sealed class World
    {
        public readonly SkillSystem Skills;
        public readonly HeroProgress Progress;
        public readonly EnemySwarm Enemies = new(64);
        public readonly SpatialGrid Grid = new(32, 2f, 64);
        public readonly ProjectilePool Projectiles = new(256);
        public readonly PointBuffer Hits = new(64);
        XorShift32 _rng = new(5);

        public World(int divisor, params SkillDef[] defs)
        {
            Skills = new SkillSystem(defs, 64);
            Progress = new HeroProgress(new LevelingSettings
            {
                XpBase = 5, XpPerLevel = 0, LevelDivisor = divisor, MaxSkillLevel = 10, MaxedBonusDamage = 0.02f,
            }, defs.Length);
        }

        public World(params SkillDef[] defs) : this(3, defs) { }

        public void Enemy(Vector2 p, float hp = 100f) => Enemies.Spawn(p, hp, ref _rng);

        public void Update(float dt)
        {
            Hits.Clear();
            Grid.Build(Enemies.Position, Enemies.Count, Vector2.Zero);
            Skills.Update(dt, Vector2.Zero, HeroRadius, Progress, Enemies, Grid, Projectiles, Hits, EnemyRadius, 0f, ref _rng);
        }
    }

    [Fact]
    public void Bolt_fires_at_the_nearest_enemy_with_its_damage_and_range()
    {
        var w = new World(Bolt);
        w.Enemy(new Vector2(0, 8));
        w.Enemy(new Vector2(5, 0));

        w.Update(0.1f);

        Assert.Equal(1, w.Projectiles.Count);
        Assert.Equal(new Vector2(20, 0), w.Projectiles.Velocity[0]);
        Assert.Equal(new Vector2(HeroRadius, 0), w.Projectiles.Position[0]);
        Assert.Equal(6f, w.Projectiles.Damage[0]);
        Assert.Equal(10f / 20f, w.Projectiles.Life[0]);
        Assert.True(w.Skills.FiredThisStep[0]);
    }

    [Fact]
    public void Bolt_without_a_target_in_range_waits_and_fires_when_one_arrives()
    {
        var w = new World(Bolt);
        w.Enemy(new Vector2(0, 11));
        for (int i = 0; i < 5; i++) w.Update(0.1f);
        Assert.Equal(0, w.Projectiles.Count);

        w.Enemies.Position[0] = new Vector2(0, 9);
        w.Update(0.1f);
        Assert.Equal(1, w.Projectiles.Count);
    }

    [Fact]
    public void Bolt_level_three_fires_two_projectiles_at_the_two_nearest()
    {
        var w = new World(1, Bolt);
        w.Progress.DevGainLevel();
        w.Progress.Choose(0);
        w.Progress.DevGainLevel();
        w.Progress.Choose(0);
        Assert.Equal(3, w.Progress.SkillLevel(0));
        w.Enemy(new Vector2(-4, 0));
        w.Enemy(new Vector2(9, 0));
        w.Enemy(new Vector2(0, 3));

        w.Update(0.1f);

        Assert.Equal(2, w.Projectiles.Count);
        Assert.Equal(new Vector2(0, 20), w.Projectiles.Velocity[0]);
        Assert.Equal(new Vector2(-20, 0), w.Projectiles.Velocity[1]);
    }

    [Fact]
    public void Cooldown_paces_casts_and_keeps_the_average_rate()
    {
        var w = new World(Nova);
        for (int i = 0; i < 40; i++) w.Update(0.25f); // 10 s

        // Ready at start, then every 1 s: t = 0, 1, ..., 10 → 11 casts of 4.
        Assert.Equal(44, w.Projectiles.Count);
    }

    [Fact]
    public void Nova_ring_is_evenly_spaced_at_full_speed()
    {
        var w = new World(Nova);
        w.Update(0.1f);

        Assert.Equal(4, w.Projectiles.Count);
        var sum = Vector2.Zero;
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(10f, w.Projectiles.Velocity[i].Length(), 3);
            sum += w.Projectiles.Velocity[i];
            float dot = Vector2.Dot(Vector2.Normalize(w.Projectiles.Velocity[i]), Vector2.Normalize(w.Projectiles.Velocity[(i + 1) % 4]));
            Assert.Equal(0f, dot, 3); // 90 degrees apart
        }
        Assert.True(sum.Length() < 1e-3f);
    }

    [Fact]
    public void Aura_ticks_damage_only_enemies_touching_its_radius()
    {
        var w = new World(Aura);
        w.Enemy(new Vector2(2.9f, 0)); // 2.9 <= 2.5 + 0.45
        w.Enemy(new Vector2(0, 3.0f)); // outside

        for (int i = 0; i < 4; i++) w.Update(0.25f); // casts at steps 1, 2, 4

        Assert.Equal(100f - 3 * 3f, w.Enemies.Health[0]);
        Assert.Equal(100f, w.Enemies.Health[1]);
        Assert.Equal(2.5f, w.Skills.AuraRadius[0]);
    }

    [Fact]
    public void Orbit_blades_circle_at_range_and_hit_only_near_a_blade()
    {
        var w = new World(Orbit);
        w.Update(0.0f);
        Assert.Equal(3, w.Skills.BladeCount);
        for (int b = 0; b < 3; b++) Assert.Equal(3f, w.Skills.BladePositions[b].Length(), 3);

        var w2 = new World(Orbit);
        w2.Enemy(new Vector2(0, 3)); // blade 0 at angle 0 → (sin 0, cos 0) * 3
        w2.Enemy(Vector2.Zero);      // inside the orbit, far from every blade
        w2.Update(0.0f);

        Assert.Equal(95f, w2.Enemies.Health[0]);
        Assert.Equal(100f, w2.Enemies.Health[1]);
        Assert.Equal(1, w2.Hits.Count);
    }

    [Fact]
    public void Locked_skills_do_nothing()
    {
        var w = new World(Bolt, Nova);
        w.Update(0.1f); // no enemies: bolt can't fire, nova is locked

        Assert.Equal(0, w.Projectiles.Count);
        Assert.False(w.Skills.FiredThisStep[1]);
    }

    [Fact]
    public void Damage_bonus_multiplies_skill_damage()
    {
        var w = new World(10, Bolt);
        w.Progress.DevGainLevel();
        Assert.Equal(LevelUpKind.Bonus, w.Progress.Options()[0].Kind);
        w.Progress.Choose(0);
        w.Enemy(new Vector2(3, 0));

        w.Update(0.1f);

        Assert.Equal(6f * 1.02f, w.Projectiles.Damage[0], 4);
    }

    [Fact]
    public void Dead_enemies_are_not_targeted()
    {
        var w = new World(Bolt);
        w.Enemy(new Vector2(2, 0));
        w.Enemy(new Vector2(0, 6));
        w.Enemies.Health[0] = 0f;

        w.Update(0.1f);

        Assert.Equal(1, w.Projectiles.Count);
        Assert.Equal(new Vector2(0, 20), w.Projectiles.Velocity[0]);
    }
}
