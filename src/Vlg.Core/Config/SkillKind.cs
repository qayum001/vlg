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
    /// <summary>Delayed area impacts on enemy clusters (falling bells).</summary>
    BellDrop,
    /// <summary>Lasting ground zone around the hero that hurts on every beat.</summary>
    Quake,
    /// <summary>Flying objects that follow the hero and shoot the nearest enemy.</summary>
    Drone,
    /// <summary>Recharging block of contact damage; a block blasts enemies around the hero.</summary>
    Shield,
    /// <summary>Beam that sweeps an arc in front of the hero (move direction) over its duration.</summary>
    Sweep,
    /// <summary>Projectiles that fly out to range and return to the hero, hitting both ways.</summary>
    Boomerang,
}
