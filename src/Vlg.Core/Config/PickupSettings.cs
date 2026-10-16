namespace Vlg.Core.Config;

public sealed record PickupSettings
{
    /// <summary>Speed of a gem flying to the hero, m/s.</summary>
    public required float MagnetSpeed { get; init; }
    /// <summary>Gem is collected at this distance from the hero center.</summary>
    public required float CollectRadius { get; init; }
}
