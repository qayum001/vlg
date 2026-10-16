using System;
using Vlg.Core.Bench;
using Xunit;

namespace Vlg.Core.Tests;

public class LaunchTests
{
    [Fact]
    public void No_args_is_interactive_low_preset()
    {
        var o = LaunchOptions.Parse(Array.Empty<string>());
        Assert.Null(o.Bench);
        Assert.Equal("low", o.Preset);
        Assert.False(o.Windowed);
        Assert.False(o.WantsStressTest);
    }

    [Fact]
    public void Stress_flag_or_any_bench_opens_the_stress_test()
    {
        Assert.True(LaunchOptions.Parse(new[] { "--stress" }).WantsStressTest);
        Assert.True(LaunchOptions.Parse(new[] { "--bench", "enemies" }).WantsStressTest);
    }

    [Fact]
    public void Bench_args_parse()
    {
        var o = LaunchOptions.Parse(new[]
        {
            "--bench", "Overdraw", "--preset", "medium", "--out", "C:/x/a.csv", "--screenshot", "s.png",
            "--screenshot-at", "2.5", "--exit-after", "9", "--windowed", "--seed", "42",
        });
        Assert.Equal(BenchMode.Overdraw, o.Bench);
        Assert.Equal("medium", o.Preset);
        Assert.Equal("C:/x/a.csv", o.OutPath);
        Assert.Equal("s.png", o.ScreenshotPath);
        Assert.Equal(2.5f, o.ScreenshotAtSec);
        Assert.Equal(9f, o.ExitAfterSec);
        Assert.True(o.Windowed);
        Assert.Equal(42u, o.Seed);
    }

    [Fact]
    public void Ramp_overrides_replace_only_given_values()
    {
        var data = new Vlg.Core.Config.RampSettings { Start = 250, Step = 250, Max = 10000 };

        Assert.Equal(data, LaunchOptions.Parse(Array.Empty<string>()).ApplyRamp(data));
        var o = LaunchOptions.Parse(new[] { "--ramp-step", "2000", "--ramp-max", "50000" });
        Assert.Equal(new Vlg.Core.Config.RampSettings { Start = 250, Step = 2000, Max = 50000 }, o.ApplyRamp(data));
        Assert.Equal(0, LaunchOptions.Parse(new[] { "--ramp-start", "0" }).ApplyRamp(data).Start);
    }

    [Fact]
    public void Threads_override_parses()
    {
        Assert.Null(LaunchOptions.Parse(Array.Empty<string>()).Threads);
        Assert.Equal(0, LaunchOptions.Parse(new[] { "--threads", "0" }).Threads);
        Assert.Throws<ArgumentException>(() => LaunchOptions.Parse(new[] { "--threads", "-1" }));
    }

    [Theory]
    [InlineData("--ramp-step", "0")]
    [InlineData("--ramp-max", "-1")]
    [InlineData("--ramp-start", "1.5")]
    [InlineData("--benchh", "enemies")]
    [InlineData("--bench", "bosses")]
    [InlineData("--bench", "1")]
    [InlineData("--exit-after", "-3")]
    [InlineData("--preset")]
    public void Bad_args_throw(params string[] args) =>
        Assert.Throws<ArgumentException>(() => LaunchOptions.Parse(args));

    [Fact]
    public void Load_ramps_only_the_bench_mode_value()
    {
        var c = TestConfig.Game;
        var low = c.GetPreset("low");

        var e = BenchLoad.For(BenchMode.Enemies, 1500, c.Bench, low);
        Assert.Equal(new BenchLoad(1500, (int)(1500 * c.Bench.ProjectilesPerEnemy), 0, low.AuraParticles), e);

        var o = BenchLoad.For(BenchMode.Overdraw, 7, c.Bench, low);
        Assert.Equal(c.Bench.FixedEnemies, o.Enemies);
        Assert.Equal(7, o.OverdrawLayers);
        Assert.Equal(low.AuraParticles, o.AuraParticles);

        var p = BenchLoad.For(BenchMode.Particles, 12000, c.Bench, low);
        Assert.Equal(c.Bench.FixedEnemies, p.Enemies);
        Assert.Equal(0, p.OverdrawLayers);
        Assert.Equal(12000, p.AuraParticles);
    }

    [Fact]
    public void Each_mode_has_its_ramp()
    {
        var b = TestConfig.Game.Bench;
        Assert.Same(b.Enemies, BenchLoad.RampFor(BenchMode.Enemies, b));
        Assert.Same(b.Overdraw, BenchLoad.RampFor(BenchMode.Overdraw, b));
        Assert.Same(b.Particles, BenchLoad.RampFor(BenchMode.Particles, b));
    }
}
