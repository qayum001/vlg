using Vlg.Core.Config;
using Vlg.Core.Run;
using Xunit;

namespace Vlg.Core.Tests;

public class BeatClockTests
{
    /// <summary>120 BPM, 4-bar cycle: beats 0-7 normal, 8-11 build-up, 12-15 Drop.</summary>
    static BeatClock Clock() => new(new BeatSettings
    {
        Bpm = 120, CycleBars = 4, BuildBars = 1, DropBars = 1, DropDamage = 0.3f, DropArea = 0.2f,
        ShockDamage = 1, ShockRadius = 1, ShockSpeed = 1, ShockKnockback = 0,
    });

    [Fact]
    public void First_advance_crosses_beat_zero_then_ticks_every_half_beat()
    {
        var c = Clock();
        c.Advance(0.0625f);
        Assert.True(c.BeatTick && c.HalfTick);
        Assert.Equal(0, c.Beat);

        int halves = 0, beats = 0;
        for (int i = 0; i < 32; i++) // 2 s more
        {
            c.Advance(0.0625f);
            if (c.HalfTick) halves++;
            if (c.BeatTick)
            {
                beats++;
                Assert.True(c.HalfTick);
            }
        }
        Assert.Equal(8, halves);
        Assert.Equal(4, beats);
        Assert.Equal(4, c.Beat);
    }

    [Fact]
    public void Build_up_then_drop_at_the_end_of_each_cycle()
    {
        var c = Clock();
        int dropStarts = 0, buildStarts = 0;
        bool sawBuild = false;
        for (int i = 0; i < 16 * 8 * 2; i++) // two cycles of 16 beats, 8 steps per beat
        {
            c.Advance(0.0625f);
            if (c.DropStarted)
            {
                dropStarts++;
                Assert.Equal(12, c.BeatInCycle);
            }
            if (c.BuildStarted)
            {
                buildStarts++;
                Assert.Equal(8, c.BeatInCycle);
            }
            Assert.Equal(c.BeatInCycle >= 12, c.InDrop);
            Assert.Equal(c.BeatInCycle is >= 8 and < 12, c.InBuild);
            Assert.Equal(c.InDrop ? 1.3f : 1f, c.DamageMultiplier);
            Assert.Equal(c.InDrop ? 1.2f : 1f, c.AreaMultiplier);
            if (c.InBuild)
            {
                sawBuild = true;
                Assert.InRange(c.BuildProgress, 0f, 1f);
            }
        }
        Assert.Equal(2, dropStarts);
        Assert.Equal(2, buildStarts);
        Assert.Equal(2, c.Drops);
        Assert.True(sawBuild);
    }

    [Fact]
    public void Long_step_reports_one_tick()
    {
        var c = Clock();
        c.Advance(0.01f);
        c.Advance(2f);
        Assert.True(c.BeatTick);
        Assert.Equal(4, c.Beat);
    }
}
