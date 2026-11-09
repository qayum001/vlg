using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Vlg.Core.Config;

/// <summary>Root of game/data/items.json: credits, shop, chest, slots and the item pool. Strict like RunConfig.</summary>
public sealed record ItemsConfig
{
    /// <summary>Chance per kill to drop a credit coin, and its value.</summary>
    public required float CreditDropChance { get; init; }
    public required float CreditValue { get; init; }
    public required ShopSettings Shop { get; init; }
    /// <summary>Chest offers this many rare items, pick 1 (domain.md: no reroll, no skip).</summary>
    public required int ChestOptions { get; init; }
    /// <summary>Starting active / usable slots (meta upgrades raise them later, domain.md: up to 5 + 5).</summary>
    public required int ActiveSlots { get; init; }
    public required int UsableSlots { get; init; }
    /// <summary>Sockets per skill: 1 when unlocked, +1 per branch level reached, at most this.</summary>
    public required int MaxSockets { get; init; }
    public required ItemDef[] Items { get; init; }

    public const int MaxSlots = 5;

    public static ItemsConfig Parse(string json)
    {
        var c = JsonSerializer.Deserialize<ItemsConfig>(json, ConfigJson.Options) ?? throw new FormatException("items config is null");
        c.Validate();
        return c;
    }

    public int IndexOf(string id) =>
        Array.FindIndex(Items, i => i.Id == id) is var k and >= 0 ? k : throw new FormatException($"items: unknown item '{id}'");

    void Validate()
    {
        Require(CreditDropChance is >= 0 and <= 1 && CreditValue > 0, "creditDropChance 0..1, creditValue > 0");
        var s = Shop;
        Require(s.Size >= 1 && s.RerollBase >= 0 && s.RerollStep >= 0 && s.RerollPerLoop >= 0, "shop size >= 1, reroll costs >= 0");
        Require(s.Prices.Common >= 0 && s.Prices.Uncommon >= 0 && s.Prices.Rare >= 0, "shop prices >= 0");
        Require(s.Weights.Common >= 0 && s.Weights.Uncommon >= 0 && s.Weights.Rare >= 0
            && s.Weights.Common + s.Weights.Uncommon + s.Weights.Rare > 0, "shop weights >= 0, sum > 0");
        Require(ChestOptions >= 1, "chestOptions >= 1");
        Require(ActiveSlots is >= 0 and <= MaxSlots && UsableSlots is >= 0 and <= MaxSlots, $"activeSlots/usableSlots 0..{MaxSlots}");
        Require(MaxSockets >= 1, "maxSockets >= 1");
        Require(Items.Length > 0, "no items");
        var ids = new HashSet<string>();
        foreach (var it in Items) ValidateItem(it, ids);
    }

    static void ValidateItem(ItemDef it, HashSet<string> ids)
    {
        string at = $"item '{it.Id}'";
        Require(ids.Add(it.Id), $"duplicate item id '{it.Id}'");
        Require(it.Name.Length > 0 && it.Text.Length > 0, $"{at}: needs name and text");
        int parts = (it.Modifier != null ? 1 : 0) + (it.Active != null ? 1 : 0) + (it.Usable != null ? 1 : 0) + (it.Passive != null ? 1 : 0);
        Require(parts == 1, $"{at}: exactly one of modifier/active/usable/passive");
        switch (it.Type)
        {
            case ItemType.Modifier:
                var m = it.Modifier;
                Require(m != null && m.Fits.Length > 0, $"{at}: modifier needs fits");
                Require(m!.HexVuln >= 0 && m.HexSec >= 0 && (m.HexVuln == 0) == (m.HexSec == 0), $"{at}: hexVuln and hexSec go together, >= 0");
                Require(m.PoisonDamage >= 0 && (m.PoisonDamage == 0 || (m.PoisonRadius > 0 && m.PoisonSec > 0 && m.PoisonInterval > 0)),
                    $"{at}: poison needs radius, sec and interval > 0");
                break;
            case ItemType.Active:
                Require(it.Active != null, $"{at}: active item needs 'active'");
                var def = it.ToSkillDef();
                FormRules.Validate(def.Resolve(1, ReadOnlySpan<int>.Empty, Array.Empty<int>()), $"items config: {at}");
                break;
            case ItemType.Usable:
                var u = it.Usable;
                Require(u != null && u.Cooldown > 0 && u.Duration > 0, $"{at}: usable needs cooldown and duration > 0");
                Require(u!.Kind switch
                {
                    UsableKind.Rush => u.RateBonus >= 0 && u.MoveBonus >= 0 && u.CrashSec >= 0 && u.CrashMove is >= 0 and < 1,
                    UsableKind.Smoke => u.MoveBonus >= 0,
                    UsableKind.HolyWater => u.Range >= 0 && u.Radius > 0 && u.Damage > 0 && u.Interval > 0 && u.UndeadFactor >= 1,
                    UsableKind.Singularity => u.Range >= 0 && u.Radius > 0 && u.Pull > 0 && u.PopRadius > 0 && u.Damage >= 0,
                    _ => false,
                }, $"{at}: usable numbers out of range for {u.Kind}");
                break;
            case ItemType.Passive:
                var p = it.Passive;
                Require(p != null && p.Armor is >= 0 and < 1 && p.Cooldown is >= 0 and < 1 && p.LifeSteal is >= 0 and <= 1
                    && p.LedgerKills >= 0 && (p.LedgerKills > 0) == (p.LedgerDamage > 0),
                    $"{at}: passive numbers out of range (armor/cooldown 0..<1, ledgerKills with ledgerDamage)");
                break;
        }
    }

    static void Require(bool ok, string message)
    {
        if (!ok) throw new FormatException("items config: " + message);
    }
}
