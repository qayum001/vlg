using System;
using System.Numerics;
using Vlg.Core.Sim;

namespace Vlg.Core.Run;

/// <summary>
/// Expanding damage rings (Low End, Drop shockwave). A ring hits each enemy whose near edge its front
/// passes this step, so one ring hits an enemy once while knockback stays below the expansion speed.
/// A collapsing ring turns at max radius and sweeps back to the center, hitting again and pulling inward.
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
    public readonly byte[] Source;
    /// <summary>On its way back to the center (collapse).</summary>
    public readonly bool[] Inward;
    readonly RingShot[] _shot;
    readonly int[] _query;

    public RingPool(int capacity, int queryCapacity)
    {
        Capacity = capacity;
        Center = new Vector2[capacity];
        Radius = new float[capacity];
        MaxRadius = new float[capacity];
        Source = new byte[capacity];
        Inward = new bool[capacity];
        _shot = new RingShot[capacity];
        _query = new int[queryCapacity];
    }

    public ref readonly RingShot Shot(int i) => ref _shot[i];

    public bool Spawn(in RingShot s)
    {
        if (Count == Capacity) return false;
        int i = Count++;
        Center[i] = s.Center;
        Radius[i] = -1f;
        MaxRadius[i] = s.MaxRadius;
        Source[i] = s.Source;
        Inward[i] = false;
        _shot[i] = s;
        return true;
    }

    /// <param name="queryPad">Max enemy movement since the grid build.</param>
    public void Update(float dt, EnemySwarm enemies, SpatialGrid grid, PointBuffer hits, float enemyRadius, float queryPad)
    {
        for (int i = Count - 1; i >= 0; i--)
        {
            ref readonly var s = ref _shot[i];
            float r0 = Radius[i];
            bool inward = Inward[i];
            float r1 = inward
                ? MathF.Max(0f, r0 - s.Speed * dt)
                : MathF.Min(MaxRadius[i], MathF.Max(0f, r0) + s.Speed * dt);
            var c = Center[i];
            float outer = MathF.Max(r0, r1);
            int n = grid.Query(c, outer + enemyRadius + queryPad, _query, enemies.Position);
            for (int k = 0; k < n; k++)
            {
                int j = _query[k];
                if (enemies.Health[j] <= 0f) continue;
                var off = enemies.Position[j] - c;
                float len = off.Length();
                float edge = len - enemyRadius;
                bool crossed = inward ? edge >= r1 && edge < r0 : edge > r0 && edge <= r1;
                if (!crossed) continue;
                float damage = s.Damage;
                if (s.DistanceDamage) damage *= 0.5f + MathF.Max(0f, edge) / MaxRadius[i];
                enemies.Damage(j, damage, s.Source);
                hits.Add(enemies.Position[j]);
                var dir = len > 1e-4f ? off / len : Vector2.UnitY;
                if (s.Knockback > 0f) enemies.AddPush(j, inward ? -dir * s.Knockback : dir * s.Knockback);
                if (s.Slow > 0f) enemies.ApplySlow(j, s.Slow, s.SlowSec);
                if (s.Stun > 0f) enemies.ApplyStun(j, s.Stun);
            }
            Radius[i] = r1;
            if (!inward && r1 >= MaxRadius[i])
            {
                if (s.Collapse) Inward[i] = true;
                else RemoveAt(i);
            }
            else if (inward && r1 <= 0f)
            {
                RemoveAt(i);
            }
        }
    }

    void RemoveAt(int i)
    {
        int last = --Count;
        if (i == last) return;
        Center[i] = Center[last];
        Radius[i] = Radius[last];
        MaxRadius[i] = MaxRadius[last];
        Source[i] = Source[last];
        Inward[i] = Inward[last];
        _shot[i] = _shot[last];
    }
}
