namespace Vlg.Core.Run;

/// <param name="Slot">Loadout index; -1 for Bonus.</param>
/// <param name="NewLevel">Skill level after picking; 0 for Bonus.</param>
public readonly record struct LevelUpOption(LevelUpKind Kind, int Slot, int NewLevel);
