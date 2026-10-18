namespace Vlg.Core.Config;

/// <summary>Hero level-up and MOBA skill-level rule (domain.md Build).</summary>
public sealed record LevelingSettings
{
    /// <summary>XP from level 1 to 2. Level L to L+1 needs XpBase + XpPerLevel * (L - 1).</summary>
    public required float XpBase { get; init; }
    public required float XpPerLevel { get; init; }
    /// <summary>Skill level cap = ceil(hero level / LevelDivisor).</summary>
    public required int LevelDivisor { get; init; }
    public required int MaxSkillLevel { get; init; }
    /// <summary>Damage bonus per level-up when no skill can be unlocked or raised.</summary>
    public required float MaxedBonusDamage { get; init; }
    /// <summary>Skill levels that open a branch pick, ascending (domain.md: 5 and 10).</summary>
    public required int[] BranchLevels { get; init; }
}
