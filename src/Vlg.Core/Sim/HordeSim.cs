using System;
using System.Numerics;
using Vlg.Core.Config;

namespace Vlg.Core.Sim;

/// <summary>One stress-test world step: grid, enemies, weapons, projectiles, deaths, refill.</summary>
public sealed class HordeSim
{
    public readonly EnemySwarm Enemies;
    public readonly ProjectilePool Projectiles;
    public readonly SpatialGrid Grid;
    public readonly PointBuffer Hits;
    public readonly PointBuffer Deaths;
    public SpawnMode SpawnMode { get; set; } = SpawnMode.Ring;
    public int KilledTotal { get; private set; }

    readonly WeaponDirector _weapons = new();
    readonly SimTuning _t;
    XorShift32 _rng;

    public HordeSim(SimTuning tuning, uint seed)
    {
        _t = tuning;
        _rng = new XorShift32(seed);
        Enemies = new EnemySwarm(tuning.EnemyCapacity);
        Projectiles = new ProjectilePool(tuning.ProjectileCapacity);
        Grid = new SpatialGrid(tuning.GridDim, tuning.GridCellSize, tuning.EnemyCapacity);
        Hits = new PointBuffer(1024);
        Deaths = new PointBuffer(1024);
    }

    /// <param name="aim">Unit camera forward on the ground plane.</param>
    public void Step(float dt, Vector2 player, Vector2 aim, int targetEnemies, int targetProjectiles)
    {
        dt = MathF.Min(dt, _t.MaxStepSec);
        Hits.Clear();
        Deaths.Clear();

        Grid.Build(Enemies.Position, Enemies.Count, player);
        Enemies.Update(dt, player, Grid, _t);
        _weapons.Update(dt, targetProjectiles, player, aim, Enemies, Projectiles, ref _rng, _t);
        Projectiles.Update(dt, Enemies, Grid, Hits, _t);
        KilledTotal += Enemies.RemoveDead(Deaths);
        Refill(Math.Min(targetEnemies, Enemies.Capacity), player);
    }

    void Refill(int target, Vector2 player)
    {
        if (Enemies.Count > target)
        {
            Enemies.Truncate(target);
            return;
        }
        float min = SpawnMode == SpawnMode.Ring ? _t.SpawnRadiusMin : _t.SpawnAreaMin;
        float max = SpawnMode == SpawnMode.Ring ? _t.SpawnRadiusMax : _t.SpawnAreaMax;
        while (Enemies.Count < target)
            Enemies.Spawn(player + RandomInAnnulus(min, max), _t.EnemyHealth, ref _rng);
    }

    /// <summary>Uniform by area.</summary>
    Vector2 RandomInAnnulus(float min, float max)
    {
        float a = _rng.Range(-MathF.PI, MathF.PI);
        float r = MathF.Sqrt(_rng.Range(min * min, max * max));
        return new Vector2(MathF.Sin(a), MathF.Cos(a)) * r;
    }
}
