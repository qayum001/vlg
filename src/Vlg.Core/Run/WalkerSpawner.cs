using System;
using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Sim;

namespace Vlg.Core.Run;

/// <summary>Spawns walkers at the stage rate just outside the camera view; moves outrun walkers back to such a point.</summary>
public sealed class WalkerSpawner
{
    float _pending;

    public void Update(float dt, StageDef stage, int loop, Vector2 hero, in ViewFootprint view, EnemySwarm enemies, ref XorShift32 rng, RunConfig c)
    {
        if (stage.IsTimed)
        {
            float mult = c.LoopScaling.Spawn(loop);
            int cap = Math.Min(enemies.Capacity, (int)(stage.MaxAlive * mult));
            float hp = c.Walker.Hp * c.LoopScaling.Hp(loop);
            _pending += stage.SpawnPerSec * mult * dt;
            while (_pending >= 1f && enemies.Count < cap)
            {
                enemies.Spawn(hero + OffScreenOffset(view, ref rng, c.Spawn), hp, ref rng);
                _pending -= 1f;
            }
            // At the cap spawns don't bank up: at most one waits for a free slot.
            if (_pending > 1f) _pending = 1f;
        }
        Recycle(hero, view, enemies, ref rng, c.Spawn);
    }

    /// <summary>Uniform direction; distance past the view edge, kept inside the despawn radius.</summary>
    public static Vector2 OffScreenOffset(in ViewFootprint view, ref XorShift32 rng, SpawnSettings s)
    {
        float a = rng.Range(-MathF.PI, MathF.PI);
        var dir = new Vector2(MathF.Sin(a), MathF.Cos(a));
        float r = view.DistanceToEdge(dir) + s.ViewMargin + rng.Range(0f, s.Band);
        return dir * MathF.Min(r, s.DespawnRadius * 0.95f);
    }

    static void Recycle(Vector2 hero, in ViewFootprint view, EnemySwarm enemies, ref XorShift32 rng, SpawnSettings s)
    {
        float far2 = s.DespawnRadius * s.DespawnRadius;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (Vector2.DistanceSquared(enemies.Position[i], hero) <= far2) continue;
            enemies.Position[i] = hero + OffScreenOffset(view, ref rng, s);
            enemies.Velocity[i] = Vector2.Zero;
        }
    }
}
