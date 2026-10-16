using System.Numerics;

namespace Vlg.Core.Sim;

/// <summary>Per-frame event positions (hits, deaths). Full buffer drops and counts.</summary>
public sealed class PointBuffer
{
    public readonly Vector2[] Items;
    public int Count { get; private set; }
    public int Dropped { get; private set; }

    public PointBuffer(int capacity) => Items = new Vector2[capacity];

    public void Add(Vector2 p)
    {
        if (Count < Items.Length) Items[Count++] = p;
        else Dropped++;
    }

    public void Clear()
    {
        Count = 0;
        Dropped = 0;
    }
}
