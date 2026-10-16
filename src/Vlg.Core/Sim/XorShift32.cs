namespace Vlg.Core.Sim;

/// <summary>Deterministic RNG so benchmark runs are repeatable. Pass by ref.</summary>
public struct XorShift32
{
    uint _state;

    public XorShift32(uint seed) => _state = seed == 0 ? 0x9E3779B9u : seed;

    public uint NextUInt()
    {
        var x = _state;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        _state = x;
        return x;
    }

    /// <summary>[0, 1)</summary>
    public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);

    public float Range(float min, float max) => min + (max - min) * NextFloat();

    public int Below(int count) => (int)(NextUInt() % (uint)count);
}
