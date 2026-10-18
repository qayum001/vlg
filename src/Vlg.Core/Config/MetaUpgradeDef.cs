namespace Vlg.Core.Config;

/// <summary>One meta upgrade sold in the hub. Rank r (0-based) costs CostBase + CostStep * r meta credits.</summary>
public sealed record MetaUpgradeDef
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Text { get; init; }
    public required MetaKind Kind { get; init; }
    /// <summary>Effect per rank (slots: 1).</summary>
    public required float PerRank { get; init; }
    public required int MaxRank { get; init; }
    public required int CostBase { get; init; }
    public required int CostStep { get; init; }

    public int Cost(int rank) => CostBase + CostStep * rank;
}
