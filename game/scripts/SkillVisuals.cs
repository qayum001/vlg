using System;
using Godot;
using Vlg.Core.Config;
using Vlg.Core.Run;

namespace Vlg.Game;

/// <summary>Placeholder visuals for non-projectile skills: Aura ground disc (pulses per tick), Orbit blades.</summary>
public sealed class SkillVisuals
{
    const float DiscAlpha = 0.18f;
    const float PulseAlpha = 0.3f;
    const float PulseDecay = 6f;
    const float BladeHeight = 0.8f;

    readonly MeshInstance3D[] _discs;
    readonly StandardMaterial3D[] _discMaterials;
    readonly float[] _pulse;
    readonly MultiMesh _blades;
    readonly float[] _bladeBuffer = new float[RunConfig.MaxBlades * RunConfig.MaxSkills * 12];

    public SkillVisuals(Node3D parent, int skillCount)
    {
        _discs = new MeshInstance3D[skillCount];
        _discMaterials = new StandardMaterial3D[skillCount];
        _pulse = new float[skillCount];
        var disc = new CylinderMesh { TopRadius = 1f, BottomRadius = 1f, Height = 0.04f, RadialSegments = 48, Rings = 1 };
        for (int i = 0; i < skillCount; i++)
        {
            _discMaterials[i] = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                BlendMode = BaseMaterial3D.BlendModeEnum.Add,
                AlbedoColor = new Color(0.7f, 0.35f, 1f, DiscAlpha),
            };
            _discs[i] = new MeshInstance3D
            {
                Name = $"AuraDisc{i}",
                Mesh = disc,
                MaterialOverride = _discMaterials[i],
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Visible = false,
            };
            parent.AddChild(_discs[i]);
        }

        _blades = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = new BoxMesh { Size = new Vector3(0.22f, 0.22f, 1.1f) },
            CustomAabb = EnemyRenderer.WorldAabb,
            InstanceCount = RunConfig.MaxBlades * RunConfig.MaxSkills,
            VisibleInstanceCount = 0,
        };
        parent.AddChild(new MultiMeshInstance3D
        {
            Name = "OrbitBlades",
            Multimesh = _blades,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(1f, 0.7f, 0.25f),
                EmissionEnabled = true,
                Emission = new Color(1f, 0.55f, 0.15f),
                EmissionEnergyMultiplier = 3f,
            },
        });
    }

    public void Update(SkillSystem skills, System.Numerics.Vector2 hero, float dt)
    {
        for (int i = 0; i < _discs.Length; i++)
        {
            float r = skills.AuraRadius[i];
            _discs[i].Visible = r > 0f;
            if (r <= 0f) continue;
            // dt 0 = sim paused: FiredThisStep is stale.
            if (dt > 0f && skills.FiredThisStep[i]) _pulse[i] = 1f;
            _pulse[i] = MathF.Max(0f, _pulse[i] - PulseDecay * dt);
            _discs[i].Position = new Vector3(hero.X, 0.03f, hero.Y);
            _discs[i].Scale = new Vector3(r, 1f, r);
            var color = _discMaterials[i].AlbedoColor;
            color.A = DiscAlpha + PulseAlpha * _pulse[i];
            _discMaterials[i].AlbedoColor = color;
        }

        var b = _bladeBuffer;
        int n = skills.BladeCount;
        for (int k = 0, o = 0; k < n; k++, o += 12)
        {
            var p = skills.BladePositions[k];
            // Long axis (+Z) along the orbit tangent.
            var off = p - hero;
            float len = off.Length();
            float sin = len > 1e-4f ? off.Y / len : 0f, cos = len > 1e-4f ? -off.X / len : 1f;
            b[o] = cos; b[o + 1] = 0f; b[o + 2] = sin; b[o + 3] = p.X;
            b[o + 4] = 0f; b[o + 5] = 1f; b[o + 6] = 0f; b[o + 7] = BladeHeight;
            b[o + 8] = -sin; b[o + 9] = 0f; b[o + 10] = cos; b[o + 11] = p.Y;
        }
        RenderingServer.MultimeshSetBuffer(_blades.GetRid(), b);
        _blades.VisibleInstanceCount = n;
    }
}
