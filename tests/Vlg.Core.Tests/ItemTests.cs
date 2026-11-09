using System;
using System.Linq;
using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Run;
using Vlg.Core.Sim;
using Xunit;

namespace Vlg.Core.Tests;

/// <summary>Item pool data, inventory rules, shop and chest.</summary>
public class ItemTests
{
    static ItemsConfig Items => TestConfig.Items;
    static int Id(string id) => Items.IndexOf(id);

    static readonly int[] Branches = { 5, 10 };

    static SkillForm Form(params SkillTag[] tags) => new()
    {
        Kind = SkillKind.Ring, Tags = tags, Flags = 0, Name = "F",
        Stats = new SkillStats { Damage = 10, Cooldown = 2, Radius = 4, Bounces = 1 },
    };

    [Fact]
    public void Shipped_items_json_has_the_approved_pool()
    {
        var c = Items;
        Assert.Equal(25, c.Items.Length);
        Assert.Equal(new[] { 8, 4, 4, 9 }, new[] { ItemType.Modifier, ItemType.Active, ItemType.Usable, ItemType.Passive }
            .Select(t => c.Items.Count(i => i.Type == t)));
        Assert.Equal(new[] { 9, 8, 8 }, new[] { Rarity.Common, Rarity.Uncommon, Rarity.Rare }.Select(r => c.Items.Count(i => i.Rarity == r)));
        Assert.Equal((10, 3, 2, 2, 3), (c.Shop.Size, c.ChestOptions, c.ActiveSlots, c.UsableSlots, c.MaxSockets));
    }

    [Fact]
    public void Bad_item_data_throws()
    {
        Assert.ThrowsAny<Exception>(() => ItemsConfig.Parse(TestConfig.ItemsJson.Replace("\"chestOptions\": 3,", "\"chestOptions\": 3, \"bogus\": 1,")));
        // Tesla chain without a jump radius fails the shared skill rules.
        var e = Assert.Throws<FormatException>(() => ItemsConfig.Parse(TestConfig.ItemsJson.Replace("\"range\": 9, \"radius\": 5", "\"range\": 9")));
        Assert.Contains("tesla_backpack", e.Message);
        Assert.Throws<FormatException>(() => ItemsConfig.Parse(TestConfig.ItemsJson.Replace("\"armor\": 0.08", "\"armor\": 1")));
    }

    [Fact]
    public void Credits_spend_only_what_is_there()
    {
        var inv = new Inventory(Items, 2);
        inv.AddCredits(20);
        Assert.False(inv.Spend(25));
        Assert.True(inv.Spend(15));
        Assert.Equal(5f, inv.Credits, 4);
    }

    [Fact]
    public void Active_and_usable_need_a_free_slot_and_one_copy_each()
    {
        var inv = new Inventory(Items, 2);
        inv.Grant(Id("toy_drone"));
        Assert.False(inv.CanTake(Id("toy_drone")));
        inv.Grant(Id("haunted_lantern"));
        Assert.False(inv.CanTake(Id("old_mortar"))); // 2 slots
        Assert.True(inv.CanTake(Id("whetstone")));
        inv.Grant(Id("whetstone"));
        inv.Grant(Id("whetstone"));
        Assert.Equal(2, inv.PassiveCount(Id("whetstone")));
        Assert.Throws<InvalidOperationException>(() => inv.Grant(Id("old_mortar")));
    }

    [Fact]
    public void Passives_stack_add_and_multiply()
    {
        var inv = new Inventory(Items, 1);
        inv.Grant(Id("whetstone"));
        inv.Grant(Id("whetstone"));
        inv.Grant(Id("knights_pauldron"));
        inv.Grant(Id("knights_pauldron"));
        inv.Grant(Id("clockwork_heart"));
        inv.Grant(Id("grim_ledger"));
        var t = inv.Totals;
        Assert.Equal(0.16f, t.Damage, 4);
        Assert.Equal(0.92f * 0.92f, t.DamageTaken, 4);
        Assert.Equal(0.92f, t.CooldownMult, 4);
        Assert.Equal(0.02f, inv.LedgerBonus(450), 4); // two full sets of 200
    }

    [Fact]
    public void Sockets_open_with_unlock_and_branch_levels()
    {
        var inv = new Inventory(Items, 1);
        Assert.Equal(new[] { 0, 1, 1, 2, 2, 3 }, new[] { 0, 1, 4, 5, 9, 10 }.Select(l => inv.SocketCount(l, Branches)));
    }

    [Fact]
    public void Modifier_fits_by_tag_once_per_skill_within_free_sockets()
    {
        var inv = new Inventory(Items, 2);
        int shard = Id("bell_shard");
        inv.Grant(shard);
        inv.Grant(shard);
        var area = Form(SkillTag.Area);
        Assert.False(inv.CanSocket(shard, 0, Form(SkillTag.Projectile), 1));
        Assert.False(inv.CanSocket(shard, 0, null, 1));
        inv.Socket(0, 0, area, 2);
        Assert.False(inv.CanSocket(shard, 0, area, 2)); // one copy per skill
        Assert.True(inv.CanSocket(shard, 1, area, 1));
        inv.Unsocket(0, 0);
        Assert.Equal(2, inv.Stash.Count);
    }

    [Fact]
    public void Socketed_modifiers_change_the_form_and_chain_bonus_needs_the_chain_tag()
    {
        var inv = new Inventory(Items, 2);
        inv.Grant(Id("bell_shard"));
        inv.Socket(0, 0, Form(SkillTag.Area), 1);
        Assert.Equal(5f, inv.Apply(0, Form(SkillTag.Area)).Stats.Radius, 4);

        inv.Grant(Id("copper_coil"));
        inv.Socket(0, 1, Form(SkillTag.Projectile), 1);
        Assert.Equal(2f, inv.Apply(1, Form(SkillTag.Projectile)).Stats.Bounces, 4);
        Assert.Equal(3f, inv.Apply(1, Form(SkillTag.Projectile, SkillTag.Chain)).Stats.Bounces, 4);
    }

    [Fact]
    public void Revalidate_moves_modifiers_that_stop_fitting()
    {
        var inv = new Inventory(Items, 1);
        inv.Grant(Id("bell_shard"));
        inv.Socket(0, 0, Form(SkillTag.Area), 1);
        Assert.Single(inv.WouldUnfit(0, new[] { SkillTag.Projectile }));
        Assert.Equal(0, inv.Revalidate(0, Form(SkillTag.Zone), 1));
        Assert.Equal(1, inv.Revalidate(0, Form(SkillTag.Projectile), 1));
        Assert.Empty(inv.Sockets(0));
        Assert.Single(inv.Stash);
    }

    [Fact]
    public void Shop_rolls_distinct_offers_and_skips_owned_actives()
    {
        var inv = new Inventory(Items, 1);
        inv.Grant(Id("toy_drone"));
        var shop = new Shop(Items, 3);
        for (int round = 0; round < 30; round++)
        {
            shop.Open(1, inv);
            Assert.Equal(Items.Shop.Size, shop.Offers.Count);
            Assert.Equal(shop.Offers.Count, shop.Offers.Select(o => o.Item).Distinct().Count());
            Assert.DoesNotContain(shop.Offers, o => o.Item == Id("toy_drone"));
            Assert.All(shop.Offers, o => Assert.Equal(Items.Shop.Prices[Items.Items[o.Item].Rarity], o.Price));
        }
    }

    [Fact]
    public void Reroll_costs_more_each_time_and_more_in_later_loops()
    {
        var inv = new Inventory(Items, 1);
        var shop = new Shop(Items, 3);
        shop.Open(3, inv);
        float first = shop.RerollCost;
        Assert.Equal(5f + 2f * 2, first);
        Assert.False(shop.Reroll(inv));
        inv.AddCredits(100);
        Assert.True(shop.Reroll(inv));
        Assert.Equal(100f - first, inv.Credits, 4);
        Assert.Equal(first + 5f, shop.RerollCost);
        shop.Open(3, inv);
        Assert.Equal(first, shop.RerollCost);
    }

    [Fact]
    public void Buying_spends_grants_and_marks_sold()
    {
        var inv = new Inventory(Items, 1);
        var shop = new Shop(Items, 9);
        shop.Open(1, inv);
        var o = shop.Offers[0];
        Assert.False(shop.Buy(0, inv));
        inv.AddCredits(o.Price);
        Assert.True(shop.Buy(0, inv));
        Assert.True(inv.Owns(o.Item));
        Assert.Equal(0f, inv.Credits, 4);
        Assert.True(shop.Offers[0].Sold);
        inv.AddCredits(1000);
        Assert.False(shop.Buy(0, inv));
    }

    [Fact]
    public void Chest_offers_distinct_rares_it_can_give()
    {
        var inv = new Inventory(Items, 1);
        inv.Grant(Id("old_mortar"));
        var shop = new Shop(Items, 5);
        for (int i = 0; i < 20; i++)
        {
            var options = shop.ChestOptions(inv);
            Assert.Equal(3, options.Count);
            Assert.Equal(3, options.Distinct().Count());
            Assert.All(options, k => Assert.Equal(Rarity.Rare, Items.Items[k].Rarity));
            Assert.DoesNotContain(Id("old_mortar"), options);
        }
    }
}
