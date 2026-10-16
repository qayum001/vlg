using System;
using System.Text.Json.Serialization;

namespace Vlg.Core.Config;

/// <summary>
/// Skill numbers at one level. Omitted keys are 0. Use per kind:
/// Bolt/Nova: damage, cooldown, count (projectiles), speed (m/s), range (travel distance).
/// Aura: damage, cooldown (tick), radius.
/// Orbit: damage, cooldown (tick), count (blades), speed (rad/s), range (orbit distance), radius (blade hit radius).
/// </summary>
public readonly record struct SkillStats
{
    public float Damage { get; init; }
    public float Cooldown { get; init; }
    public float Count { get; init; }
    public float Speed { get; init; }
    public float Range { get; init; }
    public float Radius { get; init; }

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
        };
    }
}
