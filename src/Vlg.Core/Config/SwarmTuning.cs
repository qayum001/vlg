namespace Vlg.Core.Config;

/// <summary>What <see cref="Sim.EnemySwarm"/> needs per update. Built from stress or run config.</summary>
public sealed record SwarmTuning
{
    public required float EnemySpeed { get; init; }
    public required float EnemyRadius { get; init; }
    /// <summary>Enemies stop at PlayerRadius + EnemyRadius from the target.</summary>
    public required float PlayerRadius { get; init; }
    public required float SeparationRadius { get; init; }
    public required float SeparationStrength { get; init; }
    public required int MaxNeighbors { get; init; }
    public required float HeadingTurnRate { get; init; }
    public required float AnimRate { get; init; }
    public required float HitFlashDecay { get; init; }
}
