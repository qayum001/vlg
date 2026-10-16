using System;
using System.Globalization;
using System.Numerics;
using System.Threading;
using Vlg.Core.Bench;
using Vlg.Core.Config;
using Xunit;

namespace Vlg.Core.Tests;

public class BenchTests
{
    [Fact]
    public void Percentile_uses_nearest_rank()
    {
        var stats = new FrameStats(200);
        for (int i = 100; i >= 1; i--) stats.Add(i);

        Assert.Equal(50f, stats.Percentile(50));
        Assert.Equal(95f, stats.Percentile(95));
        Assert.Equal(99f, stats.Percentile(99));
        Assert.Equal(100f, stats.Percentile(100));
        Assert.Equal(100f, stats.Max);
        Assert.Equal(50.5f, stats.Average);
    }

    [Fact]
    public void Full_stats_buffer_counts_dropped_samples()
    {
        var stats = new FrameStats(2);
        stats.Add(1); stats.Add(2); stats.Add(3);

        Assert.Equal(2, stats.Count);
        Assert.Equal(1, stats.Dropped);
        stats.Reset();
        Assert.Equal(0, stats.Count);
        Assert.Equal(0f, stats.Percentile(95));
    }

    static BenchSettings Bench(float warmup = 0.5f, float measure = 1f) => TestConfig.Game.Bench with
    {
        WarmupSec = warmup,
        MeasureSec = measure,
        ThresholdMs = 16.7f,
        FailStepsToStop = 2,
    };

    /// <summary>Feeds frames whose time grows with the ramp value: value 1000 → 10 ms.</summary>
    static RampController RunLinearLoad(RampSettings ramp, BenchSettings bench)
    {
        var ctl = new RampController(ramp, bench);
        for (int guard = 0; guard < 1_000_000 && !ctl.Done; guard++)
            ctl.Tick(new FrameSample(ctl.Value / 100f, 1, 1, 0.5f, 10, 100, ctl.Value, 0));
        Assert.True(ctl.Done);
        return ctl;
    }

    [Fact]
    public void Ramp_stops_after_consecutive_failures_and_reports_best_passing()
    {
        var ramp = new RampSettings { Start = 200, Step = 200, Max = 10000 };
        var ctl = RunLinearLoad(ramp, Bench());

        // 1600 → 16 ms passes, 1800 and 2000 fail.
        Assert.Equal(1600, ctl.BestPassing);
        Assert.Equal(10, ctl.Results.Count);
        Assert.False(ctl.Results[^1].Pass);
        Assert.Equal(2000, ctl.Results[^1].Value);
    }

    [Fact]
    public void Ramp_stops_at_max_when_everything_passes()
    {
        var ramp = new RampSettings { Start = 100, Step = 100, Max = 500 };
        var ctl = RunLinearLoad(ramp, Bench());

        Assert.Equal(5, ctl.Results.Count);
        Assert.Equal(500, ctl.BestPassing);
    }

    [Fact]
    public void Warmup_frames_are_not_measured()
    {
        var ramp = new RampSettings { Start = 1, Step = 1, Max = 1 };
        var ctl = new RampController(ramp, Bench(warmup: 1f, measure: 1f));
        // Warmup: one 1000 ms spike. Measure: 100 frames of 10 ms.
        ctl.Tick(new FrameSample(1000, 0, 0, 0, 0, 0, 0, 0));
        StepResult? result = null;
        for (int i = 0; i < 100 && result == null; i++)
            result = ctl.Tick(new FrameSample(10, 2, 3, 0.5f, 40, 9000, 1, 2));

        Assert.NotNull(result);
        Assert.Equal(100, result!.Frames);
        Assert.Equal(10f, result.MaxMs);
        Assert.Equal(2f, result.GpuMs);
        Assert.Equal(40f, result.DrawCalls);
        Assert.True(result.Pass);
    }

    [Fact]
    public void Csv_row_matches_header_and_ignores_culture()
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var r = new StepResult(0, 250, 180, 12.5f, 12f, 15.25f, 16f, 20f, 9f, 3f, 1.25f, 120, 500000, 250, 125, true);
            string row = BenchCsv.Row(r, new MemorySample(300, 700, 650, 12.5f));

            Assert.Equal(BenchCsv.ColumnCount(BenchCsv.Header), BenchCsv.ColumnCount(row));
            Assert.StartsWith("0,250,180,12.50,12.00,15.25,", row);
            Assert.Equal("# adapter=A; B", BenchCsv.Meta("adapter", "A, B"));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Bench_path_is_slower_than_enemies()
    {
        float maxSpeed = 0;
        for (float t = 0; t < BenchPath.PeriodSec; t += 0.05f)
            maxSpeed = MathF.Max(maxSpeed, Vector2.Distance(BenchPath.PlayerAt(t), BenchPath.PlayerAt(t + 0.05f)) / 0.05f);

        Assert.True(maxSpeed < TestConfig.Sim.EnemySpeed, $"path speed {maxSpeed}");
        Assert.True(Vector2.Distance(BenchPath.PlayerAt(0), BenchPath.PlayerAt(BenchPath.PeriodSec)) < 1e-3f);
    }
}
