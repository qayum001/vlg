using System.Numerics;
using Vlg.Core.Sim;
using Xunit;

namespace Vlg.Core.Tests;

/// <summary>Pierce, bounce, burst, explode and returning shots (<see cref="ProjectileShot"/>).</summary>
public class ProjectileShotTests
{
    const float EnemyRadius = 0.45f;

    sealed class World
    {
        public readonly EnemySwarm Enemies = new(32);
        public readonly SpatialGrid Grid = new(32, 2f, 32);
        public readonly ProjectilePool Pool = new(16, 0.25f, 32);
        public readonly PointBuffer Hits = new(64);
        public readonly FlashEvents Flashes = new(16);
        XorShift32 _rng = new(2);

        public int Enemy(float x, float y, float hp = 100f)
        {
            Enemies.Spawn(new Vector2(x, y), hp, ref _rng);
            return Enemies.Count - 1;
        }

        public void Run(float seconds, float dt = 1f / 60f)
        {
            for (float t = 0; t < seconds; t += dt)
            {
                Hits.Clear();
                Grid.Build(Enemies.Position, Enemies.Count, Vector2.Zero);
                Pool.Update(dt, Enemies, Grid, Hits, EnemyRadius, 0f, Vector2.Zero, Flashes);
            }
        }
    }

    static ProjectileShot Shot(float life = 1f) => new() { Velocity = new Vector2(20, 0), Life = life, Damage = 10, Source = 3 };

    [Fact]
    public void Pierce_passes_through_that_many_extra_enemies_once_each()
    {
        var w = new World();
        int a = w.Enemy(3, 0), b = w.Enemy(5, 0), c = w.Enemy(7, 0), d = w.Enemy(9, 0);
        var s = Shot();
        s.Pierce = 2;
        w.Pool.Spawn(s);

        w.Run(1f);

        Assert.Equal(new[] { 90f, 90f, 90f, 100f }, new[] { w.Enemies.Health[a], w.Enemies.Health[b], w.Enemies.Health[c], w.Enemies.Health[d] });
        Assert.Equal(0, w.Pool.Count);
    }

    [Fact]
    public void Bounce_jumps_to_the_nearest_unhit_enemy_with_falloff()
    {
        var w = new World();
        int a = w.Enemy(3, 0), b = w.Enemy(3, 4), c = w.Enemy(3, 8), far = w.Enemy(-20, 0);
        var s = Shot(0.5f);
        s.Bounces = 2;
        s.Falloff = 0.5f;
        w.Pool.Spawn(s);

        w.Run(1.5f);

        Assert.Equal(new[] { 90f, 95f, 97.5f, 100f }, new[] { w.Enemies.Health[a], w.Enemies.Health[b], w.Enemies.Health[c], w.Enemies.Health[far] });
    }

    [Fact]
    public void Burst_on_bounce_hits_neighbors_of_the_struck_enemy()
    {
        var w = new World();
        int a = w.Enemy(3, 0), side = w.Enemy(3, -1.2f);
        var s = Shot(0.5f);
        s.Bounces = 1;
        s.BurstOnBounce = true;
        w.Pool.Spawn(s);

        w.Run(0.3f);

        Assert.Equal(90f, w.Enemies.Health[a]);
        // Burst (half damage), then the bounce lands on it too.
        Assert.Equal(100f - 5f - 10f, w.Enemies.Health[side]);
        Assert.True(w.Flashes.Count > 0);
    }

    [Fact]
    public void Explosion_damages_enemies_around_the_target()
    {
        var w = new World();
        int a = w.Enemy(3, 0), near = w.Enemy(3, 1.8f), far = w.Enemy(3, 4f);
        var s = Shot();
        s.ExplodeRadius = 2f;
        s.ExplodeFactor = 0.5f;
        w.Pool.Spawn(s);

        w.Run(0.5f);

        Assert.Equal(new[] { 90f, 95f, 100f }, new[] { w.Enemies.Health[a], w.Enemies.Health[near], w.Enemies.Health[far] });
        Assert.Equal(0, w.Pool.Count);
    }

    [Fact]
    public void Returning_shot_hits_on_the_way_out_and_back_then_is_caught()
    {
        var w = new World();
        int a = w.Enemy(3, 0), b = w.Enemy(4, 0);
        var s = Shot(0.3f); // out to 6 m
        s.Returns = true;
        w.Pool.Spawn(s);

        w.Run(0.25f);
        Assert.Equal(new[] { 90f, 90f }, new[] { w.Enemies.Health[a], w.Enemies.Health[b] });
        Assert.True(w.Pool.IsReturning(0));

        w.Run(1f);
        Assert.Equal(new[] { 80f, 80f }, new[] { w.Enemies.Health[a], w.Enemies.Health[b] });
        Assert.Equal(0, w.Pool.Count);
    }
}
