using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Godot;
using Vlg.Core.Bench;
using Vlg.Core.Config;
using Vlg.Core.Jobs;
using Vlg.Core.Sim;
using NVec2 = System.Numerics.Vector2;

namespace Vlg.Game;

/// <summary>
/// Horde stress test. Play mode: WASD + mouse orbit, keys change load. Bench mode (--bench): scripted path,
/// ramp per BenchLoad, CSV out, quits when done. Simulation lives in Vlg.Core; this class only wires engine parts.
/// </summary>
public partial class StressTest : Node3D
{
    const float MouseYawPerPixel = 0.005f;
    const float StickYawRate = 2.5f;
    const float StickDeadzone = 0.2f;
    const int PlayEnemyStep = 250;
    const int PlayAuraStep = 500;
    const float HudRefreshSec = 0.25f;
    /// <summary>Bench starts after a few frames so the window mode switch has settled.</summary>
    const int BenchStartFrame = 5;

    LaunchOptions _options = null!;
    StressConfig _config = null!;
    string[] _presetNames = null!;
    string _presetName = null!;
    QualityPreset _preset = null!;
    HordeSim _sim = null!;
    WorkerPool _workers = null!;
    EnemyRenderer _enemies = null!;
    ProjectileRenderer _projectiles = null!;
    VfxSystem _vfx = null!;
    OrbitCamera _camera = null!;
    Arena _arena = null!;
    Hud _hud = null!;
    Telemetry _telemetry = null!;
    MeshInstance3D _player = null!;
    BenchRun? _bench;
    bool _benchFinished;

    NVec2 _playerPos;
    int _playEnemies, _playLayers, _playAura;
    float _time;
    int _frame;
    readonly Stopwatch _clock = Stopwatch.StartNew();
    double _lastFrameMs = -1;
    readonly FrameStats _hudFrames = new(4096);
    float _hudTimer;
    bool _screenshotDone;
    string _lastStep = "";

    public override void _Ready()
    {
        _options = LaunchOptions.Parse(OS.GetCmdlineUserArgs());
        _config = StressConfig.Parse(Godot.FileAccess.GetFileAsString("res://data/stress.json"));
        _presetNames = _config.Presets.Keys.ToArray();
        _workers = new WorkerPool(_options.Threads ?? _config.Sim.WorkerThreads, _config.Sim.ParallelMinCount);
        _sim = new HordeSim(_config.Sim, _options.Seed, _workers);

        _arena = new Arena(this, GD.Load<Shader>("res://shaders/ground.gdshader"), _options.Seed);
        _enemies = new EnemyRenderer(this, GD.Load<Shader>("res://shaders/enemy_vat.gdshader"));
        _projectiles = new ProjectileRenderer(this);
        _camera = new OrbitCamera(this);
        _vfx = new VfxSystem(this, _camera.Camera, GD.Load<Shader>("res://shaders/overdraw_layer.gdshader"));
        _player = new MeshInstance3D
        {
            Name = "Player",
            Mesh = new CapsuleMesh { Radius = 0.4f, Height = 1.7f },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.9f, 0.95f, 1f),
                EmissionEnabled = true,
                Emission = new Color(0.4f, 0.7f, 1f),
                EmissionEnergyMultiplier = 0.5f,
            },
        };
        AddChild(_player);
        _hud = new Hud(this);
        _telemetry = new Telemetry(GetViewport());

        SetPreset(_options.Preset);
        _playEnemies = _preset.MaxEnemies;
        _playAura = _preset.AuraParticles;

        GetViewport().SizeChanged += FitLayers;
        FitLayers();

        if (_options.Bench != null)
        {
            _sim.SpawnMode = SpawnMode.Area;
            if (!_options.Windowed) DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
        }
        else
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
        _camera.Snap(PlayerWorld());
    }

    public override void _ExitTree()
    {
        FinishBench();
        _workers.Dispose();
    }

    public override void _Process(double delta)
    {
        double now = _clock.Elapsed.TotalMilliseconds;
        float frameMs = _lastFrameMs < 0 ? (float)(delta * 1000.0) : (float)(now - _lastFrameMs);
        _lastFrameMs = now;
        float dt = (float)delta;
        _time += dt;
        _frame++;

        BenchLoad load;
        if (_options.Bench is { } mode)
        {
            if (_bench == null && !_benchFinished && _frame >= BenchStartFrame) StartBench(mode);
            int value = _bench?.Ramp.Value ?? _options.ApplyRamp(BenchLoad.RampFor(mode, _config.Bench)).Start;
            load = BenchLoad.For(mode, value, _config.Bench, _preset);
            _playerPos = BenchPath.PlayerAt(_time);
            _camera.Yaw = BenchPath.YawAt(_time);
        }
        else
        {
            UpdatePlayInput(dt);
            int projectiles = Math.Min((int)(_playEnemies * _config.Bench.ProjectilesPerEnemy), _preset.MaxProjectiles);
            load = new BenchLoad(_playEnemies, projectiles, _playLayers, _playAura);
        }

        long simStart = Stopwatch.GetTimestamp();
        _sim.Step(dt, _playerPos, CameraMath.Forward(_camera.Yaw), load.Enemies, load.Projectiles);
        _enemies.Update(_sim.Enemies, _workers);
        _projectiles.Update(_sim.Projectiles);
        float simMs = (float)Stopwatch.GetElapsedTime(simStart).TotalMilliseconds;

        var playerWorld = PlayerWorld();
        _player.Position = playerWorld + new Vector3(0, 0.85f, 0);
        _camera.Update(dt, playerWorld);
        _vfx.SetLayers(load.OverdrawLayers);
        _vfx.SetAura(load.AuraParticles);
        _vfx.Update(delta, playerWorld, _sim.Hits, _sim.Deaths, _preset.BurstsPerFrame);

        var sample = new FrameSample(frameMs, _telemetry.GpuMs, _telemetry.CpuRenderMs, simMs,
            Telemetry.DrawCalls, Telemetry.Primitives, _sim.Enemies.Count, _sim.Projectiles.Count);
        _hudFrames.Add(frameMs);
        if (_bench != null) TickBench(sample);
        UpdateHud(dt, sample);
        HandleScreenshotAndExit();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured && _options.Bench == null)
        {
            // Mouse right turns the view right = clockwise from above = negative yaw.
            _camera.Yaw -= motion.Relative.X * MouseYawPerPixel;
            return;
        }
        if (e is not InputEventKey { Pressed: true, Echo: false } key) return;

        if (_options.Bench != null)
        {
            if (key.PhysicalKeycode == Key.Escape) Quit();
            return;
        }
        switch (key.PhysicalKeycode)
        {
            case Key.Escape:
                Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
                break;
            case >= Key.Key1 and <= Key.Key9 when key.PhysicalKeycode - Key.Key1 < _presetNames.Length:
                SetPreset(_presetNames[key.PhysicalKeycode - Key.Key1]);
                break;
            case Key.Bracketleft: _playEnemies = Math.Max(0, _playEnemies - PlayEnemyStep); break;
            case Key.Bracketright: _playEnemies = Math.Min(_config.Sim.EnemyCapacity, _playEnemies + PlayEnemyStep); break;
            case Key.Comma: _playLayers = Math.Max(0, _playLayers - 1); break;
            case Key.Period: _playLayers = Math.Min(_vfx.MaxLayerCount, _playLayers + 1); break;
            case Key.Minus: _playAura = Math.Max(0, _playAura - PlayAuraStep); break;
            case Key.Equal: _playAura += PlayAuraStep; break;
            case Key.F1: _hud.Visible = !_hud.Visible; break;
            case Key.F11:
                DisplayServer.WindowSetMode(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Windowed
                    ? DisplayServer.WindowMode.Fullscreen
                    : DisplayServer.WindowMode.Windowed);
                break;
            case Key.F12:
                SaveScreenshot(ProjectSettings.GlobalizePath($"user://screenshots/stress_{DateTime.Now:yyyyMMdd_HHmmss}.png"));
                break;
        }
    }

    void SetPreset(string name)
    {
        _preset = _config.GetPreset(name);
        _presetName = name;
        QualityApplier.Apply(_preset, GetViewport(), _arena, _enemies, _vfx);
    }

    void UpdatePlayInput(float dt)
    {
        var keys = new NVec2(
            (Input.IsPhysicalKeyPressed(Key.D) ? 1f : 0f) - (Input.IsPhysicalKeyPressed(Key.A) ? 1f : 0f),
            (Input.IsPhysicalKeyPressed(Key.W) ? 1f : 0f) - (Input.IsPhysicalKeyPressed(Key.S) ? 1f : 0f));
        var stick = new NVec2(Input.GetJoyAxis(0, JoyAxis.LeftX), -Input.GetJoyAxis(0, JoyAxis.LeftY));
        var input = keys + (stick.Length() > StickDeadzone ? stick : NVec2.Zero);
        _playerPos += CameraMath.MoveToWorld(input, _camera.Yaw) * _config.Sim.PlayerSpeed * dt;

        float look = Input.GetJoyAxis(0, JoyAxis.RightX);
        if (MathF.Abs(look) > StickDeadzone) _camera.Yaw -= look * StickYawRate * dt;
    }

    void StartBench(BenchMode mode)
    {
        string path = _options.OutPath
            ?? ProjectSettings.GlobalizePath($"user://bench/{mode}_{_presetName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        var ramp = _options.ApplyRamp(BenchLoad.RampFor(mode, _config.Bench));
        _bench = new BenchRun(mode, ramp, _config.Bench, path, BenchMeta(mode, ramp));
        GD.Print($"bench {mode} preset {_presetName} on {Telemetry.Adapter} [{Telemetry.Renderer}] -> {path}");
    }

    void TickBench(in FrameSample sample)
    {
        var r = _bench!.Tick(sample);
        if (r != null)
        {
            _lastStep = string.Create(CultureInfo.InvariantCulture,
                $"step {r.Step}: value {r.Value}  p95 {r.P95Ms:F1} ms  avg {r.AvgMs:F1}  gpu {r.GpuMs:F1}  sim {r.SimMs:F2}  {(r.Pass ? "pass" : "FAIL")}");
            GD.Print(_lastStep);
        }
        if (_bench.Ramp.Done)
        {
            GD.Print($"bench done: best passing = {_bench.Ramp.BestPassing?.ToString() ?? "none"} -> {_bench.CsvPath}");
            Quit();
        }
    }

    void FinishBench()
    {
        _bench?.Dispose();
        _bench = null;
        _benchFinished = true;
    }

    void Quit()
    {
        FinishBench();
        GetTree().Quit();
    }

    IEnumerable<KeyValuePair<string, string>> BenchMeta(BenchMode mode, RampSettings ramp)
    {
        var size = GetViewport().GetVisibleRect().Size;
        var inv = CultureInfo.InvariantCulture;
        KeyValuePair<string, string> M(string k, string v) => new(k, v);
        yield return M("date", DateTime.Now.ToString("yyyy-MM-dd HH:mm", inv));
        yield return M("godot", Engine.GetVersionInfo()["string"].AsString());
        yield return M("build", OS.IsDebugBuild() ? "debug" : "release");
        yield return M("adapter", Telemetry.Adapter);
        yield return M("renderer", Telemetry.Renderer);
        yield return M("cpu", $"{OS.GetProcessorName()} ({OS.GetProcessorCount()} threads)");
        yield return M("ram_total_mb", (OS.GetMemoryInfo()["physical"].AsInt64() / (1024 * 1024)).ToString(inv));
        yield return M("mode", mode.ToString());
        yield return M("preset", _presetName);
        yield return M("window", $"{size.X:F0}x{size.Y:F0}");
        yield return M("render_scale", _preset.RenderScale.ToString(inv));
        yield return M("internal", $"{size.X * _preset.RenderScale:F0}x{size.Y * _preset.RenderScale:F0}");
        yield return M("enemy_tris", _enemies.TrianglesPerEnemy.ToString(inv));
        yield return M("threshold_ms", _config.Bench.ThresholdMs.ToString(inv));
        yield return M("ramp", $"start {ramp.Start} step {ramp.Step} max {ramp.Max}");
        yield return M("seed", _options.Seed.ToString(inv));
        yield return M("worker_threads", _workers.WorkerCount.ToString(inv));
    }

    void UpdateHud(float dt, in FrameSample s)
    {
        _hudTimer += dt;
        if (_hudTimer < HudRefreshSec) return;
        _hudTimer = 0f;
        float avg = _hudFrames.Average, p95 = _hudFrames.Percentile(95f);
        _hudFrames.Reset();
        var mem = Telemetry.SampleMemory();
        var size = GetViewport().GetVisibleRect().Size;
        string bench = _options.Bench is { } mode
            ? $"\nbench {mode}: value {_bench?.Ramp.Value.ToString() ?? "-"}   {_lastStep}"
            : "\nWASD move · mouse orbit · 1-4 preset · [ ] enemies · , . overdraw layers · - = aura · F1 HUD · F11 fullscreen · F12 shot · Esc mouse";
        _hud.SetText(string.Create(CultureInfo.InvariantCulture,
            $"vlg horde stress · preset {_presetName} · {Telemetry.Renderer} · {Telemetry.Adapter}\n" +
            $"FPS {1000f / Math.Max(avg, 0.01f):F0}   frame {avg:F1} ms (p95 {p95:F1})   GPU {s.GpuMs:F1}   CPU render {s.CpuRenderMs:F1}   sim {s.SimMs:F2} ms ({_workers.WorkerCount + 1} threads)\n" +
            $"enemies {s.Enemies} ({_enemies.TrianglesPerEnemy} tris)   projectiles {s.Projectiles}   particles ~{_vfx.EstimatedLiveParticles}   layers {_vfx.Layers}   lights {_vfx.Lights}\n" +
            $"draw calls {s.DrawCalls}   primitives {s.Primitives / 1e6f:F2} M   video {mem.VideoMb:F0} MB   RAM {mem.WorkingSetMb:F0} MB (private {mem.PrivateMb:F0})\n" +
            $"output {size.X:F0}x{size.Y:F0}   internal {size.X * _preset.RenderScale:F0}x{size.Y * _preset.RenderScale:F0}{bench}"));
    }

    void HandleScreenshotAndExit()
    {
        if (_options.ScreenshotPath is { } shot && !_screenshotDone && _time >= _options.ScreenshotAtSec)
        {
            SaveScreenshot(shot);
            _screenshotDone = true;
        }
        if (_options.ExitAfterSec is { } exit && _time >= exit) Quit();
    }

    void SaveScreenshot(string path)
    {
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))!);
        var error = GetViewport().GetTexture().GetImage().SavePng(path);
        if (error != Error.Ok) GD.PushError($"screenshot failed: {error} ({path})");
        else GD.Print($"screenshot -> {path}");
    }

    void FitLayers() => _vfx.FitLayers(_camera.Camera, GetViewport().GetVisibleRect().Size);

    Vector3 PlayerWorld() => new(_playerPos.X, 0f, _playerPos.Y);
}
