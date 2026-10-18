namespace Vlg.Core.Run;

/// <summary>What owned meta upgrades give a run. Default = none.</summary>
public readonly record struct MetaBonuses
{
    public int ActiveSlots { get; init; }
    public int UsableSlots { get; init; }
    public float Damage { get; init; }
    public float MaxHp { get; init; }
    public float Pickup { get; init; }
    public float StartCredits { get; init; }
}
