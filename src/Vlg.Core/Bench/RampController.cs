using System;
using System.Collections.Generic;
using Vlg.Core.Config;

namespace Vlg.Core.Bench;

/// <summary>
/// Raises a load value in steps (warmup, then measure) until p95 frame time fails the threshold
/// <see cref="BenchSettings.FailStepsToStop"/> steps in a row, or the ramp max is passed.
/// Time is the sum of frame times, so a slow frame also counts as slow time.
/// </summary>
public sealed class RampController
{
    readonly RampSettings _ramp;
    readonly float _warmupMs;
    readonly float _measureMs;
    readonly float _thresholdMs;
    readonly int _failStepsToStop;
    readonly FrameStats _frames;
    readonly List<StepResult> _results = new();

    bool _measuring;
    float _phaseMs;
    int _failsInRow;
    double _gpu, _cpuRender, _sim, _draws, _prims, _enemies, _projectiles;

    public int StepIndex { get; private set; }
    public int Value => _ramp.Start + _ramp.Step * StepIndex;
    public bool Done { get; private set; }
    /// <summary>Highest value whose step passed, null if none.</summary>
    public int? BestPassing { get; private set; }
    public IReadOnlyList<StepResult> Results => _results;

    public RampController(RampSettings ramp, BenchSettings bench, int maxFramesPerStep = 20000)
    {
        _ramp = ramp;
        _warmupMs = bench.WarmupSec * 1000f;
        _measureMs = bench.MeasureSec * 1000f;
        _thresholdMs = bench.ThresholdMs;
        _failStepsToStop = bench.FailStepsToStop;
        _frames = new FrameStats(maxFramesPerStep);
    }

    /// <returns>The finished step, or null while a step runs.</returns>
    public StepResult? Tick(in FrameSample s)
    {
        if (Done) return null;
        _phaseMs += s.FrameMs;
        if (!_measuring)
        {
            if (_phaseMs >= _warmupMs)
            {
                _measuring = true;
                _phaseMs = 0f;
            }
            return null;
        }

        _frames.Add(s.FrameMs);
        _gpu += s.GpuMs;
        _cpuRender += s.CpuRenderMs;
        _sim += s.SimMs;
        _draws += s.DrawCalls;
        _prims += s.Primitives;
        _enemies += s.Enemies;
        _projectiles += s.Projectiles;
        if (_phaseMs < _measureMs) return null;

        var result = Finish();
        _results.Add(result);
        if (result.Pass)
        {
            _failsInRow = 0;
            BestPassing = Math.Max(BestPassing ?? int.MinValue, result.Value);
        }
        else
        {
            _failsInRow++;
        }

        if (_failsInRow >= _failStepsToStop || Value + _ramp.Step > _ramp.Max)
            Done = true;
        else
            StepIndex++;
        return result;
    }

    StepResult Finish()
    {
        int n = _frames.Count;
        float p95 = _frames.Percentile(95f);
        var r = new StepResult(
            StepIndex, Value, n,
            _frames.Average, _frames.Percentile(50f), p95, _frames.Percentile(99f), _frames.Max,
            (float)(_gpu / n), (float)(_cpuRender / n), (float)(_sim / n),
            (float)(_draws / n), (float)(_prims / n), (float)(_enemies / n), (float)(_projectiles / n),
            p95 <= _thresholdMs);
        _frames.Reset();
        _gpu = _cpuRender = _sim = _draws = _prims = _enemies = _projectiles = 0;
        _measuring = false;
        _phaseMs = 0f;
        return r;
    }
}
