namespace Vlg.Core.Run;

/// <summary>Everything but Playing pauses <see cref="RunSim.Step"/>. Priority: Dead, LevelUp, Chest/Shop.</summary>
public enum RunPhase
{
    Playing,
    LevelUp,
    Chest,
    Shop,
    Dead,
}
