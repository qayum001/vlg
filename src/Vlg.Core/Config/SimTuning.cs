namespace Vlg.Core.Config;

public sealed record SimTuning
{
    public required float MaxStepSec { get; init; }
    public required float EnemySpeed { get; init; }
    public required float EnemyRadius { get; init; }
    public required float EnemyHealth { get; init; }
    public required float SeparationRadius { get; init; }
    public required float SeparationStrength { get; init; }
    public required int MaxNeighbors { get; init; }
    public required float HeadingTurnRate { get; init; }
    public required float AnimRate { get; init; }
    public required float HitFlashDecay { get; init; }
    public required float SpawnRadiusMin { get; init; }
    public required float SpawnRadiusMax { get; init; }
    public required float SpawnAreaMin { get; init; }
    public required float SpawnAreaMax { get; init; }
    public required float GridCellSize { get; init; }
    public required int GridDim { get; init; }
    public required int EnemyCapacity { get; init; }
    public required float PlayerSpeed { get; init; }
    public required float PlayerRadius { get; init; }
    public required float ProjectileSpeed { get; init; }
    public required float ProjectileLife { get; init; }
    public required float ProjectileRadius { get; init; }
    public required float ProjectileDamage { get; init; }
    public required int ProjectileCapacity { get; init; }
    public required float TargetedShare { get; init; }
    public required float AimedShare { get; init; }
    public required float AimSpreadRad { get; init; }
}
