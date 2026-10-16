using System;
using System.Collections.Generic;
using System.Text.Json;
using Vlg.Core.Config;
using Xunit;

namespace Vlg.Core.Tests;

public class StressConfigTests
{
    [Fact]
    public void Game_config_parses_with_all_presets()
    {
        var c = TestConfig.Game;

        foreach (var name in new[] { "very_low", "low", "medium", "high" })
            Assert.NotNull(c.GetPreset(name));
        Assert.Equal(ScalingMode.Bilinear, c.GetPreset("low").Scaling);
        Assert.Equal(AntiAliasing.Taa, c.GetPreset("high").AntiAliasing);
    }

    [Fact]
    public void Presets_scale_up_in_order()
    {
        var c = TestConfig.Game;
        var order = new[] { "very_low", "low", "medium", "high" };
        for (int i = 1; i < order.Length; i++)
        {
            QualityPreset lo = c.GetPreset(order[i - 1]), hi = c.GetPreset(order[i]);
            Assert.True(hi.MaxEnemies > lo.MaxEnemies, order[i]);
            Assert.True(hi.RenderScale >= lo.RenderScale, order[i]);
            Assert.True(hi.AuraParticles > lo.AuraParticles, order[i]);
        }
    }

    [Fact]
    public void Unknown_preset_name_lists_available_ones()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() => TestConfig.Game.GetPreset("ultra"));
        Assert.Contains("very_low", ex.Message);
    }

    [Fact]
    public void Unknown_key_throws()
    {
        string json = TestConfig.GameJson.Replace("\"enemySpeed\":", "\"enemySpeeed\": 1, \"enemySpeed\":");
        Assert.ThrowsAny<JsonException>(() => StressConfig.Parse(json));
    }

    [Fact]
    public void Missing_key_throws()
    {
        string json = TestConfig.GameJson.Replace("\"enemySpeed\": 3.2,", "");
        Assert.ThrowsAny<JsonException>(() => StressConfig.Parse(json));
    }

    [Fact]
    public void Out_of_range_value_throws()
    {
        string json = TestConfig.GameJson.Replace("\"maxNeighbors\": 12", "\"maxNeighbors\": 500");
        Assert.Throws<FormatException>(() => StressConfig.Parse(json));
    }
}
