using System;
using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Sim;

namespace Vlg.Core.Run;

/// <summary>
/// Fires unlocked loadout skills in their current form (level + branch choices, re-resolved when
/// <see cref="HeroProgress.Version"/> changes). With a beat clock, cooldown and duration are in beats, a ready
/// skill waits for the next half-beat tick, beat-driven parts (speaker pulses, scratches, zones) follow beat ticks,
/// and the Drop scales damage and area. Without one (item actives later) they are seconds and fire when ready.
/// Writes enemy health: single thread only.
/// </summary>
public sealed class SkillSystem
{
    /// <summary>Placement samples this many candidate enemies and keeps the most crowded.</summary>
    const int PlacementSamples = 6;
    /// <summary>Aura vuln outlasts the tick interval by this much, so enemies inside stay Distorted.</summary>
    const float VulnLingerSec = 0.25f;
    const float SpeakerRingRadius = 4f;
    const float PuddleRadius = 1.3f;
    const float PuddleSpacing = 1f;
    const float DroneOrbitRadius = 1.6f;
    const float DroneSpin = 1.2f;
    const float DroneExplodeFactor = 0.6f;
    const float BellFallUnits = 0.5f;
    const float RampStep = 0.25f;
    const float RampMax = 2f;
    const float SplitAngle = MathF.PI / 6f;
    const float SplitLength = 0.4f;
    const float EndBurstRadius = 2f;
    const float DeathBurstRadius = 1.5f;
    const float DeathBurstFactor = 2f;
    const float CrushRadius = 1f;
    const float SpinbackRange = 12f;
    const float DoubleDeckGap = 1.5f;
    const float ScratchReach = 1.5f;
    const float ScratchPull = 8f;
    const float LinkWidth = 1f;
    const float SidechainBonus = 0.15f;
    const float TwinSpeed = 1.5f;
    const float TwinDamage = 0.5f;

    readonly SkillDef[] _defs;
    readonly SkillWorld _w;
    readonly BeatClock? _beat;
    readonly byte _sourceBase;
    readonly SkillForm?[] _forms;
    readonly float[] _timer;
    readonly float[] _orbitAngle;
    readonly float[] _orbitDir;
    readonly int[] _casts;
    readonly int[] _pulses;
    readonly int[] _beats;
    /// <summary>Sweep progress [0, 1); negative = not sweeping.</summary>
    readonly float[] _sweepT;
    readonly float[] _sweepBase;
    readonly bool[] _shield;
    readonly Vector2[] _puddleAt;
    readonly bool[] _hasPuddle;
    readonly float[] _droneAngle;
    readonly int[] _query;
    readonly int[] _query2;
    readonly int[] _targets;
    readonly float[] _targetD2;
    readonly Vector2[] _expired = new Vector2[RunConfig.MaxSummonsPerSkill];
    int _formsVersion = -1;
    float _dmgMul = 1f;
    double _units;
    XorShift32 _rng;

    /// <summary>Per slot: aura radius of an unlocked Aura skill, else 0. For rendering.</summary>
    public readonly float[] AuraRadius;
    /// <summary>Per slot: radius stat after Drop multipliers this frame; 0 when locked. For rendering.</summary>
    public readonly float[] CurrentRadius;
    /// <summary>All orbit object centers this frame, [0, BladeCount), with their slot.</summary>
    public readonly Vector2[] BladePositions;
    public readonly int[] BladeSlot;
    public int BladeCount { get; private set; }
    /// <summary>All drone centers this frame, [0, DroneCount), with their slot.</summary>
    public readonly Vector2[] DronePositions;
    public readonly int[] DroneSlot;
    public int DroneCount { get; private set; }
    /// <summary>Per slot: true on frames the skill fired. For rendering and audio.</summary>
    public readonly bool[] FiredThisStep;
    /// <summary>Per slot: true on frames its speakers pulsed.</summary>
    public readonly bool[] PulsedThisStep;
    /// <summary>Per slot: a charged shield.</summary>
    public readonly bool[] ShieldUp;
    /// <summary>A shield blocked contact damage this step.</summary>
    public bool BlockedThisStep { get; private set; }

    /// <param name="sourceBase">Damage source of slot 0; slot i uses sourceBase + i.</param>
    public SkillSystem(SkillDef[] defs, SkillWorld world, BeatClock? beat, byte sourceBase, uint seed)
    {
        int n = defs.Length;
        _defs = defs;
        _w = world;
        _beat = beat;
        _sourceBase = sourceBase;
        _rng = new XorShift32(seed);
        _forms = new SkillForm?[n];
        _timer = new float[n];
        _orbitAngle = new float[n];
        _orbitDir = new float[n];
        Array.Fill(_orbitDir, 1f);
        _casts = new int[n];
        _pulses = new int[n];
        _beats = new int[n];
        _sweepT = new float[n];
        Array.Fill(_sweepT, -1f);
        _sweepBase = new float[n];
        _shield = new bool[n];
        _puddleAt = new Vector2[n];
        _hasPuddle = new bool[n];
        _droneAngle = new float[n];
        _query = new int[world.Enemies.Capacity];
        _query2 = new int[world.Enemies.Capacity];
        _targets = new int[RunConfig.MaxProjectilesPerCast];
        _targetD2 = new float[RunConfig.MaxProjectilesPerCast];
        AuraRadius = new float[n];
        CurrentRadius = new float[n];
        BladePositions = new Vector2[n * RunConfig.MaxBlades];
        BladeSlot = new int[n * RunConfig.MaxBlades];
        DronePositions = new Vector2[n * RunConfig.MaxDrones];
        DroneSlot = new int[n * RunConfig.MaxDrones];
        FiredThisStep = new bool[n];
        PulsedThisStep = new bool[n];
        ShieldUp = new bool[n];
    }

    public int Count => _defs.Length;
    public SkillDef Def(int slot) => _defs[slot];
    /// <summary>Current form; null while locked. Valid after the first Update.</summary>
    public SkillForm? Form(int slot) => _forms[slot];
    /// <summary>Seconds per cooldown/duration unit.</summary>
    public float UnitSec => _beat?.BeatSec ?? 1f;

    /// <summary>Re-resolves forms when the progress changed (also called by Update).</summary>
    public void Refresh(HeroProgress progress)
    {
        if (progress.Version == _formsVersion) return;
        _formsVersion = progress.Version;
        for (int slot = 0; slot < _defs.Length; slot++)
        {
            int level = progress.SkillLevel(slot);
            var before = _forms[slot]?.Kind;
            _forms[slot] = level == 0 ? null : _defs[slot].Resolve(level, progress.Path(slot), progress.BranchLevels);
            if (before != _forms[slot]?.Kind) ResetSlot(slot);
        }
    }

    /// <param name="damageMult">Global damage multiplier (level-up bonus, items).</param>
    public void Update(float dt, HeroProgress progress, float damageMult)
    {
        Refresh(progress);
        BladeCount = 0;
        DroneCount = 0;
        BlockedThisStep = false;
        float units = dt / UnitSec;
        double before = _units;
        _units += units;
        bool unitTick = _beat?.BeatTick ?? Math.Floor(_units) > Math.Floor(before);
        bool canFire = _beat?.HalfTick ?? true;
        bool inDrop = _beat?.InDrop ?? false;
        float dmgMul = damageMult * (_beat?.DamageMultiplier ?? 1f);
        float areaMul = _beat?.AreaMultiplier ?? 1f;
        if (SidechainActive()) dmgMul *= 1f + SidechainBonus;
        _dmgMul = dmgMul;

        for (int slot = 0; slot < _defs.Length; slot++)
        {
            FiredThisStep[slot] = false;
            PulsedThisStep[slot] = false;
            AuraRadius[slot] = 0f;
            CurrentRadius[slot] = 0f;
            ShieldUp[slot] = false;
            var form = _forms[slot];
            if (form == null) continue;
            var st = form.Stats;
            float boost = form.Has(SkillFlag.DropDouble) && inDrop ? 2f : 1f;
            float damage = st.Damage * dmgMul * boost;
            float radius = st.Radius * areaMul * boost;
            CurrentRadius[slot] = radius;
            byte source = (byte)(_sourceBase + slot);
            int bladeStart = BladeCount;
            int droneStart = DroneCount;

            switch (form.Kind)
            {
                case SkillKind.Aura:
                    AuraRadius[slot] = radius;
                    if (form.Has(SkillFlag.Trail) && canFire) DropPuddle(slot, form, damage, source);
                    break;
                case SkillKind.Orbit:
                    PlaceBlades(slot, form, dt, inDrop);
                    if (form.Has(SkillFlag.Scratch) && unitTick && ++_beats[slot] % Math.Max(1, (int)st.Period) == 0)
                        Scratch(slot, st, radius, damage, source);
                    break;
                case SkillKind.Drone:
                    PlaceDrones(slot, st, dt);
                    break;
                case SkillKind.Sweep:
                    AdvanceSweep(slot, st, damage, areaMul, units, source);
                    break;
                case SkillKind.Shield:
                    ShieldUp[slot] = _shield[slot];
                    break;
            }

            float cooldown = form.Has(SkillFlag.DropEveryBeat) && inDrop ? MathF.Min(st.Cooldown, 1f) : st.Cooldown;
            _timer[slot] -= units;
            if (_timer[slot] <= 0f && canFire)
            {
                if (Fire(slot, form, damage, radius, areaMul, inDrop, source, bladeStart, droneStart))
                {
                    FiredThisStep[slot] = true;
                    _timer[slot] = MathF.Max(0f, _timer[slot] + cooldown);
                }
                else
                {
                    // No target / no room / already charged: stay ready, retry on the next allowed frame.
                    _timer[slot] = 0f;
                }
            }

            // After Fire: a speaker placed on a beat pulses at once.
            if (form.Kind == SkillKind.Speaker)
            {
                int expired = _w.Summons.Age(slot, units, dt, _expired);
                if (form.Has(SkillFlag.Blowout))
                    for (int k = 0; k < expired; k++) AreaHit(_expired[k], radius * 2f, damage * 6f, source, true);
                if (unitTick) PulsedThisStep[slot] = PulseSpeakers(slot, form, radius, damage, source);
            }
        }
    }

    /// <summary>
    /// Contact damage is about to land: a charged shield takes it instead, blasts enemies around the hero
    /// and starts recharging. False = no shield up.
    /// </summary>
    public bool TryBlock()
    {
        for (int slot = 0; slot < _defs.Length; slot++)
        {
            var form = _forms[slot];
            if (form == null || form.Kind != SkillKind.Shield || !_shield[slot]) continue;
            _shield[slot] = false;
            ShieldUp[slot] = false;
            var st = form.Stats;
            _timer[slot] = st.Cooldown;
            var e = _w.Enemies;
            float radius = CurrentRadius[slot];
            int n = Around(_w.Hero, radius + _w.EnemyRadius, _query);
            for (int k = 0; k < n; k++)
            {
                int j = _query[k];
                e.Damage(j, st.Damage * _dmgMul, (byte)(_sourceBase + slot));
                _w.Hits.Add(e.Position[j]);
                if (st.Knockback > 0f) e.AddPush(j, Away(e.Position[j], _w.Hero) * st.Knockback);
            }
            _w.Flashes.Add(_w.Hero, radius);
            BlockedThisStep = true;
            return true;
        }
        return false;
    }

    /// <summary>After deaths were removed this step: Aura death bursts for deaths inside the aura.</summary>
    public void OnDeaths(PointBuffer deaths)
    {
        for (int slot = 0; slot < _defs.Length; slot++)
        {
            var form = _forms[slot];
            if (form == null || form.Kind != SkillKind.Aura || !form.Has(SkillFlag.DeathBurst)) continue;
            float reach = AuraRadius[slot] + _w.EnemyRadius;
            float damage = form.Stats.Damage * _dmgMul * DeathBurstFactor;
            for (int i = 0; i < deaths.Count; i++)
            {
                var p = deaths.Items[i];
                if (Vector2.DistanceSquared(p, _w.Hero) > reach * reach) continue;
                AreaHit(p, DeathBurstRadius, damage, (byte)(_sourceBase + slot), true);
            }
        }
    }

    bool Fire(int slot, SkillForm form, float damage, float radius, float areaMul, bool inDrop, byte source, int bladeStart, int droneStart)
    {
        var st = form.Stats;
        switch (form.Kind)
        {
            case SkillKind.Projectile: return CastProjectiles(form, damage, source);
            case SkillKind.Ring: return CastRing(form, damage, radius, source);
            case SkillKind.Speaker: return PlaceSpeakers(slot, form, radius);
            case SkillKind.Aura: return CastAura(radius, damage, st.Vuln, st.Cooldown * UnitSec + VulnLingerSec, source);
            case SkillKind.Beam: return CastBeams(slot, form, st.Width * areaMul, damage, source);
            case SkillKind.Orbit: return CastOrbit(form, bladeStart, radius, damage, source);
            case SkillKind.BellDrop: return CastBells(slot, st, radius, damage, source);
            case SkillKind.Quake: return CastQuake(slot, form, radius, damage, inDrop, source);
            case SkillKind.Drone: return CastDrones(form, droneStart, damage, source);
            case SkillKind.Shield:
                if (_shield[slot]) return false;
                _shield[slot] = true;
                ShieldUp[slot] = true;
                return true;
            case SkillKind.Sweep:
                _sweepT[slot] = 0f;
                _sweepBase[slot] = Angle(_w.Facing) - st.Arc * (MathF.PI / 180f) * 0.5f;
                return true;
            case SkillKind.Boomerang: return CastBoomerangs(form, damage, source);
            default: throw new InvalidOperationException($"unknown skill kind {form.Kind}");
        }
    }

    void ResetSlot(int slot)
    {
        _timer[slot] = 0f;
        _sweepT[slot] = -1f;
        _shield[slot] = false;
        _hasPuddle[slot] = false;
        _casts[slot] = 0;
        _pulses[slot] = 0;
        _beats[slot] = 0;
        _orbitDir[slot] = 1f;
    }

    bool SidechainActive()
    {
        for (int slot = 0; slot < _defs.Length; slot++)
        {
            var f = _forms[slot];
            if (f != null && f.Kind == SkillKind.Shield && f.Has(SkillFlag.Sidechain) && _shield[slot]) return true;
        }
        return false;
    }

    // ---- Projectiles ----

    /// <summary>At the nearest enemies in range; extra shots cycle the targets. False = no target.</summary>
    bool CastProjectiles(SkillForm form, float damage, byte source)
    {
        var st = form.Stats;
        var hero = _w.Hero;
        int found = NearestInRange(hero, st.Range, st.Amount);
        if (found == 0) return false;
        var shot = new ProjectileShot
        {
            Life = st.Range / st.Speed,
            Damage = damage,
            Source = source,
            Radius = st.Radius,
            Pierce = st.PierceAmount,
            Bounces = st.BounceAmount,
            Falloff = st.Falloff,
            BurstOnBounce = form.Has(SkillFlag.BurstOnBounce),
        };
        for (int k = 0; k < st.Amount; k++)
        {
            var dir = Toward(_w.Enemies.Position[_targets[k % found]], hero);
            shot.Position = hero + dir * _w.HeroRadius;
            shot.Velocity = dir * st.Speed;
            _w.Projectiles.Spawn(shot);
            if (!form.Has(SkillFlag.Twin)) continue;
            var twin = shot;
            twin.Velocity *= TwinSpeed;
            twin.Damage *= TwinDamage;
            twin.Life /= TwinSpeed;
            _w.Projectiles.Spawn(twin);
        }
        return true;
    }

    /// <summary>Out to random enemies in range and back to the hero. False = none in range.</summary>
    bool CastBoomerangs(SkillForm form, float damage, byte source)
    {
        var st = form.Stats;
        var hero = _w.Hero;
        int n = Around(hero, st.Range, _query);
        if (n == 0) return false;
        var shot = new ProjectileShot
        {
            Life = st.Range / st.Speed,
            Damage = damage,
            Source = source,
            Radius = st.Radius,
            Bounces = st.BounceAmount,
            Returns = true,
        };
        for (int k = 0; k < st.Amount; k++)
        {
            var dir = Toward(_w.Enemies.Position[_query[_rng.Below(n)]], hero);
            shot.Position = hero + dir * _w.HeroRadius;
            shot.Velocity = dir * st.Speed;
            _w.Projectiles.Spawn(shot);
        }
        return true;
    }

    // ---- Rings, zones, strikes ----

    bool CastRing(SkillForm form, float damage, float radius, byte source)
    {
        var st = form.Stats;
        return _w.Rings.Spawn(new RingShot
        {
            Center = _w.Hero,
            MaxRadius = radius,
            Speed = st.Speed,
            Damage = damage,
            Knockback = st.Knockback,
            Slow = st.Slow,
            SlowSec = st.SlowSec,
            Stun = st.Stun,
            DistanceDamage = form.Has(SkillFlag.DistanceDamage),
            Collapse = form.Has(SkillFlag.Collapse),
            Source = source,
        });
    }

    bool CastQuake(int slot, SkillForm form, float radius, float damage, bool inDrop, byte source)
    {
        var st = form.Stats;
        _w.Zones.Spawn(new ZoneShot
        {
            Center = _w.Hero,
            Radius = radius,
            LifeSec = st.Duration * UnitSec,
            Damage = damage,
            Slow = st.Slow,
            OnBeat = _beat != null,
            BeatSec = UnitSec,
            Interval = UnitSec,
            Source = source,
            Slot = slot,
        });
        _casts[slot]++;
        if (form.Has(SkillFlag.Aftershock) && (inDrop || _casts[slot] % Math.Max(1, (int)st.Period) == 0))
            AreaHit(_w.Hero, radius, damage * 4f, source, true);
        return true;
    }

    /// <summary>Aura trail: a puddle every half-beat once the hero moved PuddleSpacing from the last one.</summary>
    void DropPuddle(int slot, SkillForm form, float damage, byte source)
    {
        if (_hasPuddle[slot] && Vector2.DistanceSquared(_w.Hero, _puddleAt[slot]) < PuddleSpacing * PuddleSpacing) return;
        var st = form.Stats;
        _w.Zones.Spawn(new ZoneShot
        {
            Center = _w.Hero,
            Radius = PuddleRadius,
            LifeSec = st.Duration * UnitSec,
            Damage = damage,
            Slow = st.Slow,
            Vuln = st.Vuln,
            OnBeat = _beat != null,
            BeatSec = UnitSec,
            Interval = UnitSec,
            Zap = form.Has(SkillFlag.Zap),
            Source = source,
            Slot = slot,
        });
        _puddleAt[slot] = _w.Hero;
        _hasPuddle[slot] = true;
    }

    bool CastBells(int slot, SkillStats st, float radius, float damage, byte source)
    {
        int n = Around(_w.Hero, st.Range, _query);
        if (n == 0) return false;
        for (int b = 0; b < st.Amount; b++)
            _w.Strikes.Spawn(_w.Enemies.Position[Crowded(n, radius)], radius, BellFallUnits * UnitSec, damage, st.Stun, source, slot);
        return true;
    }

    // ---- Speakers ----

    bool PlaceSpeakers(int slot, SkillForm form, float radius)
    {
        var st = form.Stats;
        int free = st.Amount - _w.Summons.CountOf(slot);
        if (free <= 0) return false;
        float life = st.Duration;
        if (form.Has(SkillFlag.RingPlacement))
        {
            int k = Math.Min(3, free);
            float start = _casts[slot]++ * 0.7f;
            for (int i = 0; i < k; i++)
                _w.Summons.Spawn(_w.Hero + Dir(start + MathF.Tau * i / 3f) * SpeakerRingRadius, slot, life);
            return true;
        }
        int n = Around(_w.Hero, st.Range, _query);
        if (n == 0) return false;
        return _w.Summons.Spawn(_w.Enemies.Position[Crowded(n, radius)], slot, life);
    }

    bool PulseSpeakers(int slot, SkillForm form, float radius, float damage, byte source)
    {
        var s = _w.Summons;
        var e = _w.Enemies;
        var st = form.Stats;
        if (s.CountOf(slot) == 0) return false;
        bool big = form.Has(SkillFlag.BigPulse) && ++_pulses[slot] % Math.Max(1, (int)st.Period) == 0;
        float r = big ? radius * 1.5f : radius;
        float dmg = big ? damage * 3f : damage;
        float crush = CrushRadius + _w.EnemyRadius;
        int prev = -1;
        for (int i = 0; i < s.Count; i++)
        {
            if (s.Slot[i] != slot) continue;
            var c = s.Position[i];
            s.SincePulse[i] = 0f;
            int n = Around(c, r + _w.EnemyRadius, _query);
            float mult = form.Has(SkillFlag.CrowdScaling) ? MathF.Min(3f, 1f + 0.1f * n) : 1f;
            for (int k = 0; k < n; k++)
            {
                int j = _query[k];
                e.Damage(j, dmg * mult, source);
                _w.Hits.Add(e.Position[j]);
                if (st.Pull > 0f) e.AddPush(j, Toward(c, e.Position[j]) * st.Pull);
                if (form.Has(SkillFlag.Crush) && Vector2.DistanceSquared(e.Position[j], c) <= crush * crush)
                    e.Damage(j, MathF.Max(0f, e.Health[j]) + 1f, source);
            }
            if (form.Has(SkillFlag.Link) && prev >= 0)
            {
                var a = s.Position[prev];
                var off = c - a;
                float len = off.Length();
                if (len > 1e-3f) LineHit(slot, a, off / len, len, LinkWidth, dmg, source, false, 0.3f);
            }
            if (big) _w.Flashes.Add(c, r);
            prev = i;
        }
        return true;
    }

    // ---- Aura, beams, sweep ----

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

    /// <summary>Count beams evenly around the aim direction; end bursts and splits per flags.</summary>
    bool CastBeams(int slot, SkillForm form, float width, float damage, byte source)
    {
        var st = form.Stats;
        float aim = Angle(_w.Aim);
        bool ramp = form.Has(SkillFlag.Ramp);
        for (int d = 0; d < st.Amount; d++)
        {
            float a = aim + MathF.Tau * d / st.Amount;
            var dir = d == 0 ? _w.Aim : Dir(a);
            LineHit(slot, _w.Hero, dir, st.Range, width, damage, source, ramp, width);
            var end = _w.Hero + dir * st.Range;
            if (form.Has(SkillFlag.EndBurst)) AreaHit(end, EndBurstRadius, damage, source, true);
            if (!form.Has(SkillFlag.Split)) continue;
            for (int k = -1; k <= 1; k++)
                LineHit(slot, end, Dir(a + k * SplitAngle), st.Range * SplitLength, width, damage, source, ramp, width);
        }
        return true;
    }

    /// <summary>Hits each enemy whose angle the beam passes this step (center test), then records the beam.</summary>
    void AdvanceSweep(int slot, SkillStats st, float damage, float areaMul, float units, byte source)
    {
        float t0 = _sweepT[slot];
        if (t0 < 0f) return;
        float t1 = MathF.Min(1f, t0 + units / st.Duration);
        float arc = st.Arc * (MathF.PI / 180f);
        var e = _w.Enemies;
        int n = Around(_w.Hero, st.Range + _w.EnemyRadius, _query);
        for (int b = 0; b < st.Amount; b++)
        {
            float start = _sweepBase[slot] + MathF.Tau * b / st.Amount;
            for (int k = 0; k < n; k++)
            {
                int j = _query[k];
                float rel = Wrap(Angle(e.Position[j] - _w.Hero) - start);
                bool crossed = t0 == 0f ? rel <= arc * t1 : rel > arc * t0 && rel <= arc * t1;
                if (!crossed) continue;
                e.Damage(j, damage, source);
                _w.Hits.Add(e.Position[j]);
            }
            _w.Beams.Add(_w.Hero, Dir(start + arc * t1), st.Range, st.Width * areaMul, slot);
        }
        _sweepT[slot] = t1 >= 1f ? -1f : t1;
    }

    /// <summary>Damages enemies overlapping the segment; ramp adds heat (more damage while it keeps hitting).</summary>
    void LineHit(int slot, Vector2 origin, Vector2 dir, float length, float width, float damage, byte source, bool ramp, float drawWidth)
    {
        var e = _w.Enemies;
        float half = width * 0.5f + _w.EnemyRadius;
        float reach = length * 0.5f + half;
        int n = _w.Grid.Query(origin + dir * (length * 0.5f), reach + _w.QueryPad, _query2, e.Position);
        for (int k = 0; k < n; k++)
        {
            int j = _query2[k];
            if (e.Health[j] <= 0f) continue;
            var off = e.Position[j] - origin;
            float t = Vector2.Dot(off, dir);
            if (t < -_w.EnemyRadius || t > length + _w.EnemyRadius) continue;
            if ((off - dir * t).LengthSquared() > half * half) continue;
            float dmg = damage;
            if (ramp)
            {
                dmg *= 1f + e.Heat[j];
                e.Heat[j] = MathF.Min(RampMax, e.Heat[j] + RampStep);
            }
            e.Damage(j, dmg, source);
            _w.Hits.Add(e.Position[j]);
        }
        _w.Beams.Add(origin, dir, length, drawWidth, slot);
    }

    // ---- Orbit, drones ----

    void PlaceBlades(int slot, SkillForm form, float dt, bool inDrop)
    {
        var st = form.Stats;
        _orbitAngle[slot] = (_orbitAngle[slot] + st.Speed * dt * _orbitDir[slot]) % MathF.Tau;
        float range = st.Range;
        if (form.Has(SkillFlag.Spinback) && inDrop && _beat != null)
        {
            float barPhase = (_beat.BeatInBar + _beat.BeatPhase) / BeatClock.BeatsPerBar;
            range += (SpinbackRange - range) * MathF.Sin(MathF.PI * barPhase);
        }
        int n = st.Amount;
        for (int b = 0; b < n; b++) AddBlade(slot, _orbitAngle[slot] + MathF.Tau * b / n, range);
        if (!form.Has(SkillFlag.DoubleDeck)) return;
        for (int b = 0; b < n; b++) AddBlade(slot, -_orbitAngle[slot] + MathF.Tau * (b + 0.5f) / n, range + DoubleDeckGap);
    }

    void AddBlade(int slot, float angle, float range)
    {
        BladeSlot[BladeCount] = slot;
        BladePositions[BladeCount++] = _w.Hero + Dir(angle) * range;
    }

    bool CastOrbit(SkillForm form, int bladeStart, float radius, float damage, byte source)
    {
        var e = _w.Enemies;
        var st = form.Stats;
        // Burn scales with the same multipliers as the hit (Drop, bonuses).
        float burn = st.Burn * damage / st.Damage;
        for (int b = bladeStart; b < BladeCount; b++)
        {
            int n = Around(BladePositions[b], radius + _w.EnemyRadius, _query);
            for (int k = 0; k < n; k++)
            {
                int j = _query[k];
                e.Damage(j, damage, source);
                if (burn > 0f) e.ApplyBurn(j, burn, st.BurnSec, source);
                _w.Hits.Add(e.Position[j]);
            }
        }
        return true;
    }

    /// <summary>Orbit reverses with a burst over the ring area that pulls enemies onto the ring.</summary>
    void Scratch(int slot, SkillStats st, float radius, float damage, byte source)
    {
        _orbitDir[slot] = -_orbitDir[slot];
        var e = _w.Enemies;
        float reach = st.Range + radius + ScratchReach;
        float pull = st.Pull > 0f ? st.Pull : ScratchPull;
        int n = Around(_w.Hero, reach + _w.EnemyRadius, _query);
        for (int k = 0; k < n; k++)
        {
            int j = _query[k];
            e.Damage(j, damage * 2f, source);
            _w.Hits.Add(e.Position[j]);
            var onRing = _w.Hero + Away(e.Position[j], _w.Hero) * st.Range;
            e.AddPush(j, Toward(onRing, e.Position[j]) * pull);
        }
        _w.Flashes.Add(_w.Hero, reach);
    }

    void PlaceDrones(int slot, SkillStats st, float dt)
    {
        _droneAngle[slot] = (_droneAngle[slot] + DroneSpin * dt) % MathF.Tau;
        int n = st.Amount;
        for (int d = 0; d < n; d++)
        {
            DroneSlot[DroneCount] = slot;
            DronePositions[DroneCount++] = _w.Hero + Dir(_droneAngle[slot] + MathF.Tau * d / n) * DroneOrbitRadius;
        }
    }

    bool CastDrones(SkillForm form, int droneStart, float damage, byte source)
    {
        var st = form.Stats;
        bool any = false;
        var shot = new ProjectileShot
        {
            Life = st.Range / st.Speed,
            Damage = damage,
            Source = source,
            ExplodeRadius = form.Has(SkillFlag.ExplodeOnHit) ? st.Radius : 0f,
            ExplodeFactor = DroneExplodeFactor,
        };
        for (int d = droneStart; d < DroneCount; d++)
        {
            var from = DronePositions[d];
            if (NearestInRange(from, st.Range, 1) == 0) continue;
            var dir = Toward(_w.Enemies.Position[_targets[0]], from);
            shot.Position = from;
            shot.Velocity = dir * st.Speed;
            _w.Projectiles.Spawn(shot);
            any = true;
        }
        return any;
    }

    // ---- Shared helpers ----

    void AreaHit(Vector2 center, float radius, float damage, byte source, bool flash)
    {
        var e = _w.Enemies;
        int n = Around(center, radius + _w.EnemyRadius, _query2);
        for (int k = 0; k < n; k++)
        {
            int j = _query2[k];
            e.Damage(j, damage, source);
            _w.Hits.Add(e.Position[j]);
        }
        if (flash) _w.Flashes.Add(center, radius);
    }

    /// <summary>Among <paramref name="n"/> enemies in _query, the most crowded of a few random samples.</summary>
    int Crowded(int n, float radius)
    {
        int best = _query[0], bestCount = -1;
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
        return best;
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
    int NearestInRange(Vector2 from, float range, int k)
    {
        var e = _w.Enemies;
        float range2 = range * range;
        int found = 0;
        for (int i = 0; i < e.Count; i++)
        {
            if (e.Health[i] <= 0f) continue;
            float d2 = Vector2.DistanceSquared(e.Position[i], from);
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

    /// <summary>Ground direction for angle a: (sin a, cos a), as orbit and sweep use.</summary>
    static Vector2 Dir(float a) => new(MathF.Sin(a), MathF.Cos(a));

    static float Angle(Vector2 v) => MathF.Atan2(v.X, v.Y);

    /// <summary>[0, 2pi).</summary>
    static float Wrap(float a)
    {
        a %= MathF.Tau;
        return a < 0f ? a + MathF.Tau : a;
    }

    static Vector2 Toward(Vector2 to, Vector2 from)
    {
        var d = to - from;
        float len = d.Length();
        return len > 1e-4f ? d / len : Vector2.UnitY;
    }

    static Vector2 Away(Vector2 p, Vector2 from) => Toward(p, from);
}
