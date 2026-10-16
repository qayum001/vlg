using System;
using Godot;
using Vlg.Core.Sim;

namespace Vlg.Game;

/// <summary>
/// Ground, scattered low props, sun and environment. Props repeat every PropTile meters (n x n tiles in one
/// MultiMesh), so <see cref="Follow"/> can shift them by whole tiles without popping: endless map (domain.md).
/// </summary>
public sealed class Arena
{
    const int PropsPerTile = 44;
    const float PropTile = 60f;
    const float PropClearRadius = 6f;

    readonly MeshInstance3D _ground;

    public DirectionalLight3D Sun { get; }
    public Godot.Environment Environment { get; }
    public MultiMeshInstance3D Props { get; }

    /// <param name="propTiles">Odd tile count per side; props are guaranteed within (propTiles / 2) * 60 m of the follow target.</param>
    public Arena(Node3D parent, Shader groundShader, uint seed, int propTiles = 3)
    {
        if (propTiles < 1 || propTiles % 2 == 0) throw new ArgumentException($"propTiles must be odd and >= 1, got {propTiles}");
        int tiles = propTiles * propTiles;
        int halfTiles = propTiles / 2;
        var groundMaterial = new ShaderMaterial { Shader = groundShader };
        _ground = new MeshInstance3D
        {
            Name = "Ground",
            Mesh = new PlaneMesh { Size = new Vector2(400, 400) },
            MaterialOverride = groundMaterial,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        parent.AddChild(_ground);

        var props = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = new BoxMesh { Size = new Vector3(1.4f, 0.7f, 1.1f) },
            InstanceCount = PropsPerTile * tiles,
        };
        var rng = new XorShift32(seed);
        const float half = PropTile / 2f;
        for (int i = 0; i < PropsPerTile; i++)
        {
            Vector3 p;
            do p = new Vector3(rng.Range(-half, half), 0.3f, rng.Range(-half, half));
            while (p.Length() < PropClearRadius);
            var basis = new Basis(Vector3.Up, rng.Range(0, MathF.Tau)).Scaled(Vector3.One * rng.Range(0.6f, 1.8f));
            for (int t = 0; t < tiles; t++)
            {
                var offset = new Vector3((t % propTiles - halfTiles) * PropTile, 0f, (t / propTiles - halfTiles) * PropTile);
                props.SetInstanceTransform(t * PropsPerTile + i, new Transform3D(basis, p + offset));
            }
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

    /// <summary>Keeps ground and props around <paramref name="target"/>. Ground shader is world-space, so moving it is invisible.</summary>
    public void Follow(Vector3 target)
    {
        _ground.Position = new Vector3(target.X, 0f, target.Z);
        Props.Position = new Vector3(MathF.Round(target.X / PropTile) * PropTile, 0f, MathF.Round(target.Z / PropTile) * PropTile);
    }
}
