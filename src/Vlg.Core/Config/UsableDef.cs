namespace Vlg.Core.Config;

/// <summary>Pressed item (domain.md: effect ~3 s, cooldown 20-60 s from the press). Omitted numbers are 0.</summary>
public sealed record UsableDef
{
    public required UsableKind Kind { get; init; }
    /// <summary>Seconds, from the press.</summary>
    public required float Cooldown { get; init; }
    /// <summary>Effect seconds.</summary>
    public required float Duration { get; init; }
    /// <summary>Move speed bonus (fraction) during the effect.</summary>
    public float MoveBonus { get; init; }
    /// <summary>Recharge speed bonus (fraction) for skills and actives during the effect.</summary>
    public float RateBonus { get; init; }
    /// <summary>After the effect: seconds of move speed penalty (fraction).</summary>
    public float CrashSec { get; init; }
    public float CrashMove { get; init; }
    /// <summary>Throw distance along the camera, m.</summary>
    public float Range { get; init; }
    public float Radius { get; init; }
    /// <summary>Per tick (pool) or on pop (singularity).</summary>
    public float Damage { get; init; }
    public float Interval { get; init; }
    /// <summary>Damage multiplier against undead enemies.</summary>
    public float UndeadFactor { get; init; }
    /// <summary>Pull speed, m/s, toward the point (singularity).</summary>
    public float Pull { get; init; }
    /// <summary>Pop radius at the end (singularity).</summary>
    public float PopRadius { get; init; }
}
