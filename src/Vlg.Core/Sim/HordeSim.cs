using System;
using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Jobs;

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
    readonly SwarmTuning _swarm;
    readonly WorkerPool? _workers;
    XorShift32 _rng;

    /// <param name="workers">Shared pool for the enemy update; null = single thread. Same result either way.</param>
    public HordeSim(SimTuning tuning, uint seed, WorkerPool? workers = null)
    {
        _t = tuning;
        _swarm = tuning.ToSwarm();
        _workers = workers;
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
        Enemies.Update(dt, player, Grid, _swarm, _workers);
        _weapons.Update(dt, targetProjectiles, player, aim, Enemies, Projectiles, ref _rng, _t);
        Projectiles.Update(dt, Enemies, Grid, Hits, _t.ProjectileRadius + _t.EnemyRadius, _t.EnemySpeed * EnemySwarm.MaxSpeedFactor);
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
            Enemies.Spawn(player + RandomPoints.InAnnulus(ref _rng, min, max), _t.EnemyHealth, ref _rng);
    }
}
