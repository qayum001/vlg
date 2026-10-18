namespace Vlg.Core.Config;

/// <summary>
/// Hero beat clock (design/heroes/lowkey.md, The Drop). One cycle = cycleBars bars of 4 beats; its last
/// buildBars + dropBars bars are build-up then Drop. The Drop scales hero skill damage and area and opens
/// with a shockwave ring around the hero.
/// </summary>
public sealed record BeatSettings
{
    public required float Bpm { get; init; }
    public required int CycleBars { get; init; }
    public required int BuildBars { get; init; }
    public required int DropBars { get; init; }
    /// <summary>+fraction of hero skill damage during the Drop.</summary>
    public required float DropDamage { get; init; }
    /// <summary>+fraction of hero skill radius and width during the Drop.</summary>
    public required float DropArea { get; init; }
    public required float ShockDamage { get; init; }
    /// <summary>Max ring radius, m.</summary>
    public required float ShockRadius { get; init; }
    /// <summary>Ring expansion, m/s.</summary>
    public required float ShockSpeed { get; init; }
    /// <summary>Push speed given to hit enemies, m/s (must stay below shockSpeed).</summary>
    public required float ShockKnockback { get; init; }
}
