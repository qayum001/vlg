using System;
using Godot;
using Vlg.Core.Config;

namespace Vlg.Game;

/// <summary>Maps a data preset onto engine settings. Unsupported options on a renderer (SSAO, TAA on Mobile) are ignored by Godot.</summary>
public static class QualityApplier
{
    /// <summary>Particles per hit burst at multiplier 1.</summary>
    const int BurstParticlesAtFull = 16;

    public static void Apply(QualityPreset p, Viewport viewport, Arena arena, EnemyRenderer enemies, VfxSystem vfx)
    {
        viewport.Scaling3DMode = p.Scaling == ScalingMode.Fsr ? Viewport.Scaling3DModeEnum.Fsr : Viewport.Scaling3DModeEnum.Bilinear;
        viewport.Scaling3DScale = p.RenderScale;
        viewport.ScreenSpaceAA = p.AntiAliasing == AntiAliasing.Fxaa ? Viewport.ScreenSpaceAAEnum.Fxaa : Viewport.ScreenSpaceAAEnum.Disabled;
        viewport.UseTaa = p.AntiAliasing == AntiAliasing.Taa;
        viewport.Msaa3D = Viewport.Msaa.Disabled;

        arena.Sun.ShadowEnabled = p.Shadows;
        arena.Sun.DirectionalShadowMode = p.EnemyShadows
            ? DirectionalLight3D.ShadowMode.Parallel2Splits
            : DirectionalLight3D.ShadowMode.Orthogonal;
        RenderingServer.DirectionalShadowAtlasSetSize(p.ShadowSize, true);
        arena.Props.CastShadow = p.Shadows ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off;
        arena.Environment.GlowEnabled = p.Glow;
        arena.Environment.SsaoEnabled = p.Ssao;

        enemies.SetDetail(p.EnemyDetail);
        enemies.SetCastShadows(p.Shadows && p.EnemyShadows);
        vfx.SetBurstAmount((int)MathF.Round(BurstParticlesAtFull * p.ParticleMultiplier));
        vfx.SetLights(p.Lights);
    }
}
