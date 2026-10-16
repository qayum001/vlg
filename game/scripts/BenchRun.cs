using System;
using System.Collections.Generic;
using System.IO;
using Vlg.Core.Bench;
using Vlg.Core.Config;

namespace Vlg.Game;

/// <summary>Ramp + CSV file for one benchmark run. Rows are flushed per step so a crash keeps the data.</summary>
public sealed class BenchRun : IDisposable
{
    readonly StreamWriter _csv;

    public BenchMode Mode { get; }
    public RampController Ramp { get; }
    public string CsvPath { get; }

    public BenchRun(BenchMode mode, BenchSettings bench, string csvPath, IEnumerable<KeyValuePair<string, string>> meta)
    {
        Mode = mode;
        Ramp = new RampController(BenchLoad.RampFor(mode, bench), bench);
        CsvPath = csvPath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(csvPath))!);
        _csv = new StreamWriter(csvPath, append: false);
        foreach (var (k, v) in meta) _csv.WriteLine(BenchCsv.Meta(k, v));
        _csv.WriteLine(BenchCsv.Header);
        _csv.Flush();
    }

    /// <returns>The finished step, if this frame ended one.</returns>
    public StepResult? Tick(in FrameSample sample)
    {
        var result = Ramp.Tick(sample);
        if (result == null) return null;
        _csv.WriteLine(BenchCsv.Row(result, Telemetry.SampleMemory()));
        _csv.Flush();
        return result;
    }

    public void Dispose()
    {
        _csv.WriteLine(BenchCsv.Meta("best_passing", Ramp.BestPassing?.ToString() ?? "none"));
        _csv.Dispose();
    }
}
