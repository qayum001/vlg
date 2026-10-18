using System.Numerics;
using Vlg.Core.Sim;

namespace Vlg.Core.Run;

/// <summary>
/// What skill systems act on: shared pools plus per-step hero state. RunSim owns it and sets the
/// per-step fields before skills update.
/// </summary>
public sealed class SkillWorld
{
    public readonly EnemySwarm Enemies;
    public readonly SpatialGrid Grid;
    public readonly ProjectilePool Projectiles;
    public readonly RingPool Rings;
    public readonly SummonPool Summons;
    public readonly BeamEvents Beams;
    public readonly PointBuffer Hits;
    public readonly float HeroRadius;
    public readonly float EnemyRadius;

    public Vector2 Hero;
    /// <summary>Unit camera forward on the ground (aimed skills).</summary>
    public Vector2 Aim = new(0f, -1f);
    /// <summary>Max enemy movement since the grid build.</summary>
    public float QueryPad;

    public SkillWorld(EnemySwarm enemies, SpatialGrid grid, ProjectilePool projectiles, RingPool rings, SummonPool summons,
        BeamEvents beams, PointBuffer hits, float heroRadius, float enemyRadius)
    {
        Enemies = enemies;
        Grid = grid;
        Projectiles = projectiles;
        Rings = rings;
        Summons = summons;
        Beams = beams;
        Hits = hits;
        HeroRadius = heroRadius;
        EnemyRadius = enemyRadius;
    }
}
