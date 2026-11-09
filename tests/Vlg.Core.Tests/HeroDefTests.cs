using System;
using System.Linq;
using Vlg.Core.Config;
using Xunit;

namespace Vlg.Core.Tests;

public class HeroDefTests
{
    [Fact]
    public void Shipped_lowkey_parses_with_six_skills_and_a_four_skill_loadout()
    {
        var h = TestConfig.Hero;

        Assert.Equal("lowkey", h.Id);
        Assert.Equal(new[] { "cowbell", "low_end", "speaker_stack", "distortion", "slap", "vinyl" }, h.Skills.Select(s => s.Id));
        Assert.Equal(new[] { SkillKind.Projectile, SkillKind.Ring, SkillKind.Speaker, SkillKind.Aura, SkillKind.Beam, SkillKind.Orbit },
            h.Skills.Select(s => s.Kind));
        Assert.Equal(new[] { "cowbell", "low_end", "speaker_stack", "slap" }, h.LoadoutSkills().Select(s => s.Id));
        Assert.Equal(130f, h.Beat.Bpm);
        Assert.Contains(SkillTag.Aimed, h.Skills.Single(s => s.Id == "slap").Tags);
    }

    [Fact]
    public void Loadout_override_keeps_its_order_and_rejects_unknown_or_duplicate_ids()
    {
        var h = TestConfig.Hero;

        Assert.Equal(new[] { "vinyl", "cowbell" }, h.LoadoutSkills(new[] { "vinyl", "cowbell" }).Select(s => s.Id));
        Assert.Contains("bogus", Assert.Throws<FormatException>(() => h.LoadoutSkills(new[] { "cowbell", "bogus" })).Message);
        Assert.Throws<FormatException>(() => h.LoadoutSkills(new[] { "cowbell", "cowbell" }));
        Assert.Throws<FormatException>(() => h.LoadoutSkills(Array.Empty<string>()));
    }

    [Fact]
    public void Saved_setlists_are_checked_against_the_hero()
    {
        var h = TestConfig.Hero;
        Assert.True(h.IsValidLoadout(null));
        Assert.True(h.IsValidLoadout(new[] { "vinyl", "slap" }));
        Assert.False(h.IsValidLoadout(new[] { "vinyl", "removed_skill" }));
        Assert.False(h.IsValidLoadout(new[] { "vinyl", "vinyl" }));
        Assert.True(h.HasUltimate(null) && h.HasUltimate("encore"));
        Assert.False(h.HasUltimate("old_ultimate"));
    }

    [Fact]
    public void Unknown_key_throws()
    {
        string json = TestConfig.HeroJson.Replace("\"maxHp\": 90,", "\"maxHp\": 90, \"bogus\": 1,");
        Assert.ThrowsAny<Exception>(() => HeroDef.Parse(json, TestConfig.Run));
    }

    [Fact]
    public void Skill_cooldown_reaching_zero_at_max_level_throws()
    {
        string json = TestConfig.HeroJson.Replace("\"cooldown\": -0.5", "\"cooldown\": -1");
        var e = Assert.Throws<FormatException>(() => HeroDef.Parse(json, TestConfig.Run));
        Assert.Contains("speaker_stack", e.Message);
    }

    [Fact]
    public void Ring_knockback_not_below_its_speed_throws()
    {
        string json = TestConfig.HeroJson.Replace("\"speed\": 14, \"knockback\": 9", "\"speed\": 14, \"knockback\": 14");
        var e = Assert.Throws<FormatException>(() => HeroDef.Parse(json, TestConfig.Run));
        Assert.Contains("low_end", e.Message);
    }

    [Fact]
    public void Shock_knockback_not_below_shock_speed_throws()
    {
        string json = TestConfig.HeroJson.Replace("\"shockKnockback\": 12", "\"shockKnockback\": 20");
        var e = Assert.Throws<FormatException>(() => HeroDef.Parse(json, TestConfig.Run));
        Assert.Contains("shockKnockback", e.Message);
    }

    [Fact]
    public void Default_loadout_with_an_unknown_skill_throws()
    {
        string json = TestConfig.HeroJson.Replace("\"loadout\": [\"cowbell\",", "\"loadout\": [\"bogus\",");
        Assert.Throws<FormatException>(() => HeroDef.Parse(json, TestConfig.Run));
    }

    [Fact]
    public void Projectile_count_above_the_per_cast_cap_throws()
    {
        string json = TestConfig.HeroJson.Replace("\"cooldown\": 2, \"count\": 1,", $"\"cooldown\": 2, \"count\": {RunConfig.MaxProjectilesPerCast + 1},");
        Assert.Throws<FormatException>(() => HeroDef.Parse(json, TestConfig.Run));
    }

    [Fact]
    public void Skill_stats_grow_linearly_and_count_floors()
    {
        var def = new SkillDef
        {
            Id = "x", Name = "X", Kind = SkillKind.Projectile, Tags = new[] { SkillTag.Projectile },
            Base = new SkillStats { Damage = 6, Cooldown = 0.8f, Count = 1, Width = 1 },
            PerLevel = new SkillStats { Damage = 1.5f, Cooldown = -0.03f, Count = 0.5f, Width = 0.25f },
        };

        var l3 = def.StatsAt(3);
        Assert.Equal(9f, l3.Damage, 4);
        Assert.Equal(0.74f, l3.Cooldown, 4);
        Assert.Equal(1.5f, l3.Width, 4);
        Assert.Equal(1, def.StatsAt(1).Amount);
        Assert.Equal(1, def.StatsAt(2).Amount);
        Assert.Equal(2, def.StatsAt(3).Amount);
        Assert.Equal(5, def.StatsAt(10).Amount);
    }

    [Fact]
    public void With_expression_copies_stats_instead_of_sharing_them()
    {
        var a = new SkillStats { Damage = 1, Range = 5 };
        var b = a with { Damage = 2 };
        Assert.Equal((1f, 5f), (a.Damage, a.Range));
        Assert.Equal((2f, 5f), (b.Damage, b.Range));
    }
}
