using System;
using Vlg.Core.Config;
using Vlg.Core.Run;
using Xunit;

namespace Vlg.Core.Tests;

public class StageLoopTests
{
    static StageLoop DomainLoop() => new(new[]
    {
        new StageDef { Kind = StageKind.Mobs, DurationSec = 120, SpawnPerSec = 1, MaxAlive = 10 },
        new StageDef { Kind = StageKind.Horde, DurationSec = 60, SpawnPerSec = 5, MaxAlive = 50 },
        new StageDef { Kind = StageKind.Chest },
        new StageDef { Kind = StageKind.Mobs, DurationSec = 120, SpawnPerSec = 1, MaxAlive = 10 },
        new StageDef { Kind = StageKind.Shop },
    });

    [Fact]
    public void Full_loop_runs_in_domain_order_and_starts_loop_two()
    {
        var s = DomainLoop();
        Assert.Equal((1, 0, StageKind.Mobs), (s.Loop, s.Index, s.Current.Kind));
        Assert.Equal(120f, s.TimeLeft);

        Assert.False(s.Update(119f));
        Assert.True(s.Update(1f));
        Assert.Equal(StageKind.Horde, s.Current.Kind);
        Assert.Equal(60f, s.TimeLeft);

        Assert.True(s.Update(60f));
        Assert.Equal(StageKind.Chest, s.Current.Kind);
        Assert.False(s.Update(1000f)); // chest waits for the player
        Assert.Equal(2, s.Index);

        s.Continue();
        Assert.Equal((3, StageKind.Mobs), (s.Index, s.Current.Kind));
        Assert.True(s.Update(120f));
        Assert.Equal(StageKind.Shop, s.Current.Kind);

        s.Continue();
        Assert.Equal((2, 0, StageKind.Mobs), (s.Loop, s.Index, s.Current.Kind));
    }

    [Fact]
    public void Leftover_time_carries_into_the_next_timed_stage()
    {
        var s = DomainLoop();
        s.Update(119.5f);
        s.Update(1f);

        Assert.Equal(StageKind.Horde, s.Current.Kind);
        Assert.Equal(0.5f, s.Elapsed, 4);
    }

    [Fact]
    public void Continue_on_a_timed_stage_throws()
    {
        var s = DomainLoop();
        Assert.Throws<InvalidOperationException>(s.Continue);
    }

    [Fact]
    public void Skip_moves_to_the_next_stage_and_wraps_the_loop()
    {
        var s = DomainLoop();
        for (int i = 0; i < 5; i++) s.Skip();

        Assert.Equal((2, 0), (s.Loop, s.Index));
        Assert.Equal(0f, s.Elapsed);
    }
}
