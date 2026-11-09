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
/// Level run. Rules live in Vlg.Core <see cref="RunSim"/>; this class reads input, draws the world, plays the
/// beat and answers the sim's pauses (level-up, chest, shop, death) through <see cref="ChoicePanel"/>.
/// Hero selection comes from the hub save or --hero. Quality presets still come from stress.json.
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
    const float DevCredits = 100f;
    const string HubScene = "res://scenes/hub.tscn";

    /// <summary>Never equals a real (phase, pending) pair: forces SyncPanel to rebuild after a pick.</summary>
    static readonly (RunPhase, int) PanelStale = (RunPhase.Playing, -1);
    static readonly Color HeroEmission = new(0.4f, 0.7f, 1f);
    static readonly Color DropEmission = new(1f, 0.3f, 0.8f);
    static readonly Color HurtEmission = new(1f, 0.15f, 0.1f);

    LaunchOptions _options = null!;
    RunConfig _config = null!;
    HeroDef _heroDef = null!;
    ItemsConfig _items = null!;
    QualityPreset _preset = null!;
    WorkerPool _workers = null!;
    RunSim _sim = null!;
    Arena _arena = null!;
    EnemyRenderer _enemies = null!;
    ProjectileRenderer _projectiles = null!;
    GemRenderer _gems = null!;
    GemRenderer _coins = null!;
    GemRenderer _metaOrbs = null!;
    SkillVisuals _skillVisuals = null!;
    VfxSystem _vfx = null!;
    OrbitCamera _camera = null!;
    MeshInstance3D _hero = null!;
    StandardMaterial3D _heroMaterial = null!;
    RunHud _hud = null!;
    ChoicePanel _panel = null!;
    ShopPanel _shop = null!;
    BeatAudio _audio = null!;
    BeatOverlay _overlay = null!;

    /// <summary>What the panel currently answers; Playing = hidden. Pending count refreshes chained level-ups.</summary>
    (RunPhase Phase, int Pending) _shown = (RunPhase.Playing, 0);
    bool _pauseMenu;
    float _hurtTimer;
    float _hudTimer;
    float _time;
    bool _screenshotDone;
    bool _banked;
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
        _heroDef = GameSession.LoadHero(_config, _options.Hero);
        _items = ItemsConfig.Parse(Godot.FileAccess.GetFileAsString("res://data/items.json"));
        _preset = StressConfig.Parse(Godot.FileAccess.GetFileAsString("res://data/stress.json")).GetPreset(_options.Preset);
        _workers = new WorkerPool(_options.Threads ?? _config.Sim.WorkerThreads, _config.Sim.ParallelMinCount);
        var save = GameSession.Save;
        bool savedHero = _heroDef.Id == save.HeroId;
        if (savedHero) GameSession.Sanitize(_heroDef);
        _sim = new RunSim(_config, _heroDef, _items, _options.Seed, _workers, _options.Loadout ?? (savedHero ? save.Loadout : null),
            _options.Ultimate ?? (savedHero ? save.Ultimate : null), GameSession.Shop.Config, GameSession.Shop.Bonuses());
        if (_options.SkillLevel is { } level) _sim.Progress.DevSetAllSkills(level);
        if (_options.UltimateLevel is { } ultLevel) _sim.Progress.DevSetUltimate(ultLevel);
        if (_options.Credits is { } credits) _sim.DevAddCredits(credits);
        foreach (string id in _options.Items ?? Array.Empty<string>()) _sim.DevGrant(id);
        if (_options.Stage is { } stage) _sim.DevJumpToStage(stage);
        foreach (int pick in _options.Branches ?? Array.Empty<int>())
        {
            if (!_sim.Progress.PendingBranch) throw new ArgumentException("--branch: more picks than queued branch choices");
            _sim.ChooseLevelUp(pick);
        }

        _arena = new Arena(this, GD.Load<Shader>("res://shaders/ground.gdshader"), _options.Seed, PropTiles);
        _enemies = new EnemyRenderer(this, GD.Load<Shader>("res://shaders/enemy_vat.gdshader"));
        _projectiles = new ProjectileRenderer(this, _sim);
        _gems = new GemRenderer(this);
        _coins = GemRenderer.Coins(this);
        _metaOrbs = GemRenderer.MetaOrbs(this);
        _skillVisuals = new SkillVisuals(this, _sim.Loadout.Length, _heroDef.Id == "pyromancer");
        var cam = _config.Camera;
        _camera = new OrbitCamera(this, cam.TiltDeg, cam.Distance, cam.FovDeg, cam.FollowRate);
        _vfx = new VfxSystem(this, _camera.Camera, GD.Load<Shader>("res://shaders/overdraw_layer.gdshader"));
        _heroMaterial = new StandardMaterial3D
        {
            AlbedoColor = _heroDef.Id == "pyromancer" ? new Color(0.85f, 0.22f, 0.055f) : new Color(0.9f, 0.95f, 1f),
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
        _overlay = new BeatOverlay(this);
        _hud = new RunHud(this, _heroDef.Rhythmic);
        _panel = new ChoicePanel(this);
        _shop = new ShopPanel(this, _panel);
        _audio = new BeatAudio(this);

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

    public override void _ExitTree()
    {
        Bank();
        _workers?.Dispose();
    }

    /// <summary>Once per run: meta credits earned and the loop reached go into the save (domain.md: kept on death).</summary>
    void Bank()
    {
        if (_banked || _sim == null) return;
        _banked = true;
        GameSession.Save.AddRun(_sim.MetaCreditsEarned, _sim.Stages.Loop);
        GameSession.Persist();
    }

    void BackToHub()
    {
        Bank();
        Input.MouseMode = Input.MouseModeEnum.Captured;
        GetTree().ChangeSceneToFile(HubScene);
    }

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
        var beat = _sim.Beat;
        float pump = _heroDef.Rhythmic && beat.Beat >= 0 ? MathF.Exp(-beat.BeatPhase * 7f) : 0f;
        _heroMaterial.Emission = _hurtTimer > 0f ? HurtEmission : !_heroDef.Rhythmic ? new Color(1f, 0.32f, 0.04f) : beat.InDrop ? DropEmission : HeroEmission;
        _heroMaterial.EmissionEnergyMultiplier = _hurtTimer > 0f ? 2f : 0.4f + (beat.InDrop ? 1.6f : 0.8f) * pump;

        var cull = new Vector3(MathF.Round(hero.X / CullSnap) * CullSnap, 0f, MathF.Round(hero.Z / CullSnap) * CullSnap);
        if (cull != _cullCenter)
        {
            _cullCenter = cull;
            _enemies.SetCullCenter(cull);
            _projectiles.SetCullCenter(cull);
            _gems.SetCullCenter(cull);
            _coins.SetCullCenter(cull);
            _metaOrbs.SetCullCenter(cull);
            _skillVisuals.SetCullCenter(cull);
        }
        _enemies.Update(_sim.Enemies, _workers);
        _projectiles.Update(_sim.Projectiles);
        _gems.Update(_sim.Gems, _time);
        _coins.Update(_sim.Coins, _time);
        _metaOrbs.Update(_sim.MetaOrbs, _time);
        _skillVisuals.Update(_sim, stepped ? dt : 0f);
        if (_heroDef.Rhythmic)
        {
            _audio.Update(_sim, stepped);
            _overlay.Update(beat, _sim.Ultimate, stepped, dt);
        }
        _hud.UpdateBeat(beat, _heroDef.Rhythmic);
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
        if (e is InputEventJoypadButton { Pressed: true } pad)
        {
            switch (pad.ButtonIndex)
            {
                case JoyButton.Start: TogglePauseMenu(); break;
                case JoyButton.Y: PressUltimate(); break;
                case JoyButton.LeftShoulder: UseItem(0); break;
                case JoyButton.RightShoulder: UseItem(1); break;
                case JoyButton.X: UseItem(2); break;
            }
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
            case Key.M: _audio.Muted = !_audio.Muted; break;
            case Key.F2: _sim.DevSkipStage(); break;
            case Key.F3: _sim.DevGainLevel(); break;
            case Key.F4: _sim.DevFillUltimate(); break;
            case Key.F5: _sim.DevAddCredits(DevCredits); break;
            case Key.Space: PressUltimate(); break;
            case Key.Q: UseItem(0); break;
            case Key.E: UseItem(1); break;
            case Key.R: UseItem(2); break;
            case Key.F: UseItem(3); break;
            case Key.G: UseItem(4); break;
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

    void PressUltimate()
    {
        if (!_pauseMenu) _sim.PressUltimate();
    }

    void UseItem(int slot)
    {
        if (!_pauseMenu) _sim.UseItem(slot);
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
        _panel.Show("Paused", MetaLine(), new[] { "Resume", "Back to hub", "Restart run", "Quit" }, i =>
        {
            switch (i)
            {
                case 0: TogglePauseMenu(); break;
                case 1: BackToHub(); break;
                case 2: Restart(); break;
                default: GetTree().Quit(); break;
            }
        });
    }

    /// <summary>Shows the panel the sim's phase needs; hides it when play resumes.</summary>
    void SyncPanel()
    {
        if (_pauseMenu) return;
        var want = (_sim.Phase, _sim.Progress.PendingPicks);
        if (want == _shown) return;
        _shown = want;
        var inv = CultureInfo.InvariantCulture;
        switch (want.Phase)
        {
            case RunPhase.Playing:
                _panel.Hide();
                _shop.Hide();
                break;
            case RunPhase.LevelUp:
                _shop.Hide();
                ShowLevelUp();
                break;
            case RunPhase.Chest:
                ShowChest();
                break;
            case RunPhase.Shop:
                _panel.Hide();
                _shop.Show(_sim, ContinueStage);
                break;
            case RunPhase.Dead:
                var st = _sim.Stages;
                _panel.Show("You died", string.Create(inv,
                    $"Loop {st.Loop} · {RunHud.StageName(st.Current.Kind)} · {(int)_sim.Time / 60}:{(int)_sim.Time % 60:D2}\nLevel {_sim.Progress.Level} · {_sim.Kills} kills\n{MetaLine()}"),
                    new[] { "Back to hub", "Restart run", "Quit" }, i =>
                    {
                        if (i == 0) BackToHub();
                        else if (i == 1) Restart();
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
        string title, body;
        if (p.PendingBranch)
        {
            int slot = options[0].Slot;
            title = $"{_sim.Loadout[slot].Name}: choose a form";
            body = $"Skill level {p.SkillLevel(slot)} reached. The choice is final for this run.";
        }
        else
        {
            string pending = p.PendingPicks > 1 ? $" ({p.PendingPicks} picks)" : "";
            var l = _config.Leveling;
            title = $"Level {p.Level}{pending}";
            body = $"Skill level cap {p.SkillCap} (hero level / {l.LevelDivisor}, rounded up, max {l.MaxSkillLevel}) · forms at {string.Join(" and ", l.BranchLevels)}";
        }
        var icons = new List<Texture2D?>(options.Count);
        foreach (var o in options)
            icons.Add(o.Kind switch
            {
                LevelUpKind.Bonus => Icons.Skill("the_drop"),
                LevelUpKind.Ultimate => Icons.Skill(_sim.Ultimate.Def.Id),
                _ => Icons.Skill(_sim.Loadout[o.Slot].Id),
            });
        _panel.Show(title, body, labels, i =>
        {
            _sim.ChooseLevelUp(i);
            _shown = PanelStale;
        }, icons);
    }

    void ContinueStage()
    {
        _sim.ContinueStage();
        _shown = PanelStale;
    }

    /// <summary>Pick 1 of the chest's rare items (no skip); a modifier then asks for a socket.</summary>
    void ShowChest()
    {
        var offer = _sim.ChestOffer;
        if (offer.Count == 0)
        {
            _panel.Show("Chest", "Empty: nothing left you can take.", new[] { "Continue" }, _ => ContinueStage());
            return;
        }
        var items = _items.Items;
        var labels = new List<string>(offer.Count);
        var icons = new List<Texture2D?>(offer.Count);
        foreach (int item in offer)
        {
            labels.Add(ItemText.Card(items[item]));
            icons.Add(Icons.Item(items[item].Id));
        }
        _panel.Show("Chest", "Pick one rare item.", labels, i =>
        {
            int item = offer[i];
            _sim.TakeChest(i);
            if (items[item].Type == ItemType.Modifier)
                ShopPanel.AskSocket(_sim, _panel, _sim.Inventory.Stash.Count - 1, () => _shown = PanelStale);
            else _shown = PanelStale;
        }, icons);
    }

    string Describe(LevelUpOption o)
    {
        if (o.Kind == LevelUpKind.Bonus)
            return string.Create(CultureInfo.InvariantCulture, $"+{_config.Leveling.MaxedBonusDamage * 100f:0.#}% damage to all skills");
        if (o.Kind == LevelUpKind.Ultimate)
        {
            var u = _sim.Ultimate.Def;
            string uhead = o.NewLevel == 1 ? $"Unlock ultimate: {u.Name}" : $"{u.Name} (ultimate)  {o.NewLevel - 1} → {o.NewLevel}";
            return $"{uhead}\n      {UltimateText(u, o.NewLevel)}";
        }
        var p = _sim.Progress;
        var def = _sim.Loadout[o.Slot];
        if (o.Kind == LevelUpKind.Branch)
        {
            var b = p.BranchOptions(o.Slot, p.PendingBranchPoint)[o.Choice];
            string warn = "";
            if (b.Tags != null)
            {
                var lost = new List<string>();
                foreach (int item in _sim.Inventory.WouldUnfit(o.Slot, b.Tags)) lost.Add(_items.Items[item].Name);
                if (lost.Count > 0) warn = $"\n      moves {string.Join(", ", lost)} to the stash (doesn't fit the new form)";
            }
            return $"{b.Name}  ·  {b.Text}{warn}";
        }
        var form = def.Resolve(o.NewLevel, p.Path(o.Slot), p.BranchLevels);
        string head = o.Kind == LevelUpKind.Unlock ? $"Unlock {def.Name}" : $"{form.Name}  {o.NewLevel - 1} → {o.NewLevel}";
        int point = Array.IndexOf(_config.Leveling.BranchLevels, o.NewLevel);
        string formNext = point >= 0 && def.OptionsAt(p.Path(o.Slot), point).Count > 0 ? "  ·  new form!" : "";
        return $"{head}{formNext}\n      {SkillText(form)}";
    }

    static string UltimateText(UltimateDef u, int level)
    {
        var s = u.At(level);
        var inv = CultureInfo.InvariantCulture;
        string charge = u.Charge switch
        {
            ChargeKind.Cooldown => string.Create(inv, $"every {s.Charge:0} s"),
            ChargeKind.Kills => string.Create(inv, $"charged by {s.Charge:0} kills"),
            _ => string.Create(inv, $"charged by {s.Charge:0} Drops"),
        };
        string what = u.Kind switch
        {
            UltimateKind.SubQuake => string.Create(inv, $"{s.Damage:0} dmg + {s.Stun:0.#} s stun on screen") + (s.Tick > 0 ? string.Create(inv, $" · ground shakes {s.Duration / 4:0} bars") : ""),
            UltimateKind.Encore => string.Create(inv, $"Drop on for {s.Duration / 4:0} bars · skills +{s.Speed * 100f:0}% faster") + (s.Heal > 0 ? string.Create(inv, $" · +{s.Heal:0} HP per kill") : ""),
            UltimateKind.BassCannon => string.Create(inv, $"{s.Width:0} m beam for {s.Duration / 4:0} bars, kills what it touches") + (s.Tick > 0 ? " · leaves a burning line" : ""),
            UltimateKind.Inferno => string.Create(inv, $"{s.Damage:0} dmg · radius {s.Radius:0.#} m · burning ground {s.Duration:0.#} s"),
            UltimateKind.MeteorShower => string.Create(inv, $"{s.Amount} meteors · {s.Damage:0} dmg each · radius {s.Radius:0.#} m"),
            UltimateKind.Phoenix => string.Create(inv, $"{s.Damage:0} dmg · radius {s.Radius:0.#} m · restores {s.Heal:0} HP"),
            _ => u.Kind.ToString(),
        };
        return $"{what} · {charge} · Space / Y";
    }

    string SkillText(SkillForm f)
    {
        var s = f.Stats;
        var inv = CultureInfo.InvariantCulture;
        string unit = _heroDef.Rhythmic ? "beats" : "s";
        string every = string.Create(inv, $"every {s.Cooldown:0.##} {unit}");
        return f.Kind switch
        {
            SkillKind.Projectile => string.Create(inv, $"{s.Amount} projectile(s) at nearest · {s.Damage:0.#} dmg · {every} · range {s.Range:0.#} m") + (s.Blast > 0 ? $" · blast {s.Blast:0.#} m" : ""),
            SkillKind.Ring => string.Create(inv, $"expanding ring · {s.Damage:0.#} dmg · {every} · radius {s.Radius:0.#} m"),
            SkillKind.Speaker => string.Create(inv, $"up to {s.Amount} speakers · {s.Damage:0.#} dmg per beat · radius {s.Radius:0.#} m · last {s.Duration:0.#} beats"),
            SkillKind.Aura => string.Create(inv, $"{s.Damage:0.#} dmg · {every} · radius {s.Radius:0.#} m") + (s.Vuln > 0 ? $" · +{s.Vuln * 100f:0}% damage taken" : ""),
            SkillKind.Beam => string.Create(inv, $"beam where the camera looks · {s.Damage:0.#} dmg · {every} · {s.Range:0.#} m long"),
            SkillKind.Orbit => string.Create(inv, $"{s.Amount} orbiting {(_heroDef.Rhythmic ? "records" : "flames")} · {s.Damage:0.#} dmg per hit · orbit {s.Range:0.#} m"),
            SkillKind.BellDrop => string.Create(inv, $"{s.Amount} {(_heroDef.Rhythmic ? "bell(s)" : "meteor(s)")} on enemy groups · {s.Damage:0.#} dmg · {every} · radius {s.Radius:0.#} m"),
            SkillKind.Quake => string.Create(inv, $"cracks · {s.Damage:0.#} dmg per beat · {every} · radius {s.Radius:0.#} m · last {s.Duration:0.#} beats"),
            SkillKind.Drone => string.Create(inv, $"{s.Amount} drone(s) · {s.Damage:0.#} dmg per bolt · {every} · range {s.Range:0.#} m"),
            SkillKind.Shield => string.Create(inv, $"blocks a hit {every} · blast {s.Damage:0.#} dmg · radius {s.Radius:0.#} m"),
            SkillKind.Sweep => string.Create(inv, $"sweeps {s.Arc:0} degrees · {s.Damage:0.#} dmg · {every} · {s.Range:0.#} m long"),
            SkillKind.Boomerang => string.Create(inv, $"{s.Amount} record(s) out and back · {s.Damage:0.#} dmg · {every} · range {s.Range:0.#} m"),
            _ => f.Kind.ToString(),
        };
    }

    string MetaLine() => string.Create(CultureInfo.InvariantCulture, $"{MathF.Floor(_sim.MetaCreditsEarned):0} meta credits this run (kept on death)");

    void Restart()
    {
        Bank();
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
