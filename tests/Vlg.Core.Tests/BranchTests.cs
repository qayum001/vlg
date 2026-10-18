using System;
using System.Linq;
using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Run;
using Xunit;

namespace Vlg.Core.Tests;

public class BranchTests
{
    static readonly int[] Levels = { 5, 10 };

    /// <summary>Two options at 5; the first has two options at 10.</summary>
    static readonly SkillDef Tree = new()
    {
        Id = "t", Name = "Tree", Kind = SkillKind.Projectile, Tags = new[] { SkillTag.Projectile },
        Base = new SkillStats { Damage = 10, Cooldown = 2, Count = 1, Speed = 10, Range = 10 },
        PerLevel = new SkillStats { Damage = 1 },
        Branches = new[]
        {
            new BranchDef
            {
                Id = "a", Name = "A", Text = "a", Scale = SkillStats.Filled(1f).With(Stat.Cooldown, 2f), Add = new SkillStats { Pierce = 2 },
                Branches = new[]
                {
                    new BranchDef { Id = "a1", Name = "A1", Text = "a1", Add = new SkillStats { Count = 3 }, Flags = new[] { SkillFlag.Twin } },
                    new BranchDef { Id = "a2", Name = "A2", Text = "a2", Kind = SkillKind.Boomerang, Tags = new[] { SkillTag.Chain } },
                },
            },
            new BranchDef { Id = "b", Name = "B", Text = "b", Kind = SkillKind.BellDrop, Tags = new[] { SkillTag.Area }, Add = new SkillStats { Radius = 2 } },
        },
    };

    static LevelingSettings Leveling(int divisor = 1) => new()
    {
        XpBase = 5, XpPerLevel = 0, LevelDivisor = divisor, MaxSkillLevel = 10, MaxedBonusDamage = 0.02f, BranchLevels = Levels, UltimateLevels = new[] { 10, 20, 30 },
    };

    [Fact]
    public void Resolve_applies_chosen_branches_in_order_from_their_level()
    {
        var path = new[] { 0, 0 };
        var l4 = Tree.Resolve(4, path, Levels);
        Assert.Equal(("Tree", 2f, 0f), (l4.Name, l4.Stats.Cooldown, l4.Stats.Pierce));

        var l5 = Tree.Resolve(5, path, Levels);
        Assert.Equal(("Tree · A", 4f, 2f, 14f), (l5.Name, l5.Stats.Cooldown, l5.Stats.Pierce, l5.Stats.Damage));
        Assert.False(l5.Has(SkillFlag.Twin));

        var l10 = Tree.Resolve(10, path, Levels);
        Assert.Equal(("Tree · A · A1", 4, 19f), (l10.Name, l10.Stats.Amount, l10.Stats.Damage));
        Assert.True(l10.Has(SkillFlag.Twin));

        var other = Tree.Resolve(10, new[] { 0, 1 }, Levels);
        Assert.Equal((SkillKind.Boomerang, SkillTag.Chain), (other.Kind, other.Tags.Single()));
        var bell = Tree.Resolve(7, new[] { 1, -1 }, Levels);
        Assert.Equal((SkillKind.BellDrop, 2f), (bell.Kind, bell.Stats.Radius));
    }

    [Fact]
    public void Options_follow_the_earlier_choice()
    {
        Assert.Equal(2, Tree.OptionsAt(new[] { -1, -1 }, 0).Count);
        Assert.Equal(new[] { "a1", "a2" }, Tree.OptionsAt(new[] { 0, -1 }, 1).Select(b => b.Id));
        Assert.Empty(Tree.OptionsAt(new[] { 1, -1 }, 1));
        Assert.Empty(Tree.OptionsAt(new[] { -1, -1 }, 1));
    }

    [Fact]
    public void Reaching_a_branch_level_queues_a_branch_pick_before_the_next_level_up()
    {
        var p = new HeroProgress(Leveling(), 1, new[] { Tree });
        for (int i = 0; i < 4; i++)
        {
            p.DevGainLevel();
            p.Choose(0); // 2, 3, 4, 5
        }
        Assert.Equal(5, p.SkillLevel(0));
        Assert.True(p.PendingBranch && p.HasPendingPick);
        p.DevGainLevel();
        var options = p.Options();
        Assert.Equal(2, options.Count);
        Assert.All(options, o => Assert.Equal(LevelUpKind.Branch, o.Kind));
        Assert.Equal(new[] { 0, 1 }, options.Select(o => o.Choice));
        Assert.Equal(0, p.PendingBranchPoint);

        int version = p.Version;
        p.Choose(0);
        Assert.Equal(0, p.Path(0)[0]);
        Assert.True(p.Version > version);
        Assert.False(p.PendingBranch);
        Assert.Equal(LevelUpKind.Upgrade, p.Options()[0].Kind); // the waiting level-up comes next
    }

    [Fact]
    public void Second_branch_point_offers_the_chosen_subtree_and_an_empty_one_is_skipped()
    {
        var p = new HeroProgress(Leveling(), 1, new[] { Tree });
        p.DevSetAllSkills(10);
        Assert.Equal(0, p.PendingBranchPoint);
        p.Choose(0); // A
        Assert.Equal(1, p.PendingBranchPoint);
        Assert.Equal(2, p.Options().Count);
        p.Choose(1); // A2
        Assert.Equal(new[] { 0, 1 }, p.Path(0).ToArray());
        Assert.False(p.HasPendingPick);

        var q = new HeroProgress(Leveling(), 1, new[] { Tree });
        q.DevSetAllSkills(10);
        q.Choose(1); // B has no level-10 options
        Assert.False(q.HasPendingPick);
    }

    [Fact]
    public void Skills_without_branches_never_ask()
    {
        var plain = Tree with { Branches = Array.Empty<BranchDef>() };
        var p = new HeroProgress(Leveling(), 1, new[] { plain });
        p.DevSetAllSkills(10);
        Assert.False(p.HasPendingPick);
    }

    [Fact]
    public void Shipped_lowkey_has_three_forms_at_5_and_two_follow_ups_each_at_10()
    {
        foreach (var sk in TestConfig.Hero.Skills)
        {
            Assert.Equal(3, sk.Branches.Length);
            Assert.All(sk.Branches, b => Assert.Equal(2, b.Branches.Length));
        }
    }

    [Fact]
    public void Invalid_stats_in_a_deep_branch_throw_with_the_form_name()
    {
        // Pinball (cowbell → ricochet → pinball): falloff 1 would be invalid.
        string json = TestConfig.HeroJson.Replace("\"scale\": { \"falloff\": 0 }, \"add\": { \"bounces\": 3 }", "\"add\": { \"bounces\": 3, \"falloff\": 0.9 }");
        var e = Assert.Throws<FormatException>(() => HeroDef.Parse(json, TestConfig.Run));
        Assert.Contains("Pinball", e.Message);
    }

    [Fact]
    public void Unknown_stat_key_throws()
    {
        string json = TestConfig.HeroJson.Replace("\"add\": { \"stun\": 0.5 }", "\"add\": { \"stunn\": 0.5 }");
        Assert.ThrowsAny<Exception>(() => HeroDef.Parse(json, TestConfig.Run));
    }

    [Fact]
    public void Branch_pick_in_a_run_changes_how_the_skill_plays()
    {
        var c = TestConfig.Run with { Stages = new[] { new StageDef { Kind = StageKind.Mobs, DurationSec = 1000 } } };
        var sim = new RunSim(c, TestConfig.Hero, TestConfig.Items, 1);
        sim.Progress.DevSetAllSkills(5);
        Assert.Equal(RunPhase.LevelUp, sim.Phase);
        // Cowbell first: option 2 = Cowbell Rain.
        sim.ChooseLevelUp(2);
        while (sim.Phase == RunPhase.LevelUp) sim.ChooseLevelUp(0);
        var rng = new Vlg.Core.Sim.XorShift32(4);
        sim.Enemies.Spawn(new Vector2(4, 0), 1e6f, ref rng);

        for (int i = 0; i < 20 && sim.Strikes.Count == 0; i++) sim.Step(0.05f, Vector2.Zero);

        Assert.Equal(SkillKind.BellDrop, sim.Skills.Form(0)!.Kind);
        Assert.Equal("Cowbell · Cowbell Rain", sim.Skills.Form(0)!.Name);
        Assert.True(sim.Strikes.Count > 0);
    }
}
