using System;
using System.Linq;
using Vlg.Core.Config;
using Vlg.Core.Run;
using Xunit;

namespace Vlg.Core.Tests;

public class HeroProgressTests
{
    static LevelingSettings Settings(int divisor = 3, int maxSkill = 10) => new()
    {
        XpBase = 5, XpPerLevel = 5, LevelDivisor = divisor, MaxSkillLevel = maxSkill, MaxedBonusDamage = 0.02f,
    };

    static (LevelUpKind, int, int)[] Shape(HeroProgress p) =>
        p.Options().Select(o => (o.Kind, o.Slot, o.NewLevel)).ToArray();

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 2)]
    [InlineData(12, 4)]
    [InlineData(13, 5)]
    [InlineData(27, 9)]
    [InlineData(28, 10)]
    [InlineData(40, 10)]
    public void Skill_cap_is_ceil_of_hero_level_over_three_up_to_max(int heroLevel, int cap)
    {
        Assert.Equal(cap, LevelRule.SkillCap(heroLevel, 3, 10));
    }

    [Fact]
    public void Run_starts_at_level_one_with_first_skill_free()
    {
        var p = new HeroProgress(Settings(), 4);

        Assert.Equal(1, p.Level);
        Assert.Equal(new[] { 1, 0, 0, 0 }, Enumerable.Range(0, 4).Select(p.SkillLevel));
        Assert.Equal(0, p.PendingLevelUps);
        Assert.Empty(p.Options());
    }

    [Fact]
    public void Xp_curve_levels_up_and_keeps_the_remainder()
    {
        var p = new HeroProgress(Settings(), 4);

        p.AddXp(4);
        Assert.Equal(1, p.Level);
        p.AddXp(3); // 7: level 2 at 5, 2 left of the 10 needed for level 3
        Assert.Equal(2, p.Level);
        Assert.Equal(2f, p.Xp);
        Assert.Equal(10f, p.XpToNext);
        Assert.Equal(1, p.PendingLevelUps);
    }

    [Fact]
    public void One_big_pickup_gives_several_pending_level_ups()
    {
        var p = new HeroProgress(Settings(), 4);

        p.AddXp(5 + 10 + 15);

        Assert.Equal(4, p.Level);
        Assert.Equal(3, p.PendingLevelUps);
        Assert.Equal(0f, p.Xp);
    }

    [Fact]
    public void Multi_level_jump_lets_every_pending_pick_use_the_final_cap()
    {
        var p = new HeroProgress(Settings(), 4);
        p.AddXp(5 + 10 + 15); // level 1 -> 4 at once, cap 2

        Assert.Contains((LevelUpKind.Upgrade, 0, 2), Shape(p));
        p.Choose(0); // slot 0 -> 2
        Assert.Contains((LevelUpKind.Unlock, 1, 1), Shape(p));
        Assert.DoesNotContain(p.Options(), o => o.Slot == 0);
        Assert.Equal(2, p.PendingLevelUps);
    }

    [Fact]
    public void Hero_levels_two_and_three_can_only_unlock()
    {
        var p = new HeroProgress(Settings(), 4);
        p.AddXp(5);

        Assert.Equal(new[] { (LevelUpKind.Unlock, 1, 1), (LevelUpKind.Unlock, 2, 1), (LevelUpKind.Unlock, 3, 1) }, Shape(p));
        p.Choose(0);
        p.AddXp(10);
        Assert.Equal(3, p.Level);
        Assert.Equal(new[] { (LevelUpKind.Unlock, 2, 1), (LevelUpKind.Unlock, 3, 1) }, Shape(p));
    }

    [Fact]
    public void Hero_level_four_allows_upgrades_to_two_but_not_past_the_cap()
    {
        var p = new HeroProgress(Settings(), 4);
        p.AddXp(5);
        p.Choose(0); // slot 1
        p.AddXp(10);
        p.Choose(0); // slot 2
        p.AddXp(15);

        Assert.Equal(4, p.Level);
        Assert.Equal(new[]
        {
            (LevelUpKind.Upgrade, 0, 2), (LevelUpKind.Upgrade, 1, 2), (LevelUpKind.Upgrade, 2, 2), (LevelUpKind.Unlock, 3, 1),
        }, Shape(p));

        p.Choose(0); // slot 0 -> 2
        p.AddXp(20);
        Assert.Equal(5, p.Level);
        Assert.DoesNotContain(p.Options(), o => o.Slot == 0);
    }

    [Fact]
    public void Nothing_to_unlock_or_raise_offers_a_damage_bonus()
    {
        // Divisor 10: cap stays 1 until hero level 10.
        var p = new HeroProgress(Settings(divisor: 10), 2);
        p.AddXp(5);
        p.Choose(0); // unlock slot 1
        p.AddXp(10);

        Assert.Equal(new[] { (LevelUpKind.Bonus, -1, 0) }, Shape(p));
        p.Choose(0);
        Assert.Equal(1, p.BonusCount);
        Assert.Equal(1.02f, p.DamageMultiplier, 5);
        Assert.Equal(new[] { 1, 1 }, new[] { p.SkillLevel(0), p.SkillLevel(1) });
    }

    [Fact]
    public void Max_skill_level_is_never_exceeded()
    {
        var p = new HeroProgress(Settings(divisor: 1, maxSkill: 2), 1);
        p.AddXp(5);
        Assert.Equal(new[] { (LevelUpKind.Upgrade, 0, 2) }, Shape(p));
        p.Choose(0);
        p.AddXp(10);

        Assert.Equal(new[] { (LevelUpKind.Bonus, -1, 0) }, Shape(p));
    }

    [Fact]
    public void Choose_out_of_range_or_with_nothing_pending_throws()
    {
        var p = new HeroProgress(Settings(), 4);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.Choose(0));
        p.AddXp(5);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.Choose(3));
        Assert.Equal(1, p.PendingLevelUps);
    }

    [Fact]
    public void Dev_level_gain_adds_one_pending_level_and_keeps_xp()
    {
        var p = new HeroProgress(Settings(), 4);
        p.AddXp(3);
        p.DevGainLevel();

        Assert.Equal(2, p.Level);
        Assert.Equal(1, p.PendingLevelUps);
        Assert.Equal(3f, p.Xp);
    }

    [Fact]
    public void Dev_set_all_skills_ignores_the_cap_and_checks_the_range()
    {
        var p = new HeroProgress(Settings(), 3);
        p.DevSetAllSkills(5);
        Assert.Equal(new[] { 5, 5, 5 }, new[] { p.SkillLevel(0), p.SkillLevel(1), p.SkillLevel(2) });
        Assert.Throws<ArgumentOutOfRangeException>(() => p.DevSetAllSkills(11));
    }
}
