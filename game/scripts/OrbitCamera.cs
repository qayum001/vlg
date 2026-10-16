using System;
using Godot;

namespace Vlg.Game;

/// <summary>
/// concept.md camera: angled top-down, follows the player, free yaw orbit, fixed tilt and distance.
/// Hierarchy: Rig (yaw, follows) → Pitch → Camera at +Z distance, looking back at the rig.
/// </summary>
public sealed class OrbitCamera
{
    // TODO: move to data once the real camera is tuned; these are stress-test values.
    const float PitchDeg = -55f;
    const float Distance = 22f;
    const float FovDeg = 45f;
    const float FollowRate = 10f;

    public Node3D Rig { get; }
    public Camera3D Camera { get; }
    /// <summary>Radians, rotation about world Y. Matches Vlg.Core CameraMath.</summary>
    public float Yaw { get; set; }

    public OrbitCamera(Node3D parent)
    {
        Rig = new Node3D { Name = "CameraRig" };
        var pitch = new Node3D { Name = "Pitch", RotationDegrees = new Vector3(PitchDeg, 0, 0) };
        Camera = new Camera3D { Name = "Camera", Position = new Vector3(0, 0, Distance), Fov = FovDeg, Far = 300f, Current = true };
        parent.AddChild(Rig);
        Rig.AddChild(pitch);
        pitch.AddChild(Camera);
    }

    public void Snap(Vector3 target)
    {
        Rig.Position = target;
        Rig.Rotation = new Vector3(0, Yaw, 0);
    }

    public void Update(float dt, Vector3 target)
    {
        Rig.Position = Rig.Position.Lerp(target, 1f - MathF.Exp(-FollowRate * dt));
        Rig.Rotation = new Vector3(0, Yaw, 0);
    }
}
