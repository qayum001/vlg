using System;
using Godot;

namespace Vlg.Game;

/// <summary>
/// concept.md camera: angled top-down, follows the player, free yaw orbit, fixed tilt and distance.
/// Hierarchy: Rig (yaw, follows) → Pitch → Camera at +Z distance, looking back at the rig.
/// </summary>
public sealed class OrbitCamera
{
    readonly float _followRate;

    public Node3D Rig { get; }
    public Camera3D Camera { get; }
    /// <summary>Radians, rotation about world Y. Matches Vlg.Core CameraMath.</summary>
    public float Yaw { get; set; }

    /// <param name="tiltDeg">Angle below the horizon.</param>
    /// <param name="fovDeg">Vertical field of view.</param>
    public OrbitCamera(Node3D parent, float tiltDeg, float distance, float fovDeg, float followRate)
    {
        _followRate = followRate;
        Rig = new Node3D { Name = "CameraRig" };
        var pitch = new Node3D { Name = "Pitch", RotationDegrees = new Vector3(-tiltDeg, 0, 0) };
        Camera = new Camera3D { Name = "Camera", Position = new Vector3(0, 0, distance), Fov = fovDeg, Far = 300f, Current = true };
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
        Rig.Position = Rig.Position.Lerp(target, 1f - MathF.Exp(-_followRate * dt));
        Rig.Rotation = new Vector3(0, Yaw, 0);
    }
}
