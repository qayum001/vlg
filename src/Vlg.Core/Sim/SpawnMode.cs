namespace Vlg.Core.Sim;

public enum SpawnMode
{
    /// <summary>Ring beyond the camera view (gameplay, concept.md).</summary>
    Ring,
    /// <summary>Annulus around the player, on screen (benchmark: counts are visible enemies).</summary>
    Area,
}
