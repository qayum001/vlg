using System.Numerics;

namespace Vlg.Core.Run;

/// <summary>
/// Placed skill objects (speakers). Each belongs to a skill slot and lives Duration in the slot's time unit.
/// Pulse numbers come from the slot's current stats (<see cref="SkillSystem"/>). Packed, swap-remove.
/// </summary>
public sealed class SummonPool
{
    public readonly int Capacity;
    public int Count { get; private set; }

    public readonly Vector2[] Position;
    public readonly int[] Slot;
    /// <summary>Time units left.</summary>
    public readonly float[] Life;
    /// <summary>Seconds since the last pulse; negative = never pulsed (rendering).</summary>
    public readonly float[] SincePulse;

    public SummonPool(int capacity)
    {
        Capacity = capacity;
        Position = new Vector2[capacity];
        Slot = new int[capacity];
        Life = new float[capacity];
        SincePulse = new float[capacity];
    }

    public bool Spawn(Vector2 position, int slot, float life)
    {
        if (Count == Capacity) return false;
        int i = Count++;
        Position[i] = position;
        Slot[i] = slot;
        Life[i] = life;
        SincePulse[i] = -1f;
        return true;
    }

    public int CountOf(int slot)
    {
        int n = 0;
        for (int i = 0; i < Count; i++)
            if (Slot[i] == slot) n++;
        return n;
    }

    /// <summary>Ages summons of <paramref name="slot"/> by <paramref name="units"/>; removes expired ones.</summary>
    public void Age(int slot, float units, float dt)
    {
        for (int i = Count - 1; i >= 0; i--)
        {
            if (Slot[i] != slot) continue;
            Life[i] -= units;
            if (SincePulse[i] >= 0f) SincePulse[i] += dt;
            if (Life[i] <= 0f) RemoveAt(i);
        }
    }

    void RemoveAt(int i)
    {
        int last = --Count;
        if (i == last) return;
        Position[i] = Position[last];
        Slot[i] = Slot[last];
        Life[i] = Life[last];
        SincePulse[i] = SincePulse[last];
    }
}
