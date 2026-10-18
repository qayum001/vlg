using System.Numerics;

namespace Vlg.Core.Sim;

/// <summary>Area impacts this step (explosions, strikes, blasts), for rendering. Full buffer drops.</summary>
public sealed class FlashEvents
{
    public readonly Vector2[] Position;
    public readonly float[] Radius;
    public int Count { get; private set; }

    public FlashEvents(int capacity)
    {
        Position = new Vector2[capacity];
        Radius = new float[capacity];
    }

    public void Add(Vector2 position, float radius)
    {
        if (Count == Position.Length) return;
        Position[Count] = position;
        Radius[Count++] = radius;
    }

    public void Clear() => Count = 0;
}
