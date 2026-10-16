using System;
using System.Numerics;

namespace Vlg.Core.Sim;

public static class RandomPoints
{
    /// <summary>Offset uniform by area in the ring [min, max]. Consumes two RNG values (angle, then radius).</summary>
    public static Vector2 InAnnulus(ref XorShift32 rng, float min, float max)
    {
        float a = rng.Range(-MathF.PI, MathF.PI);
        float r = MathF.Sqrt(rng.Range(min * min, max * max));
        return new Vector2(MathF.Sin(a), MathF.Cos(a)) * r;
    }
}
