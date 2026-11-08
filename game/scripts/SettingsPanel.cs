using System;
using Godot;

namespace Vlg.Game;

public sealed class SettingsPanel
{
    readonly Control _root;
    readonly HSlider _volume;
    readonly Label _value;
    readonly Label _status;

    public bool Visible => _root.Visible;

    public SettingsPanel(Node parent, Action onBack)
    {
        var layer = new CanvasLayer { Name = "SettingsPanel", Layer = 11 };
        parent.AddChild(layer);
        _root = new Control { Name = "Root", Visible = false };
        layer.AddChild(_root);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var dim = new ColorRect { Color = new Color(0, 0, 0, 0.55f) };
        _root.AddChild(dim);
        dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var center = new CenterContainer();
        _root.AddChild(center);
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(560, 0) };
        center.AddChild(panel);
        var margin = new MarginContainer();
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            margin.AddThemeConstantOverride(side, 24);
        panel.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 18);
        margin.AddChild(column);

        var title = new Label { Text = "Settings", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 32);
        column.AddChild(title);

        var row = new HBoxContainer();
        column.AddChild(row);
        var label = new Label { Text = "Master volume", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        label.AddThemeFontSizeOverride("font_size", 20);
        row.AddChild(label);
        _value = new Label { Name = "VolumeValue", CustomMinimumSize = new Vector2(90, 0), HorizontalAlignment = HorizontalAlignment.Right };
        _value.AddThemeFontSizeOverride("font_size", 20);
        row.AddChild(_value);

        _volume = new HSlider
        {
            Name = "MasterVolume",
            MinValue = 0,
            MaxValue = 100,
            Step = 1,
            CustomMinimumSize = new Vector2(0, 40),
            FocusMode = Control.FocusModeEnum.All,
            TooltipText = "Master volume (0% mutes all audio)",
        };
        column.AddChild(_volume);
        _status = new Label
        {
            Name = "SaveStatus",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        column.AddChild(_status);
        _volume.ValueChanged += value =>
        {
            int percent = (int)value;
            UpdateValue(percent);
            _status.Text = GameSettings.SetMasterVolume(percent)
                ? "Changes are saved automatically."
                : "Volume applied. Could not save settings.";
        };
        var back = new Button { Name = "Back", Text = "Back", CustomMinimumSize = new Vector2(0, 52) };
        back.AddThemeFontSizeOverride("font_size", 20);
        column.AddChild(back);
        back.Pressed += onBack;
        _volume.FocusNeighborBottom = _volume.GetPathTo(back);
        _volume.FocusNext = _volume.GetPathTo(back);
        back.FocusNeighborTop = back.GetPathTo(_volume);
        back.FocusPrevious = back.GetPathTo(_volume);
    }

    public void Show()
    {
        int percent = GameSettings.Current.MasterVolumePercent;
        _volume.SetValueNoSignal(percent);
        UpdateValue(percent);
        _status.Text = "Changes are saved automatically.";
        _root.Show();
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _volume.CallDeferred(Control.MethodName.GrabFocus);
    }

    public void Hide() => _root.Hide();

    void UpdateValue(int percent) => _value.Text = percent == 0 ? "Muted" : $"{percent}%";
}
