namespace Vlg.Core.Config;

public sealed record QualityPreset
{
    public required float RenderScale { get; init; }
    public required ScalingMode Scaling { get; init; }
    public required AntiAliasing AntiAliasing { get; init; }
    public required bool Shadows { get; init; }
    public required bool EnemyShadows { get; init; }
    public required int ShadowSize { get; init; }
    public required bool Glow { get; init; }
    public required bool Ssao { get; init; }
    public required float ParticleMultiplier { get; init; }
    public required int MaxEnemies { get; init; }
    public required int MaxProjectiles { get; init; }
    public required int BurstsPerFrame { get; init; }
    public required int AuraParticles { get; init; }
    public required int Lights { get; init; }
    /// <summary>Sphere subdivision level of the placeholder enemy mesh (0 = coarsest).</summary>
    public required int EnemyDetail { get; init; }
}
