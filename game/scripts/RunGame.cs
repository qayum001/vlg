using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Vlg.Core.Bench;
using Vlg.Core.Config;
using Vlg.Core.Jobs;
using Vlg.Core.Run;
using Vlg.Core.Sim;
using NVec2 = System.Numerics.Vector2;

namespace Vlg.Game;

/// <summary>
/// Level run (first gameplay slice). Rules live in Vlg.Core <see cref="RunSim"/>; this class reads input,
/// draws the world and answers the sim's pauses (level-up, chest, shop, death) through <see cref="ChoicePanel"/>.
/// Quality presets still come from stress.json (only preset source so far).
/// </summary>
public partial class RunGame : Node3D
{
    const float MouseYawPerPixel = 0.005f;
    const float StickYawRate = 2.5f;
    const float StickDeadzone = 0.2f;
    const float HurtFlashSec = 0.12f;
    const float HudRefreshSec = 0.1f;
    /// <summary>Renderer cull boxes recenter in these steps (box is 500 m, view reaches ~30 m).</summary>
    const float CullSnap = 50f;
    /// <summary>5x5 prop tiles: props reach past the farthest visible point of the lower, farther run camera.</summary>
    const int PropTiles = 5;

    /// <summary>Never equals a real (phase, pending) pair: forces SyncPanel to rebuild after a pick.</summary>
    static readonly (RunPhase, int) PanelStale = (RunPhase.Playing, -1);
    static readonly Color HeroEmission = new(0.4f, 0.7f, 1f);
    static readonly Color HurtEmission = new(1f, 0.15f, 0.1f);

    LaunchOptions _options = null!;
    RunConfig _config = null!;
    QualityPreset _preset = null!;
    WorkerPool _workers = null!;
    RunSim _sim = null!;
    Arena _arena = null!;
    EnemyRenderer _enemies = null!;
    ProjectileRenderer _projectiles = null!;
    GemRenderer _gems = null!;
    SkillVisuals _skillVisuals = null!;
    VfxSystem _vfx = null!;
    OrbitCamera _camera = null!;
    MeshInstance3D _hero = null!;
    StandardMaterial3D _heroMaterial = null!;
    RunHud _hud = null!;
    ChoicePanel _panel = null!;

    /// <summary>What the panel currently answers; Playing = hidden. Pending count refreshes chained level-ups.</summary>
    (RunPhase Phase, int Pending) _shown = (RunPhase.Playing, 0);
    bool _pauseMenu;
    float _hurtTimer;
    float _hudTimer;
    float _time;
    bool _screenshotDone;
    Vector3 _cullCenter;

    public override void _Ready()
    {
        try
        {
            Setup();
        }
        catch (Exception e)
        {
            StartupFailure.Quit(this, e);
        }
    }

    void Setup()
    {
        _options = LaunchOptions.Parse(OS.GetCmdlineUserArgs());
        if (_options.WantsStressTest)
            throw new ArgumentException("--stress/--bench open the stress test via the boot scene (main scene), not run.tscn");
        _config = RunConfig.Parse(Godot.FileAccess.GetFileAsString("res://data/run.json"));
        _preset = StressConfig.Parse(Godot.FileAccess.GetFileAsString("res://data/stress.json")).GetPreset(_options.Preset);
        _workers = new WorkerPool(_options.Threads ?? _config.Sim.WorkerThreads, _config.Sim.ParallelMinCount);
        _sim = new RunSim(_config, _options.Seed, _workers);

        _arena = new Arena(this, GD.Load<Shader>("res://shaders/ground.gdshader"), _options.Seed, PropTiles);
        _enemies = new EnemyRenderer(this, GD.Load<Shader>("res://shaders/enemy_vat.gdshader"));
        _projectiles = new ProjectileRenderer(this);
        _gems = new GemRenderer(this);
        _skillVisuals = new SkillVisuals(this, _config.Skills.Length);
        var cam = _config.Camera;
        _camera = new OrbitCamera(this, cam.TiltDeg, cam.Distance, cam.FovDeg, cam.FollowRate);
        _vfx = new VfxSystem(this, _camera.Camera, GD.Load<Shader>("res://shaders/overdraw_layer.gdshader"));
        _heroMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.9f, 0.95f, 1f),
            EmissionEnabled = true,
            Emission = HeroEmission,
            EmissionEnergyMultiplier = 0.5f,
        };
        _hero = new MeshInstance3D
        {
            Name = "Hero",
            Mesh = new CapsuleMesh { Radius = _config.Hero.Radius, Height = 1.7f },
            MaterialOverride = _heroMaterial,
        };
        AddChild(_hero);
        _hud = new RunHud(this);
        _panel = new ChoicePanel(this);

        QualityApplier.Apply(_preset, GetViewport(), _arena, _enemies, _vfx);
        _arena.Sun.DirectionalShadowMaxDistance = cam.ShadowDistance;
        // Stress-only effects off: spell lights, aura particles, overdraw layers.
        _vfx.SetLights(0);
        _vfx.SetAura(0);
        _vfx.SetLayers(0);

        Input.MouseMode = Input.MouseModeEnum.Captured;
        _camera.Snap(HeroWorld());
        _arena.Follow(HeroWorld());
    }

    public override void _ExitTree() => _workers?.Dispose();

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _time += dt;
        bool stepped = false;
        if (_sim.Phase == RunPhase.Playing && !_pauseMenu)
        {
            var move = ReadMove(dt);
            var size = GetViewport().GetVisibleRect().Size;
            _sim.SetView(_camera.Yaw, size.X / size.Y);
            _sim.Step(dt, move);
            stepped = true;
        }

        var hero = HeroWorld();
        _hero.Position = hero + new Vector3(0, 0.85f, 0);
        // HeroHurt is only refreshed by Step: stale while paused.
        if (stepped && _sim.HeroHurt) _hurtTimer = HurtFlashSec;
        _hurtTimer = MathF.Max(0f, _hurtTimer - dt);
        _heroMaterial.Emission = _hurtTimer > 0f ? HurtEmission : HeroEmission;
        _heroMaterial.EmissionEnergyMultiplier = _hurtTimer > 0f ? 2f : 0.5f;

        var cull = new Vector3(MathF.Round(hero.X / CullSnap) * CullSnap, 0f, MathF.Round(hero.Z / CullSnap) * CullSnap);
        if (cull != _cullCenter)
        {
            _cullCenter = cull;
            _enemies.SetCullCenter(cull);
            _projectiles.SetCullCenter(cull);
            _gems.SetCullCenter(cull);
            _skillVisuals.SetCullCenter(cull);
        }
        _enemies.Update(_sim.Enemies, _workers);
        _projectiles.Update(_sim.Projectiles);
        _gems.Update(_sim.Gems, _time);
        _skillVisuals.Update(_sim.Skills, _sim.HeroPosition, stepped ? dt : 0f);
        _camera.Update(dt, hero);
        _arena.Follow(hero);
        _vfx.Update(delta, hero, _sim.Hits, _sim.Deaths, stepped ? _preset.BurstsPerFrame : 0);

        SyncPanel();
        _hudTimer -= dt;
        if (_hudTimer <= 0f)
        {
            _hudTimer = HudRefreshSec;
            _hud.Update(_sim, (float)Engine.GetFramesPerSecond());
        }
        HandleScreenshotAndExit();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            // Mouse right turns the view right = clockwise from above = negative yaw.
            _camera.Yaw -= motion.Relative.X * MouseYawPerPixel;
            return;
        }
        if (e is InputEventJoypadButton { Pressed: true, ButtonIndex: JoyButton.Start })
        {
            TogglePauseMenu();
            return;
        }
        if (e is not InputEventKey { Pressed: true, Echo: false } key) return;
        switch (key.PhysicalKeycode)
        {
            case >= Key.Key1 and <= Key.Key9:
                _panel.Pick((int)(key.PhysicalKeycode - Key.Key1));
                break;
            case Key.Escape: TogglePauseMenu(); break;
            case Key.F1: _hud.Visible = !_hud.Visible; break;
            case Key.F2: _sim.DevSkipStage(); break;
            case Key.F3: _sim.DevGainLevel(); break;
            case Key.F11:
                DisplayServer.WindowSetMode(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Windowed
                    ? DisplayServer.WindowMode.Fullscreen
                    : DisplayServer.WindowMode.Windowed);
                break;
            case Key.F12:
                SaveScreenshot(ProjectSettings.GlobalizePath($"user://screenshots/run_{DateTime.Now:yyyyMMdd_HHmmss}.png"));
                break;
        }
    }

    NVec2 ReadMove(float dt)
    {
        var keys = new NVec2(
            (Input.IsPhysicalKeyPressed(Key.D) ? 1f : 0f) - (Input.IsPhysicalKeyPressed(Key.A) ? 1f : 0f),
            (Input.IsPhysicalKeyPressed(Key.W) ? 1f : 0f) - (Input.IsPhysicalKeyPressed(Key.S) ? 1f : 0f));
        var stick = new NVec2(Input.GetJoyAxis(0, JoyAxis.LeftX), -Input.GetJoyAxis(0, JoyAxis.LeftY));
        var input = keys + (stick.Length() > StickDeadzone ? stick : NVec2.Zero);

        float look = Input.GetJoyAxis(0, JoyAxis.RightX);
        if (MathF.Abs(look) > StickDeadzone) _camera.Yaw -= look * StickYawRate * dt;
        return CameraMath.MoveToWorld(input, _camera.Yaw);
    }

    void TogglePauseMenu()
    {
        if (_pauseMenu)
        {
            _pauseMenu = false;
            _panel.Hide();
            _shown = (RunPhase.Playing, 0);
            return;
        }
        if (_sim.Phase != RunPhase.Playing) return;
        _pauseMenu = true;
        _panel.Show("Paused", "", new[] { "Resume", "Restart run", "Quit" }, i =>
        {
            switch (i)
            {
                case 0: TogglePauseMenu(); break;
                case 1: Restart(); break;
                default: GetTree().Quit(); break;
            }
        });
    }

    /// <summary>Shows the panel the sim's phase needs; hides it when play resumes.</summary>
    void SyncPanel()
    {
        if (_pauseMenu) return;
        var want = (_sim.Phase, _sim.Progress.PendingLevelUps);
        if (want == _shown) return;
        _shown = want;
        var inv = CultureInfo.InvariantCulture;
        switch (want.Phase)
        {
            case RunPhase.Playing:
                _panel.Hide();
                break;
            case RunPhase.LevelUp:
                ShowLevelUp();
                break;
            case RunPhase.Chest:
                _panel.Show("Chest", "Rare items (pick 1 of 3) arrive with the item system.\nNothing to take yet.",
                    new[] { "Continue" }, _ => ContinueStage());
                break;
            case RunPhase.Shop:
                _panel.Show("Shop", "Fully healed.\nItems and rerolls arrive with the item system.",
                    new[] { "Continue" }, _ => ContinueStage());
                break;
            case RunPhase.Dead:
                var st = _sim.Stages;
                _panel.Show("You died", string.Create(inv,
                    $"Loop {st.Loop} · {RunHud.StageName(st.Current.Kind)} · {(int)_sim.Time / 60}:{(int)_sim.Time % 60:D2}\nLevel {_sim.Progress.Level} · {_sim.Kills} kills"),
                    new[] { "Restart run", "Quit" }, i =>
                    {
                        if (i == 0) Restart();
                        else GetTree().Quit();
                    });
                break;
        }
    }

    void ShowLevelUp()
    {
        var p = _sim.Progress;
        var options = _sim.LevelUpOptions();
        var labels = new List<string>(options.Count);
        foreach (var o in options) labels.Add(Describe(o));
        string pending = p.PendingLevelUps > 1 ? $" ({p.PendingLevelUps} picks)" : "";
        var l = _config.Leveling;
        _panel.Show($"Level {p.Level}{pending}",
            $"Skill level cap {p.SkillCap} (hero level / {l.LevelDivisor}, rounded up, max {l.MaxSkillLevel})",
            labels, i =>
            {
                _sim.ChooseLevelUp(i);
                _shown = PanelStale;
            });
    }

    void ContinueStage()
    {
        _sim.ContinueStage();
        _shown = PanelStale;
    }

    string Describe(LevelUpOption o)
    {
        if (o.Kind == LevelUpKind.Bonus)
            return string.Create(CultureInfo.InvariantCulture, $"+{_config.Leveling.MaxedBonusDamage * 100f:0.#}% damage to all skills");
        var def = _config.Skills[o.Slot];
        string head = o.Kind == LevelUpKind.Unlock ? $"Unlock {def.Name}" : $"{def.Name}  {o.NewLevel - 1} → {o.NewLevel}";
        return $"{head}\n      {SkillText(def, o.NewLevel)}";
    }

    static string SkillText(SkillDef def, int level)
    {
        var s = def.StatsAt(level);
        var inv = CultureInfo.InvariantCulture;
        return def.Kind switch
        {
            SkillKind.Bolt => string.Create(inv, $"{s.Amount} bolt(s) at nearest · {s.Damage:0.#} dmg · every {s.Cooldown:0.##} s · range {s.Range:0.#} m"),
            SkillKind.Nova => string.Create(inv, $"ring of {s.Amount} · {s.Damage:0.#} dmg · every {s.Cooldown:0.##} s · range {s.Range:0.#} m"),
            SkillKind.Aura => string.Create(inv, $"{s.Damage:0.#} dmg every {s.Cooldown:0.##} s · radius {s.Radius:0.#} m"),
            SkillKind.Orbit => string.Create(inv, $"{s.Amount} blades · {s.Damage:0.#} dmg every {s.Cooldown:0.##} s · orbit {s.Range:0.#} m"),
            _ => def.Kind.ToString(),
        };
    }

    void Restart()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        GetTree().ReloadCurrentScene();
    }

    void HandleScreenshotAndExit()
    {
        if (_options.ScreenshotPath is { } shot && !_screenshotDone && _time >= _options.ScreenshotAtSec)
        {
            SaveScreenshot(shot);
            _screenshotDone = true;
        }
        if (_options.ExitAfterSec is { } exit && _time >= exit) GetTree().Quit();
    }

    void SaveScreenshot(string path)
    {
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))!);
        var error = GetViewport().GetTexture().GetImage().SavePng(path);
        if (error != Error.Ok) GD.PushError($"screenshot failed: {error} ({path})");
        else GD.Print($"screenshot -> {path}");
    }

    Vector3 HeroWorld() => new(_sim.HeroPosition.X, 0f, _sim.HeroPosition.Y);
}
