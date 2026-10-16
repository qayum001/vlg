namespace Vlg.Core.Config;

/// <summary>Behavior in code, numbers in data. Stat use per kind: <see cref="SkillStats"/>.</summary>
public enum SkillKind
{
    /// <summary>Projectiles at the nearest enemies in range.</summary>
    Bolt,
    /// <summary>Ring of projectiles in all directions.</summary>
    Nova,
    /// <summary>Damage tick to every enemy around the hero.</summary>
    Aura,
    /// <summary>Blades circling the hero, damage tick around each blade.</summary>
    Orbit,
}
