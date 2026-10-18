using System.Numerics;

namespace Vlg.Core.Sim;

/// <summary>Everything a projectile carries. Defaults: one hit, pool default radius, no extras.</summary>
public struct ProjectileShot
{
    public Vector2 Position;
    public Vector2 Velocity;
    /// <summary>Seconds; for a returning shot, the outbound time.</summary>
    public float Life;
    public float Damage;
    public byte Source;
    /// <summary>Hit radius; 0 = pool default.</summary>
    public float Radius;
    /// <summary>Extra enemies to pass through.</summary>
    public int Pierce;
    /// <summary>Jumps to the nearest unhit enemy after a hit.</summary>
    public int Bounces;
    /// <summary>Damage lost per bounce, fraction.</summary>
    public float Falloff;
    /// <summary>Area hit around each struck enemy; 0 = none.</summary>
    public float ExplodeRadius;
    public float ExplodeFactor;
    /// <summary>Each bounce bursts around the target (half damage, 1.5 m).</summary>
    public bool BurstOnBounce;
    /// <summary>Flies back to the hero after Life, piercing everything, one hit per enemy per direction.</summary>
    public bool Returns;
}
