using System;
using System.Text.Json.Serialization;

namespace Vlg.Core.Config;

/// <summary>
/// Socketed into a skill whose current form shares a tag with <see cref="Fits"/>. Applies stats * Scale + Add
/// (+ ChainAdd for Chain-tagged forms). Hex: hits by that skill make enemies take more damage. Poison: kills by
/// that skill leave a puddle.
/// </summary>
public sealed record ModifierDef
{
    public required SkillTag[] Fits { get; init; }
    [JsonConverter(typeof(StatScaleJson))]
    public SkillStats Scale { get; init; } = SkillStats.Filled(1f);
    public SkillStats Add { get; init; }
    public SkillStats ChainAdd { get; init; }
    /// <summary>Extra damage taken (fraction) / seconds, applied on every hit by the skill.</summary>
    public float HexVuln { get; init; }
    public float HexSec { get; init; }
    /// <summary>Puddle on kill: damage per tick, radius m, life s, tick interval s.</summary>
    public float PoisonDamage { get; init; }
    public float PoisonRadius { get; init; }
    public float PoisonSec { get; init; }
    public float PoisonInterval { get; init; }

    public bool FitsAny(SkillTag[] tags) => Array.Exists(Fits, f => Array.IndexOf(tags, f) >= 0);
}
