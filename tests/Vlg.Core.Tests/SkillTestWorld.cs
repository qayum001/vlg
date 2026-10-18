using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Run;
using Vlg.Core.Sim;

namespace Vlg.Core.Tests;

/// <summary>Skill system with its pools and a beat clock, hero at the origin. Shared by skill tests.</summary>
sealed class SkillTestWorld
{
    public const float HeroRadius = 0.5f, EnemyRadius = 0.45f;
    /// <summary>120 BPM: beat 0.5 s, half-beat 0.25 s. Steps of 1/16 s land exactly on boundaries.</summary>
    public const float Dt = 0.0625f;

    public readonly SkillSystem Skills;
    public readonly HeroProgress Progress;
    public readonly BeatClock Clock;
    public readonly EnemySwarm Enemies = new(64);
    public readonly SpatialGrid Grid = new(32, 2f, 64);
    public readonly ProjectilePool Projectiles = new(256, 0.25f, 64);
    public readonly RingPool Rings = new(16, 64);
    public readonly SummonPool Summons = new(32);
    public readonly ZonePool Zones = new(64, 64);
    public readonly StrikePool Strikes = new(32, 64);
    public readonly BeamEvents Beams = new(64);
    public readonly FlashEvents Flashes = new(32);
    public readonly PointBuffer Hits = new(512);
    public readonly SkillWorld Ctx;
    XorShift32 _rng = new(5);

    public static BeatSettings Beat(int dropBars = 0, float dropDamage = 0f, float dropArea = 0f) => new()
    {
        Bpm = 120, CycleBars = 1, BuildBars = 0, DropBars = dropBars, DropDamage = dropDamage, DropArea = dropArea,
        ShockDamage = 0, ShockRadius = 1, ShockSpeed = 1, ShockKnockback = 0,
    };

    public static SkillDef Skill(SkillKind kind, SkillStats stats, SkillStats perLevel = default) => new()
    {
        Id = kind.ToString(), Name = kind.ToString(), Kind = kind, Tags = new[] { SkillTag.Area }, Base = stats, PerLevel = perLevel,
    };

    /// <summary>One branch at skill level 5 that applies <paramref name="add"/> (and optionally kind/flags).</summary>
    public static SkillDef WithBranch(SkillDef def, SkillStats add, SkillKind? kind = null, params SkillFlag[] flags) => def with
    {
        Branches = new[] { new BranchDef { Id = "b", Name = "B", Text = "b", Kind = kind, Add = add, Flags = flags } },
    };

    public SkillTestWorld(BeatSettings beat, int divisor, params SkillDef[] defs)
    {
        Clock = new BeatClock(beat);
        Ctx = new SkillWorld(Enemies, Grid, Projectiles, Rings, Summons, Zones, Strikes, Beams, Flashes, Hits, HeroRadius, EnemyRadius);
        Skills = new SkillSystem(defs, Ctx, Clock, 1, 7);
        Progress = new HeroProgress(new LevelingSettings
        {
            XpBase = 5, XpPerLevel = 0, LevelDivisor = divisor, MaxSkillLevel = 10, MaxedBonusDamage = 0.02f, BranchLevels = new[] { 5, 10 },
        }, defs.Length, defs);
    }

    public SkillTestWorld(params SkillDef[] defs) : this(Beat(), 3, defs) { }

    public int Enemy(Vector2 p, float hp = 100f)
    {
        Enemies.Spawn(p, hp, ref _rng);
        return Enemies.Count - 1;
    }

    /// <summary>Skills only.</summary>
    public void Update(float dt = Dt)
    {
        Hits.Clear();
        Beams.Clear();
        Flashes.Clear();
        Clock.Advance(dt);
        Grid.Build(Enemies.Position, Enemies.Count, Vector2.Zero);
        Skills.Update(dt, Progress, Progress.DamageMultiplier);
    }

    /// <summary>Skills plus strikes, zones, rings and projectiles, in RunSim order (no enemy movement, no removal).</summary>
    public void Full(float dt = Dt)
    {
        Update(dt);
        Strikes.Update(dt, Enemies, Grid, Hits, EnemyRadius, 0f, Flashes);
        Zones.Update(dt, Clock.BeatTick, Enemies, Grid, Hits, EnemyRadius, 0f, Beams);
        Rings.Update(dt, Enemies, Grid, Hits, EnemyRadius, 0f);
        Projectiles.Update(dt, Enemies, Grid, Hits, EnemyRadius, 0f, Ctx.Hero, Flashes);
    }

    /// <summary>Raises <paramref name="slot"/> by level-ups (dev levels); answers branch picks with option 0.</summary>
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
            while (Progress.PendingBranch) Progress.Choose(0);
        }
    }

    /// <summary>Skill at level 5 with branch option 0 (dev set + pick).</summary>
    public void BranchAll()
    {
        Progress.DevSetAllSkills(5);
        while (Progress.PendingBranch) Progress.Choose(0);
    }
}
