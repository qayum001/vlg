using System;
using System.Globalization;
using Godot;
using Vlg.Core.Config;

namespace Vlg.Game;

/// <summary>Player-facing item strings and rarity colors (shop, chest, HUD).</summary>
public static class ItemText
{
    public static Color RarityColor(Rarity r) => r switch
    {
        Rarity.Common => new Color(0.8f, 0.8f, 0.85f),
        Rarity.Uncommon => new Color(0.4f, 1f, 0.5f),
        _ => new Color(1f, 0.6f, 1f),
    };

    public static string TypeName(ItemDef it) => it.Type switch
    {
        ItemType.Modifier => "Modifier · fits " + string.Join(", ", Array.ConvertAll(it.Modifier!.Fits, f => f.ToString())),
        ItemType.Active => "Active",
        ItemType.Usable => string.Create(CultureInfo.InvariantCulture, $"Usable · {it.Usable!.Cooldown:0} s cooldown"),
        _ => "Passive",
    };

    /// <summary>Two lines: name, rarity and type; effect and flavor.</summary>
    public static string Card(ItemDef it) => $"{it.Name}  ·  {it.Rarity}  ·  {TypeName(it)}\n{it.Text}  \"{it.Flavor}\"";
}
