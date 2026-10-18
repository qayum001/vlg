using System;
using System.Numerics;

namespace Vlg.Core.Sim;

/// <summary>
/// Packed struct-of-arrays like <see cref="EnemySwarm"/>. A plain shot dies on its first hit; pierce, bounces,
/// explosions and returning shots come from <see cref="ProjectileShot"/>. Hit memory (last enemy ids) keeps a
/// shot from striking the same enemy again while it overlaps or bounces. Writes enemy health: single thread.
/// </summary>
public sealed class ProjectilePool
{
    /// <summary>Recent enemy ids a shot remembers.</summary>
    public const int HitMemory = 8;
    /// <summary>Bounce target search radius, m.</summary>
    public const float BounceRange = 6f;
    const float BurstRadius = 1.5f;
    const float BurstFactor = 0.5f;
    /// <summary>A returning shot is caught at this distance from the hero.</summary>
    const float CatchRadius = 1f;

    public readonly int Capacity;
    public readonly float DefaultRadius;
    public int Count { get; private set; }

    public readonly Vector2[] Position;
    public readonly Vector2[] Velocity;
    public readonly float[] Life;
    public readonly float[] Damage;
    /// <summary>Damage source for kill credit (<see cref="EnemySwarm.LastHit"/>).</summary>
    public readonly byte[] Source;
    public readonly float[] Radius;
    readonly int[] _pierce;
    readonly int[] _bounces;
    readonly float[] _falloff;
    readonly float[] _explodeRadius;
    readonly float[] _explodeFactor;
    readonly bool[] _burstOnBounce;
    /// <summary>0 = plain, 1 = outbound returning shot, 2 = on its way back.</summary>
    readonly byte[] _return;
    readonly uint[] _hitIds;
    readonly int[] _area;

    public ProjectilePool(int capacity, float defaultRadius = 0.25f, int queryCapacity = 256)
    {
        Capacity = capacity;
        DefaultRadius = defaultRadius;
        Position = new Vector2[capacity];
        Velocity = new Vector2[capacity];
        Life = new float[capacity];
        Damage = new float[capacity];
        Source = new byte[capacity];
        Radius = new float[capacity];
        _pierce = new int[capacity];
        _bounces = new int[capacity];
        _falloff = new float[capacity];
        _explodeRadius = new float[capacity];
        _explodeFactor = new float[capacity];
        _burstOnBounce = new bool[capacity];
        _return = new byte[capacity];
        _hitIds = new uint[capacity * HitMemory];
        _area = new int[queryCapacity];
    }

    public bool Spawn(Vector2 position, Vector2 velocity, float life, float damage, byte source = 0) =>
        Spawn(new ProjectileShot { Position = position, Velocity = velocity, Life = life, Damage = damage, Source = source });

    public bool Spawn(in ProjectileShot s)
    {
        if (Count == Capacity) return false;
        int i = Count++;
        Position[i] = s.Position;
        Velocity[i] = s.Velocity;
        Life[i] = s.Life;
        Damage[i] = s.Damage;
        Source[i] = s.Source;
        Radius[i] = s.Radius > 0f ? s.Radius : DefaultRadius;
        _pierce[i] = s.Pierce;
        _bounces[i] = s.Bounces;
        _falloff[i] = s.Falloff;
        _explodeRadius[i] = s.ExplodeRadius;
        _explodeFactor[i] = s.ExplodeFactor;
        _burstOnBounce[i] = s.BurstOnBounce;
        _return[i] = s.Returns ? (byte)1 : (byte)0;
        Array.Clear(_hitIds, i * HitMemory, HitMemory);
        return true;
    }

    public bool IsReturning(int i) => _return[i] != 0;

    /// <param name="grid">Built from enemy positions at frame start; enemies moved since, radius is padded.</param>
    /// <param name="enemyMaxSpeed">Fastest enemy movement since the grid build (steering + push), for query padding.</param>
    /// <param name="hero">Where returning shots fly back to.</param>
    /// <param name="flashes">Explosion events for rendering; null = none recorded.</param>
    public void Update(float dt, EnemySwarm enemies, SpatialGrid grid, PointBuffer hits, float enemyRadius, float enemyMaxSpeed,
        Vector2 hero = default, FlashEvents? flashes = null)
    {
        Span<int> candidates = stackalloc int[32];
        float pad = enemyMaxSpeed * dt;

        // Reverse order: swap-remove pulls in an already-processed entry.
        for (int i = Count - 1; i >= 0; i--)
        {
            if (_return[i] == 2)
            {
                var back = hero - Position[i];
                float dist = back.Length();
                if (dist <= CatchRadius)
                {
                    RemoveAt(i);
                    continue;
                }
                Velocity[i] = back * (Velocity[i].Length() / dist);
            }
            else
            {
                float life = Life[i] - dt;
                if (life <= 0f)
                {
                    if (_return[i] == 0)
                    {
                        RemoveAt(i);
                        continue;
                    }
                    _return[i] = 2;
                    Array.Clear(_hitIds, i * HitMemory, HitMemory);
                }
                Life[i] = life;
            }
            var p = Position[i] + Velocity[i] * dt;
            Position[i] = p;

            float hitR = Radius[i] + enemyRadius;
            float hitR2 = hitR * hitR;
            int n = grid.Query(p, hitR + pad, candidates, enemies.Position);
            for (int k = 0; k < n; k++)
            {
                int j = candidates[k];
                if (enemies.Health[j] <= 0f) continue;
                if (Vector2.DistanceSquared(enemies.Position[j], p) > hitR2) continue;
                if (Remembers(i, enemies.Id[j])) continue;
                if (Hit(i, j, enemies, grid, hits, enemyRadius, pad, flashes)) break;
            }
        }
    }

    /// <summary>Applies one hit. True = the shot is done with this frame (removed or redirected).</summary>
    bool Hit(int i, int j, EnemySwarm enemies, SpatialGrid grid, PointBuffer hits, float enemyRadius, float pad, FlashEvents? flashes)
    {
        var at = enemies.Position[j];
        enemies.Damage(j, Damage[i], Source[i]);
        hits.Add(at);
        Remember(i, enemies.Id[j]);
        if (_explodeRadius[i] > 0f)
        {
            AreaHit(at, _explodeRadius[i], Damage[i] * _explodeFactor[i], Source[i], enemies, grid, hits, enemyRadius, pad, j);
            flashes?.Add(at, _explodeRadius[i]);
        }

        if (_bounces[i] > 0)
        {
            if (_burstOnBounce[i])
            {
                AreaHit(at, BurstRadius, Damage[i] * BurstFactor, Source[i], enemies, grid, hits, enemyRadius, pad, j);
                flashes?.Add(at, BurstRadius);
            }
            int next = NearestUnhit(i, at, enemies, grid, pad);
            if (next >= 0)
            {
                var dir = enemies.Position[next] - at;
                float len = dir.Length();
                float speed = Velocity[i].Length();
                Velocity[i] = (len > 1e-4f ? dir / len : Vector2.UnitY) * speed;
                Position[i] = at;
                Damage[i] *= 1f - _falloff[i];
                _bounces[i]--;
                if (_return[i] == 0) Life[i] = MathF.Max(Life[i], BounceRange / MathF.Max(speed, 1e-3f) + 0.1f);
                return true;
            }
            _bounces[i] = 0;
        }
        if (_return[i] != 0) return false;
        if (_pierce[i] > 0)
        {
            _pierce[i]--;
            return false;
        }
        RemoveAt(i);
        return true;
    }

    void AreaHit(Vector2 center, float radius, float damage, byte source, EnemySwarm enemies, SpatialGrid grid, PointBuffer hits,
        float enemyRadius, float pad, int skip)
    {
        float reach = radius + enemyRadius;
        int n = grid.Query(center, reach + pad, _area, enemies.Position);
        for (int k = 0; k < n; k++)
        {
            int j = _area[k];
            if (j == skip || enemies.Health[j] <= 0f) continue;
            if (Vector2.DistanceSquared(enemies.Position[j], center) > reach * reach) continue;
            enemies.Damage(j, damage, source);
            hits.Add(enemies.Position[j]);
        }
    }

    int NearestUnhit(int i, Vector2 from, EnemySwarm enemies, SpatialGrid grid, float pad)
    {
        int n = grid.Query(from, BounceRange + pad, _area, enemies.Position);
        int best = -1;
        float bestD2 = float.MaxValue;
        for (int k = 0; k < n; k++)
        {
            int j = _area[k];
            if (enemies.Health[j] <= 0f || Remembers(i, enemies.Id[j])) continue;
            float d2 = Vector2.DistanceSquared(enemies.Position[j], from);
            if (d2 < bestD2)
            {
                bestD2 = d2;
                best = j;
            }
        }
        return best;
    }

    bool Remembers(int i, uint id)
    {
        int o = i * HitMemory;
        for (int k = 0; k < HitMemory; k++)
            if (_hitIds[o + k] == id) return true;
        return false;
    }

    /// <summary>Shifts in the newest id; the oldest falls out.</summary>
    void Remember(int i, uint id)
    {
        int o = i * HitMemory;
        Array.Copy(_hitIds, o, _hitIds, o + 1, HitMemory - 1);
        _hitIds[o] = id;
    }

    void RemoveAt(int i)
    {
        int last = --Count;
        if (i == last) return;
        Position[i] = Position[last];
        Velocity[i] = Velocity[last];
        Life[i] = Life[last];
        Damage[i] = Damage[last];
        Source[i] = Source[last];
        Radius[i] = Radius[last];
        _pierce[i] = _pierce[last];
        _bounces[i] = _bounces[last];
        _falloff[i] = _falloff[last];
        _explodeRadius[i] = _explodeRadius[last];
        _explodeFactor[i] = _explodeFactor[last];
        _burstOnBounce[i] = _burstOnBounce[last];
        _return[i] = _return[last];
        Array.Copy(_hitIds, last * HitMemory, _hitIds, i * HitMemory, HitMemory);
    }
}
