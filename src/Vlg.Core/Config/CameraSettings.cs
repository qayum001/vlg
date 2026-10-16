namespace Vlg.Core.Config;

/// <summary>Run camera (concept.md: fixed tilt and distance, free yaw orbit). Also drives off-screen spawning.</summary>
public sealed record CameraSettings
{
    /// <summary>Angle below the horizon, degrees.</summary>
    public required float TiltDeg { get; init; }
    /// <summary>Camera distance from the hero, meters.</summary>
    public required float Distance { get; init; }
    /// <summary>Vertical field of view, degrees (Godot keeps height, width follows the aspect).</summary>
    public required float FovDeg { get; init; }
    /// <summary>Exponential follow rate, 1/s.</summary>
    public required float FollowRate { get; init; }
    /// <summary>Directional shadow max distance from the camera, meters.</summary>
    public required float ShadowDistance { get; init; }
}
