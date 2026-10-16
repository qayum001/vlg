using System;
using Vlg.Core.Config;

namespace Vlg.Core.Run;

/// <summary>Walks the stage list; after the last stage the next loop starts. Loop is 1-based.</summary>
public sealed class StageLoop
{
    readonly StageDef[] _stages;

    public int Loop { get; private set; } = 1;
    public int Index { get; private set; }
    /// <summary>Seconds spent in the current timed stage.</summary>
    public float Elapsed { get; private set; }

    public StageLoop(StageDef[] stages) => _stages = stages;

    public StageDef Current => _stages[Index];
    public float TimeLeft => Current.IsTimed ? MathF.Max(0f, Current.DurationSec - Elapsed) : 0f;

    /// <summary>Advances a timed stage. Returns true when the stage changed. Leftover time carries into a timed next stage.</summary>
    public bool Update(float dt)
    {
        if (!Current.IsTimed) return false;
        Elapsed += dt;
        if (Elapsed < Current.DurationSec) return false;
        float over = Elapsed - Current.DurationSec;
        Advance();
        if (Current.IsTimed) Elapsed = MathF.Min(over, Current.DurationSec);
        return true;
    }

    /// <summary>Leaves a Chest/Shop stage.</summary>
    public void Continue()
    {
        if (Current.IsTimed) throw new InvalidOperationException($"stage {Current.Kind} is timed, nothing to continue");
        Advance();
    }

    /// <summary>Dev shortcut: next stage now.</summary>
    public void Skip() => Advance();

    void Advance()
    {
        Elapsed = 0f;
        if (++Index < _stages.Length) return;
        Index = 0;
        Loop++;
    }
}
