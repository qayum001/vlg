using System;
using System.Collections.Generic;
using System.Text.Json;
using Vlg.Core.Config;

namespace Vlg.Core.Meta;

/// <summary>
/// Persistent player state (domain.md: meta credits kept fully on death and win). JSON on disk: lenient (unknown
/// keys ignored, missing keys default) so older saves keep loading.
/// </summary>
public sealed class MetaSave
{
    public int Version { get; set; } = 1;
    public string HeroId { get; set; } = HeroRoster.DefaultId;
    public float MetaCredits { get; set; }
    /// <summary>Upgrade id → owned ranks.</summary>
    public Dictionary<string, int> Ranks { get; set; } = new();
    /// <summary>Chosen loadout skill ids (null = hero default).</summary>
    public string[]? Loadout { get; set; }
    public string? Ultimate { get; set; }
    public int BestLoop { get; set; }
    public int Runs { get; set; }

    static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static MetaSave Parse(string json)
    {
        var save = string.IsNullOrWhiteSpace(json) ? new MetaSave() : JsonSerializer.Deserialize<MetaSave>(json, Json) ?? new MetaSave();
        if (!HeroRoster.Contains(save.HeroId)) save.SelectHero(HeroRoster.DefaultId);
        return save;
    }

    public void SelectHero(string id)
    {
        if (!HeroRoster.Contains(id)) throw new ArgumentException($"unknown hero '{id}'", nameof(id));
        if (HeroId == id) return;
        HeroId = id;
        Loadout = null;
        Ultimate = null;
    }

    public string ToJson() => JsonSerializer.Serialize(this, Json);

    public int Rank(string id) => Ranks.TryGetValue(id, out int r) ? r : 0;

    /// <summary>Banks a finished run: credits earned, loop reached (1-based).</summary>
    public void AddRun(float metaCredits, int loopReached)
    {
        if (metaCredits < 0f) throw new ArgumentOutOfRangeException(nameof(metaCredits));
        MetaCredits += metaCredits;
        BestLoop = Math.Max(BestLoop, loopReached);
        Runs++;
    }
}
