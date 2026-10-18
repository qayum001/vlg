namespace Vlg.Core.Config;

/// <summary>Global stats; copies add up. Omitted numbers are 0.</summary>
public sealed record PassiveDef
{
    /// <summary>+fraction damage (skills, ultimate, actives).</summary>
    public float Damage { get; init; }
    public float MoveSpeed { get; init; }
    /// <summary>HP per second.</summary>
    public float Regen { get; init; }
    /// <summary>+fraction pickup radius.</summary>
    public float Pickup { get; init; }
    /// <summary>Fraction of damage dealt healed.</summary>
    public float LifeSteal { get; init; }
    /// <summary>Damage taken x (1 - armor), copies multiply.</summary>
    public float Armor { get; init; }
    /// <summary>+fraction credits from pickups.</summary>
    public float Credits { get; init; }
    /// <summary>Cooldown cut (fraction) for skills and actives.</summary>
    public float Cooldown { get; init; }
    /// <summary>Every LedgerKills kills this run: +LedgerDamage damage.</summary>
    public int LedgerKills { get; init; }
    public float LedgerDamage { get; init; }
}
