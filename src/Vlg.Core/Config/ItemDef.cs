using System;

namespace Vlg.Core.Config;

/// <summary>One item of the shared pool (design/items.md). Exactly the part matching <see cref="Type"/> is set.</summary>
public sealed record ItemDef
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required ItemType Type { get; init; }
    public required Rarity Rarity { get; init; }
    /// <summary>Effect one-liner.</summary>
    public required string Text { get; init; }
    /// <summary>Lore one-liner.</summary>
    public required string Flavor { get; init; }
    public ModifierDef? Modifier { get; init; }
    public ActiveDef? Active { get; init; }
    public UsableDef? Usable { get; init; }
    public PassiveDef? Passive { get; init; }

    /// <summary>Active item as a level-1 skill for SkillSystem (no tags: modifiers never go into actives).</summary>
    public SkillDef ToSkillDef() => new()
    {
        Id = Id,
        Name = Name,
        Kind = (Active ?? throw new InvalidOperationException($"item '{Id}' is not active")).Kind,
        Tags = Array.Empty<SkillTag>(),
        Base = Active.Stats,
        PerLevel = default,
        Flags = Active.Flags,
    };
}
