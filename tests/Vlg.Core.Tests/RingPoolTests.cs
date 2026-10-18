using System.Numerics;
using Vlg.Core.Run;
using Vlg.Core.Sim;
using Xunit;

namespace Vlg.Core.Tests;

public class RingPoolTests
{
    const float EnemyRadius = 0.45f;

    sealed class World
    {
        public readonly EnemySwarm Enemies = new(64);
        public readonly SpatialGrid Grid = new(32, 2f, 64);
        public readonly RingPool Rings = new(8, 64);
        public readonly PointBuffer Hits = new(64);
        XorShift32 _rng = new(3);

        public int Enemy(Vector2 p)
        {
            Enemies.Spawn(p, 100f, ref _rng);
            return Enemies.Count - 1;
        }

        public void Step(float dt)
        {
            Hits.Clear();
            Grid.Build(Enemies.Position, Enemies.Count, Vector2.Zero);
            Rings.Update(dt, Enemies, Grid, Hits, EnemyRadius, 0f);
        }
    }

    [Fact]
    public void Ring_hits_each_enemy_once_as_its_front_passes_then_ends_at_max_radius()
    {
        var w = new World();
        int center = w.Enemy(Vector2.Zero);
        int mid = w.Enemy(new Vector2(3, 0));
        int edge = w.Enemy(new Vector2(0, -5.4f)); // near edge 4.95 <= 5
        int outside = w.Enemy(new Vector2(6, 0));
        w.Rings.Spawn(new RingShot { MaxRadius = 5f, Speed = 10f, Damage = 7f, Source = 9 });

        w.Step(0.1f);
        Assert.Equal(93f, w.Enemies.Health[center]);
        Assert.Equal(100f, w.Enemies.Health[mid]);

        for (int i = 0; i < 10; i++) w.Step(0.1f);

        Assert.Equal(new[] { 93f, 93f, 93f, 100f },
            new[] { w.Enemies.Health[center], w.Enemies.Health[mid], w.Enemies.Health[edge], w.Enemies.Health[outside] });
        Assert.Equal(0, w.Rings.Count);
        Assert.Equal(9, w.Enemies.LastHit[mid]);
    }

    [Fact]
    public void Knockback_pushes_hit_enemies_away_from_the_center()
    {
        var w = new World();
        int e = w.Enemy(new Vector2(0, 2));
        w.Rings.Spawn(new RingShot { MaxRadius = 5f, Speed = 10f, Damage = 1f, Knockback = 6f, Source = 1 });

        for (int i = 0; i < 4; i++) w.Step(0.1f);

        Assert.Equal(new Vector2(0, 6), w.Enemies.Push[e]);
    }
}
