using System;
using System.Linq;
using System.Numerics;
using Vlg.Core.Sim;
using Xunit;

namespace Vlg.Core.Tests;

public class EnemySwarmTests
{
    static (EnemySwarm swarm, SpatialGrid grid) Make(params Vector2[] positions)
    {
        var t = TestConfig.Sim;
        var swarm = new EnemySwarm(64);
        var rng = new XorShift32(1);
        foreach (var p in positions) swarm.Spawn(p, t.EnemyHealth, ref rng);
        return (swarm, new SpatialGrid(64, t.GridCellSize, 64));
    }

    static void Step(EnemySwarm swarm, SpatialGrid grid, Vector2 target, int frames, float dt = 1f / 60f)
    {
        var t = TestConfig.Sim;
        for (int f = 0; f < frames; f++)
        {
            grid.Build(swarm.Position, swarm.Count, target);
            swarm.Update(dt, target, grid, t);
        }
    }

    [Fact]
    public void Enemy_moves_toward_target_at_enemy_speed()
    {
        var (swarm, grid) = Make(new Vector2(10, 0));
        Step(swarm, grid, Vector2.Zero, 60);

        float expected = 10f - TestConfig.Sim.EnemySpeed;
        Assert.InRange(swarm.Position[0].X, expected - 0.05f, expected + 0.05f);
        Assert.InRange(swarm.Position[0].Y, -0.01f, 0.01f);
    }

    [Fact]
    public void Enemy_stops_at_the_player_instead_of_overlapping()
    {
        var (swarm, grid) = Make(new Vector2(3, 0));
        Step(swarm, grid, Vector2.Zero, 300);

        var t = TestConfig.Sim;
        Assert.InRange(swarm.Position[0].Length(), t.PlayerRadius, t.PlayerRadius + t.EnemyRadius + 0.1f);
    }

    [Fact]
    public void Overlapping_enemies_are_pushed_apart()
    {
        var (swarm, grid) = Make(new Vector2(0, 0), new Vector2(0.05f, 0), new Vector2(0.05f, 0));
        // Target far along +Y: same desired velocity for all, so spread comes only from separation.
        Step(swarm, grid, new Vector2(0, 1000), 60);

        var t = TestConfig.Sim;
        for (int i = 0; i < 3; i++)
        for (int j = i + 1; j < 3; j++)
            Assert.True(Vector2.Distance(swarm.Position[i], swarm.Position[j]) > t.SeparationRadius * 0.6f,
                $"enemies {i} and {j} still overlap");
    }

    [Fact]
    public void Heading_turns_toward_movement()
    {
        var (swarm, grid) = Make(new Vector2(0, -10));
        Step(swarm, grid, Vector2.Zero, 60);

        // Moving +Y (world +Z) means heading 0.
        Assert.InRange(swarm.Heading[0], -0.01f, 0.01f);
    }

    [Fact]
    public void Anim_phase_advances_and_wraps()
    {
        var (swarm, grid) = Make(new Vector2(100, 0));
        float start = swarm.AnimPhase[0];
        Step(swarm, grid, Vector2.Zero, 120);

        float expectedCycles = TestConfig.Sim.AnimRate * 2f;
        float expected = (start + expectedCycles) % 1f;
        float diff = MathF.Abs(swarm.AnimPhase[0] - expected);
        Assert.InRange(swarm.AnimPhase[0], 0f, 1f);
        Assert.InRange(MathF.Min(diff, 1f - diff), 0f, 0.02f);
    }

    [Fact]
    public void RemoveDead_compacts_keeps_living_data_and_reports_positions()
    {
        var (swarm, _) = Make(new Vector2(1, 0), new Vector2(2, 0), new Vector2(3, 0), new Vector2(4, 0));
        swarm.Health[0] = 0;
        swarm.Health[2] = -1;
        var deaths = new PointBuffer(8);

        int removed = swarm.RemoveDead(deaths);

        Assert.Equal(2, removed);
        Assert.Equal(2, swarm.Count);
        Assert.Equal(new[] { new Vector2(1, 0), new Vector2(3, 0) }, new[] { deaths.Items[0], deaths.Items[1] }.OrderBy(v => v.X));
        var alive = new[] { swarm.Position[0], swarm.Position[1] }.OrderBy(v => v.X).ToArray();
        Assert.Equal(new[] { new Vector2(2, 0), new Vector2(4, 0) }, alive);
    }

    [Fact]
    public void Spawn_fails_when_full()
    {
        var swarm = new EnemySwarm(1);
        var rng = new XorShift32(1);
        Assert.True(swarm.Spawn(Vector2.Zero, 1, ref rng));
        Assert.False(swarm.Spawn(Vector2.Zero, 1, ref rng));
        Assert.Equal(1, swarm.Count);
    }
}
