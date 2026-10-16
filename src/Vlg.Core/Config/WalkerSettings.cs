namespace Vlg.Core.Config;

/// <summary>Basic mob (domain.md walker). HP and damage scale per loop via <see cref="LoopScaling"/>.</summary>
public sealed record WalkerSettings
{
    public required float Hp { get; init; }
    public required float Speed { get; init; }
    public required float Radius { get; init; }
    /// <summary>Damage per second each touching walker deals to the hero.</summary>
    public required float ContactDps { get; init; }
    /// <summary>XP of the gem dropped on death.</summary>
    public required float Xp { get; init; }
    public required float SeparationRadius { get; init; }
    public required float SeparationStrength { get; init; }
    public required int MaxNeighbors { get; init; }
    public required float HeadingTurnRate { get; init; }
    public required float AnimRate { get; init; }
    public required float HitFlashDecay { get; init; }

    public SwarmTuning ToSwarm(float heroRadius) => new()
    {
        EnemySpeed = Speed,
        EnemyRadius = Radius,
        PlayerRadius = heroRadius,
        SeparationRadius = SeparationRadius,
        SeparationStrength = SeparationStrength,
        MaxNeighbors = MaxNeighbors,
        HeadingTurnRate = HeadingTurnRate,
        AnimRate = AnimRate,
        HitFlashDecay = HitFlashDecay,
    };
}
