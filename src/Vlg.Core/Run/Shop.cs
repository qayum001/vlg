using System.Collections.Generic;
using Vlg.Core.Config;
using Vlg.Core.Sim;

namespace Vlg.Core.Run;

/// <summary>
/// Shop and chest offers (domain.md Shop and chest). A shop rolls Size distinct items by rarity weight, skipping
/// actives/usables the player already owns or has no slot for; rerolls cost more each time within a shop and the
/// base grows per loop. A chest offers distinct rare items under the same skip rule.
/// </summary>
public sealed class Shop
{
    readonly ItemsConfig _c;
    readonly List<ShopOffer> _offers = new();
    readonly List<int> _candidates = new();
    XorShift32 _rng;

    public int Loop { get; private set; } = 1;
    public int Rerolls { get; private set; }
    public IReadOnlyList<ShopOffer> Offers => _offers;

    public Shop(ItemsConfig items, uint seed)
    {
        _c = items;
        _rng = new XorShift32(seed);
    }

    public float RerollCost
    {
        get
        {
            var s = _c.Shop;
            return s.RerollBase + s.RerollPerLoop * (Loop - 1) + s.RerollStep * Rerolls;
        }
    }

    public void Open(int loop, Inventory inv)
    {
        Loop = loop;
        Rerolls = 0;
        Roll(inv);
    }

    public bool Reroll(Inventory inv)
    {
        if (!inv.Spend(RerollCost)) return false;
        Rerolls++;
        Roll(inv);
        return true;
    }

    /// <summary>False = sold, can't take it (slots) or too expensive.</summary>
    public bool Buy(int offer, Inventory inv)
    {
        var o = _offers[offer];
        if (o.Sold || !inv.CanTake(o.Item) || o.Price > inv.Credits + 1e-4f) return false;
        inv.Spend(o.Price);
        inv.Grant(o.Item);
        _offers[offer] = o with { Sold = true };
        return true;
    }

    /// <summary>Distinct rare items for a chest; fewer when the pool runs out.</summary>
    public List<int> ChestOptions(Inventory inv)
    {
        var result = new List<int>();
        _candidates.Clear();
        for (int i = 0; i < _c.Items.Length; i++)
            if (_c.Items[i].Rarity == Rarity.Rare && inv.CanTake(i)) _candidates.Add(i);
        while (result.Count < _c.ChestOptions && _candidates.Count > 0)
        {
            int k = _rng.Below(_candidates.Count);
            result.Add(_candidates[k]);
            _candidates.RemoveAt(k);
        }
        return result;
    }

    void Roll(Inventory inv)
    {
        _offers.Clear();
        var w = _c.Shop.Weights;
        float total = w.Common + w.Uncommon + w.Rare;
        for (int n = 0; n < _c.Shop.Size; n++)
        {
            float r = _rng.NextFloat() * total;
            var rarity = r < w.Common ? Rarity.Common : r < w.Common + w.Uncommon ? Rarity.Uncommon : Rarity.Rare;
            int item = Pick(rarity, inv);
            if (item < 0) break;
            _offers.Add(new ShopOffer(item, _c.Shop.Prices[_c.Items[item].Rarity], false));
        }
    }

    /// <summary>Random eligible item of that rarity not offered yet; any rarity if none left. -1 = pool empty.</summary>
    int Pick(Rarity rarity, Inventory inv)
    {
        for (int pass = 0; pass < 2; pass++)
        {
            _candidates.Clear();
            for (int i = 0; i < _c.Items.Length; i++)
            {
                if (pass == 0 && _c.Items[i].Rarity != rarity) continue;
                if (!inv.CanTake(i) || Offered(i)) continue;
                _candidates.Add(i);
            }
            if (_candidates.Count > 0) return _candidates[_rng.Below(_candidates.Count)];
        }
        return -1;
    }

    bool Offered(int item)
    {
        foreach (var o in _offers)
            if (o.Item == item) return true;
        return false;
    }
}
