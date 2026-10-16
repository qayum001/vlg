namespace Vlg.Core.Config;

/// <summary>Walkers spawn on a ring around the hero, outside the camera view at any yaw.</summary>
public sealed record SpawnSettings
{
    public required float RingMin { get; init; }
    public required float RingMax { get; init; }
    /// <summary>Walkers farther than this are moved back onto the ring (endless map, hero outruns them).</summary>
    public required float DespawnRadius { get; init; }
}
