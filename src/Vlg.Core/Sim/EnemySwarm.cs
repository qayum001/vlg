using System;
using System.Numerics;
using Vlg.Core.Config;

namespace Vlg.Core.Sim;

/// <summary>
/// Enemies as packed struct-of-arrays: live entries are [0, Count). Removal swaps with the last,
/// so indices are only stable within one frame.
/// Ground plane: Vector2(x, y) = world (x, z).
/// </summary>
public sealed class EnemySwarm
{
    public readonly int Capacity;
    public int Count { get; private set; }

    public readonly Vector2[] Position;
    public readonly Vector2[] Velocity;
    /// <summary>Yaw in radians; model +Z faces (sin, cos).</summary>
    public readonly float[] Heading;
    /// <summary>[0, 1) walk cycle.</summary>
    public readonly float[] AnimPhase;
    public readonly float[] Health;
    /// <summary>1 on hit, decays to 0.</summary>
    public readonly float[] HitFlash;
    public readonly byte[] Variant;

    public const int VariantCount = 4;

    public EnemySwarm(int capacity)
    {
        Capacity = capacity;
        Position = new Vector2[capacity];
        Velocity = new Vector2[capacity];
        Heading = new float[capacity];
        AnimPhase = new float[capacity];
        Health = new float[capacity];
        HitFlash = new float[capacity];
        Variant = new byte[capacity];
    }

    public bool Spawn(Vector2 position, float health, ref XorShift32 rng)
    {
        if (Count == Capacity) return false;
        int i = Count++;
        Position[i] = position;
        Velocity[i] = Vector2.Zero;
        Heading[i] = rng.Range(-MathF.PI, MathF.PI);
        AnimPhase[i] = rng.NextFloat();
        Health[i] = health;
        HitFlash[i] = 0f;
        Variant[i] = (byte)rng.Below(VariantCount);
        return true;
    }

    /// <summary>Removes from the end (newest first).</summary>
    public void Truncate(int count) => Count = Math.Clamp(count, 0, Count);

    /// <param name="grid">Built from Position this frame, before this call.</param>
    public void Update(float dt, Vector2 target, SpatialGrid grid, SimTuning t)
    {
        Span<int> neighbors = stackalloc int[t.MaxNeighbors];
        float sepR = t.SeparationRadius;
        float sepR2 = sepR * sepR;
        float stopR2 = (t.PlayerRadius + t.EnemyRadius) * (t.PlayerRadius + t.EnemyRadius);
        float maxSpeed = t.EnemySpeed * 1.5f;

        // Pass 1 reads only old positions (order-independent), pass 2 integrates.
        for (int i = 0; i < Count; i++)
        {
            var p = Position[i];
            var toTarget = target - p;
            float d2 = toTarget.LengthSquared();
            var desired = d2 > stopR2 ? toTarget * (t.EnemySpeed / MathF.Sqrt(d2)) : Vector2.Zero;

            var push = Vector2.Zero;
            int n = grid.Query(p, sepR, neighbors, Position);
            for (int k = 0; k < n; k++)
            {
                int j = neighbors[k];
                if (j == i) continue;
                var away = p - Position[j];
                float dd2 = away.LengthSquared();
                if (dd2 >= sepR2) continue;
                if (dd2 < 1e-8f)
                {
                    // Exact overlap: deterministic tie-break by index.
                    away = new Vector2(i < j ? 1e-3f : -1e-3f, 0f);
                    dd2 = 1e-6f;
                }
                float dd = MathF.Sqrt(dd2);
                // Unit direction * linear falloff (1 at contact, 0 at sepR).
                push += away * ((sepR - dd) / (dd * sepR));
            }

            var v = desired + push * t.SeparationStrength;
            float speed2 = v.LengthSquared();
            if (speed2 > maxSpeed * maxSpeed) v *= maxSpeed / MathF.Sqrt(speed2);
            Velocity[i] = v;
        }

        float turn = t.HeadingTurnRate * dt;
        float flashDecay = t.HitFlashDecay * dt;
        float animStep = t.AnimRate * dt / t.EnemySpeed;
        for (int i = 0; i < Count; i++)
        {
            var v = Velocity[i];
            Position[i] += v * dt;
            float speed2 = v.LengthSquared();
            if (speed2 > 1e-4f)
            {
                float want = MathF.Atan2(v.X, v.Y);
                float diff = WrapAngle(want - Heading[i]);
                Heading[i] = WrapAngle(Heading[i] + Math.Clamp(diff, -turn, turn));
            }
            float phase = AnimPhase[i] + animStep * MathF.Sqrt(speed2);
            AnimPhase[i] = phase - MathF.Floor(phase);
            HitFlash[i] = MathF.Max(0f, HitFlash[i] - flashDecay);
        }
    }

    /// <summary>Compacts out entries with Health &lt;= 0, records their positions. Returns removed count.</summary>
    public int RemoveDead(PointBuffer deaths)
    {
        int removed = 0;
        for (int i = Count - 1; i >= 0; i--)
        {
            if (Health[i] > 0f) continue;
            deaths.Add(Position[i]);
            int last = --Count;
            if (i != last) MoveSlot(last, i);
            removed++;
        }
        return removed;
    }

    void MoveSlot(int from, int to)
    {
        Position[to] = Position[from];
        Velocity[to] = Velocity[from];
        Heading[to] = Heading[from];
        AnimPhase[to] = AnimPhase[from];
        Health[to] = Health[from];
        HitFlash[to] = HitFlash[from];
        Variant[to] = Variant[from];
    }

    static float WrapAngle(float a)
    {
        a = (a + MathF.PI) % (2f * MathF.PI);
        if (a < 0f) a += 2f * MathF.PI;
        return a - MathF.PI;
    }
}
