namespace Vlg.Core.Config;

public sealed record BenchSettings
{
    public required float WarmupSec { get; init; }
    public required float MeasureSec { get; init; }
    public required float ThresholdMs { get; init; }
    public required int FailStepsToStop { get; init; }
    public required float ProjectilesPerEnemy { get; init; }
    /// <summary>Enemy count held constant during overdraw and particle ramps.</summary>
    public required int FixedEnemies { get; init; }
    /// <summary>Value = enemy count.</summary>
    public required RampSettings Enemies { get; init; }
    /// <summary>Value = full-screen additive layers.</summary>
    public required RampSettings Overdraw { get; init; }
    /// <summary>Value = live aura particles.</summary>
    public required RampSettings Particles { get; init; }
}
