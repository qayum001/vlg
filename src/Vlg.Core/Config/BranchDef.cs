using System;
using System.Text.Json.Serialization;

namespace Vlg.Core.Config;

/// <summary>
/// One option at a skill branch point (domain.md Skill branching). Applied on top of the stats so far:
/// stats * Scale + Add; Kind/Tags replace when set; Flags add up. Branches = options at the next branch point.
/// </summary>
public sealed record BranchDef
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    /// <summary>Player-facing one-liner.</summary>
    public required string Text { get; init; }
    public SkillKind? Kind { get; init; }
    public SkillTag[]? Tags { get; init; }
    [JsonConverter(typeof(StatScaleJson))]
    public SkillStats Scale { get; init; } = SkillStats.Filled(1f);
    public SkillStats Add { get; init; }
    public SkillFlag[] Flags { get; init; } = Array.Empty<SkillFlag>();
    public BranchDef[] Branches { get; init; } = Array.Empty<BranchDef>();
}
