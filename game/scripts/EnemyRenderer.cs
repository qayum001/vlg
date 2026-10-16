using System;
using Godot;
using Vlg.Core.Sim;

namespace Vlg.Game;

/// <summary>All enemies in one MultiMesh draw (per pass), animated by enemy_vat.gdshader.</summary>
public sealed class EnemyRenderer
{
    /// <summary>12 floats transform (3x4 rows) + 4 floats INSTANCE_CUSTOM.</summary>
    const int Stride = 16;
    /// <summary>Instance capacity grows/shrinks in steps; the whole buffer is uploaded every frame.</summary>
    const int CapacityStep = 512;

    /// <summary>Large fixed AABB: no per-frame AABB rebuild from the buffer, never culled.</summary>
    public static readonly Aabb WorldAabb = new(new Vector3(-250, -5, -250), new Vector3(500, 20, 500));

    readonly MultiMesh _multimesh;
    readonly ShaderMaterial _material;
    float[] _buffer = Array.Empty<float>();

    public MultiMeshInstance3D Node { get; }
    public int TrianglesPerEnemy { get; private set; }

    public EnemyRenderer(Node3D parent, Shader shader)
    {
        _material = new ShaderMaterial { Shader = shader };
        _multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseCustomData = true,
            CustomAabb = WorldAabb,
        };
        Node = new MultiMeshInstance3D { Name = "Enemies", Multimesh = _multimesh, MaterialOverride = _material };
        parent.AddChild(Node);
    }

    public void SetDetail(int detail)
    {
        var asset = PlaceholderMeshes.BuildEnemy(detail);
        _multimesh.Mesh = asset.Mesh;
        _material.SetShaderParameter("anim_tex", asset.AnimTexture);
        _material.SetShaderParameter("frame_count", PlaceholderMeshes.AnimFrames);
        TrianglesPerEnemy = asset.Triangles;
    }

    public void SetCastShadows(bool on) =>
        Node.CastShadow = on ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off;

    public void Update(EnemySwarm s)
    {
        int capacity = (Math.Max(s.Count, 1) + CapacityStep - 1) / CapacityStep * CapacityStep;
        if (capacity != _multimesh.InstanceCount)
        {
            _multimesh.InstanceCount = capacity;
            _buffer = new float[capacity * Stride];
        }

        var b = _buffer;
        for (int i = 0, o = 0; i < s.Count; i++, o += Stride)
        {
            float cos = MathF.Cos(s.Heading[i]), sin = MathF.Sin(s.Heading[i]);
            var p = s.Position[i];
            // Rotation about Y: model +Z -> (sin, 0, cos), matching EnemySwarm.Heading.
            b[o] = cos; b[o + 1] = 0f; b[o + 2] = sin; b[o + 3] = p.X;
            b[o + 4] = 0f; b[o + 5] = 1f; b[o + 6] = 0f; b[o + 7] = 0f;
            b[o + 8] = -sin; b[o + 9] = 0f; b[o + 10] = cos; b[o + 11] = p.Y;
            b[o + 12] = s.AnimPhase[i]; b[o + 13] = s.HitFlash[i]; b[o + 14] = s.Variant[i]; b[o + 15] = 0f;
        }
        RenderingServer.MultimeshSetBuffer(_multimesh.GetRid(), b);
        _multimesh.VisibleInstanceCount = s.Count;
    }
}
