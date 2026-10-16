namespace Vlg.Core.Config;

public sealed record HeroSettings
{
    public required float MaxHp { get; init; }
    public required float Speed { get; init; }
    public required float Radius { get; init; }
    /// <summary>Gems inside this distance start flying to the hero.</summary>
    public required float PickupRadius { get; init; }
    /// <summary>Added to hero + walker radius for contact damage; walkers stop exactly at touching distance.</summary>
    public required float ContactMargin { get; init; }
}
