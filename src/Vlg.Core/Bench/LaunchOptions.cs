using System;
using System.Globalization;
using Vlg.Core.Config;

namespace Vlg.Core.Bench;

/// <summary>
/// Game user args (after "--" on the Godot command line). Unknown args throw so typos in
/// benchmark scripts fail loudly instead of silently running defaults.
/// </summary>
public sealed record LaunchOptions
{
    public BenchMode? Bench { get; init; }
    public string Preset { get; init; } = "low";
    public string? OutPath { get; init; }
    public string? ScreenshotPath { get; init; }
    public float ScreenshotAtSec { get; init; } = 5f;
    public float? ExitAfterSec { get; init; }
    public bool Windowed { get; init; }
    public uint Seed { get; init; } = 1;
    /// <summary>Overrides of the data ramp for the bench mode (e.g. fast GPUs past the default max).</summary>
    public int? RampStart { get; init; }
    public int? RampStep { get; init; }
    public int? RampMax { get; init; }
    /// <summary>Overrides sim.workerThreads (0 = single thread) for comparisons.</summary>
    public int? Threads { get; init; }
    /// <summary>Open the stress test instead of the game run. --bench implies it.</summary>
    public bool Stress { get; init; }
    /// <summary>Open the level run directly instead of the hub. Any run dev arg implies it.</summary>
    public bool Run { get; init; }
    /// <summary>Hero skill ids for the run loadout; null = the hero's default.</summary>
    public string[]? Loadout { get; init; }
    /// <summary>Dev: every loadout skill starts at this level (ignores the level cap).</summary>
    public int? SkillLevel { get; init; }
    /// <summary>Dev: answers the queued branch picks at start, in order (option index, 0-based).</summary>
    public int[]? Branches { get; init; }
    /// <summary>Ultimate id for the run; null = the hero's default.</summary>
    public string? Ultimate { get; init; }
    /// <summary>Dev: ultimate starts at this level (hero level ignored).</summary>
    public int? UltimateLevel { get; init; }
    /// <summary>Dev: start at this stage index of loop 1 (0 = first).</summary>
    public int? Stage { get; init; }
    /// <summary>Dev: starting credits.</summary>
    public int? Credits { get; init; }
    /// <summary>Dev: item ids granted at start (modifiers go to the stash).</summary>
    public string[]? Items { get; init; }

    public bool WantsStressTest => Stress || Bench != null;

    /// <summary>Skip the hub: --run or any run-only arg (loadout, levels, stage, items...).</summary>
    public bool WantsRun => Run || Loadout != null || SkillLevel != null || Branches != null || Ultimate != null
        || UltimateLevel != null || Stage != null || Credits != null || Items != null;

    public const string Usage =
        "--run  --stress  --bench enemies|overdraw|particles  --preset <name>  --out <csv>  --screenshot <png>  " +
        "--screenshot-at <sec>  --exit-after <sec>  --windowed  --seed <n>  --ramp-start <n>  --ramp-step <n>  --ramp-max <n>  --threads <n>  " +
        "--loadout <id,id,...>  --skill-level <n>  --branch <i,i,...>  --ultimate <id>  --ultimate-level <n>  --stage <n>  --credits <n>  --items <id,id,...>";

    public static LaunchOptions Parse(string[] args)
    {
        var o = new LaunchOptions();
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{a} needs a value. Usage: {Usage}");
            o = a switch
            {
                "--bench" => o with { Bench = ParseMode(Next()) },
                "--preset" => o with { Preset = Next() },
                "--out" => o with { OutPath = Next() },
                "--screenshot" => o with { ScreenshotPath = Next() },
                "--screenshot-at" => o with { ScreenshotAtSec = ParseFloat(a, Next()) },
                "--exit-after" => o with { ExitAfterSec = ParseFloat(a, Next()) },
                "--windowed" => o with { Windowed = true },
                "--stress" => o with { Stress = true },
                "--run" => o with { Run = true },
                "--seed" => o with { Seed = uint.Parse(Next(), CultureInfo.InvariantCulture) },
                "--ramp-start" => o with { RampStart = ParseInt(a, Next(), 0) },
                "--ramp-step" => o with { RampStep = ParseInt(a, Next(), 1) },
                "--ramp-max" => o with { RampMax = ParseInt(a, Next(), 0) },
                "--threads" => o with { Threads = ParseInt(a, Next(), 0) },
                "--loadout" => o with { Loadout = ParseList(a, Next()) },
                "--skill-level" => o with { SkillLevel = ParseInt(a, Next(), 1) },
                "--ultimate" => o with { Ultimate = Next() },
                "--ultimate-level" => o with { UltimateLevel = ParseInt(a, Next(), 0) },
                "--stage" => o with { Stage = ParseInt(a, Next(), 0) },
                "--credits" => o with { Credits = ParseInt(a, Next(), 0) },
                "--items" => o with { Items = ParseList(a, Next()) },
                "--branch" => o with { Branches = Array.ConvertAll(ParseList(a, Next()), x => ParseInt(a, x, 0)) },
                _ => throw new ArgumentException($"unknown arg '{a}'. Usage: {Usage}"),
            };
        }
        return o;
    }

    public RampSettings ApplyRamp(RampSettings ramp) => ramp with
    {
        Start = RampStart ?? ramp.Start,
        Step = RampStep ?? ramp.Step,
        Max = RampMax ?? ramp.Max,
    };

    static int ParseInt(string arg, string s, int min) =>
        int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v >= min
            ? v
            : throw new ArgumentException($"{arg} needs an integer >= {min}, got '{s}'");

    static BenchMode ParseMode(string s) =>
        Enum.TryParse<BenchMode>(s, ignoreCase: true, out var m) && !int.TryParse(s, out _)
            ? m
            : throw new ArgumentException($"unknown bench mode '{s}'. Usage: {Usage}");

    static string[] ParseList(string arg, string s)
    {
        var items = s.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return items.Length > 0 ? items : throw new ArgumentException($"{arg} needs comma-separated ids, got '{s}'");
    }

    static float ParseFloat(string arg, string s) =>
        float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v >= 0
            ? v
            : throw new ArgumentException($"{arg} needs a number >= 0, got '{s}'");
}
