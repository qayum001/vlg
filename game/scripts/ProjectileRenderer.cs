using System;
using Godot;
using Vlg.Core.Sim;

namespace Vlg.Game;

/// <summary>All projectiles in one MultiMesh. Emissive opaque mesh: glow without transparency cost.</summary>
public sealed class ProjectileRenderer
{
    const int Stride = 12;
    const int CapacityStep = 512;
    const float Height = 0.8f;

    readonly MultiMesh _multimesh;
    float[] _buffer = Array.Empty<float>();

    public ProjectileRenderer(Node3D parent)
    {
        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.4f, 0.95f, 1f),
            EmissionEnabled = true,
            Emission = new Color(0.3f, 0.9f, 1f),
            EmissionEnergyMultiplier = 4f,
        };
        _multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = PlaceholderMeshes.BuildProjectile(),
            CustomAabb = EnemyRenderer.WorldAabb,
        };
        parent.AddChild(new MultiMeshInstance3D
        {
            Name = "Projectiles",
            Multimesh = _multimesh,
            MaterialOverride = material,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
    }

    public void SetCullCenter(Vector3 center) => _multimesh.CustomAabb = EnemyRenderer.CullBoxAround(center);

    public void Update(ProjectilePool pool)
    {
        int capacity = (Math.Max(pool.Count, 1) + CapacityStep - 1) / CapacityStep * CapacityStep;
        if (capacity != _multimesh.InstanceCount)
        {
            _multimesh.InstanceCount = capacity;
            _buffer = new float[capacity * Stride];
        }

        var b = _buffer;
        for (int i = 0, o = 0; i < pool.Count; i++, o += Stride)
        {
            var v = pool.Velocity[i];
            float len = v.Length();
            float sin = len > 1e-4f ? v.X / len : 0f, cos = len > 1e-4f ? v.Y / len : 1f;
            var p = pool.Position[i];
            // Bigger hit radius (Big Disc) draws bigger.
            float k = pool.Radius[i] / pool.DefaultRadius;
            float c = cos * k, s = sin * k;
            b[o] = c; b[o + 1] = 0f; b[o + 2] = s; b[o + 3] = p.X;
            b[o + 4] = 0f; b[o + 5] = k; b[o + 6] = 0f; b[o + 7] = Height;
            b[o + 8] = -s; b[o + 9] = 0f; b[o + 10] = c; b[o + 11] = p.Y;
        }
        RenderingServer.MultimeshSetBuffer(_multimesh.GetRid(), b);
        _multimesh.VisibleInstanceCount = pool.Count;
    }
}
