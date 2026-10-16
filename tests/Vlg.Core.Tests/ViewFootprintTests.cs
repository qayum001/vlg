using System;
using System.Numerics;
using Vlg.Core.Sim;
using Xunit;

namespace Vlg.Core.Tests;

public class ViewFootprintTests
{
    const float Tilt = 40f, Dist = 28f, Fov = 45f, Aspect = 16f / 9f;

    static float Rad(float deg) => deg * MathF.PI / 180f;

    // Screen-center column: rays at tilt ± fov/2 below the horizon, from a camera at height D sin t, D cos t behind.
    static float AheadByTrig => Dist * MathF.Sin(Rad(Tilt)) / MathF.Tan(Rad(Tilt - Fov / 2)) - Dist * MathF.Cos(Rad(Tilt));
    static float BehindByTrig => Dist * MathF.Cos(Rad(Tilt)) - Dist * MathF.Sin(Rad(Tilt)) / MathF.Tan(Rad(Tilt + Fov / 2));

    [Fact]
    public void Top_and_bottom_edges_match_plain_trigonometry()
    {
        var v = ViewFootprint.Compute(Tilt, Dist, Fov, Aspect, 0f);

        Assert.Equal(AheadByTrig, v.DistanceToEdge(CameraMath.Forward(0f)), 2);
        Assert.Equal(BehindByTrig, v.DistanceToEdge(-CameraMath.Forward(0f)), 2);
        Assert.InRange(AheadByTrig, 35f, 36.5f);
        Assert.InRange(BehindByTrig, 11.5f, 12.5f);
    }

    [Fact]
    public void Footprint_turns_with_the_camera_yaw()
    {
        const float yaw = 1.1f;
        var v = ViewFootprint.Compute(Tilt, Dist, Fov, Aspect, yaw);

        Assert.Equal(AheadByTrig, v.DistanceToEdge(CameraMath.Forward(yaw)), 2);
        Assert.Equal(BehindByTrig, v.DistanceToEdge(-CameraMath.Forward(yaw)), 2);
    }

    [Fact]
    public void Wider_screen_sees_farther_to_the_sides()
    {
        var right = CameraMath.Right(0f);
        float narrow = ViewFootprint.Compute(Tilt, Dist, Fov, 4f / 3f, 0f).DistanceToEdge(right);
        float wide = ViewFootprint.Compute(Tilt, Dist, Fov, 21f / 9f, 0f).DistanceToEdge(right);

        Assert.True(wide > narrow * 1.5f, $"narrow {narrow}, wide {wide}");
    }

    [Fact]
    public void Hero_is_inside_and_the_edge_is_finite_in_every_direction()
    {
        var v = ViewFootprint.Compute(Tilt, Dist, Fov, Aspect, 0.3f);
        float far = v.FarthestCorner;
        for (int i = 0; i < 64; i++)
        {
            float a = MathF.Tau * i / 64;
            float d = v.DistanceToEdge(new Vector2(MathF.Sin(a), MathF.Cos(a)));
            Assert.InRange(d, BehindByTrig * 0.9f, far + 1e-3f);
        }
    }

    [Fact]
    public void Tilt_not_below_half_the_fov_throws()
    {
        Assert.Throws<ArgumentException>(() => ViewFootprint.Compute(22.5f, Dist, Fov, Aspect, 0f));
    }
}
