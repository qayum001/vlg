using System;
using System.Linq;
using System.Numerics;
using Vlg.Core.Sim;
using Xunit;

namespace Vlg.Core.Tests;

public class SpatialGridTests
{
    [Fact]
    public void Query_matches_brute_force()
    {
        var rng = new XorShift32(7);
        const int count = 3000;
        var points = new Vector2[count];
        for (int i = 0; i < count; i++) points[i] = new Vector2(rng.Range(-40, 40), rng.Range(-40, 40));
        var grid = new SpatialGrid(64, 1.5f, count);
        grid.Build(points, count, new Vector2(3, -2));

        var results = new int[count];
        for (int q = 0; q < 200; q++)
        {
            var p = new Vector2(rng.Range(-45, 45), rng.Range(-45, 45));
            float r = rng.Range(0.1f, 6f);
            int n = grid.Query(p, r, results, points);
            var expected = Enumerable.Range(0, count).Where(i => Vector2.DistanceSquared(points[i], p) <= r * r).ToHashSet();
            Assert.Equal(expected, results.Take(n).ToHashSet());
        }
    }

    [Fact]
    public void Points_outside_the_grid_are_still_found()
    {
        var points = new[] { new Vector2(500, 500), new Vector2(-500, 0) };
        var grid = new SpatialGrid(8, 1f, points.Length);
        grid.Build(points, points.Length, Vector2.Zero);
        var results = new int[2];

        Assert.Equal(1, grid.Query(new Vector2(500.5f, 500), 1f, results, points));
        Assert.Equal(0, results[0]);
        Assert.Equal(1, grid.Query(new Vector2(-500, 0.2f), 1f, results, points));
        Assert.Equal(1, results[0]);
    }

    [Fact]
    public void Query_stops_when_results_are_full()
    {
        var points = Enumerable.Range(0, 50).Select(i => new Vector2(i * 0.01f, 0)).ToArray();
        var grid = new SpatialGrid(16, 1f, points.Length);
        grid.Build(points, points.Length, Vector2.Zero);

        Assert.Equal(5, grid.Query(Vector2.Zero, 2f, new int[5], points));
    }

    [Fact]
    public void Build_ignores_entries_past_count()
    {
        var points = new[] { Vector2.Zero, Vector2.Zero, Vector2.Zero };
        var grid = new SpatialGrid(4, 1f, points.Length);
        grid.Build(points, 1, Vector2.Zero);

        Assert.Equal(1, grid.Query(Vector2.Zero, 1f, new int[3], points));
    }
}
