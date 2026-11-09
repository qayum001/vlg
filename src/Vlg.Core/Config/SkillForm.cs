namespace Vlg.Core.Config;

/// <summary>A skill as it plays at one level after its branch choices: what SkillSystem reads.</summary>
public sealed class SkillForm
{
    public required SkillKind Kind { get; init; }
    public required SkillTag[] Tags { get; init; }
    public required ulong Flags { get; init; }
    public required SkillStats Stats { get; init; }
    /// <summary>Skill name plus chosen branch names, e.g. "Cowbell · Ricochet".</summary>
    public required string Name { get; init; }

    public bool Has(SkillFlag f) => (Flags & Bit(f)) != 0;

    public static ulong Bit(SkillFlag f) => 1UL << (int)f;
}
