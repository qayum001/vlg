using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Vlg.Core.Config;

/// <summary>
/// One hero (game/data/heroes/&lt;id&gt;.json): stats, beat clock passive, skill pool, default loadout
/// (domain.md Heroes). Strict like <see cref="RunConfig"/>; validated against the run config it plays in.
/// </summary>
public sealed record HeroDef
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required float MaxHp { get; init; }
    public required float Speed { get; init; }
    /// <summary>Gems inside this distance start flying to the hero.</summary>
    public required float PickupRadius { get; init; }
    public required BeatSettings Beat { get; init; }
    /// <summary>Skill pool (domain.md: 6 per hero).</summary>
    public required SkillDef[] Skills { get; init; }
    /// <summary>Skill ids in loadout order; slot 0 starts at level 1.</summary>
    public required string[] Loadout { get; init; }

    public static HeroDef Parse(string json, RunConfig run)
    {
        var hero = JsonSerializer.Deserialize<HeroDef>(json, ConfigJson.Options)
            ?? throw new FormatException("hero is null");
        hero.Validate(run);
        return hero;
    }

    /// <summary>Loadout skills in order. <paramref name="ids"/> null = the default loadout.</summary>
    public SkillDef[] LoadoutSkills(IReadOnlyList<string>? ids = null)
    {
        ids ??= Loadout;
        Require(ids.Count is > 0 and <= RunConfig.MaxSkills, $"loadout: 1..{RunConfig.MaxSkills} skills needed");
        var result = new SkillDef[ids.Count];
        var seen = new HashSet<string>();
        for (int i = 0; i < ids.Count; i++)
        {
            Require(seen.Add(ids[i]), $"loadout: duplicate skill '{ids[i]}'");
            result[i] = Array.Find(Skills, s => s.Id == ids[i])
                ?? throw new FormatException($"hero '{Id}': loadout skill '{ids[i]}' not in the pool ({string.Join(", ", Array.ConvertAll(Skills, s => s.Id))})");
        }
        return result;
    }

    void Validate(RunConfig run)
    {
        Require(MaxHp > 0 && Speed > 0 && PickupRadius >= 0, "maxHp/speed must be > 0, pickupRadius >= 0");
        Require(run.Pickups.DespawnRadius > PickupRadius, "pickupRadius must stay below run pickups.despawnRadius");

        var b = Beat;
        Require(b.Bpm is >= 30 and <= 300, "beat.bpm must be 30..300");
        Require(b.CycleBars >= 1 && b.BuildBars >= 0 && b.DropBars >= 0 && b.BuildBars + b.DropBars <= b.CycleBars,
            "beat: cycleBars >= 1, buildBars/dropBars >= 0, buildBars + dropBars <= cycleBars");
        Require(b.DropDamage >= 0 && b.DropArea >= 0, "beat dropDamage/dropArea must be >= 0");
        Require(b.ShockDamage >= 0 && b.ShockRadius > 0 && b.ShockSpeed > 0 && b.ShockKnockback >= 0,
            "beat shock: damage/knockback >= 0, radius/speed > 0");
        Require(b.ShockKnockback < b.ShockSpeed, "beat.shockKnockback must be below shockSpeed (ring must outrun pushed enemies)");

        Require(Skills.Length > 0, "no skills");
        var ids = new HashSet<string>();
        foreach (var sk in Skills)
        {
            Require(ids.Add(sk.Id), $"duplicate skill id '{sk.Id}'");
            Require(sk.Tags.Length > 0, $"skill '{sk.Id}': needs at least one tag");
            // Linear growth: checking both ends covers every level.
            ValidateSkill(sk, 1);
            ValidateSkill(sk, run.Leveling.MaxSkillLevel);
        }
        LoadoutSkills();
    }

    static void ValidateSkill(SkillDef sk, int level)
    {
        var st = sk.StatsAt(level);
        string at = $"skill '{sk.Id}' level {level}";
        Require(st.Damage > 0 && st.Cooldown > 0, $"{at}: damage and cooldown must be > 0");
        switch (sk.Kind)
        {
            case SkillKind.Projectile:
                Require(st.Amount is >= 1 and <= RunConfig.MaxProjectilesPerCast && st.Speed > 0 && st.Range > 0,
                    $"{at}: needs count 1..{RunConfig.MaxProjectilesPerCast}, speed > 0, range > 0");
                break;
            case SkillKind.Ring:
                Require(st.Radius > 0 && st.Speed > 0 && st.Knockback >= 0 && st.Knockback < st.Speed,
                    $"{at}: needs radius > 0, speed > 0, 0 <= knockback < speed");
                break;
            case SkillKind.Speaker:
                Require(st.Amount is >= 1 and <= RunConfig.MaxSummonsPerSkill && st.Duration > 0 && st.Radius > 0 && st.Range > 0,
                    $"{at}: needs count 1..{RunConfig.MaxSummonsPerSkill}, duration > 0, radius > 0, range > 0");
                break;
            case SkillKind.Aura:
                Require(st.Radius > 0 && st.Vuln >= 0, $"{at}: needs radius > 0, vuln >= 0");
                break;
            case SkillKind.Beam:
                Require(st.Range > 0 && st.Width > 0, $"{at}: needs range > 0, width > 0");
                break;
            case SkillKind.Orbit:
                Require(st.Amount is >= 1 and <= RunConfig.MaxBlades && st.Speed > 0 && st.Range > 0 && st.Radius > 0,
                    $"{at}: needs count 1..{RunConfig.MaxBlades}, speed > 0, range > 0, radius > 0");
                break;
            default:
                throw new FormatException($"{at}: unknown kind {sk.Kind}");
        }
    }

    static void Require(bool ok, string message)
    {
        if (!ok) throw new FormatException("hero config: " + message);
    }
}
