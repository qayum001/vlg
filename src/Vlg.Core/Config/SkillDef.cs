using System;
using System.Collections.Generic;
using System.Text;

namespace Vlg.Core.Config;

/// <summary>
/// Hero skill. Stats at level L = Base + PerLevel * (L - 1) (domain.md: own growth per level), then each
/// chosen branch (from level BranchLevels[i] on) applies in order.
/// </summary>
public sealed record SkillDef
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required SkillKind Kind { get; init; }
    public required SkillTag[] Tags { get; init; }
    public required SkillStats Base { get; init; }
    public required SkillStats PerLevel { get; init; }
    /// <summary>Options at the first branch point; each holds the next point's options.</summary>
    public BranchDef[] Branches { get; init; } = Array.Empty<BranchDef>();

    /// <param name="level">1-based.</param>
    public SkillStats StatsAt(int level) => SkillStats.AtLevel(Base, PerLevel, level);

    /// <summary>Options at branch point <paramref name="point"/> after the earlier choices in <paramref name="path"/> (-1 = none).</summary>
    public IReadOnlyList<BranchDef> OptionsAt(ReadOnlySpan<int> path, int point)
    {
        var options = Branches;
        for (int p = 0; p < point; p++)
        {
            if (path[p] < 0) return Array.Empty<BranchDef>();
            options = options[path[p]].Branches;
        }
        return options;
    }

    /// <param name="path">Chosen option per branch point, -1 = not chosen. Branch i applies when level >= branchLevels[i].</param>
    public SkillForm Resolve(int level, ReadOnlySpan<int> path, IReadOnlyList<int> branchLevels)
    {
        var kind = Kind;
        var tags = Tags;
        ulong flags = 0;
        var stats = StatsAt(level);
        var name = new StringBuilder(Name);
        var options = Branches;
        for (int p = 0; p < path.Length && p < branchLevels.Count; p++)
        {
            if (path[p] < 0 || level < branchLevels[p]) break;
            var b = options[path[p]];
            stats = stats.Apply(b.Scale, b.Add);
            if (b.Kind is { } k) kind = k;
            if (b.Tags != null) tags = b.Tags;
            foreach (var f in b.Flags) flags |= SkillForm.Bit(f);
            name.Append(" · ").Append(b.Name);
            options = b.Branches;
        }
        return new SkillForm { Kind = kind, Tags = tags, Flags = flags, Stats = stats, Name = name.ToString() };
    }
}
