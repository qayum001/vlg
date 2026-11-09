namespace Vlg.Core.Run;

/// <param name="Slot">Loadout index; -1 for Bonus.</param>
/// <param name="NewLevel">Skill level after picking; 0 for Bonus; current level for Branch.</param>
/// <param name="Choice">Branch option index (Branch only), else -1.</param>
public readonly record struct LevelUpOption(LevelUpKind Kind, int Slot, int NewLevel, int Choice = -1);
