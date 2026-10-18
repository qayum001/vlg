using System;
using System.Linq;
using Vlg.Core.Config;
using Xunit;

namespace Vlg.Core.Tests;

public class RunConfigTests
{
    [Fact]
    public void Shipped_run_json_parses_with_domain_loop()
    {
        var c = TestConfig.Run;

        Assert.Equal(new[] { StageKind.Mobs, StageKind.Horde, StageKind.Chest, StageKind.Mobs, StageKind.Shop },
            c.Stages.Select(s => s.Kind));
        Assert.Equal(3, c.Leveling.LevelDivisor);
        Assert.Equal((40f, 28f), (c.Camera.TiltDeg, c.Camera.Distance));
        Assert.Equal(10, c.Leveling.MaxSkillLevel);
    }

    [Fact]
    public void Unknown_key_throws()
    {
        string json = TestConfig.RunJson.Replace("\"contactMargin\": 0.3 }", "\"contactMargin\": 0.3, \"bogus\": 1 }");
        Assert.ThrowsAny<Exception>(() => RunConfig.Parse(json));
    }

    [Fact]
    public void Missing_required_key_throws()
    {
        string json = TestConfig.RunJson.Replace(", \"contactMargin\": 0.3 }", " }");
        Assert.ThrowsAny<Exception>(() => RunConfig.Parse(json));
    }

    [Fact]
    public void Chest_stage_with_a_duration_throws()
    {
        string json = TestConfig.RunJson.Replace("{ \"kind\": \"chest\" }", "{ \"kind\": \"chest\", \"durationSec\": 5 }");
        var e = Assert.Throws<FormatException>(() => RunConfig.Parse(json));
        Assert.Contains("chest", e.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Loop_scaling_is_one_on_loop_one_and_linear_after()
    {
        var s = new LoopScaling { HpPerLoop = 0.3f, DamagePerLoop = 0.2f, SpawnPerLoop = 0.25f };

        Assert.Equal(1f, s.Hp(1));
        Assert.Equal(1.6f, s.Hp(3), 4);
        Assert.Equal(1.4f, s.Damage(3), 4);
        Assert.Equal(1.5f, s.Spawn(3), 4);
    }

    [Fact]
    public void Camera_that_sees_the_horizon_throws()
    {
        // 25 - 45 / 2 = 2.5 degrees below the horizon: under the 5 degree minimum.
        string json = TestConfig.RunJson.Replace("\"tiltDeg\": 40", "\"tiltDeg\": 25");
        var e = Assert.Throws<FormatException>(() => RunConfig.Parse(json));
        Assert.Contains("tiltDeg", e.Message);
    }

    [Fact]
    public void Despawn_radius_inside_the_visible_area_throws()
    {
        string json = TestConfig.RunJson.Replace("\"band\": 6, \"despawnRadius\": 80", "\"band\": 6, \"despawnRadius\": 50");
        var e = Assert.Throws<FormatException>(() => RunConfig.Parse(json));
        Assert.Contains("spawn.despawnRadius", e.Message);
    }
}
