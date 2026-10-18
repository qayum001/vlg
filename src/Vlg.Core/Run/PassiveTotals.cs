namespace Vlg.Core.Run;

/// <summary>Sum of owned passive items (copies add; armor and cooldown cuts multiply).</summary>
public readonly record struct PassiveTotals
{
    public float Damage { get; init; }
    public float MoveSpeed { get; init; }
    public float Regen { get; init; }
    public float Pickup { get; init; }
    public float LifeSteal { get; init; }
    /// <summary>Damage taken multiplier, product of (1 - armor). 1 = none.</summary>
    public float DamageTaken { get; init; }
    public float Credits { get; init; }
    /// <summary>Cooldown multiplier, product of (1 - cut). 1 = none.</summary>
    public float CooldownMult { get; init; }

    public static PassiveTotals None => new() { DamageTaken = 1f, CooldownMult = 1f };
}
