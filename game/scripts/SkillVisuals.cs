using System;
using Godot;
using Vlg.Core.Config;
using Vlg.Core.Run;
using Vlg.Core.Sim;
using NVec2 = System.Numerics.Vector2;

namespace Vlg.Game;

/// <summary>
/// Placeholder visuals for non-projectile skills: aura discs, vinyl records (orbit), expanding rings,
/// speakers with pulse discs, beam flashes, ground zones, falling bells, drones, impact flashes, shield bubble.
/// Neon phonk palette, additive unshaded.
/// </summary>
public sealed class SkillVisuals
{
    const int Stride = 16;
    const float DiscAlpha = 0.16f;
    const float PulseAlpha = 0.3f;
    const float PulseDecay = 6f;
    const float VinylHeight = 0.7f;
    const float BeamFadeSec = 0.18f;
    const int BeamSlots = 32;
    const int SpeakerCap = RunConfig.MaxSkills * RunConfig.MaxSummonsPerSkill;
    const int FlashSlots = 64;
    const float FlashSec = 0.3f;
    const float BellDropHeight = 9f;

    static readonly Color AuraColor = new(0.75f, 0.3f, 1f);
    static readonly Color RingColor = new(1f, 0.25f, 0.75f);
    static readonly Color ShockColor = new(1f, 0.85f, 1f);
    static readonly Color PulseColor = new(0.3f, 0.95f, 1f);
    static readonly Color BeamColor = new(1f, 0.35f, 0.8f);
    static readonly Color CrackColor = new(1f, 0.45f, 0.2f);
    static readonly Color PuddleColor = new(0.6f, 0.2f, 1f);
    static readonly Color BellColor = new(1f, 0.85f, 0.3f);
    static readonly Color FlashColor = new(1f, 0.9f, 1f);
    static readonly Color ShieldColor = new(0.3f, 0.95f, 1f);
    static readonly Color HolyColor = new(1f, 0.95f, 0.6f);
    static readonly Color PoisonColor = new(0.4f, 1f, 0.3f);
    static readonly Color SmokeColor = new(0.55f, 0.55f, 0.6f);

    readonly MeshInstance3D[] _discs;
    readonly StandardMaterial3D[] _discMaterials;
    readonly float[] _pulse;
    readonly MultiMesh _vinyls;
    readonly MultiMesh _labels;
    readonly MultiMesh _rings;
    readonly MultiMesh _speakers;
    readonly MultiMesh _speakerPulses;
    readonly MultiMesh _beams;
    readonly MultiMesh _zones;
    readonly MultiMesh _strikeMarks;
    readonly MultiMesh _bells;
    readonly MultiMesh _drones;
    readonly MultiMesh _flashes;
    readonly MeshInstance3D _shield;
    readonly StandardMaterial3D _shieldMaterial;
    readonly MeshInstance3D _smoke;
    readonly MeshInstance3D _singularity;
    float[] _zoneBuffer = Array.Empty<float>();
    float[] _strikeBuffer = Array.Empty<float>();
    float[] _bellBuffer = Array.Empty<float>();
    readonly float[] _droneBuffer = new float[RunConfig.MaxSkills * RunConfig.MaxDrones * Stride];
    readonly float[] _flashBuffer = new float[FlashSlots * Stride];
    readonly NVec2[] _flashPos = new NVec2[FlashSlots];
    readonly float[] _flashRadius = new float[FlashSlots];
    readonly float[] _flashAge = new float[FlashSlots];
    int _nextFlash;
    readonly float[] _vinylBuffer = new float[RunConfig.MaxBlades * RunConfig.MaxSkills * Stride];
    readonly float[] _labelBuffer = new float[RunConfig.MaxBlades * RunConfig.MaxSkills * Stride];
    readonly float[] _speakerBuffer = new float[SpeakerCap * Stride];
    readonly float[] _pulseBuffer = new float[SpeakerCap * Stride];
    readonly float[] _beamBuffer = new float[BeamSlots * Stride];
    float[] _ringBuffer = Array.Empty<float>();

    readonly NVec2[] _beamOrigin = new NVec2[BeamSlots];
    readonly NVec2[] _beamDir = new NVec2[BeamSlots];
    readonly float[] _beamLength = new float[BeamSlots];
    readonly float[] _beamWidth = new float[BeamSlots];
    readonly float[] _beamAge = new float[BeamSlots];
    int _nextBeam;
    float _spin;

    public SkillVisuals(Node3D parent, int skillCount)
    {
        _discs = new MeshInstance3D[skillCount];
        _discMaterials = new StandardMaterial3D[skillCount];
        _pulse = new float[skillCount];
        var disc = new CylinderMesh { TopRadius = 1f, BottomRadius = 1f, Height = 0.04f, RadialSegments = 48, Rings = 1 };
        for (int i = 0; i < skillCount; i++)
        {
            _discMaterials[i] = Glow(new Color(AuraColor, DiscAlpha), false);
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

        _vinyls = AddMultiMesh(parent, "Vinyls", new CylinderMesh { TopRadius = 1f, BottomRadius = 1f, Height = 0.06f, RadialSegments = 24, Rings = 1 },
            new StandardMaterial3D { AlbedoColor = new Color(0.04f, 0.03f, 0.06f), Metallic = 0.5f, Roughness = 0.15f },
            RunConfig.MaxBlades * RunConfig.MaxSkills);
        _labels = AddMultiMesh(parent, "VinylLabels", new CylinderMesh { TopRadius = 0.4f, BottomRadius = 0.4f, Height = 0.08f, RadialSegments = 16, Rings = 1 },
            new StandardMaterial3D { AlbedoColor = new Color(1f, 0.3f, 0.75f), EmissionEnabled = true, Emission = new Color(1f, 0.2f, 0.7f), EmissionEnergyMultiplier = 2f },
            RunConfig.MaxBlades * RunConfig.MaxSkills);
        _rings = AddMultiMesh(parent, "Rings", Annulus(0.9f, 64), Glow(Colors.White, true), 0);
        _speakers = AddMultiMesh(parent, "Speakers", new BoxMesh { Size = new Vector3(0.9f, 1.3f, 0.8f) },
            new StandardMaterial3D
            {
                AlbedoColor = new Color(0.12f, 0.06f, 0.2f),
                EmissionEnabled = true,
                Emission = new Color(0.55f, 0.2f, 1f),
                EmissionEnergyMultiplier = 0.8f,
            }, SpeakerCap);
        _speakerPulses = AddMultiMesh(parent, "SpeakerPulses", disc, Glow(Colors.White, true), SpeakerCap);
        _beams = AddMultiMesh(parent, "Beams", new PlaneMesh { Size = Vector2.One }, Glow(Colors.White, true), BeamSlots);
        _zones = AddMultiMesh(parent, "Zones", disc, Glow(Colors.White, true), 0);
        _strikeMarks = AddMultiMesh(parent, "StrikeMarks", Annulus(0.8f, 48), Glow(Colors.White, true), 0);
        _bells = AddMultiMesh(parent, "Bells", new CylinderMesh { TopRadius = 0.25f, BottomRadius = 0.75f, Height = 1.1f, RadialSegments = 12, Rings = 1 },
            new StandardMaterial3D { AlbedoColor = BellColor, Metallic = 0.8f, Roughness = 0.25f, EmissionEnabled = true, Emission = BellColor, EmissionEnergyMultiplier = 0.6f }, 0);
        _drones = AddMultiMesh(parent, "Drones", new BoxMesh { Size = new Vector3(0.7f, 0.4f, 0.35f) },
            new StandardMaterial3D { AlbedoColor = new Color(0.1f, 0.05f, 0.15f), EmissionEnabled = true, Emission = new Color(0.3f, 0.95f, 1f), EmissionEnergyMultiplier = 1.5f },
            RunConfig.MaxSkills * RunConfig.MaxDrones);
        _flashes = AddMultiMesh(parent, "Flashes", Annulus(0.75f, 48), Glow(Colors.White, true), FlashSlots);
        _shieldMaterial = Glow(new Color(ShieldColor, 0.2f), false);
        _shield = new MeshInstance3D
        {
            Name = "ShieldBubble",
            Mesh = new SphereMesh { Radius = 1.2f, Height = 2.4f, RadialSegments = 24, Rings = 12 },
            MaterialOverride = _shieldMaterial,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Visible = false,
        };
        parent.AddChild(_shield);
        _smoke = new MeshInstance3D
        {
            Name = "SmokeCloud",
            Mesh = new SphereMesh { Radius = 3.5f, Height = 3f, RadialSegments = 16, Rings = 8 },
            MaterialOverride = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                AlbedoColor = new Color(SmokeColor, 0.45f),
            },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Visible = false,
        };
        parent.AddChild(_smoke);
        _singularity = new MeshInstance3D
        {
            Name = "Singularity",
            Mesh = new SphereMesh { Radius = 0.9f, Height = 1.8f, RadialSegments = 16, Rings = 8 },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.05f, 0f, 0.1f),
                EmissionEnabled = true,
                Emission = new Color(0.6f, 0.2f, 1f),
                EmissionEnergyMultiplier = 1.2f,
            },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Visible = false,
        };
        parent.AddChild(_singularity);
    }

    public void SetCullCenter(Vector3 center)
    {
        var box = EnemyRenderer.CullBoxAround(center);
        _vinyls.CustomAabb = box;
        _labels.CustomAabb = box;
        _rings.CustomAabb = box;
        _speakers.CustomAabb = box;
        _speakerPulses.CustomAabb = box;
        _beams.CustomAabb = box;
        _zones.CustomAabb = box;
        _strikeMarks.CustomAabb = box;
        _bells.CustomAabb = box;
        _drones.CustomAabb = box;
        _flashes.CustomAabb = box;
    }

    /// <param name="dt">0 while the sim is paused: per-step flags and events are stale.</param>
    public void Update(RunSim sim, float dt)
    {
        var skills = sim.Skills;
        var hero = sim.HeroPosition;
        _spin += dt * 9f;

        for (int i = 0; i < _discs.Length; i++)
        {
            float r = skills.AuraRadius[i];
            _discs[i].Visible = r > 0f;
            if (r <= 0f) continue;
            if (dt > 0f && skills.FiredThisStep[i]) _pulse[i] = 1f;
            _pulse[i] = MathF.Max(0f, _pulse[i] - PulseDecay * dt);
            _discs[i].Position = new Vector3(hero.X, 0.03f, hero.Y);
            _discs[i].Scale = new Vector3(r, 1f, r);
            _discMaterials[i].AlbedoColor = new Color(AuraColor, DiscAlpha + PulseAlpha * _pulse[i]);
        }

        UpdateVinyls(sim);
        UpdateRings(sim.Rings);
        UpdateSpeakers(sim);
        UpdateBeams(sim.Beams, dt);
        UpdateZones(sim);
        UpdateStrikes(sim.Strikes);
        UpdateDrones(skills, sim.Actives);
        UpdateFlashes(sim.Flashes, dt);

        bool shield = false;
        for (int i = 0; i < skills.Count; i++) shield |= skills.ShieldUp[i];
        _shield.Visible = shield;
        if (shield) _shield.Position = new Vector3(hero.X, 0.9f, hero.Y);

        var u = sim.Usables;
        _smoke.Visible = u.SmokeActive;
        if (u.SmokeActive) _smoke.Position = new Vector3(u.SmokePoint.X, 0.8f, u.SmokePoint.Y);
        _singularity.Visible = u.SingularityActive;
        if (u.SingularityActive)
        {
            float k = 1f + 0.15f * MathF.Sin(_spin * 3f);
            _singularity.Position = new Vector3(u.SingularityPoint.X, 1.2f, u.SingularityPoint.Y);
            _singularity.Scale = new Vector3(k, k, k);
        }
    }

    void UpdateZones(RunSim sim)
    {
        var z = sim.Zones;
        _zoneBuffer = Fit(_zones, _zoneBuffer, z.Capacity);
        for (int i = 0; i < z.Count; i++)
        {
            float r = z.Radius[i];
            float life = z.StartLife[i] > 0f ? z.Life[i] / z.StartLife[i] : 0f;
            float pump = z.SinceTick[i] >= 0f ? MathF.Exp(-z.SinceTick[i] * 8f) : 0f;
            int slot = z.Slot[i];
            byte source = z.SourceOf(i);
            var kind = slot >= 0 ? sim.Skills.Form(slot)?.Kind : null;
            var color = source == RunSim.UsableSource ? HolyColor
                : source == RunSim.PoisonSource ? PoisonColor
                : kind == SkillKind.Quake || source == RunSim.UltimateSource ? CrackColor
                : PuddleColor;
            Write(_zoneBuffer, i * Stride, new Vector3(r, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, r), new Vector3(z.Center[i].X, 0.04f, z.Center[i].Y),
                new Color(color, (0.05f + 0.18f * pump) * MathF.Min(1f, life * 3f)));
        }
        RenderingServer.MultimeshSetBuffer(_zones.GetRid(), _zoneBuffer);
        _zones.VisibleInstanceCount = z.Count;
    }

    void UpdateStrikes(StrikePool strikes)
    {
        _strikeBuffer = Fit(_strikeMarks, _strikeBuffer, strikes.Capacity);
        _bellBuffer = Fit(_bells, _bellBuffer, strikes.Capacity);
        for (int i = 0; i < strikes.Count; i++)
        {
            var p = strikes.Position[i];
            float r = strikes.Radius[i];
            float left = strikes.StartDelay[i] > 0f ? MathF.Max(0f, strikes.Delay[i] / strikes.StartDelay[i]) : 0f;
            Write(_strikeBuffer, i * Stride, new Vector3(r, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, r), new Vector3(p.X, 0.06f, p.Y),
                new Color(BellColor, 0.2f + 0.6f * (1f - left)));
            float k = MathF.Max(1f, r * 0.6f);
            Write(_bellBuffer, i * Stride, new Vector3(k, 0, 0), new Vector3(0, k, 0), new Vector3(0, 0, k), new Vector3(p.X, 0.55f * k + BellDropHeight * left, p.Y), Colors.White);
        }
        RenderingServer.MultimeshSetBuffer(_strikeMarks.GetRid(), _strikeBuffer);
        RenderingServer.MultimeshSetBuffer(_bells.GetRid(), _bellBuffer);
        _strikeMarks.VisibleInstanceCount = strikes.Count;
        _bells.VisibleInstanceCount = strikes.Count;
    }

    /// <summary>Hero drones (Boombox Drone) and item drones (Toy Drone) share one MultiMesh.</summary>
    void UpdateDrones(SkillSystem skills, SkillSystem actives)
    {
        int n = AddDrones(skills, 0);
        n = AddDrones(actives, n);
        RenderingServer.MultimeshSetBuffer(_drones.GetRid(), _droneBuffer);
        _drones.VisibleInstanceCount = n;
    }

    int AddDrones(SkillSystem sys, int n)
    {
        float c = MathF.Cos(_spin * 0.2f), s = MathF.Sin(_spin * 0.2f);
        for (int i = 0; i < sys.DroneCount && n < RunConfig.MaxSkills * RunConfig.MaxDrones; i++, n++)
        {
            var p = sys.DronePositions[i];
            float bob = 0.15f * MathF.Sin(_spin * 0.5f + n);
            Write(_droneBuffer, n * Stride, new Vector3(c, 0, -s), new Vector3(0, 1, 0), new Vector3(s, 0, c), new Vector3(p.X, 2.2f + bob, p.Y), Colors.White);
        }
        return n;
    }

    void UpdateFlashes(FlashEvents flashes, float dt)
    {
        if (dt > 0f)
        {
            for (int i = 0; i < flashes.Count; i++)
            {
                int k = _nextFlash;
                _nextFlash = (_nextFlash + 1) % FlashSlots;
                _flashPos[k] = flashes.Position[i];
                _flashRadius[k] = flashes.Radius[i];
                _flashAge[k] = 0f;
            }
        }
        int n = 0;
        for (int k = 0; k < FlashSlots; k++)
        {
            if (_flashRadius[k] <= 0f) continue;
            _flashAge[k] += dt;
            if (_flashAge[k] >= FlashSec)
            {
                _flashRadius[k] = 0f;
                continue;
            }
            float t = _flashAge[k] / FlashSec;
            float r = _flashRadius[k] * (0.6f + 0.5f * t);
            Write(_flashBuffer, n * Stride, new Vector3(r, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, r), new Vector3(_flashPos[k].X, 0.1f, _flashPos[k].Y),
                new Color(FlashColor, 1f - t));
            n++;
        }
        RenderingServer.MultimeshSetBuffer(_flashes.GetRid(), _flashBuffer);
        _flashes.VisibleInstanceCount = n;
    }

    /// <summary>Grows the instance count and buffer to <paramref name="capacity"/> once.</summary>
    static float[] Fit(MultiMesh mm, float[] buffer, int capacity)
    {
        if (mm.InstanceCount == capacity) return buffer;
        mm.InstanceCount = capacity;
        return new float[capacity * Stride];
    }

    void UpdateVinyls(RunSim sim)
    {
        var skills = sim.Skills;
        int n = skills.BladeCount;
        var b = _vinylBuffer;
        for (int k = 0, o = 0; k < n; k++, o += Stride)
        {
            var p = skills.BladePositions[k];
            float r = skills.CurrentRadius[skills.BladeSlot[k]];
            float c = MathF.Cos(_spin) * r, s = MathF.Sin(_spin) * r;
            Write(b, o, new Vector3(c, 0, -s), new Vector3(0, 1, 0), new Vector3(s, 0, c), new Vector3(p.X, VinylHeight, p.Y), Colors.White);
            Write(_labelBuffer, o, new Vector3(c, 0, -s), new Vector3(0, 1, 0), new Vector3(s, 0, c), new Vector3(p.X, VinylHeight + 0.01f, p.Y), Colors.White);
        }
        RenderingServer.MultimeshSetBuffer(_vinyls.GetRid(), b);
        RenderingServer.MultimeshSetBuffer(_labels.GetRid(), _labelBuffer);
        _vinyls.VisibleInstanceCount = n;
        _labels.VisibleInstanceCount = n;
    }

    void UpdateRings(RingPool rings)
    {
        int capacity = Math.Max(64, rings.Capacity);
        if (_rings.InstanceCount != capacity)
        {
            _rings.InstanceCount = capacity;
            _ringBuffer = new float[capacity * Stride];
        }
        var b = _ringBuffer;
        int n = 0;
        for (int i = 0; i < rings.Count; i++, n++)
        {
            float r = MathF.Max(0.1f, rings.Radius[i]);
            float fade = 1f - r / rings.MaxRadius[i];
            var color = rings.Source[i] == RunSim.DropShockSource ? ShockColor : RingColor;
            Write(b, n * Stride, new Vector3(r, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, r),
                new Vector3(rings.Center[i].X, 0.08f, rings.Center[i].Y), new Color(color, 0.25f + 0.75f * fade));
        }
        RenderingServer.MultimeshSetBuffer(_rings.GetRid(), b);
        _rings.VisibleInstanceCount = n;
    }

    void UpdateSpeakers(RunSim sim)
    {
        var s = sim.Summons;
        int n = s.Count;
        for (int i = 0, o = 0; i < n; i++, o += Stride)
        {
            var p = s.Position[i];
            float since = s.SincePulse[i];
            float pump = since >= 0f ? MathF.Exp(-since * 10f) : 0f;
            float k = 1f + 0.25f * pump;
            Write(_speakerBuffer, o, new Vector3(k, 0, 0), new Vector3(0, k, 0), new Vector3(0, 0, k), new Vector3(p.X, 0.65f * k, p.Y), Colors.White);
            float r = sim.Skills.CurrentRadius[s.Slot[i]];
            Write(_pulseBuffer, o, new Vector3(r, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, r), new Vector3(p.X, 0.05f, p.Y),
                new Color(PulseColor, 0.06f + 0.3f * pump));
        }
        RenderingServer.MultimeshSetBuffer(_speakers.GetRid(), _speakerBuffer);
        RenderingServer.MultimeshSetBuffer(_speakerPulses.GetRid(), _pulseBuffer);
        _speakers.VisibleInstanceCount = n;
        _speakerPulses.VisibleInstanceCount = n;
    }

    void UpdateBeams(BeamEvents beams, float dt)
    {
        if (dt > 0f)
        {
            for (int i = 0; i < beams.Count; i++)
            {
                int k = _nextBeam;
                _nextBeam = (_nextBeam + 1) % BeamSlots;
                _beamOrigin[k] = beams.Origin[i];
                _beamDir[k] = beams.Direction[i];
                _beamLength[k] = beams.Length[i];
                _beamWidth[k] = beams.Width[i];
                _beamAge[k] = 0f;
            }
        }
        int n = 0;
        for (int k = 0; k < BeamSlots; k++)
        {
            if (_beamLength[k] <= 0f) continue;
            _beamAge[k] += dt;
            if (_beamAge[k] >= BeamFadeSec)
            {
                _beamLength[k] = 0f;
                continue;
            }
            var d = _beamDir[k];
            float len = _beamLength[k], w = _beamWidth[k] * (1f + _beamAge[k] * 2f);
            var mid = _beamOrigin[k] + d * (len * 0.5f);
            Write(_beamBuffer, n * Stride, new Vector3(-d.Y * w, 0, d.X * w), new Vector3(0, 1, 0), new Vector3(d.X * len, 0, d.Y * len),
                new Vector3(mid.X, 0.6f, mid.Y), new Color(BeamColor, 0.6f * (1f - _beamAge[k] / BeamFadeSec)));
            n++;
        }
        RenderingServer.MultimeshSetBuffer(_beams.GetRid(), _beamBuffer);
        _beams.VisibleInstanceCount = n;
    }

    /// <summary>Row-major 3x4 transform from basis columns + origin, then RGBA.</summary>
    static void Write(float[] b, int o, Vector3 x, Vector3 y, Vector3 z, Vector3 origin, Color color)
    {
        b[o] = x.X; b[o + 1] = y.X; b[o + 2] = z.X; b[o + 3] = origin.X;
        b[o + 4] = x.Y; b[o + 5] = y.Y; b[o + 6] = z.Y; b[o + 7] = origin.Y;
        b[o + 8] = x.Z; b[o + 9] = y.Z; b[o + 10] = z.Z; b[o + 11] = origin.Z;
        b[o + 12] = color.R; b[o + 13] = color.G; b[o + 14] = color.B; b[o + 15] = color.A;
    }

    static StandardMaterial3D Glow(Color color, bool vertexColor) => new()
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        BlendMode = BaseMaterial3D.BlendModeEnum.Add,
        CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        AlbedoColor = color,
        VertexColorUseAsAlbedo = vertexColor,
    };

    /// <summary>Instance colors on; materials without vertex color ignore them.</summary>
    static MultiMesh AddMultiMesh(Node3D parent, string name, Mesh mesh, Material material, int count)
    {
        var mm = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = mesh,
            CustomAabb = EnemyRenderer.WorldAabb,
            InstanceCount = count,
            VisibleInstanceCount = 0,
        };
        parent.AddChild(new MultiMeshInstance3D
        {
            Name = name,
            Multimesh = mm,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = material,
        });
        return mm;
    }

    /// <summary>Flat ring in XZ, outer radius 1.</summary>
    static ArrayMesh Annulus(float inner, int segments)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < segments; i++)
        {
            float a0 = MathF.Tau * i / segments, a1 = MathF.Tau * (i + 1) / segments;
            var o0 = new Vector3(MathF.Cos(a0), 0, MathF.Sin(a0));
            var o1 = new Vector3(MathF.Cos(a1), 0, MathF.Sin(a1));
            st.AddVertex(o0 * inner);
            st.AddVertex(o0);
            st.AddVertex(o1);
            st.AddVertex(o0 * inner);
            st.AddVertex(o1);
            st.AddVertex(o1 * inner);
        }
        return st.Commit();
    }
}
