using System;
using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Sim;

namespace Vlg.Core.Run;

/// <summary>
/// Pressed items (domain.md: effect ~3 s, cooldown from the press). Effects: Rush (faster recharge and move, then a
/// crash), Smoke (enemies chase the throw point), HolyWater (burning pool), Singularity (pull to a point, then pop).
/// <see cref="Use"/> may run between steps (no grid use); pull and pop run in <see cref="Update"/> inside the step.
/// </summary>
public sealed class UsableSystem
{
    readonly Inventory _inv;
    readonly SkillWorld _w;
    readonly byte _source;
    readonly float[] _cooldown;
    readonly int[] _query;
    UsableDef? _rush;
    float _rushLeft;
    float _crashLeft;
    float _crashMove;
    UsableDef? _smoke;
    float _smokeLeft;
    UsableDef? _singularity;
    float _singularityLeft;

    /// <summary>Item index pressed this frame (-1 = none), for audio/visuals. Cleared by <see cref="BeginStep"/>.</summary>
    public int UsedItem { get; private set; } = -1;
    public bool PoppedThisStep { get; private set; }
    public Vector2 SmokePoint { get; private set; }
    public Vector2 SingularityPoint { get; private set; }
    public bool SmokeActive => _smokeLeft > 0f;
    public bool SingularityActive => _singularityLeft > 0f;
    public bool RushActive => _rushLeft > 0f;

    public UsableSystem(Inventory inv, SkillWorld world, byte source)
    {
        _inv = inv;
        _w = world;
        _source = source;
        _cooldown = new float[inv.Config.Items.Length];
        _query = new int[world.Enemies.Capacity];
    }

    public float MoveMultiplier =>
        (1f + (_rushLeft > 0f ? _rush!.MoveBonus : 0f) + (_smokeLeft > 0f ? _smoke!.MoveBonus : 0f)) * (_crashLeft > 0f ? 1f - _crashMove : 1f);

    public float RateMultiplier => _rushLeft > 0f ? 1f + _rush!.RateBonus : 1f;

    public float CooldownLeft(int item) => _cooldown[item];

    public void BeginStep()
    {
        UsedItem = -1;
        PoppedThisStep = false;
    }

    /// <summary>Uses the usable in slot <paramref name="slot"/>. False = empty slot or cooling down.</summary>
    public bool Use(int slot, float damageMult)
    {
        if ((uint)slot >= (uint)_inv.Usables.Count) return false;
        int item = _inv.Usables[slot];
        if (_cooldown[item] > 0f) return false;
        var u = _inv.Config.Items[item].Usable!;
        _cooldown[item] = u.Cooldown;
        UsedItem = item;
        switch (u.Kind)
        {
            case UsableKind.Rush:
                _rush = u;
                _rushLeft = u.Duration;
                _crashLeft = 0f;
                break;
            case UsableKind.Smoke:
                _smoke = u;
                _smokeLeft = u.Duration;
                SmokePoint = _w.Hero;
                break;
            case UsableKind.HolyWater:
                // Every enemy so far is undead (first storyline), so the undead factor always applies.
                _w.Zones.Spawn(new ZoneShot
                {
                    Center = _w.Hero + _w.Aim * u.Range,
                    Radius = u.Radius,
                    LifeSec = u.Duration,
                    Damage = u.Damage * u.UndeadFactor * damageMult,
                    Interval = u.Interval,
                    Source = _source,
                    Slot = -1,
                });
                break;
            case UsableKind.Singularity:
                _singularity = u;
                _singularityLeft = u.Duration;
                SingularityPoint = _w.Hero + _w.Aim * u.Range;
                break;
        }
        return true;
    }

    public void Update(float dt, float damageMult)
    {
        for (int i = 0; i < _cooldown.Length; i++)
            if (_cooldown[i] > 0f) _cooldown[i] = MathF.Max(0f, _cooldown[i] - dt);

        if (_rushLeft > 0f)
        {
            _rushLeft -= dt;
            if (_rushLeft <= 0f && _rush!.CrashSec > 0f)
            {
                _crashLeft = _rush.CrashSec;
                _crashMove = _rush.CrashMove;
            }
        }
        else if (_crashLeft > 0f) _crashLeft -= dt;
        if (_smokeLeft > 0f) _smokeLeft -= dt;
        if (_singularityLeft > 0f) Pull(dt, damageMult);
    }

    void Pull(float dt, float damageMult)
    {
        var u = _singularity!;
        var e = _w.Enemies;
        var c = SingularityPoint;
        _singularityLeft -= dt;
        bool pop = _singularityLeft <= 0f;
        float reach = pop ? u.PopRadius : u.Radius;
        int n = _w.Grid.Query(c, reach + _w.EnemyRadius + _w.QueryPad, _query, e.Position);
        for (int k = 0; k < n; k++)
        {
            int j = _query[k];
            if (e.Health[j] <= 0f) continue;
            var off = c - e.Position[j];
            float d2 = off.LengthSquared();
            if (d2 > (reach + _w.EnemyRadius) * (reach + _w.EnemyRadius)) continue;
            if (pop)
            {
                e.Damage(j, u.Damage * damageMult, _source);
                _w.Hits.Add(e.Position[j]);
            }
            else if (d2 > 0.25f)
            {
                // Push is capped per enemy; topping it up each step keeps a steady pull.
                var want = off / MathF.Sqrt(d2) * u.Pull;
                e.AddPush(j, want - e.Push[j]);
            }
        }
        if (!pop) return;
        PoppedThisStep = true;
        _w.Flashes.Add(c, u.PopRadius);
    }
}
