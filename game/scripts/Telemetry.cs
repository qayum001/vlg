using System;
using System.Diagnostics;
using Godot;
using Vlg.Core.Bench;

namespace Vlg.Game;

/// <summary>Engine-side measurements. GPU/CPU render times are from the last finished frame.</summary>
public sealed class Telemetry
{
    const float Mb = 1024f * 1024f;
    readonly Rid _viewport;

    public Telemetry(Viewport viewport)
    {
        _viewport = viewport.GetViewportRid();
        RenderingServer.ViewportSetMeasureRenderTime(_viewport, true);
    }

    public float GpuMs => (float)RenderingServer.ViewportGetMeasuredRenderTimeGpu(_viewport);

    public float CpuRenderMs => (float)(RenderingServer.ViewportGetMeasuredRenderTimeCpu(_viewport) + RenderingServer.GetFrameSetupTimeCpu());

    public static int DrawCalls => (int)RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalDrawCallsInFrame);

    public static int Primitives => (int)RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalPrimitivesInFrame);

    public static float VideoMb => RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.VideoMemUsed) / Mb;

    /// <summary>Allocates (Process object); call a few times per second at most.</summary>
    public static MemorySample SampleMemory()
    {
        using var p = Process.GetCurrentProcess();
        return new MemorySample(VideoMb, p.WorkingSet64 / Mb, p.PrivateMemorySize64 / Mb, GC.GetTotalMemory(false) / Mb);
    }

    public static string Adapter =>
        $"{RenderingServer.GetVideoAdapterName()} ({RenderingServer.GetVideoAdapterVendor()}, {RenderingServer.GetVideoAdapterApiVersion()})";

    public static string Renderer =>
        $"{RenderingServer.GetCurrentRenderingMethod()} / {RenderingServer.GetCurrentRenderingDriverName()}";
}
