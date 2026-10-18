using System;
using Vlg.Core.Config;

namespace Vlg.Core.Run;

/// <summary>
/// Hero tempo on play time (paused phases don't advance it). Beat b covers [b, b + 1) * BeatSec.
/// Ticks report boundaries crossed by the last <see cref="Advance"/>; the first Advance crosses beat 0.
/// A long step that crosses several boundaries reports one tick.
/// </summary>
public sealed class BeatClock
{
    public const int BeatsPerBar = 4;

    readonly BeatSettings _s;
    long _half = -1;

    public readonly float BeatSec;
    public double Time { get; private set; }
    /// <summary>Current beat index; -1 before the first Advance.</summary>
    public long Beat { get; private set; } = -1;
    /// <summary>A half-beat boundary (8th note) was crossed: beat-synced skills may fire.</summary>
    public bool HalfTick { get; private set; }
    public bool BeatTick { get; private set; }
    /// <summary>This step crossed the first beat of a Drop.</summary>
    public bool DropStarted { get; private set; }
    /// <summary>This step crossed the first beat of a build-up.</summary>
    public bool BuildStarted { get; private set; }
    /// <summary>Drops started so far.</summary>
    public int Drops { get; private set; }

    public BeatClock(BeatSettings settings)
    {
        _s = settings;
        BeatSec = 60f / settings.Bpm;
    }

    public BeatSettings Settings => _s;
    public int CycleBeats => _s.CycleBars * BeatsPerBar;
    int DropStartBeat => (_s.CycleBars - _s.DropBars) * BeatsPerBar;
    int BuildStartBeat => (_s.CycleBars - _s.DropBars - _s.BuildBars) * BeatsPerBar;

    public int BeatInCycle => Beat < 0 ? 0 : (int)(Beat % CycleBeats);
    public int BeatInBar => Beat < 0 ? 0 : (int)(Beat % BeatsPerBar);
    /// <summary>[0, 1) position inside the current beat.</summary>
    public float BeatPhase => (float)(Time / BeatSec - Math.Floor(Time / BeatSec));

    public bool InDrop => _s.DropBars > 0 && Beat >= 0 && BeatInCycle >= DropStartBeat;
    public bool InBuild => _s.BuildBars > 0 && Beat >= 0 && !InDrop && BeatInCycle >= BuildStartBeat;

    /// <summary>[0, 1] through the build-up, 0 outside it.</summary>
    public float BuildProgress => InBuild
        ? (BeatInCycle - BuildStartBeat + BeatPhase) / (_s.BuildBars * BeatsPerBar)
        : 0f;

    public float DamageMultiplier => InDrop ? 1f + _s.DropDamage : 1f;
    public float AreaMultiplier => InDrop ? 1f + _s.DropArea : 1f;

    public void Advance(float dt)
    {
        Time += dt;
        long half = (long)Math.Floor(Time * 2.0 / BeatSec);
        HalfTick = half > _half;
        _half = half;
        long beat = half >> 1;
        BeatTick = beat > Beat;
        Beat = beat;
        DropStarted = BeatTick && _s.DropBars > 0 && BeatInCycle == DropStartBeat;
        BuildStarted = BeatTick && _s.BuildBars > 0 && BeatInCycle == BuildStartBeat;
        if (DropStarted) Drops++;
    }
}
