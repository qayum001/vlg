using System;
using System.Text.Json.Serialization;

namespace Vlg.Core.Config;

/// <summary>
/// Skill numbers at one level. Omitted keys are 0. Time unit (cooldown, duration): beats for a hero
/// with a beat clock, seconds otherwise. Use per kind:
/// Projectile: damage, cooldown, count (projectiles), speed (m/s), range (travel distance).
/// Ring: damage, cooldown, radius (max), speed (expansion m/s), knockback (push m/s, below speed).
/// Speaker: damage (per pulse), cooldown (between placements), count (max alive), duration, radius (pulse), range (placement).
/// Aura: damage, cooldown (tick), radius, vuln (+damage taken while hit, fraction).
/// Beam: damage, cooldown, range (length), width.
/// Orbit: damage, cooldown (tick), count (objects), speed (rad/s), range (orbit distance), radius (object hit radius).
/// </summary>
public readonly record struct SkillStats
{
    public float Damage { get; init; }
    public float Cooldown { get; init; }
    public float Count { get; init; }
    public float Speed { get; init; }
    public float Range { get; init; }
    public float Radius { get; init; }
    public float Duration { get; init; }
    public float Width { get; init; }
    public float Knockback { get; init; }
    public float Vuln { get; init; }

    /// <summary>Count floored; fractional growth (0.5 per level) adds one every other level.</summary>
    [JsonIgnore]
    public int Amount => (int)MathF.Floor(Count + 1e-4f);

    /// <summary>Linear growth: level 1 = base.</summary>
    public static SkillStats AtLevel(SkillStats b, SkillStats perLevel, int level)
    {
        float k = level - 1;
        return new SkillStats
        {
            Damage = b.Damage + perLevel.Damage * k,
            Cooldown = b.Cooldown + perLevel.Cooldown * k,
            Count = b.Count + perLevel.Count * k,
            Speed = b.Speed + perLevel.Speed * k,
            Range = b.Range + perLevel.Range * k,
            Radius = b.Radius + perLevel.Radius * k,
            Duration = b.Duration + perLevel.Duration * k,
            Width = b.Width + perLevel.Width * k,
            Knockback = b.Knockback + perLevel.Knockback * k,
            Vuln = b.Vuln + perLevel.Vuln * k,
        };
    }
}
