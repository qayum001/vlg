using System;
using System.Collections.Generic;

namespace Vlg.Core.Run;

/// <summary>Per-slot levels and branch choices a SkillSystem plays (hero progress, owned item actives).</summary>
public interface ISkillLevels
{
    /// <summary>Changes whenever a level or choice changes.</summary>
    int Version { get; }
    /// <summary>0 = locked / not owned.</summary>
    int SkillLevel(int slot);
    ReadOnlySpan<int> Path(int slot);
    IReadOnlyList<int> BranchLevels { get; }
}
