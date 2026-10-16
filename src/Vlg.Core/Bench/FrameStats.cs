using System;

namespace Vlg.Core.Bench;

/// <summary>Fixed-capacity sample buffer. Percentile sorts a copy, so call it rarely (step end, HUD refresh).</summary>
public sealed class FrameStats
{
    readonly float[] _samples;
    readonly float[] _scratch;
    double _sum;

    public int Count { get; private set; }
    public int Dropped { get; private set; }
    public float Max { get; private set; }
    public float Average => Count == 0 ? 0f : (float)(_sum / Count);

    public FrameStats(int capacity)
    {
        _samples = new float[capacity];
        _scratch = new float[capacity];
    }

    public void Add(float value)
    {
        if (Count == _samples.Length)
        {
            Dropped++;
            return;
        }
        _samples[Count++] = value;
        _sum += value;
        if (value > Max) Max = value;
    }

    /// <summary>Nearest-rank percentile, p in (0, 100].</summary>
    public float Percentile(float p)
    {
        if (Count == 0) return 0f;
        Array.Copy(_samples, _scratch, Count);
        Array.Sort(_scratch, 0, Count);
        int rank = (int)MathF.Ceiling(p / 100f * Count);
        return _scratch[Math.Clamp(rank, 1, Count) - 1];
    }

    public void Reset()
    {
        Count = 0;
        Dropped = 0;
        Max = 0f;
        _sum = 0;
    }
}
