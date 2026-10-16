using System;
using System.Numerics;

namespace Vlg.Core.Sim;

/// <summary>
/// Ground area the orbit camera sees: 4 corners where the screen-corner rays hit y = 0, relative to the
/// camera target (hero). Convex, contains the origin. Ground Vector2(x, y) = world (x, z); yaw as <see cref="CameraMath"/>.
/// </summary>
public readonly struct ViewFootprint
{
    /// <summary>Bottom-left, bottom-right, top-right, top-left (screen space).</summary>
    readonly Vector2 _c0, _c1, _c2, _c3;

    ViewFootprint(Vector2 c0, Vector2 c1, Vector2 c2, Vector2 c3) => (_c0, _c1, _c2, _c3) = (c0, c1, c2, c3);

    /// <param name="tiltDeg">Angle below the horizon. Must exceed half the vertical FOV, else the top edge sees the horizon.</param>
    /// <param name="aspect">Viewport width / height (vertical FOV is kept).</param>
    public static ViewFootprint Compute(float tiltDeg, float distance, float fovDeg, float aspect, float yaw)
    {
        float tilt = tiltDeg * MathF.PI / 180f;
        float tanV = MathF.Tan(fovDeg * MathF.PI / 360f);
        if (tiltDeg <= fovDeg / 2f) throw new ArgumentException($"tilt {tiltDeg} must exceed half the FOV {fovDeg / 2f}: top edge would see the horizon");
        float tanH = tanV * aspect;
        // Camera at yaw 0: behind the target on +z, looking toward -z and down.
        float h = distance * MathF.Sin(tilt), back = distance * MathF.Cos(tilt);
        var forward = new Vector3(0f, -MathF.Sin(tilt), -MathF.Cos(tilt));
        var up = new Vector3(0f, MathF.Cos(tilt), -MathF.Sin(tilt));
        var right = Vector3.UnitX;

        Vector2 Hit(float sx, float sy)
        {
            var d = forward + up * (sy * tanV) + right * (sx * tanH);
            float t = h / -d.Y;
            var p = new Vector2(d.X * t, back + d.Z * t);
            // Rotation about world Y by yaw (Godot): x' = x cos + z sin, z' = -x sin + z cos.
            float c = MathF.Cos(yaw), s = MathF.Sin(yaw);
            return new Vector2(p.X * c + p.Y * s, -p.X * s + p.Y * c);
        }

        return new ViewFootprint(Hit(-1, -1), Hit(1, -1), Hit(1, 1), Hit(-1, 1));
    }

    public float FarthestCorner => MathF.Max(MathF.Max(_c0.Length(), _c1.Length()), MathF.Max(_c2.Length(), _c3.Length()));

    /// <summary>Distance from the target along unit <paramref name="dir"/> to the footprint edge.</summary>
    public float DistanceToEdge(Vector2 dir)
    {
        float best = float.MaxValue;
        Edge(_c0, _c1, dir, ref best);
        Edge(_c1, _c2, dir, ref best);
        Edge(_c2, _c3, dir, ref best);
        Edge(_c3, _c0, dir, ref best);
        return best;
    }

    /// <summary>Ray origin→dir against segment p→q (2D cross products).</summary>
    static void Edge(Vector2 p, Vector2 q, Vector2 dir, ref float best)
    {
        var e = q - p;
        float denom = Cross(dir, e);
        if (MathF.Abs(denom) < 1e-9f) return;
        float t = Cross(p, e) / denom;
        float s = Cross(p, dir) / denom;
        if (t > 0f && s >= 0f && s <= 1f && t < best) best = t;
    }

    static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
}
