using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Vlg.Core.Bench;
using Vlg.Core.Config;
using Vlg.Core.Meta;
using Vlg.Core.Sim;
using NVec2 = System.Numerics.Vector2;

namespace Vlg.Game;

/// <summary>
/// The Crossroads (domain.md hub between worlds): a small walkable plaza. Portal = start a level run, Setlist desk =
/// pick the loadout (4 of 6 skills + 1 ultimate), The Fixer = meta upgrades for meta credits. Interact with E / A
/// near an object. Choices and upgrades live in the save (<see cref="GameSession"/>). Placeholder geometry; the Fixer
/// is a capsule until the user's model (R11).
/// </summary>
public partial class HubGame : Node3D
{
    const string RunScene = "res://scenes/run.tscn";
    const float MouseYawPerPixel = 0.005f;
    const float StickYawRate = 2.5f;
    const float StickDeadzone = 0.2f;
    const float PlazaRadius = 22f;
    const float DevMetaCredits = 100f;

    record Spot(string Name, Vector3 Position, float Radius, string Prompt, Action Use);

    LaunchOptions _options = null!;
    HeroDef _hero = null!;
    OrbitCamera _camera = null!;
    MeshInstance3D _heroNode = null!;
    ChoicePanel _panel = null!;
    Label _status = null!;
    Label _prompt = null!;
    readonly List<Spot> _spots = new();
    NVec2 _pos;
    float _heroSpeed;
    Spot? _near;
    float _time;
    bool _screenshotDone;
    readonly List<string> _picking = new();

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
        var run = RunConfig.Parse(Godot.FileAccess.GetFileAsString("res://data/run.json"));
        _hero = GameSession.LoadHero(run);
        _heroSpeed = _hero.Speed;
        GameSession.Sanitize(_hero);

        var arena = new Arena(this, GD.Load<Shader>("res://shaders/ground.gdshader"), 7, 1, PlazaRadius + 4f);
        arena.Sun.DirectionalShadowMaxDistance = 60f;
        var cam = run.Camera;
        _camera = new OrbitCamera(this, cam.TiltDeg, cam.Distance * 0.75f, cam.FovDeg, cam.FollowRate);

        _heroNode = new MeshInstance3D
        {
            Name = "Hero",
            Mesh = new CapsuleMesh { Radius = run.Hero.Radius, Height = 1.7f },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.9f, 0.95f, 1f), EmissionEnabled = true, Emission = new Color(0.4f, 0.7f, 1f), EmissionEnergyMultiplier = 0.5f },
        };
        AddChild(_heroNode);

        AddPortal(new Vector3(0, 0, -12));
        AddDesk(new Vector3(-10, 0, 0));
        AddHeroStand(new Vector3(-4, 0, 4));
        AddFixer(new Vector3(10, 0, 0));
        AddChild(new Label3D
        {
            Text = "The Crossroads",
            Position = new Vector3(0, 6f, -16),
            FontSize = 160,
            OutlineSize = 24,
            Modulate = new Color(1f, 0.6f, 0.9f),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
        });

        BuildHud();
        _panel = new ChoicePanel(this);
        ApplyHeroAppearance();
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _camera.Snap(HeroWorld());
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        bool busy = _panel.IsOpen;
        if (!busy)
        {
            var move = ReadMove(dt) * _heroSpeed * dt;
            _pos += move;
            if (_pos.Length() > PlazaRadius) _pos *= PlazaRadius / _pos.Length();
        }
        _heroNode.Position = HeroWorld() + new Vector3(0, 0.85f, 0);
        _camera.Update(dt, HeroWorld());

        _near = null;
        float best = float.MaxValue;
        foreach (var s in _spots)
        {
            float d = new Vector2(s.Position.X - _pos.X, s.Position.Z - _pos.Y).Length();
            if (d <= s.Radius && d < best)
            {
                best = d;
                _near = s;
            }
        }
        _prompt.Text = _near != null && !busy ? $"E / A: {_near.Prompt}" : "";
        _time += dt;
        if (_options.ScreenshotPath is { } shot && !_screenshotDone && _time >= _options.ScreenshotAtSec)
        {
            _screenshotDone = true;
            var error = GetViewport().GetTexture().GetImage().SavePng(shot);
            if (error != Error.Ok) GD.PushError($"screenshot failed: {error} ({shot})");
            else GD.Print($"screenshot -> {shot}");
        }
        if (_options.ExitAfterSec is { } exit && _time >= exit) GetTree().Quit();
        var save = GameSession.Save;
        var inv = CultureInfo.InvariantCulture;
        _status.Text = string.Create(inv, $"The Crossroads · {MathF.Floor(save.MetaCredits):0} meta credits · best loop {save.BestLoop} · runs {save.Runs}\n")
            + $"{_hero.Name} · Setlist: {string.Join(", ", LoadoutNames())} + {_hero.UltimateById(save.Ultimate).Name}";
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _camera.Yaw -= motion.Relative.X * MouseYawPerPixel;
            return;
        }
        if (e is InputEventJoypadButton { Pressed: true, ButtonIndex: JoyButton.A } && !_panel.IsOpen)
        {
            Interact();
            return;
        }
        if (e is not InputEventKey { Pressed: true, Echo: false } key) return;
        switch (key.PhysicalKeycode)
        {
            case >= Key.Key1 and <= Key.Key9:
                _panel.Pick((int)(key.PhysicalKeycode - Key.Key1));
                break;
            case Key.E:
                if (!_panel.IsOpen) Interact();
                break;
            case Key.H:
                if (!_panel.IsOpen) PickHero();
                break;
            case Key.Escape:
                if (_panel.IsOpen) _panel.Hide();
                else ShowMenu();
                break;
            case Key.F5:
                GameSession.Save.MetaCredits += DevMetaCredits;
                GameSession.Persist();
                break;
            case Key.F11:
                DisplayServer.WindowSetMode(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Windowed
                    ? DisplayServer.WindowMode.Fullscreen
                    : DisplayServer.WindowMode.Windowed);
                break;
        }
    }

    void Interact() => _near?.Use();

    void ShowMenu() => _panel.Show("Paused", "", new[] { "Resume", "Quit" }, i =>
    {
        if (i == 1) GetTree().Quit();
        else _panel.Hide();
    });

    // ---- Spots ----

    void AddHeroStand(Vector3 at)
    {
        AddChild(new MeshInstance3D
        {
            Name = "HeroStand",
            Mesh = new CylinderMesh { TopRadius = 1.1f, BottomRadius = 1.2f, Height = 0.18f },
            Position = at + new Vector3(0, 0.09f, 0),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.8f, 0.3f, 0.08f) },
        });
        AddLabel(at + new Vector3(0, 2.2f, 0), "Change hero · H");
        _spots.Add(new Spot("Heroes", at, 2.8f, "change hero (development access)", PickHero));
    }

    void PickHero()
    {
        var run = RunConfig.Parse(Godot.FileAccess.GetFileAsString("res://data/run.json"));
        var heroes = new HeroDef[HeroRoster.Ids.Count];
        var labels = new string[heroes.Length + 1];
        for (int i = 0; i < heroes.Length; i++)
        {
            heroes[i] = GameSession.LoadHero(run, HeroRoster.Ids[i]);
            var hero = heroes[i];
            labels[i] = $"{hero.Name}{(hero.Id == _hero.Id ? " · selected" : "")}\n      HP {hero.MaxHp:0} · Speed {hero.Speed:0.#} · {(hero.Rhythmic ? "The Drop" : hero.PassiveName)}";
        }
        labels[heroes.Length] = "Cancel";
        _panel.Show("Choose hero", "Development access: both heroes are available.\nChanging hero resets the setlist to that hero's default four skills and ultimate.", labels, i =>
        {
            if (i < heroes.Length)
            {
                _hero = heroes[i];
                GameSession.Save.SelectHero(_hero.Id);
                GameSession.Sanitize(_hero);
                _heroSpeed = _hero.Speed;
                ApplyHeroAppearance();
                GameSession.Persist();
            }
            _panel.Hide();
        });
    }

    void ApplyHeroAppearance()
    {
        var material = (StandardMaterial3D)_heroNode.MaterialOverride;
        material.AlbedoColor = _hero.Id == "pyromancer" ? new Color(0.85f, 0.22f, 0.055f) : new Color(0.9f, 0.95f, 1f);
        material.Emission = _hero.Id == "pyromancer" ? new Color(1f, 0.32f, 0.04f) : new Color(0.4f, 0.7f, 1f);
    }

    void AddPortal(Vector3 at)
    {
        var ring = new MeshInstance3D
        {
            Name = "Portal",
            Mesh = new TorusMesh { InnerRadius = 1.6f, OuterRadius = 2.0f, Rings = 48, RingSegments = 12 },
            Position = at + new Vector3(0, 2.2f, 0),
            RotationDegrees = new Vector3(90, 0, 0),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.6f, 0.2f, 1f), EmissionEnabled = true, Emission = new Color(0.7f, 0.25f, 1f), EmissionEnergyMultiplier = 3f },
        };
        AddChild(ring);
        AddChild(new MeshInstance3D
        {
            Name = "PortalFace",
            Mesh = new CylinderMesh { TopRadius = 1.65f, BottomRadius = 1.65f, Height = 0.05f, RadialSegments = 48 },
            Position = at + new Vector3(0, 2.2f, 0),
            RotationDegrees = new Vector3(90, 0, 0),
            MaterialOverride = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                BlendMode = BaseMaterial3D.BlendModeEnum.Add,
                AlbedoColor = new Color(0.3f, 0.9f, 1f, 0.5f),
            },
        });
        AddLabel(at + new Vector3(0, 4.8f, 0), "Undead Town · endless");
        _spots.Add(new Spot("Portal", at, 3f, "enter Undead Town (endless loops)", StartRun));
    }

    void AddDesk(Vector3 at)
    {
        AddChild(new MeshInstance3D
        {
            Name = "SetlistDesk",
            Mesh = new BoxMesh { Size = new Vector3(2.4f, 1f, 1.2f) },
            Position = at + new Vector3(0, 0.5f, 0),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.2f, 0.12f, 0.3f), EmissionEnabled = true, Emission = new Color(0.3f, 0.95f, 1f), EmissionEnergyMultiplier = 0.4f },
        });
        AddLabel(at + new Vector3(0, 2.2f, 0), "Setlist");
        _spots.Add(new Spot("Desk", at, 2.8f, "change the setlist (skills + ultimate)", PickLoadout));
    }

    void AddFixer(Vector3 at)
    {
        AddChild(new MeshInstance3D
        {
            Name = "TheFixer",
            Mesh = new CapsuleMesh { Radius = 0.5f, Height = 1.8f },
            Position = at + new Vector3(0, 0.9f, 0),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.6f, 0.2f) },
        });
        AddLabel(at + new Vector3(0, 2.5f, 0), "The Fixer");
        _spots.Add(new Spot("Fixer", at, 2.8f, "talk to The Fixer (meta upgrades)", ShowFixer));
    }

    void AddLabel(Vector3 at, string text) => AddChild(new Label3D
    {
        Text = text,
        Position = at,
        FontSize = 72,
        OutlineSize = 16,
        Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
    });

    // ---- Actions ----

    void StartRun()
    {
        GameSession.Persist();
        Input.MouseMode = Input.MouseModeEnum.Captured;
        GetTree().ChangeSceneToFile(RunScene);
    }

    void ShowFixer()
    {
        var shop = GameSession.Shop;
        var upgrades = shop.Config.Upgrades;
        var labels = new List<string>(upgrades.Length + 1);
        var inv = CultureInfo.InvariantCulture;
        foreach (var u in upgrades)
        {
            int rank = shop.Rank(u), cost = shop.NextCost(u);
            string price = cost < 0 ? "maxed" : string.Create(inv, $"{cost} mc");
            labels.Add($"{u.Name}  {rank}/{u.MaxRank}  ·  {price}\n      {u.Text}");
        }
        labels.Add("Leave");
        _panel.Show("The Fixer", string.Create(inv, $"\"Everything has a price. Even the exit.\"   You have {MathF.Floor(shop.Save.MetaCredits):0} meta credits."),
            labels, i =>
            {
                if (i >= upgrades.Length)
                {
                    _panel.Hide();
                    return;
                }
                if (shop.Buy(upgrades[i])) GameSession.Persist();
                ShowFixer();
            });
    }

    /// <summary>Slot by slot: 4 distinct skills (first one starts unlocked), then the ultimate.</summary>
    void PickLoadout()
    {
        _picking.Clear();
        PickNextSkill();
    }

    void PickNextSkill()
    {
        const int slots = 4;
        if (_picking.Count == slots)
        {
            PickUltimate();
            return;
        }
        var options = new List<SkillDef>();
        foreach (var s in _hero.Skills)
            if (!_picking.Contains(s.Id)) options.Add(s);
        var labels = options.ConvertAll(s => $"{s.Name}  ·  {string.Join(", ", s.Tags)}");
        var icons = options.ConvertAll(s => Icons.Skill(s.Id));
        string body = _picking.Count == 0 ? "Slot 1 starts the run unlocked." : $"So far: {string.Join(", ", _picking)}";
        _panel.Show($"Setlist: skill {_picking.Count + 1} of {slots}", body, labels, i =>
        {
            _picking.Add(options[i].Id);
            PickNextSkill();
        }, icons);
    }

    void PickUltimate()
    {
        var ults = _hero.Ultimates;
        var labels = Array.ConvertAll(ults, u => $"{u.Name}  ·  {u.Text}");
        var icons = Array.ConvertAll(ults, u => Icons.Skill(u.Id));
        _panel.Show("Setlist: ultimate", "Unlocks at hero level 10 in the run.", labels, i =>
        {
            var save = GameSession.Save;
            save.Loadout = _picking.ToArray();
            save.Ultimate = ults[i].Id;
            GameSession.Persist();
            _panel.Hide();
        }, icons);
    }

    IEnumerable<string> LoadoutNames()
    {
        foreach (var s in _hero.LoadoutSkills(GameSession.Save.Loadout)) yield return s.Name;
    }

    // ---- Input, HUD ----

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

    void BuildHud()
    {
        var layer = new CanvasLayer { Name = "HubHud" };
        AddChild(layer);
        _status = new Label { Position = new Vector2(16, 12), LabelSettings = new LabelSettings { FontSize = 20, OutlineSize = 5, OutlineColor = Colors.Black } };
        layer.AddChild(_status);
        var hint = new Label
        {
            Position = new Vector2(16, 72),
            Text = "WASD / left stick move · mouse / right stick camera · E / A interact · H change hero · Esc menu · F5 +100 meta credits (dev)",
            LabelSettings = new LabelSettings { FontSize = 14, OutlineSize = 4, OutlineColor = Colors.Black },
            Modulate = new Color(1, 1, 1, 0.7f),
        };
        layer.AddChild(hint);
        _prompt = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            LabelSettings = new LabelSettings { FontSize = 26, OutlineSize = 6, OutlineColor = Colors.Black },
        };
        _prompt.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
        _prompt.OffsetTop = -120;
        _prompt.OffsetBottom = -80;
        _prompt.OffsetLeft = -400;
        _prompt.OffsetRight = 400;
        layer.AddChild(_prompt);
    }

    Vector3 HeroWorld() => new(_pos.X, 0f, _pos.Y);
}
