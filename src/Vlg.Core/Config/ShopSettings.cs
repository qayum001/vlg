namespace Vlg.Core.Config;

/// <summary>domain.md Shop: offers, prices, rerolls (cost rises within a shop, base grows per loop).</summary>
public sealed record ShopSettings
{
    public required int Size { get; init; }
    public required RarityTable Prices { get; init; }
    /// <summary>Relative chance per offer.</summary>
    public required RarityTable Weights { get; init; }
    /// <summary>Reroll n (0-based) in loop L costs RerollBase + RerollPerLoop * (L - 1) + RerollStep * n.</summary>
    public required float RerollBase { get; init; }
    public required float RerollStep { get; init; }
    public required float RerollPerLoop { get; init; }
}
