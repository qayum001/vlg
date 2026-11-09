namespace Vlg.Core.Config;

/// <summary>Branch behaviors that numbers can't express (design/heroes/lowkey.md). Meaning depends on the skill kind.</summary>
public enum SkillFlag
{
    /// <summary>Projectile: each shot gets a faster twin with half damage.</summary>
    Twin,
    /// <summary>Projectile: every bounce bursts around the target (half damage, 1.5 m).</summary>
    BurstOnBounce,
    /// <summary>BellDrop: cooldown is 1 beat during the Drop.</summary>
    DropEveryBeat,
    /// <summary>Ring: damage grows with distance from the center (x0.5 at the center to x1.5 at the edge).</summary>
    DistanceDamage,
    /// <summary>Ring: after reaching max radius it collapses back, hitting again and pulling inward.</summary>
    Collapse,
    /// <summary>Aura: puddles left behind while moving (duration, slow; zap with Zap).</summary>
    Trail,
    /// <summary>Quake: every Period-th cast (every cast in the Drop) erupts for x4 damage.</summary>
    Aftershock,
    /// <summary>Speaker: each cast places 3 speakers in a ring around the hero.</summary>
    RingPlacement,
    /// <summary>Speaker: every Period-th pulse deals x3 damage with x1.5 radius.</summary>
    BigPulse,
    /// <summary>Speaker: pulses also hit along lines between the slot's speakers.</summary>
    Link,
    /// <summary>Speaker: pulse damage +10% per enemy inside, up to x3.</summary>
    CrowdScaling,
    /// <summary>Speaker: enemies within 1 m of it on a pulse die.</summary>
    Crush,
    /// <summary>Speaker: explodes when it expires (x2 radius, x6 damage).</summary>
    Blowout,
    /// <summary>Drone / projectile: shots explode on hit (blast stat, 60% damage).</summary>
    ExplodeOnHit,
    /// <summary>Aura: enemies dying inside it explode (1.5 m, x2 tick damage).</summary>
    DeathBurst,
    /// <summary>Aura: damage and radius x2 during the Drop.</summary>
    DropDouble,
    /// <summary>Aura trail: each puddle zaps one enemy within 3 m on every beat (x1.5 damage).</summary>
    Zap,
    /// <summary>Shield: while charged, all hero skills deal +15% damage.</summary>
    Sidechain,
    /// <summary>Beam: the far end explodes (2 m, full damage).</summary>
    EndBurst,
    /// <summary>Beam: damage on an enemy ramps while it stays hit (heat, up to x3).</summary>
    Ramp,
    /// <summary>Beam: splits into 3 short beams at its end.</summary>
    Split,
    /// <summary>Orbit: a second, counter-rotating ring 1.5 m farther out.</summary>
    DoubleDeck,
    /// <summary>Orbit: every Period beats reverses with a burst that pulls enemies onto the ring.</summary>
    Scratch,
    /// <summary>Orbit: during the Drop the ring swings out to 12 m and back each bar.</summary>
    Spinback,
}
