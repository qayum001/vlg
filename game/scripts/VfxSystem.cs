using System;
using Godot;
using Vlg.Core.Sim;

namespace Vlg.Game;

/// <summary>
/// Stress VFX: hit/death bursts (pooled one-shot GPU particles), aura particles around the player,
/// full-screen additive layers (overdraw ramp) and spell lights orbiting the player.
/// </summary>
public sealed class VfxSystem
{
    const int BurstPoolSize = 64;
    const int MaxLayers = 64;
    const int MaxLights = 16;
    const float LayerDistance = 0.5f;
    const float BurstLifetime = 0.45f;

    static readonly Color[] LightColors =
    {
        new(1f, 0.45f, 0.2f), new(0.3f, 0.6f, 1f), new(0.7f, 0.35f, 1f), new(0.3f, 1f, 0.5f),
    };

    readonly GpuParticles3D[] _bursts = new GpuParticles3D[BurstPoolSize];
    readonly double[] _burstEndTime = new double[BurstPoolSize];
    readonly GpuParticles3D _aura;
    readonly MeshInstance3D[] _layers = new MeshInstance3D[MaxLayers];
    readonly QuadMesh _layerQuad = new();
    readonly OmniLight3D[] _lights = new OmniLight3D[MaxLights];
    int _nextBurst;
    double _time;

    public int BurstAmount { get; private set; }
    public int AuraAmount { get; private set; }
    public int Layers { get; private set; }
    public int Lights { get; private set; }

    public VfxSystem(Node3D world, Camera3D camera, Shader layerShader)
    {
        var dot = SoftDot();
        var additive = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BlendMode = BaseMaterial3D.BlendModeEnum.Add,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
            VertexColorUseAsAlbedo = true,
            AlbedoTexture = dot,
        };

        var burstProcess = new ParticleProcessMaterial
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
            EmissionSphereRadius = 0.2f,
            Direction = Vector3.Up,
            Spread = 180f,
            InitialVelocityMin = 3f,
            InitialVelocityMax = 7f,
            Gravity = new Vector3(0, -12f, 0),
            ScaleMin = 0.6f,
            ScaleMax = 1.2f,
            Color = new Color(1f, 0.55f, 0.15f),
            ColorRamp = FadeOut(),
        };
        var burstQuad = new QuadMesh { Size = new Vector2(0.35f, 0.35f), Material = additive };
        for (int i = 0; i < BurstPoolSize; i++)
        {
            _bursts[i] = new GpuParticles3D
            {
                Name = $"Burst{i}",
                Amount = 16,
                Lifetime = BurstLifetime,
                OneShot = true,
                Explosiveness = 1f,
                Emitting = false,
                LocalCoords = false,
                ProcessMaterial = burstProcess,
                DrawPass1 = burstQuad,
                VisibilityAabb = new Aabb(new Vector3(-4, -4, -4), new Vector3(8, 8, 8)),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            world.AddChild(_bursts[i]);
        }

        _aura = new GpuParticles3D
        {
            Name = "Aura",
            Amount = 1,
            Lifetime = 2.0,
            LocalCoords = false,
            Emitting = false,
            ProcessMaterial = new ParticleProcessMaterial
            {
                EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
                EmissionBoxExtents = new Vector3(7f, 0.4f, 7f),
                Direction = Vector3.Up,
                Spread = 30f,
                InitialVelocityMin = 0.5f,
                InitialVelocityMax = 1.5f,
                Gravity = new Vector3(0, 0.3f, 0),
                ScaleMin = 0.7f,
                ScaleMax = 1.4f,
                Color = new Color(0.35f, 0.6f, 1f),
                ColorRamp = FadeInOut(),
            },
            DrawPass1 = new QuadMesh { Size = new Vector2(0.6f, 0.6f), Material = additive },
            VisibilityAabb = new Aabb(new Vector3(-10, -2, -10), new Vector3(20, 8, 20)),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        world.AddChild(_aura);

        var layerMaterial = new ShaderMaterial { Shader = layerShader };
        layerMaterial.SetShaderParameter("tex", dot);
        _layerQuad.Material = layerMaterial;
        for (int i = 0; i < MaxLayers; i++)
        {
            _layers[i] = new MeshInstance3D
            {
                Name = $"Layer{i}",
                Mesh = _layerQuad,
                Position = new Vector3(0, 0, -LayerDistance),
                Visible = false,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            camera.AddChild(_layers[i]);
        }

        for (int i = 0; i < MaxLights; i++)
        {
            _lights[i] = new OmniLight3D
            {
                Name = $"SpellLight{i}",
                OmniRange = 6f,
                LightEnergy = 2f,
                LightColor = LightColors[i % LightColors.Length],
                ShadowEnabled = false,
                Visible = false,
            };
            world.AddChild(_lights[i]);
        }
    }

    public int MaxLayerCount => MaxLayers;

    /// <summary>Upper bound of live particles: aura + bursts still inside their lifetime.</summary>
    public int EstimatedLiveParticles
    {
        get
        {
            int active = 0;
            foreach (double end in _burstEndTime) if (end > _time) active++;
            return AuraAmount + active * BurstAmount;
        }
    }

    public void SetBurstAmount(int amount)
    {
        BurstAmount = Math.Max(1, amount);
        foreach (var b in _bursts) b.Amount = BurstAmount;
    }

    public void SetAura(int amount)
    {
        if (amount == AuraAmount) return;
        AuraAmount = Math.Max(0, amount);
        _aura.Emitting = AuraAmount > 0;
        _aura.Visible = AuraAmount > 0;
        if (AuraAmount > 0) _aura.Amount = AuraAmount;
    }

    public void SetLayers(int count)
    {
        Layers = Math.Clamp(count, 0, MaxLayers);
        for (int i = 0; i < MaxLayers; i++) _layers[i].Visible = i < Layers;
    }

    public void SetLights(int count)
    {
        Lights = Math.Clamp(count, 0, MaxLights);
        for (int i = 0; i < MaxLights; i++) _lights[i].Visible = i < Lights;
    }

    /// <summary>Sizes the layer quad to cover the camera view with margin.</summary>
    public void FitLayers(Camera3D camera, Vector2 viewportSize)
    {
        float halfH = LayerDistance * MathF.Tan(Mathf.DegToRad(camera.Fov) / 2f) * 1.1f;
        float halfW = halfH * viewportSize.X / viewportSize.Y;
        _layerQuad.Size = new Vector2(halfW * 2f, halfH * 2f);
    }

    public void Update(double dt, Vector3 player, PointBuffer hits, PointBuffer deaths, int burstsPerFrame)
    {
        _time += dt;
        _aura.GlobalPosition = player + new Vector3(0, 0.5f, 0);

        int budget = Math.Min(burstsPerFrame, BurstPoolSize);
        for (int i = 0; i < deaths.Count && budget > 0; i++, budget--) Burst(deaths.Items[i]);
        for (int i = 0; i < hits.Count && budget > 0; i++, budget--) Burst(hits.Items[i]);

        for (int i = 0; i < Lights; i++)
        {
            float a = (float)_time * (0.8f + 0.15f * i) + i * 2.4f;
            float r = 3f + (i % 3) * 1.5f;
            _lights[i].GlobalPosition = player + new Vector3(MathF.Sin(a) * r, 1.5f, MathF.Cos(a) * r);
        }
    }

    void Burst(System.Numerics.Vector2 at)
    {
        int i = _nextBurst;
        _nextBurst = (_nextBurst + 1) % BurstPoolSize;
        var b = _bursts[i];
        b.GlobalPosition = new Vector3(at.X, 0.7f, at.Y);
        b.Restart();
        _burstEndTime[i] = _time + BurstLifetime;
    }

    static GradientTexture2D SoftDot() => new()
    {
        Gradient = new Gradient { Colors = new[] { new Color(1, 1, 1, 1), new Color(1, 1, 1, 0) }, Offsets = new[] { 0f, 1f } },
        Fill = GradientTexture2D.FillEnum.Radial,
        FillFrom = new Vector2(0.5f, 0.5f),
        FillTo = new Vector2(0.5f, 0f),
        Width = 64,
        Height = 64,
    };

    static GradientTexture1D FadeOut() => new()
    {
        Gradient = new Gradient { Colors = new[] { new Color(1, 1, 1, 1), new Color(1, 1, 1, 0) }, Offsets = new[] { 0f, 1f } },
    };

    static GradientTexture1D FadeInOut() => new()
    {
        Gradient = new Gradient
        {
            Colors = new[] { new Color(1, 1, 1, 0), new Color(1, 1, 1, 1), new Color(1, 1, 1, 0) },
            Offsets = new[] { 0f, 0.3f, 1f },
        },
    };
}
