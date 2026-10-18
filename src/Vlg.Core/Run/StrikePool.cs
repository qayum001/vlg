using System.Numerics;
using Vlg.Core.Sim;

namespace Vlg.Core.Run;

/// <summary>
/// Delayed area impacts (falling bells; mortar shells later): marked on the ground, land after Delay seconds,
/// hit and stun everything in the radius. Packed, swap-remove.
/// </summary>
public sealed class StrikePool
{
    public readonly int Capacity;
    public int Count { get; private set; }

    public readonly Vector2[] Position;
    public readonly float[] Radius;
    /// <summary>Seconds left / at spawn (rendering: fall progress).</summary>
    public readonly float[] Delay;
    public readonly float[] StartDelay;
    public readonly int[] Slot;
    readonly float[] _damage;
    readonly float[] _stun;
    readonly byte[] _source;
    readonly int[] _query;

    public StrikePool(int capacity, int queryCapacity)
    {
        Capacity = capacity;
        Position = new Vector2[capacity];
        Radius = new float[capacity];
        Delay = new float[capacity];
        StartDelay = new float[capacity];
        Slot = new int[capacity];
        _damage = new float[capacity];
        _stun = new float[capacity];
        _source = new byte[capacity];
        _query = new int[queryCapacity];
    }

    public bool Spawn(Vector2 position, float radius, float delay, float damage, float stun, byte source, int slot)
    {
        if (Count == Capacity) return false;
        int i = Count++;
        Position[i] = position;
        Radius[i] = radius;
        Delay[i] = delay;
        StartDelay[i] = delay;
        Slot[i] = slot;
        _damage[i] = damage;
        _stun[i] = stun;
        _source[i] = source;
        return true;
    }

    public void Update(float dt, EnemySwarm enemies, SpatialGrid grid, PointBuffer hits, float enemyRadius, float queryPad, FlashEvents flashes)
    {
        for (int i = Count - 1; i >= 0; i--)
        {
            Delay[i] -= dt;
            if (Delay[i] > 0f) continue;
            var c = Position[i];
            float reach = Radius[i] + enemyRadius;
            int n = grid.Query(c, reach + queryPad, _query, enemies.Position);
            for (int k = 0; k < n; k++)
            {
                int j = _query[k];
                if (enemies.Health[j] <= 0f || Vector2.DistanceSquared(enemies.Position[j], c) > reach * reach) continue;
                enemies.Damage(j, _damage[i], _source[i]);
                hits.Add(enemies.Position[j]);
                if (_stun[i] > 0f) enemies.ApplyStun(j, _stun[i]);
            }
            flashes.Add(c, Radius[i]);
            RemoveAt(i);
        }
    }

    void RemoveAt(int i)
    {
        int last = --Count;
        if (i == last) return;
        Position[i] = Position[last];
        Radius[i] = Radius[last];
        Delay[i] = Delay[last];
        StartDelay[i] = StartDelay[last];
        Slot[i] = Slot[last];
        _damage[i] = _damage[last];
        _stun[i] = _stun[last];
        _source[i] = _source[last];
    }
}
