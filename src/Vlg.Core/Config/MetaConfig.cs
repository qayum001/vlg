using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Vlg.Core.Config;

/// <summary>
/// Root of game/data/meta.json (domain.md Meta-progression): meta credit drops and loop rewards in runs,
/// upgrades sold in the hub. Strict like RunConfig.
/// </summary>
public sealed record MetaConfig
{
    /// <summary>Chance per kill to drop a meta credit orb, and its value.</summary>
    public required float CreditDropChance { get; init; }
    public required float CreditValue { get; init; }
    /// <summary>Completing loop L pays LoopRewardBase + LoopRewardStep * (L - 1) meta credits.</summary>
    public required float LoopRewardBase { get; init; }
    public required float LoopRewardStep { get; init; }
    public required MetaUpgradeDef[] Upgrades { get; init; }

    public static MetaConfig Parse(string json)
    {
        var c = JsonSerializer.Deserialize<MetaConfig>(json, ConfigJson.Options) ?? throw new FormatException("meta config is null");
        c.Validate();
        return c;
    }

    public float LoopReward(int completedLoop) => LoopRewardBase + LoopRewardStep * (completedLoop - 1);

    void Validate()
    {
        Require(CreditDropChance is >= 0 and <= 1 && CreditValue > 0, "creditDropChance 0..1, creditValue > 0");
        Require(LoopRewardBase >= 0 && LoopRewardStep >= 0, "loop rewards >= 0");
        var ids = new HashSet<string>();
        foreach (var u in Upgrades)
        {
            string at = $"upgrade '{u.Id}'";
            Require(ids.Add(u.Id), $"duplicate upgrade id '{u.Id}'");
            Require(u.Name.Length > 0 && u.Text.Length > 0, $"{at}: needs name and text");
            Require(u.MaxRank >= 1 && u.CostBase >= 0 && u.CostStep >= 0 && u.PerRank > 0, $"{at}: maxRank >= 1, costs >= 0, perRank > 0");
            if (u.Kind is MetaKind.ActiveSlots or MetaKind.UsableSlots)
                Require(u.PerRank == 1 && u.MaxRank <= ItemsConfig.MaxSlots, $"{at}: slot upgrades are +1 per rank, at most {ItemsConfig.MaxSlots} ranks");
        }
    }

    static void Require(bool ok, string message)
    {
        if (!ok) throw new FormatException("meta config: " + message);
    }
}
