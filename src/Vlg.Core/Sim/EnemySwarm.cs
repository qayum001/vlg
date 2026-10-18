using System;
using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Jobs;

namespace Vlg.Core.Sim;

/// <summary>
/// Enemies as packed struct-of-arrays: live entries are [0, Count). Removal swaps with the last,
/// so indices are only stable within one frame (<see cref="Id"/> is stable).
/// Statuses (slow, stun, vuln, push) only change in single-thread code; Integrate reads and decays them per index.
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
    /// <summary>Unique per spawn, survives removal swaps.</summary>
    public readonly uint[] Id;
    /// <summary>Seconds left / speed fraction removed (0..1).</summary>
    public readonly float[] SlowTimer;
    public readonly float[] Slow;
    public readonly float[] StunTimer;
    /// <summary>Seconds left / extra damage taken (fraction).</summary>
    public readonly float[] VulnTimer;
    public readonly float[] Vuln;
    /// <summary>Knockback/pull velocity, m/s, added to steering and decayed by SwarmTuning.PushDecay.</summary>
    public readonly Vector2[] Push;
    /// <summary>Damage source of the latest hit (kill credit).</summary>
    public readonly byte[] LastHit;

    /// <summary>Health removed since <see cref="ResetDamageDealt"/>, overkill excluded (life steal).</summary>
    public float DamageDealt { get; private set; }

    public const int VariantCount = 4;
    /// <summary>Separation push can exceed EnemySpeed up to this factor. Grid query padding uses it.</summary>
    public const float MaxSpeedFactor = 1.5f;
    /// <summary>Push magnitude cap, m/s. Grid query padding adds it to the steering speed.</summary>
    public const float MaxPushSpeed = 25f;

    uint _nextId;

    readonly SteerJob _steer = new();
    readonly IntegrateJob _integrate = new();

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
        Id = new uint[capacity];
        SlowTimer = new float[capacity];
        Slow = new float[capacity];
        StunTimer = new float[capacity];
        VulnTimer = new float[capacity];
        Vuln = new float[capacity];
        Push = new Vector2[capacity];
        LastHit = new byte[capacity];
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
        Id[i] = ++_nextId;
        SlowTimer[i] = 0f;
        Slow[i] = 0f;
        StunTimer[i] = 0f;
        VulnTimer[i] = 0f;
        Vuln[i] = 0f;
        Push[i] = Vector2.Zero;
        LastHit[i] = 0;
        return true;
    }

    /// <summary>Single thread. Vuln multiplies the amount; records hit flash and kill credit.</summary>
    public void Damage(int i, float amount, byte source)
    {
        if (VulnTimer[i] > 0f) amount *= 1f + Vuln[i];
        float hp = Health[i];
        Health[i] = hp - amount;
        DamageDealt += MathF.Min(amount, MathF.Max(0f, hp));
        HitFlash[i] = 1f;
        LastHit[i] = source;
    }

    public void ResetDamageDealt() => DamageDealt = 0f;

    /// <summary>Stronger slow replaces a weaker one; timer keeps the longer.</summary>
    public void ApplySlow(int i, float fraction, float sec)
    {
        if (SlowTimer[i] <= 0f || fraction >= Slow[i]) Slow[i] = Math.Clamp(fraction, 0f, 1f);
        SlowTimer[i] = MathF.Max(SlowTimer[i], sec);
    }

    public void ApplyStun(int i, float sec) => StunTimer[i] = MathF.Max(StunTimer[i], sec);

    public void ApplyVuln(int i, float fraction, float sec)
    {
        if (VulnTimer[i] <= 0f || fraction >= Vuln[i]) Vuln[i] = fraction;
        VulnTimer[i] = MathF.Max(VulnTimer[i], sec);
    }

    /// <summary>Adds push velocity, clamped to <see cref="MaxPushSpeed"/>.</summary>
    public void AddPush(int i, Vector2 velocity)
    {
        var p = Push[i] + velocity;
        float len2 = p.LengthSquared();
        if (len2 > MaxPushSpeed * MaxPushSpeed) p *= MaxPushSpeed / MathF.Sqrt(len2);
        Push[i] = p;
    }

    /// <summary>Removes from the end (newest first).</summary>
    public void Truncate(int count) => Count = Math.Clamp(count, 0, Count);

    /// <param name="grid">Built from Position this frame, before this call.</param>
    /// <param name="workers">Null = single thread. Result is identical either way (each index writes only its own slots).</param>
    public void Update(float dt, Vector2 target, SpatialGrid grid, SwarmTuning t, WorkerPool? workers = null)
    {
        _steer.Set(this, target, grid, t);
        _integrate.Set(this, dt, t);
        // Pass 1 reads only old positions (order-independent), pass 2 integrates.
        if (workers == null)
        {
            _steer.Execute(0, Count);
            _integrate.Execute(0, Count);
        }
        else
        {
            workers.Run(_steer, Count);
            workers.Run(_integrate, Count);
        }
    }

    void Steer(int start, int end, Vector2 target, SpatialGrid grid, SwarmTuning t)
    {
        Span<int> neighbors = stackalloc int[t.MaxNeighbors];
        float sepR = t.SeparationRadius;
        float sepR2 = sepR * sepR;
        float stopR2 = (t.PlayerRadius + t.EnemyRadius) * (t.PlayerRadius + t.EnemyRadius);
        float maxSpeed = t.EnemySpeed * MaxSpeedFactor;

        for (int i = start; i < end; i++)
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
    }

    void Integrate(int start, int end, float dt, SwarmTuning t)
    {
        float turn = t.HeadingTurnRate * dt;
        float flashDecay = t.HitFlashDecay * dt;
        float animStep = t.AnimRate * dt / t.EnemySpeed;
        float pushKeep = 1f - MathF.Min(1f, t.PushDecay * dt);
        for (int i = start; i < end; i++)
        {
            var v = Velocity[i];
            if (StunTimer[i] > 0f)
            {
                v = Vector2.Zero;
                StunTimer[i] = MathF.Max(0f, StunTimer[i] - dt);
            }
            else if (SlowTimer[i] > 0f)
            {
                v *= 1f - Slow[i];
            }
            if (SlowTimer[i] > 0f) SlowTimer[i] = MathF.Max(0f, SlowTimer[i] - dt);
            if (VulnTimer[i] > 0f) VulnTimer[i] = MathF.Max(0f, VulnTimer[i] - dt);
            var push = Push[i];
            Position[i] += (v + push) * dt;
            Push[i] = push * pushKeep;
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

    /// <summary>Cached job objects: per-frame parameters in fields, no allocation per Update.</summary>
    sealed class SteerJob : IRangeJob
    {
        EnemySwarm _s = null!;
        Vector2 _target;
        SpatialGrid _grid = null!;
        SwarmTuning _t = null!;

        public void Set(EnemySwarm s, Vector2 target, SpatialGrid grid, SwarmTuning t) =>
            (_s, _target, _grid, _t) = (s, target, grid, t);

        public void Execute(int start, int end) => _s.Steer(start, end, _target, _grid, _t);
    }

    sealed class IntegrateJob : IRangeJob
    {
        EnemySwarm _s = null!;
        float _dt;
        SwarmTuning _t = null!;

        public void Set(EnemySwarm s, float dt, SwarmTuning t) => (_s, _dt, _t) = (s, dt, t);

        public void Execute(int start, int end) => _s.Integrate(start, end, _dt, _t);
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
        Id[to] = Id[from];
        SlowTimer[to] = SlowTimer[from];
        Slow[to] = Slow[from];
        StunTimer[to] = StunTimer[from];
        VulnTimer[to] = VulnTimer[from];
        Vuln[to] = Vuln[from];
        Push[to] = Push[from];
        LastHit[to] = LastHit[from];
    }

    static float WrapAngle(float a)
    {
        a = (a + MathF.PI) % (2f * MathF.PI);
        if (a < 0f) a += 2f * MathF.PI;
        return a - MathF.PI;
    }
}
