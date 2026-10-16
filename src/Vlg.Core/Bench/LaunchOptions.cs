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

    public const string Usage =
        "--bench enemies|overdraw|particles  --preset <name>  --out <csv>  --screenshot <png>  " +
        "--screenshot-at <sec>  --exit-after <sec>  --windowed  --seed <n>  --ramp-start <n>  --ramp-step <n>  --ramp-max <n>";

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
                "--seed" => o with { Seed = uint.Parse(Next(), CultureInfo.InvariantCulture) },
                "--ramp-start" => o with { RampStart = ParseInt(a, Next(), 0) },
                "--ramp-step" => o with { RampStep = ParseInt(a, Next(), 1) },
                "--ramp-max" => o with { RampMax = ParseInt(a, Next(), 0) },
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

    static float ParseFloat(string arg, string s) =>
        float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v >= 0
            ? v
            : throw new ArgumentException($"{arg} needs a number >= 0, got '{s}'");
}
