using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vlg.Core.Config;

/// <summary>Shared strict JSON settings for data files: unknown keys throw, enums as camelCase strings.</summary>
static class ConfigJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
    };
}
