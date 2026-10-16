using System;
using System.Globalization;

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

    public const string Usage =
        "--bench enemies|overdraw|particles  --preset <name>  --out <csv>  --screenshot <png>  " +
        "--screenshot-at <sec>  --exit-after <sec>  --windowed  --seed <n>";

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
                _ => throw new ArgumentException($"unknown arg '{a}'. Usage: {Usage}"),
            };
        }
        return o;
    }

    static BenchMode ParseMode(string s) =>
        Enum.TryParse<BenchMode>(s, ignoreCase: true, out var m) && !int.TryParse(s, out _)
            ? m
            : throw new ArgumentException($"unknown bench mode '{s}'. Usage: {Usage}");

    static float ParseFloat(string arg, string s) =>
        float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v >= 0
            ? v
            : throw new ArgumentException($"{arg} needs a number >= 0, got '{s}'");
}
