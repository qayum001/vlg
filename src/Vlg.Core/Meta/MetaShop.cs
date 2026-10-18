using System;
using Vlg.Core.Config;
using Vlg.Core.Run;

namespace Vlg.Core.Meta;

/// <summary>Hub upgrade rules over a <see cref="MetaSave"/>: costs, buying ranks, run bonuses.</summary>
public sealed class MetaShop
{
    readonly MetaConfig _c;
    public readonly MetaSave Save;

    public MetaShop(MetaConfig config, MetaSave save)
    {
        _c = config;
        Save = save;
    }

    public MetaConfig Config => _c;

    public int Rank(MetaUpgradeDef u) => Math.Min(Save.Rank(u.Id), u.MaxRank);

    /// <summary>Cost of the next rank; -1 when maxed.</summary>
    public int NextCost(MetaUpgradeDef u) => Rank(u) >= u.MaxRank ? -1 : u.Cost(Rank(u));

    public bool CanBuy(MetaUpgradeDef u) => NextCost(u) is var c and >= 0 && Save.MetaCredits + 1e-4f >= c;

    public bool Buy(MetaUpgradeDef u)
    {
        if (!CanBuy(u)) return false;
        Save.MetaCredits -= NextCost(u);
        Save.Ranks[u.Id] = Rank(u) + 1;
        return true;
    }

    public MetaBonuses Bonuses()
    {
        var b = new MetaBonuses();
        foreach (var u in _c.Upgrades)
        {
            float v = Rank(u) * u.PerRank;
            b = u.Kind switch
            {
                MetaKind.ActiveSlots => b with { ActiveSlots = b.ActiveSlots + (int)v },
                MetaKind.UsableSlots => b with { UsableSlots = b.UsableSlots + (int)v },
                MetaKind.Damage => b with { Damage = b.Damage + v },
                MetaKind.MaxHp => b with { MaxHp = b.MaxHp + v },
                MetaKind.Pickup => b with { Pickup = b.Pickup + v },
                MetaKind.StartCredits => b with { StartCredits = b.StartCredits + v },
                _ => b,
            };
        }
        return b;
    }
}
