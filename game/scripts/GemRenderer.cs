using System;
using Godot;
using Vlg.Core.Run;

namespace Vlg.Game;

/// <summary>All XP gems in one MultiMesh: small spinning emissive diamonds.</summary>
public sealed class GemRenderer
{
    const int Stride = 12;
    const int CapacityStep = 512;
    const float Height = 0.45f;
    const float SpinRate = 2.5f;

    readonly MultiMesh _multimesh;
    float[] _buffer = Array.Empty<float>();

    public GemRenderer(Node3D parent)
    {
        _multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = new SphereMesh { Radius = 0.16f, Height = 0.42f, RadialSegments = 4, Rings = 1 },
            CustomAabb = EnemyRenderer.WorldAabb,
        };
        parent.AddChild(new MultiMeshInstance3D
        {
            Name = "Gems",
            Multimesh = _multimesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.3f, 1f, 0.5f),
                EmissionEnabled = true,
                Emission = new Color(0.2f, 1f, 0.45f),
                EmissionEnergyMultiplier = 2.5f,
            },
        });
    }

    public void SetCullCenter(Vector3 center) => _multimesh.CustomAabb = EnemyRenderer.CullBoxAround(center);

    public void Update(GemPool gems, float time)
    {
        int capacity = (Math.Max(gems.Count, 1) + CapacityStep - 1) / CapacityStep * CapacityStep;
        if (capacity != _multimesh.InstanceCount)
        {
            _multimesh.InstanceCount = capacity;
            _buffer = new float[capacity * Stride];
        }

        var b = _buffer;
        for (int i = 0, o = 0; i < gems.Count; i++, o += Stride)
        {
            // Per-gem phase from its index so neighbors don't spin in sync.
            float a = time * SpinRate + i * 0.7f;
            float cos = MathF.Cos(a), sin = MathF.Sin(a);
            var p = gems.Position[i];
            b[o] = cos; b[o + 1] = 0f; b[o + 2] = sin; b[o + 3] = p.X;
            b[o + 4] = 0f; b[o + 5] = 1f; b[o + 6] = 0f; b[o + 7] = Height;
            b[o + 8] = -sin; b[o + 9] = 0f; b[o + 10] = cos; b[o + 11] = p.Y;
        }
        RenderingServer.MultimeshSetBuffer(_multimesh.GetRid(), b);
        _multimesh.VisibleInstanceCount = gems.Count;
    }
}
