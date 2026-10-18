using System;

namespace Vlg.Core.Config;

/// <summary>
/// Pressed hero ultimate (domain.md Build: unlocks at hero level 10, upgrades at 20 and 30, no branches).
/// Levels[i] = stats at ultimate level i + 1; Levels[i].Charge = charge needed (seconds, kills or Drops by
/// <see cref="ChargeKind"/>). Stat use per kind: <see cref="UltimateKind"/>.
/// </summary>
public sealed record UltimateDef
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    /// <summary>Player-facing one-liner.</summary>
    public required string Text { get; init; }
    public required UltimateKind Kind { get; init; }
    public required ChargeKind Charge { get; init; }
    public required SkillStats[] Levels { get; init; }

    public SkillStats At(int level) => Levels[Math.Clamp(level, 1, Levels.Length) - 1];
}
