using System;
using System.Numerics;

namespace Vlg.Core.Run;

/// <summary>
/// XP pickups, packed struct-of-arrays like <see cref="Sim.EnemySwarm"/>. A gem inside the pickup radius
/// becomes magnetized and flies to the hero until collected. Full pool: value merges into an existing gem.
/// </summary>
public sealed class GemPool
{
    public readonly int Capacity;
    public int Count { get; private set; }

    public readonly Vector2[] Position;
    public readonly float[] Value;
    public readonly bool[] Magnet;

    int _mergeCursor;

    public GemPool(int capacity)
    {
        Capacity = capacity;
        Position = new Vector2[capacity];
        Value = new float[capacity];
        Magnet = new bool[capacity];
    }

    public void Spawn(Vector2 position, float value)
    {
        if (value <= 0f) return;
        if (Count == Capacity)
        {
            Value[_mergeCursor++ % Count] += value;
            return;
        }
        int i = Count++;
        Position[i] = position;
        Value[i] = value;
        Magnet[i] = false;
    }

    /// <summary>Moves magnetized gems, removes collected ones. Returns collected XP.</summary>
    public float Update(float dt, Vector2 hero, float pickupRadius, float magnetSpeed, float collectRadius)
    {
        float pickup2 = pickupRadius * pickupRadius;
        float step = magnetSpeed * dt;
        float collected = 0f;
        // Reverse order: swap-remove pulls in an already-processed entry.
        for (int i = Count - 1; i >= 0; i--)
        {
            var toHero = hero - Position[i];
            float d2 = toHero.LengthSquared();
            if (!Magnet[i])
            {
                if (d2 > pickup2) continue;
                Magnet[i] = true;
            }
            float d = MathF.Sqrt(d2);
            if (d - step <= collectRadius)
            {
                collected += Value[i];
                RemoveAt(i);
                continue;
            }
            Position[i] += toHero * (step / d);
        }
        return collected;
    }

    void RemoveAt(int i)
    {
        int last = --Count;
        if (i == last) return;
        Position[i] = Position[last];
        Value[i] = Value[last];
        Magnet[i] = Magnet[last];
    }
}
