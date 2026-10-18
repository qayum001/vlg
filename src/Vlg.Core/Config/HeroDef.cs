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
    /// <summary>Ultimate pool (domain.md: 3 per hero).</summary>
    public required UltimateDef[] Ultimates { get; init; }
    /// <summary>Default ultimate id for the loadout.</summary>
    public required string Ultimate { get; init; }

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

    /// <summary>A saved setlist still matches this hero (null = default, also valid). Saves outlive data changes.</summary>
    public bool IsValidLoadout(IReadOnlyList<string>? ids)
    {
        if (ids == null) return true;
        try
        {
            LoadoutSkills(ids);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public bool HasUltimate(string? id) => id == null || Array.Exists(Ultimates, u => u.Id == id);

    /// <summary>The ultimate by id; null = the default.</summary>
    public UltimateDef UltimateById(string? id = null)
    {
        id ??= Ultimate;
        return Array.Find(Ultimates, u => u.Id == id)
            ?? throw new FormatException($"hero '{Id}': ultimate '{id}' not in the pool ({string.Join(", ", Array.ConvertAll(Ultimates, u => u.Id))})");
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
        var l = run.Leveling;
        var ids = new HashSet<string>();
        foreach (var sk in Skills)
        {
            Require(ids.Add(sk.Id), $"duplicate skill id '{sk.Id}'");
            Require(sk.Tags.Length > 0, $"skill '{sk.Id}': needs at least one tag");
            var path = new int[l.BranchLevels.Length];
            Array.Fill(path, -1);
            ValidateTree(sk, path, 0, l.BranchLevels, l.MaxSkillLevel);
        }
        LoadoutSkills();

        Require(Ultimates.Length > 0, "no ultimates");
        var ultIds = new HashSet<string>();
        foreach (var u in Ultimates) ValidateUltimate(u, l.UltimateLevels.Length, ultIds);
        UltimateById();
    }

    static void ValidateUltimate(UltimateDef u, int levels, HashSet<string> ids)
    {
        string at = $"ultimate '{u.Id}'";
        Require(ids.Add(u.Id), $"duplicate ultimate id '{u.Id}'");
        Require(u.Levels.Length == levels, $"{at}: needs {levels} levels (leveling.ultimateLevels)");
        for (int i = 0; i < u.Levels.Length; i++)
        {
            var st = u.Levels[i];
            string lv = $"{at} level {i + 1}";
            Require(st.Charge > 0 && st.Tick >= 0 && st.Heal >= 0, $"{lv}: charge > 0, tick/heal >= 0");
            if (st.Tick > 0) Require(st.Duration > 0, $"{lv}: tick needs duration > 0");
            switch (u.Kind)
            {
                case UltimateKind.SubQuake:
                    Require(st.Damage > 0 && st.Radius > 0 && st.Stun >= 0, $"{lv}: needs damage > 0, radius > 0, stun >= 0");
                    break;
                case UltimateKind.Encore:
                    Require(st.Duration > 0 && st.Speed >= 0, $"{lv}: needs duration > 0, speed >= 0");
                    break;
                case UltimateKind.BassCannon:
                    Require(st.Range > 0 && st.Width > 0 && st.Duration > 0, $"{lv}: needs range > 0, width > 0, duration > 0");
                    break;
                default:
                    throw new FormatException($"{lv}: unknown kind {u.Kind}");
            }
        }
    }

    /// <summary>
    /// Every reachable form at the level it starts and at max level (linear growth: both ends cover the range).
    /// </summary>
    static void ValidateTree(SkillDef sk, int[] path, int point, int[] branchLevels, int maxLevel)
    {
        int start = point == 0 ? 1 : branchLevels[point - 1];
        ValidateForm(sk, sk.Resolve(start, path, branchLevels), start);
        ValidateForm(sk, sk.Resolve(maxLevel, path, branchLevels), maxLevel);
        var options = sk.OptionsAt(path, point);
        if (options.Count == 0) return;
        string at = $"skill '{sk.Id}' branch point {point + 1}";
        Require(point < branchLevels.Length, $"{at}: deeper than leveling.branchLevels");
        Require(options.Count <= RunConfig.MaxSkills, $"{at}: at most {RunConfig.MaxSkills} options");
        var ids = new HashSet<string>();
        for (int k = 0; k < options.Count; k++)
        {
            var b = options[k];
            Require(ids.Add(b.Id), $"{at}: duplicate option '{b.Id}'");
            Require(b.Name.Length > 0 && b.Text.Length > 0, $"{at}: option '{b.Id}' needs name and text");
            Require(b.Tags == null || b.Tags.Length > 0, $"{at}: option '{b.Id}' tags must not be empty");
            path[point] = k;
            ValidateTree(sk, path, point + 1, branchLevels, maxLevel);
        }
        path[point] = -1;
    }

    static void ValidateForm(SkillDef sk, SkillForm f, int level) =>
        FormRules.Validate(f, $"hero config: skill '{sk.Id}' ({f.Name}) level {level}");

    static void Require(bool ok, string message)
    {
        if (!ok) throw new FormatException("hero config: " + message);
    }
}
