using System;
using Godot;
using Vlg.Core.Sim;

namespace Vlg.Game;

/// <summary>Ground, scattered low props, sun and environment.</summary>
public sealed class Arena
{
    const int PropCount = 400;
    const float PropArea = 90f;
    const float PropClearRadius = 6f;

    public DirectionalLight3D Sun { get; }
    public Godot.Environment Environment { get; }
    public MultiMeshInstance3D Props { get; }

    public Arena(Node3D parent, Shader groundShader, uint seed)
    {
        var groundMaterial = new ShaderMaterial { Shader = groundShader };
        parent.AddChild(new MeshInstance3D
        {
            Name = "Ground",
            Mesh = new PlaneMesh { Size = new Vector2(400, 400) },
            MaterialOverride = groundMaterial,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });

        var props = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = new BoxMesh { Size = new Vector3(1.4f, 0.7f, 1.1f) },
            InstanceCount = PropCount,
        };
        var rng = new XorShift32(seed);
        for (int i = 0; i < PropCount; i++)
        {
            Vector3 p;
            do p = new Vector3(rng.Range(-PropArea, PropArea), 0.3f, rng.Range(-PropArea, PropArea));
            while (p.Length() < PropClearRadius);
            var basis = new Basis(Vector3.Up, rng.Range(0, MathF.Tau)).Scaled(Vector3.One * rng.Range(0.6f, 1.8f));
            props.SetInstanceTransform(i, new Transform3D(basis, p));
        }
        Props = new MultiMeshInstance3D
        {
            Name = "Props",
            Multimesh = props,
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.32f, 0.36f, 0.45f), Roughness = 0.9f },
        };
        parent.AddChild(Props);

        Sun = new DirectionalLight3D
        {
            Name = "Sun",
            RotationDegrees = new Vector3(-50f, 30f, 0f),
            LightEnergy = 1.2f,
            LightColor = new Color(1f, 0.95f, 0.88f),
            DirectionalShadowMaxDistance = 45f,
        };
        parent.AddChild(Sun);

        Environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.05f, 0.06f, 0.1f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.5f, 0.55f, 0.7f),
            AmbientLightEnergy = 0.6f,
            TonemapMode = Godot.Environment.ToneMapper.Aces,
            GlowIntensity = 0.8f,
            GlowBloom = 0.05f,
            GlowHdrThreshold = 1f,
        };
        parent.AddChild(new WorldEnvironment { Name = "Environment", Environment = Environment });
    }
}
