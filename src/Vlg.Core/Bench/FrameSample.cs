namespace Vlg.Core.Bench;

/// <summary>Per-frame measurements fed to <see cref="RampController"/>. Times in ms.</summary>
public readonly record struct FrameSample(
    float FrameMs,
    float GpuMs,
    float CpuRenderMs,
    float SimMs,
    int DrawCalls,
    int Primitives,
    int Enemies,
    int Projectiles);
