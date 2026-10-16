using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Run;
using Vlg.Core.Sim;
using Xunit;

namespace Vlg.Core.Tests;

public class WalkerSpawnerTests
{
    static readonly Vector2 Hero = new(10, -4);

    static EnemySwarm Run(StageDef stage, int loop, float seconds, float dt = 0.05f)
    {
        var c = TestConfig.Run;
        var enemies = new EnemySwarm(1024);
        var spawner = new WalkerSpawner();
        var rng = new XorShift32(7);
        int steps = (int)System.MathF.Round(seconds / dt);
        for (int i = 0; i < steps; i++) spawner.Update(dt, stage, loop, Hero, enemies, ref rng, c);
        return enemies;
    }

    [Fact]
    public void Spawns_at_the_stage_rate()
    {
        var e = Run(new StageDef { Kind = StageKind.Mobs, DurationSec = 100, SpawnPerSec = 2, MaxAlive = 1000 }, 1, 10f);
        Assert.InRange(e.Count, 19, 20);
    }

    [Fact]
    public void Stops_at_max_alive()
    {
        var e = Run(new StageDef { Kind = StageKind.Horde, DurationSec = 100, SpawnPerSec = 100, MaxAlive = 10 }, 1, 2f);
        Assert.Equal(10, e.Count);
    }

    [Fact]
    public void Later_loops_spawn_faster_with_more_hp_and_a_higher_cap()
    {
        var c = TestConfig.Run;
        var stage = new StageDef { Kind = StageKind.Mobs, DurationSec = 100, SpawnPerSec = 2, MaxAlive = 1000 };
        var loop3 = Run(stage, 3, 10f);
        Assert.InRange(loop3.Count, (int)(20 * c.LoopScaling.Spawn(3)) - 1, (int)(20 * c.LoopScaling.Spawn(3)));
        Assert.Equal(c.Walker.Hp * c.LoopScaling.Hp(3), loop3.Health[0], 4);

        var capped = Run(new StageDef { Kind = StageKind.Mobs, DurationSec = 100, SpawnPerSec = 100, MaxAlive = 10 }, 3, 2f);
        Assert.Equal((int)(10 * c.LoopScaling.Spawn(3)), capped.Count);
    }

    [Fact]
    public void Spawns_on_the_ring_around_the_hero()
    {
        var c = TestConfig.Run;
        var e = Run(new StageDef { Kind = StageKind.Horde, DurationSec = 100, SpawnPerSec = 50, MaxAlive = 500 }, 1, 5f);
        Assert.True(e.Count > 100);
        for (int i = 0; i < e.Count; i++)
            Assert.InRange(Vector2.Distance(e.Position[i], Hero), c.Spawn.RingMin - 1e-3f, c.Spawn.RingMax + 1e-3f);
    }

    [Fact]
    public void Chest_and_shop_spawn_nothing()
    {
        Assert.Equal(0, Run(new StageDef { Kind = StageKind.Chest }, 1, 5f).Count);
        Assert.Equal(0, Run(new StageDef { Kind = StageKind.Shop }, 1, 5f).Count);
    }

    [Fact]
    public void Outrun_walkers_move_back_to_the_ring_and_near_ones_stay()
    {
        var c = TestConfig.Run;
        var enemies = new EnemySwarm(4);
        var rng = new XorShift32(1);
        enemies.Spawn(Hero + new Vector2(100, 0), 5, ref rng);
        enemies.Spawn(Hero + new Vector2(5, 0), 5, ref rng);

        new WalkerSpawner().Update(0.05f, new StageDef { Kind = StageKind.Chest }, 1, Hero, enemies, ref rng, c);

        Assert.InRange(Vector2.Distance(enemies.Position[0], Hero), c.Spawn.RingMin - 1e-3f, c.Spawn.RingMax + 1e-3f);
        Assert.Equal(5f, enemies.Health[0]);
        Assert.Equal(Hero + new Vector2(5, 0), enemies.Position[1]);
    }
}
