using System;
using System.Numerics;

namespace Vlg.Core.Sim;

/// <summary>Packed struct-of-arrays like <see cref="EnemySwarm"/>. One hit kills the projectile.</summary>
public sealed class ProjectilePool
{
    public readonly int Capacity;
    public int Count { get; private set; }

    public readonly Vector2[] Position;
    public readonly Vector2[] Velocity;
    public readonly float[] Life;
    public readonly float[] Damage;
    /// <summary>Damage source for kill credit (<see cref="EnemySwarm.LastHit"/>).</summary>
    public readonly byte[] Source;

    public ProjectilePool(int capacity)
    {
        Capacity = capacity;
        Position = new Vector2[capacity];
        Velocity = new Vector2[capacity];
        Life = new float[capacity];
        Damage = new float[capacity];
        Source = new byte[capacity];
    }

    public bool Spawn(Vector2 position, Vector2 velocity, float life, float damage, byte source = 0)
    {
        if (Count == Capacity) return false;
        int i = Count++;
        Position[i] = position;
        Velocity[i] = velocity;
        Life[i] = life;
        Damage[i] = damage;
        Source[i] = source;
        return true;
    }

    /// <param name="grid">Built from enemy positions at frame start; enemies moved since, radius is padded.</param>
    /// <param name="hitRadius">Projectile radius + enemy radius.</param>
    /// <param name="enemyMaxSpeed">Fastest enemy movement since the grid build (steering + push), for query padding.</param>
    public void Update(float dt, EnemySwarm enemies, SpatialGrid grid, PointBuffer hits, float hitRadius, float enemyMaxSpeed)
    {
        Span<int> candidates = stackalloc int[16];
        float hitR2 = hitRadius * hitRadius;
        float queryR = hitRadius + enemyMaxSpeed * dt;

        // Reverse order: swap-remove pulls in an already-processed entry.
        for (int i = Count - 1; i >= 0; i--)
        {
            float life = Life[i] - dt;
            if (life <= 0f)
            {
                RemoveAt(i);
                continue;
            }
            Life[i] = life;
            var p = Position[i] + Velocity[i] * dt;
            Position[i] = p;

            int n = grid.Query(p, queryR, candidates, enemies.Position);
            for (int k = 0; k < n; k++)
            {
                int j = candidates[k];
                if (enemies.Health[j] <= 0f) continue;
                if (Vector2.DistanceSquared(enemies.Position[j], p) > hitR2) continue;
                enemies.Damage(j, Damage[i], Source[i]);
                hits.Add(p);
                RemoveAt(i);
                break;
            }
        }
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
    }
}
