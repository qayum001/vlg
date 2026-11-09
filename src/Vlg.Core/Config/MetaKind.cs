namespace Vlg.Core.Config;

/// <summary>What a meta upgrade rank adds (domain.md Meta-progression: slot unlocks, small capped stat upgrades).</summary>
public enum MetaKind
{
    /// <summary>+1 active item slot per rank (up to 5 total).</summary>
    ActiveSlots,
    UsableSlots,
    /// <summary>+perRank damage fraction.</summary>
    Damage,
    /// <summary>+perRank max HP fraction.</summary>
    MaxHp,
    /// <summary>+perRank pickup radius fraction.</summary>
    Pickup,
    /// <summary>+perRank credits at run start.</summary>
    StartCredits,
}
