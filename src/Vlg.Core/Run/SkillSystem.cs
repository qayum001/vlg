using System;
using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Sim;

namespace Vlg.Core.Run;

/// <summary>
/// Fires unlocked loadout skills on their cooldowns. Projectile skills spawn into the shared pool;
/// Aura and Orbit damage enemies directly on each tick. Writes enemy health: single thread only.
/// </summary>
public sealed class SkillSystem
{
    readonly SkillDef[] _defs;
    readonly float[] _timer;
    readonly float[] _orbitAngle;
    readonly int[] _query;
    readonly int[] _targets;
    readonly float[] _targetD2;

    /// <summary>Per slot: aura radius of an unlocked Aura skill, else 0. For rendering.</summary>
    public readonly float[] AuraRadius;
    /// <summary>All blade centers of unlocked Orbit skills this frame, [0, BladeCount).</summary>
    public readonly Vector2[] BladePositions;
    public int BladeCount { get; private set; }
    /// <summary>Per slot: true on frames the skill fired. For rendering pulses.</summary>
    public readonly bool[] FiredThisStep;

    /// <param name="queryCapacity">Max enemies one Aura/Orbit tick can hit (enemy capacity).</param>
    public SkillSystem(SkillDef[] defs, int queryCapacity)
    {
        _defs = defs;
        _timer = new float[defs.Length];
        _orbitAngle = new float[defs.Length];
        _query = new int[queryCapacity];
        _targets = new int[RunConfig.MaxProjectilesPerCast];
        _targetD2 = new float[RunConfig.MaxProjectilesPerCast];
        AuraRadius = new float[defs.Length];
        BladePositions = new Vector2[defs.Length * RunConfig.MaxBlades];
        FiredThisStep = new bool[defs.Length];
    }

    /// <param name="queryPad">Max enemy movement since the grid build.</param>
    public void Update(float dt, Vector2 hero, float heroRadius, HeroProgress progress, EnemySwarm enemies,
        SpatialGrid grid, ProjectilePool projectiles, PointBuffer hits, float enemyRadius, float queryPad, ref XorShift32 rng)
    {
        BladeCount = 0;
        float damageMult = progress.DamageMultiplier;
        for (int slot = 0; slot < _defs.Length; slot++)
        {
            FiredThisStep[slot] = false;
            AuraRadius[slot] = 0f;
            int level = progress.SkillLevel(slot);
            if (level == 0) continue;
            var def = _defs[slot];
            var st = def.StatsAt(level);
            float damage = st.Damage * damageMult;
            int bladeStart = BladeCount;

            if (def.Kind == SkillKind.Aura) AuraRadius[slot] = st.Radius;
            if (def.Kind == SkillKind.Orbit)
            {
                _orbitAngle[slot] = (_orbitAngle[slot] + st.Speed * dt) % MathF.Tau;
                int n = st.Amount;
                for (int b = 0; b < n; b++)
                {
                    float a = _orbitAngle[slot] + MathF.Tau * b / n;
                    BladePositions[BladeCount++] = hero + new Vector2(MathF.Sin(a), MathF.Cos(a)) * st.Range;
                }
            }

            _timer[slot] -= dt;
            if (_timer[slot] > 0f) continue;

            bool fired = def.Kind switch
            {
                SkillKind.Bolt => CastBolt(hero, heroRadius, st, damage, enemies, projectiles),
                SkillKind.Nova => CastNova(hero, heroRadius, st, damage, projectiles, ref rng),
                SkillKind.Aura => DamageAround(hero, st.Radius + enemyRadius, queryPad, damage, enemies, grid, hits),
                SkillKind.Orbit => CastOrbit(bladeStart, st, damage, enemyRadius, queryPad, enemies, grid, hits),
                _ => throw new InvalidOperationException($"unknown skill kind {def.Kind}"),
            };
            if (!fired)
            {
                // Bolt with no target: stay ready, retry next frame.
                _timer[slot] = 0f;
                continue;
            }
            FiredThisStep[slot] = true;
            _timer[slot] = MathF.Max(0f, _timer[slot] + st.Cooldown);
        }
    }

    /// <summary>Projectiles at the nearest enemies in range; extra projectiles cycle the targets. False = no target.</summary>
    bool CastBolt(Vector2 hero, float heroRadius, SkillStats st, float damage, EnemySwarm enemies, ProjectilePool projectiles)
    {
        int found = NearestInRange(hero, st.Range, st.Amount, enemies);
        if (found == 0) return false;
        float life = st.Range / st.Speed;
        for (int k = 0; k < st.Amount; k++)
        {
            var dir = enemies.Position[_targets[k % found]] - hero;
            float len = dir.Length();
            dir = len > 1e-4f ? dir / len : Vector2.UnitY;
            projectiles.Spawn(hero + dir * heroRadius, dir * st.Speed, life, damage);
        }
        return true;
    }

    static bool CastNova(Vector2 hero, float heroRadius, SkillStats st, float damage, ProjectilePool projectiles, ref XorShift32 rng)
    {
        float offset = rng.Range(0f, MathF.Tau);
        float life = st.Range / st.Speed;
        int n = st.Amount;
        for (int k = 0; k < n; k++)
        {
            float a = offset + MathF.Tau * k / n;
            var dir = new Vector2(MathF.Sin(a), MathF.Cos(a));
            projectiles.Spawn(hero + dir * heroRadius, dir * st.Speed, life, damage);
        }
        return true;
    }

    bool CastOrbit(int bladeStart, SkillStats st, float damage, float enemyRadius, float queryPad,
        EnemySwarm enemies, SpatialGrid grid, PointBuffer hits)
    {
        for (int b = bladeStart; b < BladeCount; b++)
            DamageAround(BladePositions[b], st.Radius + enemyRadius, queryPad, damage, enemies, grid, hits);
        return true;
    }

    /// <summary>Damages each living enemy whose center is within <paramref name="reach"/>. Always counts as fired.</summary>
    bool DamageAround(Vector2 center, float reach, float queryPad, float damage, EnemySwarm enemies, SpatialGrid grid, PointBuffer hits)
    {
        float reach2 = reach * reach;
        int n = grid.Query(center, reach + queryPad, _query, enemies.Position);
        for (int k = 0; k < n; k++)
        {
            int j = _query[k];
            if (enemies.Health[j] <= 0f) continue;
            if (Vector2.DistanceSquared(enemies.Position[j], center) > reach2) continue;
            enemies.Health[j] -= damage;
            enemies.HitFlash[j] = 1f;
            hits.Add(enemies.Position[j]);
        }
        return true;
    }

    /// <summary>Up to <paramref name="k"/> nearest living enemies within range into _targets, nearest first. Linear scan.</summary>
    int NearestInRange(Vector2 hero, float range, int k, EnemySwarm enemies)
    {
        float range2 = range * range;
        int found = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies.Health[i] <= 0f) continue;
            float d2 = Vector2.DistanceSquared(enemies.Position[i], hero);
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
