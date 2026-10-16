using System;
using System.Numerics;
using Vlg.Core.Sim;
using Xunit;

namespace Vlg.Core.Tests;

public class CameraMathTests
{
    static void Near(Vector2 expected, Vector2 actual) =>
        Assert.True(Vector2.Distance(expected, actual) < 1e-5f, $"expected {expected}, got {actual}");

    [Fact]
    public void Yaw_zero_looks_toward_world_minus_z()
    {
        Near(new Vector2(0, -1), CameraMath.Forward(0));
        Near(new Vector2(1, 0), CameraMath.Right(0));
    }

    [Fact]
    public void Quarter_turn_left_looks_toward_world_minus_x()
    {
        // Positive yaw = counter-clockwise seen from above (Godot rotate_y).
        Near(new Vector2(-1, 0), CameraMath.Forward(MathF.PI / 2));
        Near(new Vector2(0, -1), CameraMath.Right(MathF.PI / 2));
    }

    [Fact]
    public void W_moves_away_from_camera_for_any_yaw()
    {
        for (float yaw = -3f; yaw < 3f; yaw += 0.5f)
            Near(CameraMath.Forward(yaw), CameraMath.MoveToWorld(new Vector2(0, 1), yaw));
    }

    [Fact]
    public void Diagonal_input_is_not_faster()
    {
        Assert.InRange(CameraMath.MoveToWorld(new Vector2(1, 1), 0.7f).Length(), 0.999f, 1.001f);
        Assert.InRange(CameraMath.MoveToWorld(new Vector2(0.3f, 0), 0.7f).Length(), 0.299f, 0.301f);
    }
}
