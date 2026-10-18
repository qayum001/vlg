using System.Numerics;
using Vlg.Core.Run;
using Vlg.Core.Sim;
using Xunit;

namespace Vlg.Core.Tests;

public class ZoneStrikeTests
{
    const float EnemyRadius = 0.45f;

    sealed class World
    {
        public readonly EnemySwarm Enemies = new(16);
        public readonly SpatialGrid Grid = new(32, 2f, 16);
        public readonly ZonePool Zones = new(8, 16);
        public readonly StrikePool Strikes = new(8, 16);
        public readonly RingPool Rings = new(4, 16);
        public readonly PointBuffer Hits = new(64);
        public readonly BeamEvents Zaps = new(8);
        public readonly FlashEvents Flashes = new(8);
        XorShift32 _rng = new(6);

        public int Enemy(float x, float y)
        {
            Enemies.Spawn(new Vector2(x, y), 100f, ref _rng);
            return Enemies.Count - 1;
        }

        public void Step(float dt, bool beatTick = false)
        {
            Hits.Clear();
            Grid.Build(Enemies.Position, Enemies.Count, Vector2.Zero);
            Zones.Update(dt, beatTick, Enemies, Grid, Hits, EnemyRadius, 0f, Zaps);
            Strikes.Update(dt, Enemies, Grid, Hits, EnemyRadius, 0f, Flashes);
            Rings.Update(dt, Enemies, Grid, Hits, EnemyRadius, 0f);
        }
    }

    [Fact]
    public void Interval_zone_ticks_on_its_timer_applies_statuses_and_expires()
    {
        var w = new World();
        int inside = w.Enemy(1, 0), outside = w.Enemy(4, 0);
        w.Zones.Spawn(new ZoneShot { Radius = 2, LifeSec = 1.05f, Damage = 3, Interval = 0.5f, Slow = 0.4f, Vuln = 0.2f, Slot = -1 });

        for (int i = 0; i < 25; i++) w.Step(0.05f); // ticks at 0, 0.5, 1.0

        // Its own vuln (+20%) applies from the second tick on.
        Assert.Equal(100f - 3f - 3.6f - 3.6f, w.Enemies.Health[inside], 3);
        Assert.Equal(100f, w.Enemies.Health[outside]);
        Assert.Equal(0, w.Zones.Count);
        Assert.True(w.Enemies.SlowTimer[inside] > 0f && w.Enemies.VulnTimer[inside] > 0f);
    }

    [Fact]
    public void Beat_zone_ticks_only_on_beat_ticks()
    {
        var w = new World();
        int e = w.Enemy(0, 1);
        w.Zones.Spawn(new ZoneShot { Radius = 2, LifeSec = 10, Damage = 5, OnBeat = true, BeatSec = 0.5f, Slot = -1 });
        for (int i = 0; i < 10; i++) w.Step(0.05f, beatTick: i % 5 == 0);
        Assert.Equal(90f, w.Enemies.Health[e]);
    }

    [Fact]
    public void Zap_strikes_one_enemy_near_the_zone_and_records_the_line()
    {
        var w = new World();
        int near = w.Enemy(2.5f, 0);
        w.Zones.Spawn(new ZoneShot { Radius = 1, LifeSec = 10, Damage = 4, Interval = 1, Zap = true, Slot = 2 });

        w.Step(0.05f);

        Assert.Equal(100f - 6f, w.Enemies.Health[near]);
        Assert.Equal(1, w.Zaps.Count);
        Assert.Equal(2, w.Zaps.Slot[0]);
    }

    [Fact]
    public void Strike_lands_after_its_delay_and_stuns()
    {
        var w = new World();
        int e = w.Enemy(5, 0), far = w.Enemy(9, 0);
        w.Strikes.Spawn(new Vector2(5, 0), 2f, 0.3f, 20f, 1f, 4, 0);

        w.Step(0.2f);
        Assert.Equal(100f, w.Enemies.Health[e]);
        w.Step(0.2f);

        Assert.Equal((80f, 100f), (w.Enemies.Health[e], w.Enemies.Health[far]));
        Assert.Equal(1f, w.Enemies.StunTimer[e]);
        Assert.Equal(0, w.Strikes.Count);
        Assert.Equal(1, w.Flashes.Count);
    }

    [Fact]
    public void Collapsing_ring_hits_going_out_and_coming_back_and_pulls_inward()
    {
        var w = new World();
        int e = w.Enemy(3, 0);
        w.Rings.Spawn(new RingShot { MaxRadius = 5, Speed = 10, Damage = 5, Knockback = 4, Collapse = true, Source = 1 });

        for (int i = 0; i < 6; i++) w.Step(0.1f);
        Assert.Equal(95f, w.Enemies.Health[e]);
        Assert.True(w.Rings.Inward[0]);
        w.Enemies.Push[e] = Vector2.Zero;

        for (int i = 0; i < 6; i++) w.Step(0.1f);
        Assert.Equal(90f, w.Enemies.Health[e]);
        Assert.True(w.Enemies.Push[e].X < 0f);
        Assert.Equal(0, w.Rings.Count);
    }

    [Fact]
    public void Ring_slow_stun_and_distance_damage()
    {
        var w = new World();
        int center = w.Enemy(0, 0), edge = w.Enemy(4.2f, 0);
        w.Rings.Spawn(new RingShot { MaxRadius = 5, Speed = 10, Damage = 10, Slow = 0.4f, SlowSec = 1, Stun = 0.5f, DistanceDamage = true, Source = 1 });

        for (int i = 0; i < 6; i++) w.Step(0.1f);

        Assert.Equal(95f, w.Enemies.Health[center], 3);       // x0.5 at the center
        Assert.Equal(100f - 10f * 1.25f, w.Enemies.Health[edge], 3); // near edge 3.75 of 5 → x1.25
        Assert.Equal((0.4f, 0.5f), (w.Enemies.Slow[edge], w.Enemies.StunTimer[edge]));
    }
}
