namespace Vlg.Core.Config;

/// <summary>Hero body, same for every hero. Per-hero stats: <see cref="HeroDef"/>.</summary>
public sealed record HeroSettings
{
    public required float Radius { get; init; }
    /// <summary>Added to hero + walker radius for contact damage; walkers stop exactly at touching distance.</summary>
    public required float ContactMargin { get; init; }
}
