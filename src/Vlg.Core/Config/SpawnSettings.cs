namespace Vlg.Core.Config;

/// <summary>
/// Walkers spawn just outside the camera view (concept.md: spawning depends on camera rotation):
/// random direction, distance = view edge + ViewMargin + [0, Band).
/// </summary>
public sealed record SpawnSettings
{
    /// <summary>Beyond the ground footprint edge; covers model height poking into view and camera follow lag.</summary>
    public required float ViewMargin { get; init; }
    public required float Band { get; init; }
    /// <summary>Walkers farther than this are moved back to a spawn point (endless map, hero outruns them). Must exceed the farthest visible point.</summary>
    public required float DespawnRadius { get; init; }
}
