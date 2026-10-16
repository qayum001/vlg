namespace Vlg.Core.Bench;

/// <summary>One ramp step, averaged over its measure phase. Pass = p95 frame time within threshold.</summary>
public sealed record StepResult(
    int Step,
    int Value,
    int Frames,
    float AvgMs,
    float P50Ms,
    float P95Ms,
    float P99Ms,
    float MaxMs,
    float GpuMs,
    float CpuRenderMs,
    float SimMs,
    float DrawCalls,
    float Primitives,
    float Enemies,
    float Projectiles,
    bool Pass);
