using System;
using System.Text.Json;
using Vlg.Core.Sim;

namespace Vlg.Core.Config;

/// <summary>Root of game/data/run.json. Strict: unknown or missing required keys throw.</summary>
public sealed record RunConfig
{
    public required RunSimSettings Sim { get; init; }
    public required CameraSettings Camera { get; init; }
    public required HeroSettings Hero { get; init; }
    public required WalkerSettings Walker { get; init; }
    public required SpawnSettings Spawn { get; init; }
    public required PickupSettings Pickups { get; init; }
    public required LevelingSettings Leveling { get; init; }
    public required LoopScaling LoopScaling { get; init; }
    /// <summary>One loop, in order (domain.md Run structure).</summary>
    public required StageDef[] Stages { get; init; }

    /// <summary>Level-up options map to keys 1..9; there is at most one option per skill.</summary>
    public const int MaxSkills = 9;
    /// <summary>Upper bound for Orbit object count (render pool).</summary>
    public const int MaxBlades = 32;
    /// <summary>Upper bound for Projectile count (target buffer).</summary>
    public const int MaxProjectilesPerCast = 256;
    /// <summary>Upper bound for alive speakers per skill (summon pool sizing).</summary>
    public const int MaxSummonsPerSkill = 16;
    /// <summary>Widest supported screen (21:9). Despawn radii must lie beyond the view at this aspect.</summary>
    public const float MaxAspect = 21f / 9f;
    /// <summary>Top screen edge at least this far below the horizon, else the view reaches very far.</summary>
    public const float MinTopEdgeDeg = 5f;

    public static RunConfig Parse(string json)
    {
        var config = JsonSerializer.Deserialize<RunConfig>(json, ConfigJson.Options)
            ?? throw new FormatException("run config is null");
        config.Validate();
        return config;
    }

    void Validate()
    {
        var s = Sim;
        Require(s.MaxStepSec > 0, "sim.maxStepSec must be > 0");
        Require(s.EnemyCapacity > 0 && s.ProjectileCapacity > 0 && s.GemCapacity > 0 && s.RingCapacity > 0, "sim capacities must be > 0");
        Require(s.GridDim > 0 && s.GridCellSize > 0, "sim grid must be > 0");
        Require(s.ProjectileRadius > 0, "sim.projectileRadius must be > 0");
        Require(s.WorkerThreads >= -1 && s.ParallelMinCount >= 1, "sim.workerThreads must be >= -1, parallelMinCount >= 1");

        var h = Hero;
        Require(h.Radius > 0 && h.ContactMargin >= 0, "hero radius must be > 0, contactMargin >= 0");

        var w = Walker;
        Require(w.Hp > 0 && w.Speed > 0 && w.Radius > 0, "walker hp/speed/radius must be > 0");
        Require(w.ContactDps >= 0 && w.Xp >= 0, "walker contactDps/xp must be >= 0");
        Require(w.SeparationRadius > 0 && w.SeparationStrength >= 0, "walker separation out of range");
        Require(w.MaxNeighbors is > 0 and <= 64, "walker.maxNeighbors must be 1..64 (stackalloc)");
        Require(w.PushDecay > 0, "walker.pushDecay must be > 0");

        var cam = Camera;
        Require(cam.FovDeg is > 10 and < 120 && cam.Distance > 0 && cam.FollowRate > 0 && cam.ShadowDistance > 0,
            "camera fovDeg must be 10..120, distance/followRate/shadowDistance > 0");
        Require(cam.TiltDeg <= 89 && cam.TiltDeg - cam.FovDeg / 2 >= MinTopEdgeDeg,
            $"camera tiltDeg must be <= 89 and >= fovDeg / 2 + {MinTopEdgeDeg} (top edge below the horizon)");
        float farthest = ViewFootprint.Compute(cam.TiltDeg, cam.Distance, cam.FovDeg, MaxAspect, 0f).FarthestCorner;
        Require(Spawn.ViewMargin >= 0 && Spawn.Band >= 0, "spawn viewMargin/band must be >= 0");
        Require(Spawn.DespawnRadius > farthest + Spawn.ViewMargin + Spawn.Band,
            $"spawn.despawnRadius must exceed the farthest visible point at 21:9 + viewMargin + band ({farthest + Spawn.ViewMargin + Spawn.Band:F1} m)");
        Require(Pickups.MagnetSpeed > 0 && Pickups.CollectRadius > 0, "pickups magnetSpeed/collectRadius must be > 0");
        Require(Pickups.DespawnRadius > farthest,
            $"pickups.despawnRadius must exceed the farthest visible point at 21:9 ({farthest:F1} m)");

        var l = Leveling;
        Require(l.XpBase > 0 && l.XpPerLevel >= 0, "leveling xpBase must be > 0, xpPerLevel >= 0");
        Require(l.LevelDivisor >= 1 && l.MaxSkillLevel >= 1, "leveling levelDivisor/maxSkillLevel must be >= 1");
        Require(l.MaxedBonusDamage >= 0, "leveling.maxedBonusDamage must be >= 0");

        var ls = LoopScaling;
        Require(ls.HpPerLoop >= 0 && ls.DamagePerLoop >= 0 && ls.SpawnPerLoop >= 0, "loopScaling values must be >= 0");

        Require(Stages.Length > 0, "no stages");
        bool anyTimed = false;
        for (int i = 0; i < Stages.Length; i++)
        {
            var st = Stages[i];
            if (st.IsTimed)
            {
                anyTimed = true;
                Require(st.DurationSec > 0 && st.SpawnPerSec >= 0 && st.MaxAlive >= 0, $"stage {i} ({st.Kind}) needs durationSec > 0, spawnPerSec/maxAlive >= 0");
            }
            else
            {
                Require(st.DurationSec == 0 && st.SpawnPerSec == 0 && st.MaxAlive == 0, $"stage {i} ({st.Kind}) waits for the player: no durationSec/spawnPerSec/maxAlive");
            }
        }
        Require(anyTimed, "at least one timed stage (mobs/horde) needed");
    }

    static void Require(bool ok, string message)
    {
        if (!ok) throw new FormatException("run config: " + message);
    }
}
