using System;
using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Sim;

namespace Vlg.Core.Run;

/// <summary>
/// The loadout ultimate: charges while idle (seconds, kills or Drops), fires on <see cref="Press"/> when full,
/// then runs its effect. Level comes from <see cref="HeroProgress.UltimateLevel"/> each Update (0 = locked).
/// Durations are in beats of the hero's clock. Press only inside a sim step after the grid build (RunSim queues
/// presses). Writes enemy health: single thread only.
/// </summary>
public sealed class UltimateSystem
{
    /// <summary>Bass Cannon charge-up, beats (1 bar).</summary>
    public const float WindupBeats = 4f;
    /// <summary>Burning line zones live this many beats.</summary>
    const float BurnLineBeats = 8f;
    const float ShakeRadius = 12f;

    readonly SkillWorld _w;
    readonly BeatClock _beat;
    readonly byte _source;
    readonly int[] _query;
    float _left;

    public readonly UltimateDef Def;
    public int Level { get; private set; }
    /// <summary>Progress toward <see cref="ChargeNeeded"/>.</summary>
    public float Charge { get; private set; }
    public UltimatePhase Phase { get; private set; }
    /// <summary>Pressed successfully this step (audio, visuals).</summary>
    public bool FiredThisStep { get; private set; }
    /// <summary>Bass Cannon started firing this step.</summary>
    public bool BeamStartedThisStep { get; private set; }
    /// <summary>Kills to heal for this step (Encore level with heal); RunSim applies.</summary>
    public float HealPerKill => Phase == UltimatePhase.Encore ? Stats.Heal : 0f;

    public UltimateSystem(UltimateDef def, SkillWorld world, BeatClock beat, byte source)
    {
        Def = def;
        _w = world;
        _beat = beat;
        _source = source;
        _query = new int[world.Enemies.Capacity];
    }

    public SkillStats Stats => Def.At(Level);
    public float ChargeNeeded => Stats.Charge;
    public bool Ready => Level > 0 && Phase == UltimatePhase.Idle && Charge >= ChargeNeeded;
    /// <summary>Seconds left in the current phase (0 when idle).</summary>
    public float PhaseLeft => _left;

    /// <summary>Fires if ready. False = locked, charging or already running.</summary>
    public bool Press(float damageMult)
    {
        if (!Ready) return false;
        Charge = 0f;
        FiredThisStep = true;
        var st = Stats;
        switch (Def.Kind)
        {
            case UltimateKind.SubQuake:
                Quake(st, damageMult);
                break;
            case UltimateKind.Encore:
                Phase = UltimatePhase.Encore;
                _left = st.Duration * _beat.BeatSec;
                _beat.ForceDrop = true;
                break;
            case UltimateKind.BassCannon:
                Phase = UltimatePhase.Windup;
                _left = WindupBeats * _beat.BeatSec;
                break;
            default:
                throw new InvalidOperationException($"unknown ultimate kind {Def.Kind}");
        }
        return true;
    }

    /// <summary>Start of a step, before Press: clears the per-step events.</summary>
    public void BeginStep()
    {
        FiredThisStep = false;
        BeamStartedThisStep = false;
    }

    /// <summary>Kill charge (only while idle).</summary>
    public void OnKills(int kills)
    {
        if (Level > 0 && Phase == UltimatePhase.Idle && Def.Charge == ChargeKind.Kills)
            Charge = MathF.Min(ChargeNeeded, Charge + kills);
    }

    /// <summary>Dev shortcut: full charge.</summary>
    public void DevFill()
    {
        if (Level > 0) Charge = ChargeNeeded;
    }

    /// <param name="level">Current ultimate level from HeroProgress.</param>
    public void Update(float dt, int level, float damageMult)
    {
        Level = level;
        if (Level == 0) return;
        if (Phase == UltimatePhase.Idle)
        {
            float gain = Def.Charge switch
            {
                ChargeKind.Cooldown => dt,
                ChargeKind.Drops => _beat.DropStarted ? 1f : 0f,
                _ => 0f,
            };
            Charge = MathF.Min(ChargeNeeded, Charge + gain);
            return;
        }

        _left -= dt;
        var st = Stats;
        switch (Phase)
        {
            case UltimatePhase.Encore:
                if (_left > 0f) return;
                _beat.ForceDrop = false;
                Phase = UltimatePhase.Idle;
                break;
            case UltimatePhase.Windup:
                _w.Beams.Add(_w.Hero, _w.Aim, st.Range, 0.25f, -1);
                if (_left > 0f) return;
                Phase = UltimatePhase.Firing;
                _left = st.Duration * _beat.BeatSec;
                BeamStartedThisStep = true;
                break;
            case UltimatePhase.Firing:
                Fire(st, damageMult);
                if (_left <= 0f) Phase = UltimatePhase.Idle;
                break;
        }
    }

    void Quake(SkillStats st, float damageMult)
    {
        var e = _w.Enemies;
        int n = Around(_w.Hero, st.Radius);
        for (int k = 0; k < n; k++)
        {
            int j = _query[k];
            e.Damage(j, st.Damage * damageMult, _source);
            if (st.Stun > 0f) e.ApplyStun(j, st.Stun);
            _w.Hits.Add(e.Position[j]);
        }
        _w.Flashes.Add(_w.Hero, st.Radius);
        if (st.Tick > 0f) _w.Zones.Spawn(Zone(_w.Hero, ShakeRadius, st.Duration, st.Tick * damageMult));
    }

    /// <summary>Kills every enemy overlapping the beam (void-type: basic mobs die; elites/bosses % max HP later).</summary>
    void Fire(SkillStats st, float damageMult)
    {
        var e = _w.Enemies;
        var hero = _w.Hero;
        var dir = _w.Aim;
        float half = st.Width * 0.5f + _w.EnemyRadius;
        int n = _w.Grid.Query(hero + dir * (st.Range * 0.5f), st.Range * 0.5f + half + _w.QueryPad, _query, e.Position);
        for (int k = 0; k < n; k++)
        {
            int j = _query[k];
            if (e.Health[j] <= 0f) continue;
            var off = e.Position[j] - hero;
            float t = Vector2.Dot(off, dir);
            if (t < -_w.EnemyRadius || t > st.Range + _w.EnemyRadius) continue;
            if ((off - dir * t).LengthSquared() > half * half) continue;
            e.Damage(j, MathF.Max(0f, e.Health[j]) + 1f, _source);
            _w.Hits.Add(e.Position[j]);
        }
        _w.Beams.Add(hero, dir, st.Range, st.Width, -1);
        if (st.Tick <= 0f || !_beat.BeatTick) return;
        float r = st.Width * 0.5f;
        for (float d = r; d < st.Range; d += st.Width)
            _w.Zones.Spawn(Zone(hero + dir * d, r, BurnLineBeats, st.Tick * damageMult));
    }

    ZoneShot Zone(Vector2 center, float radius, float beats, float damage) => new()
    {
        Center = center,
        Radius = radius,
        LifeSec = beats * _beat.BeatSec,
        Damage = damage,
        OnBeat = true,
        BeatSec = _beat.BeatSec,
        Source = _source,
        Slot = -1,
    };

    int Around(Vector2 center, float reach)
    {
        var e = _w.Enemies;
        int n = _w.Grid.Query(center, reach + _w.QueryPad, _query, e.Position);
        int kept = 0;
        for (int k = 0; k < n; k++)
        {
            int j = _query[k];
            if (e.Health[j] > 0f && Vector2.DistanceSquared(e.Position[j], center) <= reach * reach) _query[kept++] = j;
        }
        return kept;
    }
}
