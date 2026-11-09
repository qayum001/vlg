using System.Numerics;

namespace Vlg.Core.Run;

/// <summary>Everything a ground zone carries (<see cref="ZonePool"/>).</summary>
public struct ZoneShot
{
    public Vector2 Center;
    public float Radius;
    public float LifeSec;
    /// <summary>Per tick.</summary>
    public float Damage;
    public float Slow;
    public float Vuln;
    /// <summary>Tick on beat ticks (BeatSec = status length) instead of every Interval seconds.</summary>
    public bool OnBeat;
    public float BeatSec;
    public float Interval;
    public bool Zap;
    public byte Source;
    /// <summary>Owner skill slot (rendering color); -1 = none.</summary>
    public int Slot;
}
