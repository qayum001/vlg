using System;
using System.Numerics;
using Vlg.Core.Config;

namespace Vlg.Core.Sim;

/// <summary>Packed struct-of-arrays like <see cref="EnemySwarm"/>. One hit kills the projectile.</summary>
public sealed class ProjectilePool
{
    public readonly int Capacity;
    public int Count { get; private set; }

    public readonly Vector2[] Position;
    public readonly Vector2[] Velocity;
    public readonly float[] Life;

    public ProjectilePool(int capacity)
    {
        Capacity = capacity;
        Position = new Vector2[capacity];
        Velocity = new Vector2[capacity];
        Life = new float[capacity];
    }

    public bool Spawn(Vector2 position, Vector2 velocity, float life)
    {
        if (Count == Capacity) return false;
        int i = Count++;
        Position[i] = position;
        Velocity[i] = velocity;
        Life[i] = life;
        return true;
    }

    /// <param name="grid">Built from enemy positions at frame start; enemies moved since, radius is padded.</param>
    public void Update(float dt, EnemySwarm enemies, SpatialGrid grid, PointBuffer hits, SimTuning t)
    {
        Span<int> candidates = stackalloc int[16];
        float hitR = t.ProjectileRadius + t.EnemyRadius;
        float hitR2 = hitR * hitR;
        float queryR = hitR + t.EnemySpeed * 1.5f * dt;

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
                enemies.Health[j] -= t.ProjectileDamage;
                enemies.HitFlash[j] = 1f;
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
    }
}
