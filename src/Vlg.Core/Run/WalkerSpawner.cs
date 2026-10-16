using System;
using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Sim;

namespace Vlg.Core.Run;

/// <summary>Spawns walkers at the stage rate on a ring around the hero; moves outrun walkers back onto the ring.</summary>
public sealed class WalkerSpawner
{
    float _pending;

    public void Update(float dt, StageDef stage, int loop, Vector2 hero, EnemySwarm enemies, ref XorShift32 rng, RunConfig c)
    {
        if (stage.IsTimed)
        {
            float mult = c.LoopScaling.Spawn(loop);
            int cap = Math.Min(enemies.Capacity, (int)(stage.MaxAlive * mult));
            float hp = c.Walker.Hp * c.LoopScaling.Hp(loop);
            _pending += stage.SpawnPerSec * mult * dt;
            while (_pending >= 1f && enemies.Count < cap)
            {
                enemies.Spawn(hero + RandomPoints.InAnnulus(ref rng, c.Spawn.RingMin, c.Spawn.RingMax), hp, ref rng);
                _pending -= 1f;
            }
            // At the cap spawns don't bank up: at most one waits for a free slot.
            if (_pending > 1f) _pending = 1f;
        }
        Recycle(hero, enemies, ref rng, c.Spawn);
    }

    static void Recycle(Vector2 hero, EnemySwarm enemies, ref XorShift32 rng, SpawnSettings s)
    {
        float far2 = s.DespawnRadius * s.DespawnRadius;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (Vector2.DistanceSquared(enemies.Position[i], hero) <= far2) continue;
            enemies.Position[i] = hero + RandomPoints.InAnnulus(ref rng, s.RingMin, s.RingMax);
            enemies.Velocity[i] = Vector2.Zero;
        }
    }
}
