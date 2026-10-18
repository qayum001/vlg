using System;
using System.Collections.Generic;
using Vlg.Core.Config;

namespace Vlg.Core.Run;

/// <summary>
/// Hero level, XP and loadout skill levels. Run start: hero level 1, slot 0 at level 1 (free).
/// Each hero level-up is one pending pick: unlock or raise one skill (MOBA style).
/// </summary>
public sealed class HeroProgress
{
    readonly LevelingSettings _s;
    readonly int[] _skillLevels;
    readonly List<LevelUpOption> _options = new();

    public int Level { get; private set; } = 1;
    /// <summary>XP gathered toward the next level.</summary>
    public float Xp { get; private set; }
    public int PendingLevelUps { get; private set; }
    public int BonusCount { get; private set; }

    public HeroProgress(LevelingSettings settings, int skillCount)
    {
        _s = settings;
        _skillLevels = new int[skillCount];
        _skillLevels[0] = 1;
    }

    public int SkillLevel(int slot) => _skillLevels[slot];
    public int SkillCap => LevelRule.SkillCap(Level, _s.LevelDivisor, _s.MaxSkillLevel);
    public float XpToNext => XpForLevel(Level);
    public float DamageMultiplier => 1f + BonusCount * _s.MaxedBonusDamage;

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

    /// <summary>Dev shortcut: every skill at <paramref name="level"/>, cap ignored.</summary>
    public void DevSetAllSkills(int level)
    {
        if (level < 1 || level > _s.MaxSkillLevel)
            throw new ArgumentOutOfRangeException(nameof(level), $"1..{_s.MaxSkillLevel}, got {level}");
        Array.Fill(_skillLevels, level);
    }

    /// <summary>
    /// Picks for the current pending level-up, in slot order. Cap uses the current hero level, so after
    /// a multi-level jump every pending pick sees the final level. Empty when nothing is pending.
    /// The list is reused: read it before the next call.
    /// </summary>
    public IReadOnlyList<LevelUpOption> Options()
    {
        _options.Clear();
        if (PendingLevelUps == 0) return _options;
        int cap = SkillCap;
        for (int slot = 0; slot < _skillLevels.Length; slot++)
        {
            int level = _skillLevels[slot];
            if (level == 0) _options.Add(new LevelUpOption(LevelUpKind.Unlock, slot, 1));
            else if (level < cap) _options.Add(new LevelUpOption(LevelUpKind.Upgrade, slot, level + 1));
        }
        if (_options.Count == 0) _options.Add(new LevelUpOption(LevelUpKind.Bonus, -1, 0));
        return _options;
    }

    public void Choose(int index)
    {
        var options = Options();
        if ((uint)index >= (uint)options.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"{options.Count} options, got {index}");
        var o = options[index];
        if (o.Kind == LevelUpKind.Bonus) BonusCount++;
        else _skillLevels[o.Slot] = o.NewLevel;
        PendingLevelUps--;
    }
}
