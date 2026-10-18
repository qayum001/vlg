namespace Vlg.Core.Run;

public enum UltimatePhase
{
    /// <summary>Charging or ready.</summary>
    Idle,
    /// <summary>Encore running: Drop forced, skills faster.</summary>
    Encore,
    /// <summary>Bass Cannon charging up.</summary>
    Windup,
    /// <summary>Bass Cannon beam on.</summary>
    Firing,
}
