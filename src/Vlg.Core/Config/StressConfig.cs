using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Vlg.Core.Config;

/// <summary>Root of game/data/stress.json. Strict: unknown or missing keys throw.</summary>
public sealed record StressConfig
{
    public required SimTuning Sim { get; init; }
    public required BenchSettings Bench { get; init; }
    public required Dictionary<string, QualityPreset> Presets { get; init; }

    public static StressConfig Parse(string json)
    {
        var config = JsonSerializer.Deserialize<StressConfig>(json, ConfigJson.Options)
            ?? throw new FormatException("stress config is null");
        config.Validate();
        return config;
    }

    public QualityPreset GetPreset(string name) =>
        Presets.TryGetValue(name, out var preset)
            ? preset
            : throw new KeyNotFoundException($"unknown preset '{name}', have: {string.Join(", ", Presets.Keys)}");

    void Validate()
    {
        var s = Sim;
        Require(s.EnemySpeed > 0 && s.EnemyRadius > 0 && s.SeparationRadius > 0, "sim speeds/radii must be > 0");
        Require(s.MaxNeighbors is > 0 and <= 64, "sim.maxNeighbors must be 1..64 (stackalloc)");
        Require(s.GridDim > 0 && s.GridCellSize > 0, "sim grid must be > 0");
        Require(s.SpawnRadiusMin < s.SpawnRadiusMax && s.SpawnAreaMin < s.SpawnAreaMax, "spawn min must be < max");
        Require(s.TargetedShare + s.AimedShare <= 1f, "sim targetedShare + aimedShare must be <= 1");
        Require(s.EnemyCapacity > 0 && s.ProjectileCapacity > 0, "capacities must be > 0");
        Require(s.WorkerThreads >= -1 && s.ParallelMinCount >= 1, "sim.workerThreads must be >= -1, parallelMinCount >= 1");
        Require(Bench.ThresholdMs > 0 && Bench.MeasureSec > 0 && Bench.FailStepsToStop > 0, "bench timings must be > 0");
        foreach (var ramp in new[] { Bench.Enemies, Bench.Overdraw, Bench.Particles })
            Require(ramp.Step > 0 && ramp.Max >= ramp.Start, "bench ramp needs step > 0 and max >= start");
        Require(Presets.Count > 0, "no presets");
        foreach (var (name, p) in Presets)
            Require(p.RenderScale is > 0.1f and <= 2f && p.ParticleMultiplier >= 0, $"preset '{name}' out of range");
        Require(Presets.Keys.All(k => k == k.ToLowerInvariant()), "preset names must be lowercase");
    }

    static void Require(bool ok, string message)
    {
        if (!ok) throw new FormatException("stress config: " + message);
    }
}
