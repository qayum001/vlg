using System;
using System.Collections.Generic;
using Godot;

namespace Vlg.Game;

/// <summary>
/// Modal list of choices over a dimmed screen: level-up picks, chest/shop placeholders, pause, death.
/// Mouse click, keys 1..9 (via <see cref="Pick"/>) or gamepad focus + ui_accept.
/// </summary>
public sealed class ChoicePanel
{
    readonly Control _root;
    readonly Label _title;
    readonly Label _body;
    readonly VBoxContainer _buttons;
    Action<int>? _onPick;
    int _count;

    public ChoicePanel(Node parent)
    {
        var layer = new CanvasLayer { Name = "ChoicePanel", Layer = 10 };
        parent.AddChild(layer);

        _root = new Control { Name = "Root", Visible = false };
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        layer.AddChild(_root);

        var dim = new ColorRect { Color = new Color(0, 0, 0, 0.55f), MouseFilter = Control.MouseFilterEnum.Stop };
        dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(dim);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(center);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(560, 0) };
        center.AddChild(panel);
        var margin = new MarginContainer();
        foreach (var side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            margin.AddThemeConstantOverride(side, 24);
        panel.AddChild(margin);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 14);
        margin.AddChild(column);

        _title = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _title.AddThemeFontSizeOverride("font_size", 32);
        column.AddChild(_title);

        _body = new Label { HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _body.AddThemeFontSizeOverride("font_size", 18);
        column.AddChild(_body);

        _buttons = new VBoxContainer();
        _buttons.AddThemeConstantOverride("separation", 8);
        column.AddChild(_buttons);
    }

    public bool IsOpen => _root.Visible;

    /// <param name="icons">Per option icon (null entries or list = none).</param>
    public void Show(string title, string body, IReadOnlyList<string> options, Action<int> onPick, IReadOnlyList<Texture2D?>? icons = null)
    {
        foreach (var child in _buttons.GetChildren()) child.QueueFree();
        _onPick = onPick;
        _count = options.Count;
        _title.Text = title;
        _body.Text = body;
        _body.Visible = body.Length > 0;
        Button? first = null;
        for (int i = 0; i < options.Count; i++)
        {
            int index = i;
            var button = new Button
            {
                Text = $"{i + 1}   {options[i]}",
                Alignment = HorizontalAlignment.Left,
                CustomMinimumSize = new Vector2(0, 52),
                Icon = icons != null && i < icons.Count ? icons[i] : null,
            };
            button.AddThemeFontSizeOverride("font_size", 20);
            button.AddThemeConstantOverride("icon_max_width", 56);
            button.Pressed += () => Pick(index);
            _buttons.AddChild(button);
            first ??= button;
        }
        _root.Visible = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        first?.CallDeferred(Control.MethodName.GrabFocus);
    }

    public void Hide()
    {
        _root.Visible = false;
        _onPick = null;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    /// <summary>
    /// One pick per Show: later picks are ignored until the next Show, so two inputs in one frame
    /// can't answer a list the player hasn't seen. Ignored when hidden or out of range.
    /// </summary>
    public void Pick(int index)
    {
        if (!_root.Visible || _onPick == null || (uint)index >= (uint)_count) return;
        var onPick = _onPick;
        _onPick = null;
        onPick(index);
    }
}
