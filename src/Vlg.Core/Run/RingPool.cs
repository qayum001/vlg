using System;
using System.Numerics;
using Vlg.Core.Sim;

namespace Vlg.Core.Run;

/// <summary>
/// Expanding damage rings (Low End, Drop shockwave). A ring hits each enemy whose near edge its front
/// passes this step, so one ring hits an enemy once while knockback stays below the expansion speed.
/// Packed struct-of-arrays, swap-remove.
/// </summary>
public sealed class RingPool
{
    public readonly int Capacity;
    public int Count { get; private set; }

    public readonly Vector2[] Center;
    /// <summary>Front radius; -1 at spawn so enemies overlapping the center are hit.</summary>
    public readonly float[] Radius;
    public readonly float[] MaxRadius;
    public readonly float[] Speed;
    public readonly float[] Damage;
    public readonly float[] Knockback;
    public readonly byte[] Source;

    readonly int[] _query;

    public RingPool(int capacity, int queryCapacity)
    {
        Capacity = capacity;
        Center = new Vector2[capacity];
        Radius = new float[capacity];
        MaxRadius = new float[capacity];
        Speed = new float[capacity];
        Damage = new float[capacity];
        Knockback = new float[capacity];
        Source = new byte[capacity];
        _query = new int[queryCapacity];
    }

    public bool Spawn(Vector2 center, float maxRadius, float speed, float damage, float knockback, byte source)
    {
        if (Count == Capacity) return false;
        int i = Count++;
        Center[i] = center;
        Radius[i] = -1f;
        MaxRadius[i] = maxRadius;
        Speed[i] = speed;
        Damage[i] = damage;
        Knockback[i] = knockback;
        Source[i] = source;
        return true;
    }

    /// <param name="queryPad">Max enemy movement since the grid build.</param>
    public void Update(float dt, EnemySwarm enemies, SpatialGrid grid, PointBuffer hits, float enemyRadius, float queryPad)
    {
        for (int i = Count - 1; i >= 0; i--)
        {
            float r0 = Radius[i];
            float r1 = MathF.Min(MaxRadius[i], MathF.Max(0f, r0) + Speed[i] * dt);
            var c = Center[i];
            int n = grid.Query(c, r1 + enemyRadius + queryPad, _query, enemies.Position);
            for (int k = 0; k < n; k++)
            {
                int j = _query[k];
                if (enemies.Health[j] <= 0f) continue;
                var off = enemies.Position[j] - c;
                float len = off.Length();
                float edge = len - enemyRadius;
                if (edge <= r0 || edge > r1) continue;
                enemies.Damage(j, Damage[i], Source[i]);
                hits.Add(enemies.Position[j]);
                if (Knockback[i] > 0f) enemies.AddPush(j, (len > 1e-4f ? off / len : Vector2.UnitY) * Knockback[i]);
            }
            Radius[i] = r1;
            if (r1 >= MaxRadius[i]) RemoveAt(i);
        }
    }

    void RemoveAt(int i)
    {
        int last = --Count;
        if (i == last) return;
        Center[i] = Center[last];
        Radius[i] = Radius[last];
        MaxRadius[i] = MaxRadius[last];
        Speed[i] = Speed[last];
        Damage[i] = Damage[last];
        Knockback[i] = Knockback[last];
        Source[i] = Source[last];
    }
}
