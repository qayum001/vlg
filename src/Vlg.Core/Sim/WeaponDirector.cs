using System;
using System.Numerics;
using Vlg.Core.Config;

namespace Vlg.Core.Sim;

/// <summary>
/// Stress-test weapons: keeps the live projectile count near a target.
/// Mix of shots at nearby enemies (auto weapons), along the camera aim (aimed weapons, concept.md)
/// and random directions (novas).
/// </summary>
public sealed class WeaponDirector
{
    const int TargetSamples = 6;

    public void Update(float dt, int targetLive, Vector2 player, Vector2 aim,
        EnemySwarm enemies, ProjectilePool pool, ref XorShift32 rng, SimTuning t)
    {
        int deficit = targetLive - pool.Count;
        if (deficit <= 0) return;
        // Cap per frame so a refill spreads over a few frames instead of one ring burst.
        int maxPerFrame = (int)(targetLive / t.ProjectileLife * dt * 3f) + 1;
        int shots = Math.Min(deficit, maxPerFrame);

        for (int s = 0; s < shots; s++)
        {
            float roll = rng.NextFloat();
            Vector2 dir;
            if (roll < t.TargetedShare && enemies.Count > 0)
                dir = DirectionTo(player, PickNearbyEnemy(player, enemies, ref rng), ref rng);
            else if (roll < t.TargetedShare + t.AimedShare)
                dir = Rotate(aim, rng.Range(-t.AimSpreadRad, t.AimSpreadRad));
            else
            {
                float a = rng.Range(-MathF.PI, MathF.PI);
                dir = new Vector2(MathF.Sin(a), MathF.Cos(a));
            }
            if (!pool.Spawn(player + dir * t.PlayerRadius, dir * t.ProjectileSpeed, t.ProjectileLife, t.ProjectileDamage)) return;
        }
    }

    /// <summary>Closest of a few random samples: biased toward near enemies, cheap, no grid search.</summary>
    static Vector2 PickNearbyEnemy(Vector2 player, EnemySwarm enemies, ref XorShift32 rng)
    {
        var best = enemies.Position[rng.Below(enemies.Count)];
        float bestD2 = Vector2.DistanceSquared(best, player);
        for (int k = 1; k < TargetSamples; k++)
        {
            var p = enemies.Position[rng.Below(enemies.Count)];
            float d2 = Vector2.DistanceSquared(p, player);
            if (d2 < bestD2) (best, bestD2) = (p, d2);
        }
        return best;
    }

    static Vector2 DirectionTo(Vector2 from, Vector2 to, ref XorShift32 rng)
    {
        var d = to - from;
        float len2 = d.LengthSquared();
        if (len2 > 1e-6f) return d / MathF.Sqrt(len2);
        float a = rng.Range(-MathF.PI, MathF.PI);
        return new Vector2(MathF.Sin(a), MathF.Cos(a));
    }

    static Vector2 Rotate(Vector2 v, float angle)
    {
        float c = MathF.Cos(angle), s = MathF.Sin(angle);
        return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }
}
