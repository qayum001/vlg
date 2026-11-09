using System;
using Godot;
using Vlg.Core.Run;
using Vlg.Core.Sim;

namespace Vlg.Game;

public sealed class ProjectileRenderer
{
    const int Stride = 12;
    const int CapacityStep = 512;
    const float Height = 0.8f;
    readonly RunSim? _sim;
    readonly Batch _magic;
    readonly Batch? _fire;

    public ProjectileRenderer(Node3D parent, RunSim? sim = null)
    {
        _sim = sim;
        _magic = new Batch(parent, "Projectiles", PlaceholderMeshes.BuildProjectile(), new Color(0.3f, 0.9f, 1f));
        if (sim?.Hero.Id == "pyromancer")
            _fire = new Batch(parent, "Fireballs", new SphereMesh { Radius = 0.3f, Height = 0.6f, RadialSegments = 10, Rings = 6 }, new Color(1f, 0.3f, 0.025f));
    }

    public void SetCullCenter(Vector3 center)
    {
        _magic.Mesh.CustomAabb = EnemyRenderer.CullBoxAround(center);
        if (_fire != null) _fire.Mesh.CustomAabb = EnemyRenderer.CullBoxAround(center);
    }

    bool IsFire(ProjectilePool pool, int index)
    {
        if (_fire == null || _sim == null) return false;
        int slot = pool.Source[index] - RunSim.HeroSkillSource;
        return (uint)slot < (uint)_sim.Loadout.Length && _sim.Loadout[slot].Id != "magic_bolts";
    }

    public void Update(ProjectilePool pool)
    {
        int fireCount = 0;
        for (int i = 0; i < pool.Count; i++) if (IsFire(pool, i)) fireCount++;
        _magic.Prepare(pool.Count - fireCount);
        _fire?.Prepare(fireCount);
        int magic = 0, fire = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            bool flaming = IsFire(pool, i);
            var batch = flaming ? _fire! : _magic;
            int o = (flaming ? fire++ : magic++) * Stride;
            var b = batch.Buffer;
            var v = pool.Velocity[i];
            float len = v.Length();
            float sin = len > 1e-4f ? v.X / len : 0f, cos = len > 1e-4f ? v.Y / len : 1f;
            var p = pool.Position[i];
            float k = pool.Radius[i] / pool.DefaultRadius;
            b[o] = cos * k; b[o + 1] = 0; b[o + 2] = sin * k; b[o + 3] = p.X;
            b[o + 4] = 0; b[o + 5] = k; b[o + 6] = 0; b[o + 7] = Height;
            b[o + 8] = -sin * k; b[o + 9] = 0; b[o + 10] = cos * k; b[o + 11] = p.Y;
        }
        _magic.Upload(magic);
        _fire?.Upload(fire);
    }

    sealed class Batch
    {
        public MultiMesh Mesh { get; }
        public float[] Buffer { get; private set; } = Array.Empty<float>();

        public Batch(Node3D parent, string name, Mesh mesh, Color color)
        {
            Mesh = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = mesh, CustomAabb = EnemyRenderer.WorldAabb };
            parent.AddChild(new MultiMeshInstance3D
            {
                Name = name, Multimesh = Mesh, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                MaterialOverride = new StandardMaterial3D { AlbedoColor = color, EmissionEnabled = true, Emission = color, EmissionEnergyMultiplier = 4f },
            });
        }

        public void Prepare(int count)
        {
            int capacity = (Math.Max(count, 1) + CapacityStep - 1) / CapacityStep * CapacityStep;
            if (capacity <= Mesh.InstanceCount) return;
            Mesh.InstanceCount = capacity;
            Buffer = new float[capacity * Stride];
        }

        public void Upload(int count)
        {
            RenderingServer.MultimeshSetBuffer(Mesh.GetRid(), Buffer);
            Mesh.VisibleInstanceCount = count;
        }
    }
}
