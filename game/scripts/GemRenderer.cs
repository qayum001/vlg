using System;
using Godot;
using Vlg.Core.Run;

namespace Vlg.Game;

/// <summary>All pickups of one pool in one MultiMesh: small spinning emissive diamonds (XP gems; gold for credits).</summary>
public sealed class GemRenderer
{
    const int Stride = 12;
    const int CapacityStep = 512;
    const float Height = 0.45f;
    const float SpinRate = 2.5f;

    readonly MultiMesh _multimesh;
    float[] _buffer = Array.Empty<float>();

    public GemRenderer(Node3D parent) : this(parent, "Gems", new Color(0.3f, 1f, 0.5f), new Color(0.2f, 1f, 0.45f),
        new SphereMesh { Radius = 0.16f, Height = 0.42f, RadialSegments = 4, Rings = 1 }) { }

    public GemRenderer(Node3D parent, string name, Color albedo, Color emission, Mesh mesh)
    {
        _multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = mesh,
            CustomAabb = EnemyRenderer.WorldAabb,
        };
        parent.AddChild(new MultiMeshInstance3D
        {
            Name = name,
            Multimesh = _multimesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = albedo,
                EmissionEnabled = true,
                Emission = emission,
                EmissionEnergyMultiplier = 2.5f,
            },
        });
    }

    /// <summary>Violet orb for meta credit pickups.</summary>
    public static GemRenderer MetaOrbs(Node3D parent) => new(parent, "MetaOrbs", new Color(0.75f, 0.4f, 1f), new Color(0.7f, 0.3f, 1f),
        new SphereMesh { Radius = 0.3f, Height = 0.6f, RadialSegments = 12, Rings = 6 });

    /// <summary>Gold coin for credit pickups.</summary>
    public static GemRenderer Coins(Node3D parent) => new(parent, "Coins", new Color(1f, 0.8f, 0.2f), new Color(1f, 0.65f, 0.1f),
        new CylinderMesh { TopRadius = 0.22f, BottomRadius = 0.22f, Height = 0.06f, RadialSegments = 12, Rings = 1 });

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
