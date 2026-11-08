using System;
using System.Text.Json;

namespace Vlg.Core.Config;

public sealed record UserSettings
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public int MasterVolumePercent { get; init; } = 100;

    public static UserSettings Parse(string json)
    {
        var settings = JsonSerializer.Deserialize<UserSettings>(json, JsonOptions)
            ?? throw new FormatException("User settings must be an object.");
        settings.Validate();
        return settings;
    }

    public string ToJson()
    {
        Validate();
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    void Validate()
    {
        if (MasterVolumePercent is < 0 or > 100)
            throw new FormatException("Master volume must be between 0 and 100 percent.");
    }
}
