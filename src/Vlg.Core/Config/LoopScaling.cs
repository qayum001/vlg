namespace Vlg.Core.Config;

/// <summary>
/// Per-loop difficulty (domain.md Loop scaling). Linear: multiplier = 1 + perLoop * (loop - 1), loop is 1-based.
/// Spawn multiplier scales both spawn rate and the alive cap of a stage.
/// </summary>
public sealed record LoopScaling
{
    public required float HpPerLoop { get; init; }
    public required float DamagePerLoop { get; init; }
    public required float SpawnPerLoop { get; init; }

    public float Hp(int loop) => 1f + HpPerLoop * (loop - 1);
    public float Damage(int loop) => 1f + DamagePerLoop * (loop - 1);
    public float Spawn(int loop) => 1f + SpawnPerLoop * (loop - 1);
}
