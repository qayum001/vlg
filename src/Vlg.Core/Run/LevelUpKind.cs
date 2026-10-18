namespace Vlg.Core.Run;

public enum LevelUpKind
{
    /// <summary>Locked loadout skill → level 1.</summary>
    Unlock,
    /// <summary>Skill → level + 1, within the cap.</summary>
    Upgrade,
    /// <summary>Only when no skill can be unlocked or raised: +damage (domain.md "after every skill is maxed").</summary>
    Bonus,
    /// <summary>Skill reached a branch level: pick its next form (domain.md Skill branching).</summary>
    Branch,
    /// <summary>Unlock or raise the ultimate (Slot -1, NewLevel = ultimate level).</summary>
    Ultimate,
}
