using System.Numerics;
using Vlg.Core.Run;
using Xunit;

namespace Vlg.Core.Tests;

public class GemPoolTests
{
    const float Pickup = 3f, Speed = 10f, Collect = 0.5f, Despawn = 60f, Dt = 0.05f;

    [Fact]
    public void Gem_outside_pickup_radius_stays()
    {
        var g = new GemPool(4);
        g.Spawn(new Vector2(5, 0), 1);

        Assert.Equal(0f, g.Update(Dt, Vector2.Zero, Pickup, Speed, Collect, Despawn));
        Assert.Equal(new Vector2(5, 0), g.Position[0]);
        Assert.False(g.Magnet[0]);
    }

    [Fact]
    public void Gem_inside_radius_flies_to_the_hero_and_keeps_following()
    {
        var g = new GemPool(4);
        g.Spawn(new Vector2(2.5f, 0), 1);

        g.Update(Dt, Vector2.Zero, Pickup, Speed, Collect, Despawn);
        Assert.True(g.Magnet[0]);
        Assert.Equal(2.5f - Speed * Dt, g.Position[0].X, 4);

        // Hero runs past the pickup radius: magnetized gem still follows.
        var hero = new Vector2(-10, 0);
        float before = Vector2.Distance(g.Position[0], hero);
        g.Update(Dt, hero, Pickup, Speed, Collect, Despawn);
        Assert.Equal(before - Speed * Dt, Vector2.Distance(g.Position[0], hero), 3);
    }

    [Fact]
    public void Gem_is_collected_once_and_returns_its_value()
    {
        var g = new GemPool(4);
        g.Spawn(new Vector2(2, 0), 3);
        g.Spawn(new Vector2(0, 50), 7);

        float total = 0f;
        for (int i = 0; i < 20; i++) total += g.Update(Dt, Vector2.Zero, Pickup, Speed, Collect, Despawn);

        Assert.Equal(3f, total);
        Assert.Equal(1, g.Count);
        Assert.Equal(7f, g.Value[0]);
    }

    [Fact]
    public void Full_pool_merges_value_instead_of_losing_it()
    {
        var g = new GemPool(2);
        g.Spawn(new Vector2(10, 0), 1);
        g.Spawn(new Vector2(20, 0), 2);
        g.Spawn(new Vector2(30, 0), 4);

        Assert.Equal(2, g.Count);
        Assert.Equal(7f, g.Value[0] + g.Value[1]);
    }

    [Fact]
    public void Gem_left_beyond_despawn_radius_vanishes_but_magnetized_ones_follow()
    {
        var g = new GemPool(4);
        g.Spawn(new Vector2(70, 0), 5);  // left behind
        g.Spawn(new Vector2(59, 0), 3);  // still in range
        g.Spawn(new Vector2(2, 0), 1);   // gets magnetized
        g.Update(Dt, Vector2.Zero, Pickup, Speed, Collect, Despawn);
        Assert.Equal(2, g.Count);

        // Hero teleports far away: the magnetized gem keeps following, the idle one is now behind and vanishes.
        float xp = g.Update(Dt, new Vector2(-100, 0), Pickup, Speed, Collect, Despawn);
        Assert.Equal(0f, xp);
        Assert.Equal(1, g.Count);
        Assert.True(g.Magnet[0]);
        Assert.Equal(1f, g.Value[0]);
    }

    [Fact]
    public void Zero_value_spawns_nothing()
    {
        var g = new GemPool(2);
        g.Spawn(Vector2.Zero, 0);
        Assert.Equal(0, g.Count);
    }
}
