using System.Numerics;
using Vlg.Core.Sim;
using Xunit;

namespace Vlg.Core.Tests;

public class ProjectilePoolTests
{
    [Fact]
    public void Projectile_hits_enemy_in_its_path_once()
    {
        var t = TestConfig.Sim;
        var enemies = new EnemySwarm(4);
        var rng = new XorShift32(3);
        enemies.Spawn(new Vector2(0, 5), t.EnemyHealth, ref rng);
        var grid = new SpatialGrid(32, t.GridCellSize, 4);
        var pool = new ProjectilePool(4, TestConfig.Sim.ProjectileRadius);
        pool.Spawn(Vector2.Zero, new Vector2(0, t.ProjectileSpeed), t.ProjectileLife, t.ProjectileDamage);
        var hits = new PointBuffer(8);

        int totalHits = 0;
        for (int f = 0; f < 60; f++)
        {
            hits.Clear();
            grid.Build(enemies.Position, enemies.Count, Vector2.Zero);
            pool.Update(1f / 60f, enemies, grid, hits, t.EnemyRadius, t.EnemySpeed * EnemySwarm.MaxSpeedFactor);
            totalHits += hits.Count;
        }

        Assert.Equal(1, totalHits);
        Assert.Equal(0, pool.Count);
        Assert.Equal(t.EnemyHealth - t.ProjectileDamage, enemies.Health[0]);
        Assert.Equal(1f, enemies.HitFlash[0]);
    }

    [Fact]
    public void Projectile_expires_after_its_life()
    {
        var t = TestConfig.Sim;
        var enemies = new EnemySwarm(1);
        var grid = new SpatialGrid(8, 1f, 1);
        var pool = new ProjectilePool(2, TestConfig.Sim.ProjectileRadius);
        pool.Spawn(Vector2.Zero, new Vector2(1, 0), 0.5f, t.ProjectileDamage);
        pool.Spawn(Vector2.Zero, new Vector2(1, 0), 2f, t.ProjectileDamage);
        var hits = new PointBuffer(1);

        for (int f = 0; f < 40; f++) // 0.67 s
        {
            grid.Build(enemies.Position, 0, Vector2.Zero);
            pool.Update(1f / 60f, enemies, grid, hits, t.EnemyRadius, t.EnemySpeed * EnemySwarm.MaxSpeedFactor);
        }

        Assert.Equal(1, pool.Count);
        Assert.InRange(pool.Life[0], 1.3f, 1.4f);
    }

    [Fact]
    public void Dead_enemies_are_not_hit()
    {
        var t = TestConfig.Sim;
        var enemies = new EnemySwarm(1);
        var rng = new XorShift32(3);
        enemies.Spawn(new Vector2(0, 0.1f), t.EnemyHealth, ref rng);
        enemies.Health[0] = 0;
        var grid = new SpatialGrid(8, 1f, 1);
        grid.Build(enemies.Position, 1, Vector2.Zero);
        var pool = new ProjectilePool(1, TestConfig.Sim.ProjectileRadius);
        pool.Spawn(Vector2.Zero, Vector2.Zero, 1f, t.ProjectileDamage);
        var hits = new PointBuffer(1);

        pool.Update(1f / 60f, enemies, grid, hits, t.EnemyRadius, t.EnemySpeed * EnemySwarm.MaxSpeedFactor);

        Assert.Equal(0, hits.Count);
        Assert.Equal(1, pool.Count);
    }

    [Fact]
    public void Hit_uses_the_projectile_own_damage()
    {
        var t = TestConfig.Sim;
        var enemies = new EnemySwarm(1);
        var rng = new XorShift32(3);
        enemies.Spawn(new Vector2(0, 0.1f), 100f, ref rng);
        var grid = new SpatialGrid(8, 1f, 1);
        grid.Build(enemies.Position, 1, Vector2.Zero);
        var pool = new ProjectilePool(1, TestConfig.Sim.ProjectileRadius);
        pool.Spawn(Vector2.Zero, Vector2.Zero, 1f, 7f);

        pool.Update(1f / 60f, enemies, grid, new PointBuffer(1), t.EnemyRadius, t.EnemySpeed);

        Assert.Equal(93f, enemies.Health[0]);
    }
}
