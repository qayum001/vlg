using System;
using System.Numerics;

namespace Vlg.Core.Sim;

/// <summary>
/// Camera rig yaw (radians, rotation about world Y, Godot convention). The camera looks along its -Z,
/// so yaw 0 looks toward world -Z. Ground Vector2(x, y) = world (x, z).
/// </summary>
public static class CameraMath
{
    public static Vector2 Forward(float yaw) => new(-MathF.Sin(yaw), -MathF.Cos(yaw));

    public static Vector2 Right(float yaw) => new(MathF.Cos(yaw), -MathF.Sin(yaw));

    /// <summary>input.X = right, input.Y = forward (W = away from camera). Result length &lt;= 1.</summary>
    public static Vector2 MoveToWorld(Vector2 input, float yaw)
    {
        float len2 = input.LengthSquared();
        if (len2 > 1f) input /= MathF.Sqrt(len2);
        return Right(yaw) * input.X + Forward(yaw) * input.Y;
    }
}
