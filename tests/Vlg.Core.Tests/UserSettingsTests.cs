using System;
using System.Text.Json;
using Vlg.Core.Config;
using Xunit;

namespace Vlg.Core.Tests;

public class UserSettingsTests
{
    [Fact]
    public void Missing_preferences_use_defaults()
    {
        Assert.Equal(100, UserSettings.Parse("{}").MasterVolumePercent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(37)]
    [InlineData(100)]
    public void Volume_round_trips_including_mute(int percent)
    {
        var settings = new UserSettings { MasterVolumePercent = percent };
        Assert.Equal(percent, UserSettings.Parse(settings.ToJson()).MasterVolumePercent);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Out_of_range_volume_is_rejected_on_load_and_save(int percent)
    {
        Assert.Throws<FormatException>(() => UserSettings.Parse($"{{\"masterVolumePercent\":{percent}}}"));
        Assert.Throws<FormatException>(() => new UserSettings { MasterVolumePercent = percent }.ToJson());
    }

    [Fact]
    public void Unknown_preferences_allow_opening_settings_from_a_newer_version()
    {
        Assert.Equal(25, UserSettings.Parse("{\"masterVolumePercent\":25,\"futureSetting\":true}").MasterVolumePercent);
    }

    [Fact]
    public void Null_settings_are_rejected()
    {
        Assert.Throws<FormatException>(() => UserSettings.Parse("null"));
    }

    [Theory]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("{\"masterVolumePercent\":\"loud\"}")]
    [InlineData("{\"masterVolumePercent\":null}")]
    public void Malformed_settings_are_rejected(string json)
    {
        Assert.Throws<JsonException>(() => UserSettings.Parse(json));
    }
}
