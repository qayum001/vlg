namespace Vlg.Core.Config;

/// <summary>Hero skill. Stats at level L = Base + PerLevel * (L - 1) (domain.md: own growth per level).</summary>
public sealed record SkillDef
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required SkillKind Kind { get; init; }
    public required SkillStats Base { get; init; }
    public required SkillStats PerLevel { get; init; }

    /// <param name="level">1-based.</param>
    public SkillStats StatsAt(int level) => SkillStats.AtLevel(Base, PerLevel, level);
}
