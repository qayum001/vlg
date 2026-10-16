using Godot;

namespace Vlg.Game;

public sealed class Hud
{
    readonly Label _label;

    public bool Visible
    {
        get => _label.Visible;
        set => _label.Visible = value;
    }

    public Hud(Node parent)
    {
        var layer = new CanvasLayer { Name = "Hud" };
        _label = new Label
        {
            Position = new Vector2(12, 8),
            LabelSettings = new LabelSettings { FontSize = 16, OutlineSize = 5, OutlineColor = Colors.Black },
        };
        layer.AddChild(_label);
        parent.AddChild(layer);
    }

    public void SetText(string text) => _label.Text = text;
}
