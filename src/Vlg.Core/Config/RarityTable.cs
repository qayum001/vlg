namespace Vlg.Core.Config;

/// <summary>One number per rarity (prices, shop weights).</summary>
public sealed record RarityTable
{
    public required float Common { get; init; }
    public required float Uncommon { get; init; }
    public required float Rare { get; init; }

    public float this[Rarity r] => r switch
    {
        Rarity.Common => Common,
        Rarity.Uncommon => Uncommon,
        _ => Rare,
    };
}
