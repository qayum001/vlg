using System.Numerics;
using Vlg.Core.Sim;
using Xunit;

namespace Vlg.Core.Tests;

public class HordeSimTests
{
    const float Dt = 1f / 60f;

    [Fact]
    public void Ring_spawns_hold_the_target_count_outside_the_view_radius()
    {
        var t = TestConfig.Sim;
        var sim = new HordeSim(t, 11) { SpawnMode = SpawnMode.Ring };
        var player = new Vector2(5, -3);

        sim.Step(Dt, player, new Vector2(0, -1), 400, 0);

        Assert.Equal(400, sim.Enemies.Count);
        for (int i = 0; i < sim.Enemies.Count; i++)
            Assert.InRange(Vector2.Distance(sim.Enemies.Position[i], player), t.SpawnRadiusMin - 0.01f, t.SpawnRadiusMax + 0.01f);
    }

    [Fact]
    public void Area_spawns_are_inside_the_area_annulus()
    {
        var t = TestConfig.Sim;
        var sim = new HordeSim(t, 12) { SpawnMode = SpawnMode.Area };

        sim.Step(Dt, Vector2.Zero, new Vector2(0, -1), 300, 0);

        for (int i = 0; i < sim.Enemies.Count; i++)
            Assert.InRange(sim.Enemies.Position[i].Length(), t.SpawnAreaMin - 0.01f, t.SpawnAreaMax + 0.01f);
    }

    [Fact]
    public void Lower_target_removes_enemies()
    {
        var sim = new HordeSim(TestConfig.Sim, 13);
        sim.Step(Dt, Vector2.Zero, new Vector2(0, -1), 500, 0);
        sim.Step(Dt, Vector2.Zero, new Vector2(0, -1), 120, 0);

        Assert.Equal(120, sim.Enemies.Count);
    }

    [Fact]
    public void Target_above_capacity_is_clamped()
    {
        var t = TestConfig.Sim with { EnemyCapacity = 50 };
        var sim = new HordeSim(t, 14);
        sim.Step(Dt, Vector2.Zero, new Vector2(0, -1), 1000, 0);

        Assert.Equal(50, sim.Enemies.Count);
    }

    [Fact]
    public void Weapons_keep_live_projectiles_near_target()
    {
        // No enemies: nothing to hit, projectiles only expire.
        var sim = new HordeSim(TestConfig.Sim, 15);
        for (int f = 0; f < 180; f++) sim.Step(Dt, Vector2.Zero, new Vector2(0, -1), 0, 600);

        Assert.InRange(sim.Projectiles.Count, 540, 600);
    }

    [Fact]
    public void Combat_kills_enemies_and_refills_them()
    {
        var sim = new HordeSim(TestConfig.Sim, 16) { SpawnMode = SpawnMode.Area };
        int deathEvents = 0;
        for (int f = 0; f < 600; f++)
        {
            sim.Step(Dt, Vector2.Zero, new Vector2(0, -1), 300, 150);
            deathEvents += sim.Deaths.Count + sim.Deaths.Dropped;
        }

        Assert.True(sim.KilledTotal > 20, $"killed {sim.KilledTotal}");
        Assert.Equal(sim.KilledTotal, deathEvents);
        Assert.Equal(300, sim.Enemies.Count);
    }

    [Fact]
    public void Same_seed_gives_same_world()
    {
        var a = new HordeSim(TestConfig.Sim, 99) { SpawnMode = SpawnMode.Area };
        var b = new HordeSim(TestConfig.Sim, 99) { SpawnMode = SpawnMode.Area };
        for (int f = 0; f < 120; f++)
        {
            a.Step(Dt, Vector2.Zero, new Vector2(0, -1), 200, 100);
            b.Step(Dt, Vector2.Zero, new Vector2(0, -1), 200, 100);
        }

        Assert.Equal(a.KilledTotal, b.KilledTotal);
        for (int i = 0; i < a.Enemies.Count; i++) Assert.Equal(a.Enemies.Position[i], b.Enemies.Position[i]);
    }
}
