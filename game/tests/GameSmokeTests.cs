#if DEBUG
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vlg.Core.Config;
using Vlg.Core.Run;

namespace Vlg.Game.Tests;

public partial class GameSmokeTests : Node
{
    int _checks;

    public override async void _Ready()
    {
        string phase = OS.GetEnvironment("VLG_GAME_TEST_PHASE");
        try
        {
            if (!DisplayServer.GetName().Equals("headless", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Game smoke tests require --headless.");
            string directory = OS.GetEnvironment("VLG_GAME_TEST_DIRECTORY");
            if (directory.Length == 0 || !Path.GetFullPath(OS.GetUserDataDir()).StartsWith(
                    Path.GetFullPath(directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Game smoke tests require isolated user data via tools/test-game.ps1.");

            GetTree().Root.ContentScaleSize = new Vector2I(1280, 800);
            GetTree().Root.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;

            await Frames();
            GameSettings.Initialize();
            switch (phase)
            {
                case "settings": await TestSettings(); break;
                case "reload": CheckVolume(37); break;
                case "corrupt": CheckVolume(100); break;
                default: throw new ArgumentException($"Unknown smoke test phase: {phase}");
            }
            GD.Print($"GAME_SMOKE_PASS {phase}: {_checks} checks");
            GetTree().Quit();
        }
        catch (Exception e)
        {
            GD.PushError($"GAME_SMOKE_FAIL {phase}: {e}");
            GetTree().Quit(1);
        }
    }

    async Task TestSettings()
    {
        CheckVolume(100);
        var game = GD.Load<PackedScene>("res://scenes/run.tscn").Instantiate<RunGame>();
        GetTree().Root.AddChild(game);
        GetTree().CurrentScene = game;
        await Frames();

        var choices = game.GetNode<Control>("ChoicePanel/Root");
        var settings = game.GetNode<Control>("SettingsPanel/Root");
        var slider = (HSlider)settings.FindChild("MasterVolume", owned: false);
        var value = (Label)settings.FindChild("VolumeValue", owned: false);
        var status = (Label)settings.FindChild("SaveStatus", owned: false);
        var back = (Button)settings.FindChild("Back", owned: false);

        Check(!choices.Visible && !settings.Visible, "Run starts without a menu");
        await KeyPress(Key.Escape);
        Check(choices.Visible, "Escape opens pause");
        float pausedTime = Sim(game).Time;
        await Frames(10);
        Check(Sim(game).Time == pausedTime, "Pause stops simulation");

        await KeyPress(Key.Key2);
        Check(settings.Visible && !choices.Visible, "Settings replaces the pause choices");
        Check(slider.HasFocus(), "Volume slider receives focus");
        Check(slider.Value == 100 && value.Text == "100%", "Default volume is displayed");
        var rect = slider.GetGlobalRect();
        Check(rect.Size.X > 400 && rect.Size.Y >= 40 && GetViewport().GetVisibleRect().Encloses(rect),
            $"Slider has usable bounds inside the viewport (slider {rect}, viewport {GetViewport().GetVisibleRect()})");

        await KeyPress(Key.Left);
        Check(slider.Value == 99, "Left arrow adjusts the focused slider");
        CheckVolume(99);
        await MouseClick(rect.Position + new Vector2(rect.Size.X * 0.25f, rect.Size.Y * 0.5f));
        Check(slider.Value is >= 20 and <= 30, $"Mouse click adjusts the volume bar (got {slider.Value})");
        CheckVolume((int)slider.Value);

        slider.Value = 0;
        CheckVolume(0);
        Check(value.Text == "Muted", "Zero volume displays Muted");
        slider.Value = 37;
        CheckVolume(37);
        Check(value.Text == "37%", "Increasing volume updates the percentage");
        string path = ProjectSettings.GlobalizePath("user://settings.json");
        Check(UserSettings.Parse(File.ReadAllText(path)).MasterVolumePercent == 37, "Volume is saved on change");

        await KeyPress(Key.Tab);
        Check(back.HasFocus(), "Tab focuses Back");
        await KeyPress(Key.Enter);
        Check(!settings.Visible && choices.Visible, "Back returns to pause");
        Check(Sim(game).Time == pausedTime, "Settings and Back keep the simulation paused");
        await KeyPress(Key.Escape);
        Check(!choices.Visible, "Escape resumes from pause");
        Check(Sim(game).Time > pausedTime, "Simulation resumes");

        await KeyPress(Key.Escape);
        await KeyPress(Key.Key2);
        await KeyPress(Key.Escape);
        Check(!settings.Visible && choices.Visible, "Escape returns from Settings to pause");
        await KeyPress(Key.Key2);
        await JoyPress(JoyButton.B);
        Check(!settings.Visible && choices.Visible, "Gamepad B returns to pause");
        await JoyPress(JoyButton.Start);
        Check(!choices.Visible, "Gamepad Start resumes");
        await JoyPress(JoyButton.Start);
        await KeyPress(Key.Key2);
        await JoyPress(JoyButton.DpadRight);
        Check(slider.Value == 38, $"Gamepad d-pad adjusts volume (got {slider.Value}, focus {slider.HasFocus()})");
        await JoyPress(JoyButton.DpadLeft);
        CheckVolume(37);
        await JoyPress(JoyButton.Start);
        Check(!settings.Visible && choices.Visible, "Start returns from Settings to pause");

        ulong oldRun = game.GetInstanceId();
        await KeyPress(Key.Key3);
        await Frames();
        game = (RunGame)GetTree().CurrentScene;
        Check(game.GetInstanceId() != oldRun, "Restart replaces the run scene");
        CheckVolume(37);
        await KeyPress(Key.Escape);
        await KeyPress(Key.Key2);
        settings = game.GetNode<Control>("SettingsPanel/Root");
        slider = (HSlider)settings.FindChild("MasterVolume", owned: false);
        status = (Label)settings.FindChild("SaveStatus", owned: false);
        Check(slider.Value == 37, "Restarted run displays saved volume");

        Directory.CreateDirectory(path + ".tmp");
        slider.Value = 40;
        CheckVolume(40);
        Check(status.Text.Contains("Could not save", StringComparison.Ordinal), "Save failure is visible");
        Check(UserSettings.Parse(File.ReadAllText(path)).MasterVolumePercent == 37,
            "Failed save preserves the previous settings file");
    }

    void CheckVolume(int percent)
    {
        int bus = AudioServer.GetBusIndex("Master");
        Check(GameSettings.Current.MasterVolumePercent == percent, $"Stored volume is {percent}%");
        Check(Math.Abs(AudioServer.GetBusVolumeLinear(bus) - percent / 100f) < 0.0001f,
            $"Master bus volume is {percent}%");
        Check(AudioServer.IsBusMute(bus) == (percent == 0), "Master bus mute state matches volume");
    }

    static RunSim Sim(RunGame game) => (RunSim)typeof(RunGame)
        .GetField("_sim", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(game)!;

    async Task KeyPress(Key key)
    {
        GetViewport().PushInput(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        await Frames(1);
        GetViewport().PushInput(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
        await Frames();
    }

    async Task JoyPress(JoyButton button)
    {
        Input.ParseInputEvent(new InputEventJoypadButton { ButtonIndex = button, Pressed = true });
        await Frames(1);
        Input.ParseInputEvent(new InputEventJoypadButton { ButtonIndex = button, Pressed = false });
        await Frames();
    }

    async Task MouseClick(Vector2 position)
    {
        GetViewport().PushInput(new InputEventMouseMotion { Position = position, GlobalPosition = position }, inLocalCoords: true);
        await Frames(1);
        GetViewport().PushInput(new InputEventMouseButton
        {
            Position = position, GlobalPosition = position, ButtonIndex = MouseButton.Left,
            ButtonMask = MouseButtonMask.Left, Pressed = true,
        }, inLocalCoords: true);
        await Frames(1);
        GetViewport().PushInput(new InputEventMouseButton
        {
            Position = position, GlobalPosition = position, ButtonIndex = MouseButton.Left, Pressed = false,
        }, inLocalCoords: true);
        await Frames();
    }

    async Task Frames(int count = 3)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }
}
#endif
