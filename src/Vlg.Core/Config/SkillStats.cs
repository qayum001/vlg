using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vlg.Core.Config;

/// <summary>
/// Skill numbers at one level, one float per <see cref="Stat"/>. JSON: object of stat keys; omitted = 0.
/// Immutable after construction (operations return new blocks); resolve on level/branch change, not per frame.
/// Time unit (cooldown, duration): beats for a hero with a beat clock, seconds otherwise. Use per kind:
/// Projectile: damage, cooldown, count, speed, range (travel), radius (hit, 0 = default), pierce, bounces, falloff.
/// Ring: damage, cooldown, radius (max), speed (expansion), knockback (below speed), slow + slowSec, stun.
/// Speaker: damage (per pulse), cooldown, count (max alive), duration, radius (pulse), range (placement), pull, period.
/// Aura: damage, cooldown (tick), radius, vuln; trail puddles: duration, slow.
/// Beam: damage, cooldown, range (length), width, count (directions).
/// Orbit: damage, cooldown (tick), count, speed (rad/s), range (orbit), radius (hit), burn + burnSec, period, pull.
/// BellDrop: damage, cooldown, count (bells), radius (impact), range (target), stun.
/// Quake: damage (per beat), cooldown, radius, duration, period.
/// Drone: damage, cooldown, count (drones), speed (bolt), range, radius (explosion).
/// Shield: damage (blast), cooldown (recharge), radius (blast), knockback.
/// Sweep: damage, cooldown (tick), range (length), width, arc, duration (sweep time), count (beams).
/// Boomerang: damage, cooldown, count, speed, range, radius (hit), bounces.
/// </summary>
[JsonConverter(typeof(SkillStatsJson))]
public readonly struct SkillStats
{
    public const int Size = (int)Stat.Charge + 1;

    readonly float[]? _v;

    SkillStats(float[] v) => _v = v;

    public float this[Stat s] => _v == null ? 0f : _v[(int)s];

    public float Damage { get => this[Stat.Damage]; init => _v = Set(_v, Stat.Damage, value); }
    public float Cooldown { get => this[Stat.Cooldown]; init => _v = Set(_v, Stat.Cooldown, value); }
    public float Count { get => this[Stat.Count]; init => _v = Set(_v, Stat.Count, value); }
    public float Speed { get => this[Stat.Speed]; init => _v = Set(_v, Stat.Speed, value); }
    public float Range { get => this[Stat.Range]; init => _v = Set(_v, Stat.Range, value); }
    public float Radius { get => this[Stat.Radius]; init => _v = Set(_v, Stat.Radius, value); }
    public float Duration { get => this[Stat.Duration]; init => _v = Set(_v, Stat.Duration, value); }
    public float Width { get => this[Stat.Width]; init => _v = Set(_v, Stat.Width, value); }
    public float Knockback { get => this[Stat.Knockback]; init => _v = Set(_v, Stat.Knockback, value); }
    public float Vuln { get => this[Stat.Vuln]; init => _v = Set(_v, Stat.Vuln, value); }
    public float Pierce { get => this[Stat.Pierce]; init => _v = Set(_v, Stat.Pierce, value); }
    public float Bounces { get => this[Stat.Bounces]; init => _v = Set(_v, Stat.Bounces, value); }
    public float Falloff { get => this[Stat.Falloff]; init => _v = Set(_v, Stat.Falloff, value); }
    public float Slow { get => this[Stat.Slow]; init => _v = Set(_v, Stat.Slow, value); }
    public float SlowSec { get => this[Stat.SlowSec]; init => _v = Set(_v, Stat.SlowSec, value); }
    public float Stun { get => this[Stat.Stun]; init => _v = Set(_v, Stat.Stun, value); }
    public float Pull { get => this[Stat.Pull]; init => _v = Set(_v, Stat.Pull, value); }
    public float Burn { get => this[Stat.Burn]; init => _v = Set(_v, Stat.Burn, value); }
    public float BurnSec { get => this[Stat.BurnSec]; init => _v = Set(_v, Stat.BurnSec, value); }
    public float Period { get => this[Stat.Period]; init => _v = Set(_v, Stat.Period, value); }
    public float Arc { get => this[Stat.Arc]; init => _v = Set(_v, Stat.Arc, value); }
    public float Tick { get => this[Stat.Tick]; init => _v = Set(_v, Stat.Tick, value); }
    public float Heal { get => this[Stat.Heal]; init => _v = Set(_v, Stat.Heal, value); }
    public float Charge { get => this[Stat.Charge]; init => _v = Set(_v, Stat.Charge, value); }

    /// <summary>Count floored; fractional growth (0.5 per level) adds one every other level.</summary>
    public int Amount => Floor(Count);
    public int PierceAmount => Floor(Pierce);
    public int BounceAmount => Floor(Bounces);

    /// <summary>All stats = <paramref name="value"/> (neutral scale is 1).</summary>
    public static SkillStats Filled(float value)
    {
        var v = new float[Size];
        Array.Fill(v, value);
        return new SkillStats(v);
    }

    /// <summary>Linear growth: level 1 = base.</summary>
    public static SkillStats AtLevel(SkillStats b, SkillStats perLevel, int level)
    {
        float k = level - 1;
        var v = new float[Size];
        for (int i = 0; i < Size; i++) v[i] = b.At(i) + perLevel.At(i) * k;
        return new SkillStats(v);
    }

    /// <summary>this * scale + add, per stat.</summary>
    public SkillStats Apply(SkillStats scale, SkillStats add)
    {
        var v = new float[Size];
        for (int i = 0; i < Size; i++) v[i] = At(i) * scale.At(i, 1f) + add.At(i);
        return new SkillStats(v);
    }

    public SkillStats With(Stat s, float value)
    {
        var v = new float[Size];
        for (int i = 0; i < Size; i++) v[i] = At(i);
        v[(int)s] = value;
        return new SkillStats(v);
    }

    float At(int i, float missing = 0f) => _v == null ? missing : _v[i];

    /// <summary>Copy-on-write for init accessors: a with-expression copy shares the array until its first init writes.</summary>
    static float[] Set(float[]? current, Stat s, float value)
    {
        var v = current == null ? new float[Size] : (float[])current.Clone();
        v[(int)s] = value;
        return v;
    }

    static int Floor(float x) => (int)MathF.Floor(x + 1e-4f);

    /// <summary>Object initializer support: allocate before the first init accessor runs.</summary>
    public SkillStats() => _v = new float[Size];

    /// <summary>Reads {"damage": 9, ...}; unknown keys throw; missing keys = <paramref name="missing"/>.</summary>
    internal static SkillStats Read(ref Utf8JsonReader reader, float missing)
    {
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("stats must be an object");
        var v = new float[Size];
        Array.Fill(v, missing);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            string key = reader.GetString()!;
            if (!TryCamel(key, out var stat))
                throw new JsonException($"unknown stat '{key}' (valid: {string.Join(", ", Enum.GetNames<Stat>())})");
            reader.Read();
            v[(int)stat] = reader.GetSingle();
        }
        return new SkillStats(v);
    }

    static bool TryCamel(string key, out Stat stat)
    {
        stat = default;
        return key.Length > 0 && char.IsAsciiLetterLower(key[0])
            && Enum.TryParse(char.ToUpperInvariant(key[0]) + key[1..], ignoreCase: false, out stat)
            && Enum.IsDefined(stat);
    }
}

/// <summary>JSON for <see cref="SkillStats"/>: missing keys are 0.</summary>
sealed class SkillStatsJson : JsonConverter<SkillStats>
{
    public override SkillStats Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        SkillStats.Read(ref reader, 0f);

    public override void Write(Utf8JsonWriter writer, SkillStats value, JsonSerializerOptions options) =>
        throw new NotSupportedException("stats are read-only data");
}

/// <summary>JSON for a stat multiplier block: missing keys are 1.</summary>
sealed class StatScaleJson : JsonConverter<SkillStats>
{
    public override SkillStats Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        SkillStats.Read(ref reader, 1f);

    public override void Write(Utf8JsonWriter writer, SkillStats value, JsonSerializerOptions options) =>
        throw new NotSupportedException("stats are read-only data");
}
