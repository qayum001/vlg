using System;
using System.Linq;
using Vlg.Core.Config;
using Xunit;

namespace Vlg.Core.Tests;

public class RunConfigTests
{
    [Fact]
    public void Shipped_run_json_parses_with_domain_loop_and_four_skills()
    {
        var c = TestConfig.Run;

        Assert.Equal(new[] { StageKind.Mobs, StageKind.Horde, StageKind.Chest, StageKind.Mobs, StageKind.Shop },
            c.Stages.Select(s => s.Kind));
        Assert.Equal(new[] { SkillKind.Bolt, SkillKind.Nova, SkillKind.Aura, SkillKind.Orbit }, c.Skills.Select(s => s.Kind));
        Assert.Equal(3, c.Leveling.LevelDivisor);
        Assert.Equal(10, c.Leveling.MaxSkillLevel);
    }

    [Fact]
    public void Unknown_key_throws()
    {
        string json = TestConfig.RunJson.Replace("\"maxHp\": 100,", "\"maxHp\": 100, \"bogus\": 1,");
        Assert.ThrowsAny<Exception>(() => RunConfig.Parse(json));
    }

    [Fact]
    public void Missing_required_key_throws()
    {
        string json = TestConfig.RunJson.Replace("\"maxHp\": 100,", "");
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
    public void Skill_cooldown_reaching_zero_at_max_level_throws()
    {
        string json = TestConfig.RunJson.Replace("\"cooldown\": -0.02", "\"cooldown\": -0.1");
        var e = Assert.Throws<FormatException>(() => RunConfig.Parse(json));
        Assert.Contains("bolt", e.Message);
    }

    [Fact]
    public void Skill_stats_grow_linearly_and_count_floors()
    {
        var def = new SkillDef
        {
            Id = "x", Name = "X", Kind = SkillKind.Bolt,
            Base = new SkillStats { Damage = 6, Cooldown = 0.8f, Count = 1 },
            PerLevel = new SkillStats { Damage = 1.5f, Cooldown = -0.03f, Count = 0.5f },
        };

        var l3 = def.StatsAt(3);
        Assert.Equal(9f, l3.Damage, 4);
        Assert.Equal(0.74f, l3.Cooldown, 4);
        Assert.Equal(1, def.StatsAt(1).Amount);
        Assert.Equal(1, def.StatsAt(2).Amount);
        Assert.Equal(2, def.StatsAt(3).Amount);
        Assert.Equal(5, def.StatsAt(10).Amount);
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
    public void Bolt_count_above_the_per_cast_cap_throws()
    {
        Assert.Throws<FormatException>(() => RunConfig.Parse(TestConfig.RunJson.Replace(
            "\"base\": { \"damage\": 10, \"cooldown\": 0.6, \"count\": 1,",
            $"\"base\": {{ \"damage\": 10, \"cooldown\": 0.6, \"count\": {RunConfig.MaxProjectilesPerCast + 1},")));
    }
}
