using System;
using System.Globalization;

namespace Vlg.Core.Bench;

public static class BenchCsv
{
    public const string Header =
        "step,value,frames,avg_ms,p50_ms,p95_ms,p99_ms,max_ms,gpu_ms,cpu_render_ms,sim_ms," +
        "draw_calls,primitives,enemies,projectiles,pass,video_mb,working_set_mb,private_mb,gc_heap_mb";

    public static string Row(StepResult r, MemorySample m) => string.Create(CultureInfo.InvariantCulture,
        $"{r.Step},{r.Value},{r.Frames},{r.AvgMs:F2},{r.P50Ms:F2},{r.P95Ms:F2},{r.P99Ms:F2},{r.MaxMs:F2}," +
        $"{r.GpuMs:F2},{r.CpuRenderMs:F2},{r.SimMs:F3},{r.DrawCalls:F0},{r.Primitives:F0}," +
        $"{r.Enemies:F0},{r.Projectiles:F0},{(r.Pass ? 1 : 0)}," +
        $"{m.VideoMb:F0},{m.WorkingSetMb:F0},{m.PrivateMb:F0},{m.GcHeapMb:F1}");

    /// <summary>"# key=value" line before the header. Commas/newlines in values are replaced.</summary>
    public static string Meta(string key, string value) =>
        "# " + key + "=" + value.Replace(',', ';').Replace('\n', ' ').Replace('\r', ' ');

    public static int ColumnCount(string line) => line.Split(',', StringSplitOptions.None).Length;
}
