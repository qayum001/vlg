using System;
using Godot;
using Vlg.Core.Run;

namespace Vlg.Game;

/// <summary>
/// Full-screen tint under the HUD: rising pink in the build-up, flash on the Drop, soft beat pump in the Drop,
/// white flash when an ultimate fires.
/// </summary>
public sealed class BeatOverlay
{
    const float FlashAlpha = 0.35f;
    const float FlashDecay = 3f;

    static readonly Color Pink = new(1f, 0.2f, 0.65f);

    readonly ColorRect _rect;
    float _flash;
    float _white;

    public BeatOverlay(Node parent)
    {
        var layer = new CanvasLayer { Name = "BeatOverlay", Layer = 0 };
        parent.AddChild(layer);
        _rect = new ColorRect { Color = new Color(Pink, 0f), MouseFilter = Control.MouseFilterEnum.Ignore };
        _rect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        layer.AddChild(_rect);
    }

    /// <param name="stepped">The sim stepped this frame (DropStarted / ultimate events are fresh).</param>
    public void Update(BeatClock beat, UltimateSystem ultimate, bool stepped, float dt)
    {
        if (stepped && beat.DropStarted) _flash = FlashAlpha;
        if (stepped && (ultimate.FiredThisStep || ultimate.BeamStartedThisStep)) _white = 0.5f;
        _white = MathF.Max(0f, _white - 2f * dt);
        _flash = MathF.Max(0f, _flash - FlashDecay * dt * _flash - 0.05f * dt);
        float pump = MathF.Exp(-beat.BeatPhase * 6f);
        float a = _flash;
        if (beat.InBuild) a += 0.1f * beat.BuildProgress * pump;
        else if (beat.InDrop) a += 0.05f * pump;
        _rect.Color = new Color(Pink.Lerp(Colors.White, _white / (a + _white + 1e-4f)), a + _white);
    }
}
