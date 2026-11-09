using System;

namespace Vlg.Core.Config;

/// <summary>An active item's skill: kind, stats (seconds, no beat), flags. Plays at level 1, item tags = none.</summary>
public sealed record ActiveDef
{
    public required SkillKind Kind { get; init; }
    public required SkillStats Stats { get; init; }
    public SkillFlag[] Flags { get; init; } = Array.Empty<SkillFlag>();
}
