namespace Vlg.Core.Config;

/// <summary>Skill stat names (JSON keys, camelCase). Meaning per kind: <see cref="SkillStats"/>.</summary>
public enum Stat
{
    Damage,
    Cooldown,
    Count,
    Speed,
    Range,
    Radius,
    Duration,
    Width,
    Knockback,
    Vuln,
    /// <summary>Extra enemies a projectile passes through.</summary>
    Pierce,
    /// <summary>Jumps to another enemy after a hit.</summary>
    Bounces,
    /// <summary>Damage lost per bounce, fraction.</summary>
    Falloff,
    /// <summary>Speed fraction removed / seconds.</summary>
    Slow,
    SlowSec,
    /// <summary>Stun seconds.</summary>
    Stun,
    /// <summary>Pull speed toward a center, m/s.</summary>
    Pull,
    /// <summary>Burn damage per second / seconds.</summary>
    Burn,
    BurnSec,
    /// <summary>Every N beats or casts, per flag (scratch, aftershock, big pulse).</summary>
    Period,
    /// <summary>Sweep arc, degrees.</summary>
    Arc,
    /// <summary>Damage per beat of a lingering zone (ultimates).</summary>
    Tick,
    /// <summary>HP per kill (ultimates).</summary>
    Heal,
    /// <summary>Ultimate charge needed: seconds, kills or Drops.</summary>
    Charge,
    /// <summary>Explosion radius of shots with ExplodeOnHit, m.</summary>
    Blast,
}
