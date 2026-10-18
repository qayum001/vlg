namespace Vlg.Core.Config;

public sealed record RunSimSettings
{
    public required float MaxStepSec { get; init; }
    public required int EnemyCapacity { get; init; }
    public required int ProjectileCapacity { get; init; }
    public required int GemCapacity { get; init; }
    /// <summary>Expanding rings alive at once (Low End, Drop shockwave).</summary>
    public required int RingCapacity { get; init; }
    public required int GridDim { get; init; }
    public required float GridCellSize { get; init; }
    /// <summary>Hit radius of every skill projectile (enemy radius is added).</summary>
    public required float ProjectileRadius { get; init; }
    /// <summary>-1 = processors - 2.</summary>
    public required int WorkerThreads { get; init; }
    public required int ParallelMinCount { get; init; }
}
