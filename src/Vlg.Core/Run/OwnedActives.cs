using System;
using System.Collections.Generic;
using Vlg.Core.Config;

namespace Vlg.Core.Run;

/// <summary>
/// Levels for the active-item SkillSystem: one slot per active item of the pool, level 1 when owned.
/// Version follows the inventory.
/// </summary>
public sealed class OwnedActives : ISkillLevels
{
    readonly Inventory _inv;
    /// <summary>Item index per slot.</summary>
    public readonly int[] Items;
    public readonly SkillDef[] Defs;

    public OwnedActives(Inventory inv)
    {
        _inv = inv;
        var items = new List<int>();
        var all = inv.Config.Items;
        for (int i = 0; i < all.Length; i++)
            if (all[i].Type == ItemType.Active) items.Add(i);
        Items = items.ToArray();
        Defs = Array.ConvertAll(Items, i => all[i].ToSkillDef());
    }

    public int Version => _inv.Version;
    public int SkillLevel(int slot) => _inv.HasActive(Items[slot]) ? 1 : 0;
    public ReadOnlySpan<int> Path(int slot) => ReadOnlySpan<int>.Empty;
    public IReadOnlyList<int> BranchLevels => Array.Empty<int>();
}
