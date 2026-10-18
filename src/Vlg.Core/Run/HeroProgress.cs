using System;
using System.Collections.Generic;
using Vlg.Core.Config;

namespace Vlg.Core.Run;

/// <summary>
/// Hero level, XP, loadout skill levels and branch choices. Run start: hero level 1, slot 0 at level 1 (free).
/// Each hero level-up is one pending pick: unlock or raise one skill (MOBA style). Raising a skill to a
/// branch level queues a branch pick for it, answered before the next level-up pick. The ultimate is one more
/// level-up option once the hero level reaches its next entry in leveling.ultimateLevels.
/// </summary>
public sealed class HeroProgress : ISkillLevels
{
    readonly LevelingSettings _s;
    readonly SkillDef[]? _defs;
    readonly bool _hasUltimate;
    readonly int[] _skillLevels;
    readonly int[][] _paths;
    readonly List<LevelUpOption> _options = new();
    /// <summary>(slot, branch point) waiting for a pick, in order.</summary>
    readonly Queue<(int Slot, int Point)> _branchPicks = new();

    public int Level { get; private set; } = 1;
    /// <summary>XP gathered toward the next level.</summary>
    public float Xp { get; private set; }
    public int PendingLevelUps { get; private set; }
    public int BonusCount { get; private set; }
    /// <summary>Bumps on every skill level or branch change (SkillSystem re-resolves forms).</summary>
    public int Version { get; private set; }
    /// <summary>0 = locked.</summary>
    public int UltimateLevel { get; private set; }

    /// <param name="defs">Loadout skills for branch picks; null = skills without branches (tests).</param>
    /// <param name="hasUltimate">Offer ultimate unlock/upgrade picks.</param>
    public HeroProgress(LevelingSettings settings, int skillCount, SkillDef[]? defs = null, bool hasUltimate = false)
    {
        _s = settings;
        _defs = defs;
        _hasUltimate = hasUltimate;
        _skillLevels = new int[skillCount];
        _skillLevels[0] = 1;
        _paths = new int[skillCount][];
        for (int i = 0; i < skillCount; i++)
        {
            _paths[i] = new int[settings.BranchLevels.Length];
            Array.Fill(_paths[i], -1);
        }
    }

    public int SkillLevel(int slot) => _skillLevels[slot];
    /// <summary>Chosen option per branch point, -1 = none yet.</summary>
    public ReadOnlySpan<int> Path(int slot) => _paths[slot];
    public int SkillCap => LevelRule.SkillCap(Level, _s.LevelDivisor, _s.MaxSkillLevel);
    public float XpToNext => XpForLevel(Level);
    public float DamageMultiplier => 1f + BonusCount * _s.MaxedBonusDamage;
    public IReadOnlyList<int> BranchLevels => _s.BranchLevels;
    /// <summary>A level-up or branch pick waits for the player.</summary>
    public bool HasPendingPick => PendingLevelUps > 0 || _branchPicks.Count > 0;
    /// <summary>Level-up picks plus queued branch picks (panel refresh key).</summary>
    public int PendingPicks => PendingLevelUps + _branchPicks.Count;
    public bool PendingBranch => _branchPicks.Count > 0;

    /// <summary>XP needed to go from <paramref name="level"/> to level + 1.</summary>
    public float XpForLevel(int level) => _s.XpBase + _s.XpPerLevel * (level - 1);

    public void AddXp(float xp)
    {
        Xp += xp;
        while (Xp >= XpToNext)
        {
            Xp -= XpToNext;
            Level++;
            PendingLevelUps++;
        }
    }

    /// <summary>Dev shortcut: one level, XP toward the next level unchanged.</summary>
    public void DevGainLevel()
    {
        Level++;
        PendingLevelUps++;
    }

    /// <summary>The ultimate can be unlocked or raised at the current hero level.</summary>
    public bool UltimateOpen => _hasUltimate && UltimateLevel < _s.UltimateLevels.Length && Level >= _s.UltimateLevels[UltimateLevel];

    /// <summary>Dev shortcut: ultimate at <paramref name="level"/>, hero level ignored.</summary>
    public void DevSetUltimate(int level)
    {
        if (!_hasUltimate || level < 0 || level > _s.UltimateLevels.Length)
            throw new ArgumentOutOfRangeException(nameof(level), $"0..{_s.UltimateLevels.Length} with an ultimate, got {level}");
        UltimateLevel = level;
    }

    /// <summary>Dev shortcut: every skill at <paramref name="level"/>, cap ignored; queues the branch picks it passed.</summary>
    public void DevSetAllSkills(int level)
    {
        if (level < 1 || level > _s.MaxSkillLevel)
            throw new ArgumentOutOfRangeException(nameof(level), $"1..{_s.MaxSkillLevel}, got {level}");
        for (int slot = 0; slot < _skillLevels.Length; slot++)
        {
            for (int p = 0; p < _s.BranchLevels.Length; p++)
                if (_skillLevels[slot] < _s.BranchLevels[p] && level >= _s.BranchLevels[p]) QueueBranch(slot, p);
            _skillLevels[slot] = level;
        }
        Prune();
        Version++;
    }

    /// <summary>
    /// Picks for the current pending pick. A queued branch pick comes first (Branch options of one skill);
    /// else level-up options in slot order. Cap uses the current hero level, so after a multi-level jump every
    /// pending pick sees the final level. Empty when nothing is pending. The list is reused: read it before the next call.
    /// </summary>
    public IReadOnlyList<LevelUpOption> Options()
    {
        _options.Clear();
        if (_branchPicks.Count > 0)
        {
            var (slot, point) = _branchPicks.Peek();
            int count = BranchOptions(slot, point).Count;
            for (int k = 0; k < count; k++) _options.Add(new LevelUpOption(LevelUpKind.Branch, slot, _skillLevels[slot], k));
            return _options;
        }
        if (PendingLevelUps == 0) return _options;
        int cap = SkillCap;
        for (int slot = 0; slot < _skillLevels.Length; slot++)
        {
            int level = _skillLevels[slot];
            if (level == 0) _options.Add(new LevelUpOption(LevelUpKind.Unlock, slot, 1));
            else if (level < cap) _options.Add(new LevelUpOption(LevelUpKind.Upgrade, slot, level + 1));
        }
        if (UltimateOpen) _options.Add(new LevelUpOption(LevelUpKind.Ultimate, -1, UltimateLevel + 1));
        if (_options.Count == 0) _options.Add(new LevelUpOption(LevelUpKind.Bonus, -1, 0));
        return _options;
    }

    /// <summary>The branch point of the pending branch pick; -1 if none.</summary>
    public int PendingBranchPoint => _branchPicks.Count > 0 ? _branchPicks.Peek().Point : -1;

    /// <summary>Options at <paramref name="point"/> for <paramref name="slot"/> given its earlier choices.</summary>
    public IReadOnlyList<BranchDef> BranchOptions(int slot, int point) =>
        _defs == null ? Array.Empty<BranchDef>() : _defs[slot].OptionsAt(_paths[slot], point);

    public void Choose(int index)
    {
        var options = Options();
        if ((uint)index >= (uint)options.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"{options.Count} options, got {index}");
        var o = options[index];
        switch (o.Kind)
        {
            case LevelUpKind.Branch:
                var (slot, point) = _branchPicks.Dequeue();
                _paths[slot][point] = o.Choice;
                break;
            case LevelUpKind.Bonus:
                BonusCount++;
                PendingLevelUps--;
                break;
            case LevelUpKind.Ultimate:
                UltimateLevel = o.NewLevel;
                PendingLevelUps--;
                break;
            default:
                int before = _skillLevels[o.Slot];
                _skillLevels[o.Slot] = o.NewLevel;
                for (int p = 0; p < _s.BranchLevels.Length; p++)
                    if (before < _s.BranchLevels[p] && o.NewLevel >= _s.BranchLevels[p]) QueueBranch(o.Slot, p);
                PendingLevelUps--;
                break;
        }
        Prune();
        Version++;
    }

    void QueueBranch(int slot, int point)
    {
        if (_defs != null) _branchPicks.Enqueue((slot, point));
    }

    /// <summary>Drops queued picks with no options (skill without branches there, or an earlier choice ended the tree).</summary>
    void Prune()
    {
        while (_branchPicks.Count > 0)
        {
            var (slot, point) = _branchPicks.Peek();
            if (BranchOptions(slot, point).Count > 0) return;
            _branchPicks.Dequeue();
        }
    }
}
