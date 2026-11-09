namespace Vlg.Core.Config;

/// <summary>What fills an ultimate (domain.md: charge differs per ultimate).</summary>
public enum ChargeKind
{
    /// <summary>Seconds of play.</summary>
    Cooldown,
    Kills,
    /// <summary>Drops of the beat clock.</summary>
    Drops,
}
