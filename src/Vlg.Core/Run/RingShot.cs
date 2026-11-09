using System.Numerics;

namespace Vlg.Core.Run;

/// <summary>Everything a ring carries (<see cref="RingPool"/>).</summary>
public struct RingShot
{
    public Vector2 Center;
    public float MaxRadius;
    /// <summary>Expansion, m/s.</summary>
    public float Speed;
    public float Damage;
    /// <summary>Push given to hit enemies, m/s (outward; inward while collapsing). Must stay below Speed.</summary>
    public float Knockback;
    public float Slow;
    public float SlowSec;
    public float Stun;
    /// <summary>Damage x0.5 at the center to x1.5 at max radius.</summary>
    public bool DistanceDamage;
    /// <summary>Turns at max radius and sweeps back to the center.</summary>
    public bool Collapse;
    public byte Source;
}
