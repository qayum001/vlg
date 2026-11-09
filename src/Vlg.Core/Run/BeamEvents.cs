using System.Numerics;

namespace Vlg.Core.Run;

/// <summary>Beams fired this step, for rendering. Full buffer drops.</summary>
public sealed class BeamEvents
{
    public readonly Vector2[] Origin;
    public readonly Vector2[] Direction;
    public readonly float[] Length;
    public readonly float[] Width;
    public readonly int[] Slot;
    public int Count { get; private set; }

    public BeamEvents(int capacity)
    {
        Origin = new Vector2[capacity];
        Direction = new Vector2[capacity];
        Length = new float[capacity];
        Width = new float[capacity];
        Slot = new int[capacity];
    }

    public void Add(Vector2 origin, Vector2 direction, float length, float width, int slot)
    {
        if (Count == Origin.Length) return;
        int i = Count++;
        Origin[i] = origin;
        Direction[i] = direction;
        Length[i] = length;
        Width[i] = width;
        Slot[i] = slot;
    }

    public void Clear() => Count = 0;
}
