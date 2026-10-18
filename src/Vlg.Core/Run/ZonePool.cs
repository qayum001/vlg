using System;
using System.Numerics;
using Vlg.Core.Sim;

namespace Vlg.Core.Run;

/// <summary>
/// Lasting ground circles (quake cracks, fuzz puddles; item puddles later). Each tick hurts enemies inside and
/// applies its slow/vuln for the tick; beat zones tick on beat ticks, others every Interval seconds.
/// A zapping zone also strikes one random enemy within <see cref="ZapRange"/> per tick. Packed, swap-remove.
/// </summary>
public sealed class ZonePool
{
    public const float ZapRange = 3f;
    const float ZapFactor = 1.5f;

    public readonly int Capacity;
    public int Count { get; private set; }

    public readonly Vector2[] Center;
    public readonly float[] Radius;
    /// <summary>Seconds left / at spawn (rendering fade).</summary>
    public readonly float[] Life;
    public readonly float[] StartLife;
    public readonly int[] Slot;
    /// <summary>Seconds since the last tick; negative = not ticked yet.</summary>
    public readonly float[] SinceTick;
    readonly ZoneShot[] _shot;
    readonly float[] _timer;
    readonly int[] _query;
    XorShift32 _rng = new(0x20E5u);
    int _replace;

    public ZonePool(int capacity, int queryCapacity)
    {
        Capacity = capacity;
        Center = new Vector2[capacity];
        Radius = new float[capacity];
        Life = new float[capacity];
        StartLife = new float[capacity];
        Slot = new int[capacity];
        SinceTick = new float[capacity];
        _shot = new ZoneShot[capacity];
        _timer = new float[capacity];
        _query = new int[queryCapacity];
    }

    /// <summary>Full pool (fallback): overwrites slots round-robin.</summary>
    public void Spawn(in ZoneShot s)
    {
        int i = Count < Capacity ? Count++ : _replace++ % Capacity;
        Center[i] = s.Center;
        Radius[i] = s.Radius;
        Life[i] = s.LifeSec;
        StartLife[i] = s.LifeSec;
        Slot[i] = s.Slot;
        SinceTick[i] = -1f;
        _shot[i] = s;
        _timer[i] = 0f;
    }

    /// <param name="beatTick">A beat boundary was crossed this step (beat zones tick then).</param>
    /// <param name="zaps">Zap lines for rendering (origin, direction, length).</param>
    public void Update(float dt, bool beatTick, EnemySwarm enemies, SpatialGrid grid, PointBuffer hits, float enemyRadius, float queryPad,
        BeamEvents zaps)
    {
        for (int i = Count - 1; i >= 0; i--)
        {
            ref readonly var s = ref _shot[i];
            Life[i] -= dt;
            if (SinceTick[i] >= 0f) SinceTick[i] += dt;
            bool tick;
            if (s.OnBeat) tick = beatTick;
            else
            {
                _timer[i] -= dt;
                tick = _timer[i] <= 0f;
                if (tick) _timer[i] += s.Interval;
            }
            if (tick) Tick(i, s, enemies, grid, hits, enemyRadius, queryPad, zaps);
            if (Life[i] <= 0f) RemoveAt(i);
        }
    }

    void Tick(int i, in ZoneShot s, EnemySwarm enemies, SpatialGrid grid, PointBuffer hits, float enemyRadius, float queryPad, BeamEvents zaps)
    {
        SinceTick[i] = 0f;
        var c = Center[i];
        float reach = Radius[i] + enemyRadius;
        int n = grid.Query(c, reach + queryPad, _query, enemies.Position);
        float statusSec = (s.OnBeat ? s.BeatSec : s.Interval) + 0.1f;
        for (int k = 0; k < n; k++)
        {
            int j = _query[k];
            if (enemies.Health[j] <= 0f || Vector2.DistanceSquared(enemies.Position[j], c) > reach * reach) continue;
            enemies.Damage(j, s.Damage, s.Source);
            hits.Add(enemies.Position[j]);
            if (s.Slow > 0f) enemies.ApplySlow(j, s.Slow, statusSec);
            if (s.Vuln > 0f) enemies.ApplyVuln(j, s.Vuln, statusSec);
        }
        if (!s.Zap) return;
        n = grid.Query(c, ZapRange + queryPad, _query, enemies.Position);
        int alive = 0;
        for (int k = 0; k < n; k++)
            if (enemies.Health[_query[k]] > 0f) _query[alive++] = _query[k];
        if (alive == 0) return;
        int t = _query[_rng.Below(alive)];
        enemies.Damage(t, s.Damage * ZapFactor, s.Source);
        hits.Add(enemies.Position[t]);
        var off = enemies.Position[t] - c;
        float len = off.Length();
        if (len > 1e-4f) zaps.Add(c, off / len, len, 0.15f, s.Slot);
    }

    void RemoveAt(int i)
    {
        int last = --Count;
        if (i == last) return;
        Center[i] = Center[last];
        Radius[i] = Radius[last];
        Life[i] = Life[last];
        StartLife[i] = StartLife[last];
        Slot[i] = Slot[last];
        SinceTick[i] = SinceTick[last];
        _shot[i] = _shot[last];
        _timer[i] = _timer[last];
    }
}
