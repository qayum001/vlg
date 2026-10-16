using System;
using System.Numerics;

namespace Vlg.Core.Bench;

/// <summary>
/// Scripted player route and camera yaw for repeatable benchmark runs. Figure-8 around the origin,
/// slower than enemies so the crowd keeps up; yaw turns so every view angle is covered.
/// </summary>
public static class BenchPath
{
    public const float Radius = 14f;
    public const float PeriodSec = 40f;
    public const float YawRadPerSec = 0.25f;

    public static Vector2 PlayerAt(float t)
    {
        float w = 2f * MathF.PI * t / PeriodSec;
        return new Vector2(Radius * MathF.Sin(w), 0.5f * Radius * MathF.Sin(2f * w));
    }

    public static float YawAt(float t) => t * YawRadPerSec;
}
