using System;
using Vlg.Core.Config;

namespace Vlg.Core.Bench;

/// <summary>Scene load for one frame: ramped value for the bench mode, preset values for the rest.</summary>
public readonly record struct BenchLoad(int Enemies, int Projectiles, int OverdrawLayers, int AuraParticles)
{
    public static RampSettings RampFor(BenchMode mode, BenchSettings bench) => mode switch
    {
        BenchMode.Enemies => bench.Enemies,
        BenchMode.Overdraw => bench.Overdraw,
        BenchMode.Particles => bench.Particles,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    public static BenchLoad For(BenchMode mode, int value, BenchSettings bench, QualityPreset preset)
    {
        int enemies = mode == BenchMode.Enemies ? value : bench.FixedEnemies;
        return new BenchLoad(
            enemies,
            (int)(enemies * bench.ProjectilesPerEnemy),
            mode == BenchMode.Overdraw ? value : 0,
            mode == BenchMode.Particles ? value : preset.AuraParticles);
    }
}
