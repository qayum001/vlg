using System;
using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Sim;

namespace Vlg.Core.Run;

/// <summary>
/// Fires unlocked loadout skills on their cooldowns. With a beat clock, cooldown and duration are in beats,
/// a ready skill waits for the next half-beat tick, speakers pulse on beat ticks, and the Drop scales damage
/// and area. Without one (item actives later) they are seconds and fire when ready.
/// Writes enemy health: single thread only.
/// </summary>
public sealed class SkillSystem
{
    /// <summary>Speaker placement samples this many candidate enemies and keeps the most crowded.</summary>
    const int PlacementSamples = 6;
    /// <summary>Aura vuln outlasts the tick interval by this much, so enemies inside stay Distorted.</summary>
    const float VulnLingerSec = 0.25f;

    readonly SkillDef[] _defs;
    readonly SkillWorld _w;
    readonly BeatClock? _beat;
    readonly byte _sourceBase;
    readonly float[] _timer;
    readonly float[] _orbitAngle;
    readonly int[] _query;
    readonly int[] _query2;
    readonly int[] _targets;
    readonly float[] _targetD2;
    double _units;
    XorShift32 _rng;

    /// <summary>Per slot: aura radius of an unlocked Aura skill, else 0. For rendering.</summary>
    public readonly float[] AuraRadius;
    /// <summary>Per slot: radius stat after the Drop multiplier this frame; 0 when locked. For rendering.</summary>
    public readonly float[] CurrentRadius;
    /// <summary>All orbit object centers this frame, [0, BladeCount), with their slot.</summary>
    public readonly Vector2[] BladePositions;
    public readonly int[] BladeSlot;
    public int BladeCount { get; private set; }
    /// <summary>Per slot: true on frames the skill fired. For rendering and audio.</summary>
    public readonly bool[] FiredThisStep;
    /// <summary>Per slot: true on frames its speakers pulsed.</summary>
    public readonly bool[] PulsedThisStep;

    /// <param name="sourceBase">Damage source of slot 0; slot i uses sourceBase + i.</param>
    public SkillSystem(SkillDef[] defs, SkillWorld world, BeatClock? beat, byte sourceBase, uint seed)
    {
        _defs = defs;
        _w = world;
        _beat = beat;
        _sourceBase = sourceBase;
        _rng = new XorShift32(seed);
        _timer = new float[defs.Length];
        _orbitAngle = new float[defs.Length];
        _query = new int[world.Enemies.Capacity];
        _query2 = new int[world.Enemies.Capacity];
        _targets = new int[RunConfig.MaxProjectilesPerCast];
        _targetD2 = new float[RunConfig.MaxProjectilesPerCast];
        AuraRadius = new float[defs.Length];
        CurrentRadius = new float[defs.Length];
        BladePositions = new Vector2[defs.Length * RunConfig.MaxBlades];
        BladeSlot = new int[defs.Length * RunConfig.MaxBlades];
        FiredThisStep = new bool[defs.Length];
        PulsedThisStep = new bool[defs.Length];
    }

    public int Count => _defs.Length;
    public SkillDef Def(int slot) => _defs[slot];
    /// <summary>Seconds per cooldown/duration unit.</summary>
    public float UnitSec => _beat?.BeatSec ?? 1f;

    /// <param name="damageMult">Global damage multiplier (level-up bonus, items).</param>
    public void Update(float dt, HeroProgress progress, float damageMult)
    {
        BladeCount = 0;
        float units = dt / UnitSec;
        double before = _units;
        _units += units;
        bool unitTick = _beat?.BeatTick ?? Math.Floor(_units) > Math.Floor(before);
        bool canFire = _beat?.HalfTick ?? true;
        float dmgMul = damageMult * (_beat?.DamageMultiplier ?? 1f);
        float areaMul = _beat?.AreaMultiplier ?? 1f;

        for (int slot = 0; slot < _defs.Length; slot++)
        {
            FiredThisStep[slot] = false;
            PulsedThisStep[slot] = false;
            AuraRadius[slot] = 0f;
            CurrentRadius[slot] = 0f;
            int level = progress.SkillLevel(slot);
            if (level == 0) continue;
            var def = _defs[slot];
            var st = def.StatsAt(level);
            float damage = st.Damage * dmgMul;
            float radius = st.Radius * areaMul;
            CurrentRadius[slot] = radius;
            byte source = (byte)(_sourceBase + slot);
            int bladeStart = BladeCount;

            if (def.Kind == SkillKind.Aura) AuraRadius[slot] = radius;
            if (def.Kind == SkillKind.Orbit) PlaceBlades(slot, st, dt);

            _timer[slot] -= units;
            if (_timer[slot] <= 0f && canFire)
            {
                if (Fire(slot, def.Kind, st, damage, radius, areaMul, source, bladeStart))
                {
                    FiredThisStep[slot] = true;
                    _timer[slot] = MathF.Max(0f, _timer[slot] + st.Cooldown);
                }
                else
                {
                    // No target / no room: stay ready, retry on the next allowed frame.
                    _timer[slot] = 0f;
                }
            }

            // After Fire: a speaker placed on a beat pulses at once.
            if (def.Kind == SkillKind.Speaker)
            {
                _w.Summons.Age(slot, units, dt);
                if (unitTick) PulsedThisStep[slot] = PulseSpeakers(slot, radius, damage, source);
            }
        }
    }

    bool Fire(int slot, SkillKind kind, SkillStats st, float damage, float radius, float areaMul, byte source, int bladeStart) => kind switch
    {
        SkillKind.Projectile => CastProjectiles(st, damage, source),
        SkillKind.Ring => _w.Rings.Spawn(_w.Hero, radius, st.Speed, damage, st.Knockback, source),
        SkillKind.Speaker => PlaceSpeaker(slot, st, radius),
        SkillKind.Aura => CastAura(radius, damage, st.Vuln, st.Cooldown * UnitSec + VulnLingerSec, source),
        SkillKind.Beam => CastBeam(slot, st.Range, st.Width * areaMul, damage, source),
        SkillKind.Orbit => CastOrbit(bladeStart, radius, damage, source),
        _ => throw new InvalidOperationException($"unknown skill kind {kind}"),
    };

    void PlaceBlades(int slot, SkillStats st, float dt)
    {
        _orbitAngle[slot] = (_orbitAngle[slot] + st.Speed * dt) % MathF.Tau;
        int n = st.Amount;
        for (int b = 0; b < n; b++)
        {
            float a = _orbitAngle[slot] + MathF.Tau * b / n;
            BladeSlot[BladeCount] = slot;
            BladePositions[BladeCount++] = _w.Hero + new Vector2(MathF.Sin(a), MathF.Cos(a)) * st.Range;
        }
    }

    /// <summary>Projectiles at the nearest enemies in range; extra projectiles cycle the targets. False = no target.</summary>
    bool CastProjectiles(SkillStats st, float damage, byte source)
    {
        var hero = _w.Hero;
        int found = NearestInRange(hero, st.Range, st.Amount);
        if (found == 0) return false;
        float life = st.Range / st.Speed;
        for (int k = 0; k < st.Amount; k++)
        {
            var dir = _w.Enemies.Position[_targets[k % found]] - hero;
            float len = dir.Length();
            dir = len > 1e-4f ? dir / len : Vector2.UnitY;
            _w.Projectiles.Spawn(hero + dir * _w.HeroRadius, dir * st.Speed, life, damage, source);
        }
        return true;
    }

    bool CastAura(float radius, float damage, float vuln, float vulnSec, byte source)
    {
        var e = _w.Enemies;
        int n = Around(_w.Hero, radius + _w.EnemyRadius, _query);
        for (int k = 0; k < n; k++)
        {
            int j = _query[k];
            e.Damage(j, damage, source);
            if (vuln > 0f) e.ApplyVuln(j, vuln, vulnSec);
            _w.Hits.Add(e.Position[j]);
        }
        return true;
    }

    /// <summary>Instant hit on every enemy overlapping the line from the hero along the aim direction.</summary>
    bool CastBeam(int slot, float length, float width, float damage, byte source)
    {
        var e = _w.Enemies;
        var hero = _w.Hero;
        var dir = _w.Aim;
        float half = width * 0.5f + _w.EnemyRadius;
        float reach = length * 0.5f + half;
        int n = _w.Grid.Query(hero + dir * (length * 0.5f), reach + _w.QueryPad, _query, e.Position);
        for (int k = 0; k < n; k++)
        {
            int j = _query[k];
            if (e.Health[j] <= 0f) continue;
            var off = e.Position[j] - hero;
            float t = Vector2.Dot(off, dir);
            if (t < -_w.EnemyRadius || t > length + _w.EnemyRadius) continue;
            if ((off - dir * t).LengthSquared() > half * half) continue;
            e.Damage(j, damage, source);
            _w.Hits.Add(e.Position[j]);
        }
        _w.Beams.Add(hero, dir, length, width, slot);
        return true;
    }

    bool CastOrbit(int bladeStart, float radius, float damage, byte source)
    {
        var e = _w.Enemies;
        for (int b = bladeStart; b < BladeCount; b++)
        {
            int n = Around(BladePositions[b], radius + _w.EnemyRadius, _query);
            for (int k = 0; k < n; k++)
            {
                int j = _query[k];
                e.Damage(j, damage, source);
                _w.Hits.Add(e.Position[j]);
            }
        }
        return true;
    }

    /// <summary>On the enemy with the most neighbors among a few random enemies in range. False = none in range or at the cap.</summary>
    bool PlaceSpeaker(int slot, SkillStats st, float radius)
    {
        if (_w.Summons.CountOf(slot) >= st.Amount) return false;
        int n = Around(_w.Hero, st.Range, _query);
        if (n == 0) return false;
        int best = -1, bestCount = -1;
        for (int s = 0; s < PlacementSamples; s++)
        {
            int j = _query[_rng.Below(n)];
            int around = Around(_w.Enemies.Position[j], radius + _w.EnemyRadius, _query2);
            if (around > bestCount)
            {
                best = j;
                bestCount = around;
            }
        }
        return _w.Summons.Spawn(_w.Enemies.Position[best], slot, st.Duration);
    }

    bool PulseSpeakers(int slot, float radius, float damage, byte source)
    {
        var s = _w.Summons;
        var e = _w.Enemies;
        bool any = false;
        for (int i = 0; i < s.Count; i++)
        {
            if (s.Slot[i] != slot) continue;
            any = true;
            s.SincePulse[i] = 0f;
            int n = Around(s.Position[i], radius + _w.EnemyRadius, _query);
            for (int k = 0; k < n; k++)
            {
                int j = _query[k];
                e.Damage(j, damage, source);
                _w.Hits.Add(e.Position[j]);
            }
        }
        return any;
    }

    /// <summary>Living enemies whose center is within <paramref name="reach"/> of <paramref name="center"/>.</summary>
    int Around(Vector2 center, float reach, int[] results)
    {
        var e = _w.Enemies;
        float reach2 = reach * reach;
        int n = _w.Grid.Query(center, reach + _w.QueryPad, results, e.Position);
        int kept = 0;
        for (int k = 0; k < n; k++)
        {
            int j = results[k];
            if (e.Health[j] <= 0f) continue;
            if (Vector2.DistanceSquared(e.Position[j], center) > reach2) continue;
            results[kept++] = j;
        }
        return kept;
    }

    /// <summary>Up to <paramref name="k"/> nearest living enemies within range into _targets, nearest first. Linear scan.</summary>
    int NearestInRange(Vector2 hero, float range, int k)
    {
        var e = _w.Enemies;
        float range2 = range * range;
        int found = 0;
        for (int i = 0; i < e.Count; i++)
        {
            if (e.Health[i] <= 0f) continue;
            float d2 = Vector2.DistanceSquared(e.Position[i], hero);
            if (d2 > range2) continue;
            if (found == k && d2 >= _targetD2[found - 1]) continue;
            int at = found < k ? found++ : found - 1;
            while (at > 0 && _targetD2[at - 1] > d2)
            {
                _targets[at] = _targets[at - 1];
                _targetD2[at] = _targetD2[at - 1];
                at--;
            }
            _targets[at] = i;
            _targetD2[at] = d2;
        }
        return found;
    }
}
