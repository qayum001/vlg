namespace Vlg.Core.Config;

/// <summary>Behavior in code, numbers in data. Stat use per kind: <see cref="SkillStats"/>.</summary>
public enum SkillKind
{
    /// <summary>Projectiles at the nearest enemies in range.</summary>
    Projectile,
    /// <summary>Ring expanding from the cast point; hits each enemy once as it passes, pushes outward.</summary>
    Ring,
    /// <summary>Speaker placed on an enemy cluster; pulses damage around itself on every beat.</summary>
    Speaker,
    /// <summary>Damage tick to every enemy around the hero; hit enemies take extra damage for a while.</summary>
    Aura,
    /// <summary>Instant line hit from the hero in the camera direction.</summary>
    Beam,
    /// <summary>Objects circling the hero, damage tick around each one.</summary>
    Orbit,
}
