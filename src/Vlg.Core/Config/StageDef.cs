using System.Text.Json.Serialization;

namespace Vlg.Core.Config;

/// <summary>One stage of the level loop. Timed fields stay 0 (omitted) for Chest and Shop.</summary>
public sealed record StageDef
{
    public required StageKind Kind { get; init; }
    public float DurationSec { get; init; }
    public float SpawnPerSec { get; init; }
    public int MaxAlive { get; init; }

    [JsonIgnore]
    public bool IsTimed => Kind is StageKind.Mobs or StageKind.Horde;
}
