using System;
using System.Globalization;
using System.Text;
using Godot;
using Vlg.Core.Config;
using Vlg.Core.Run;

namespace Vlg.Game;

/// <summary>Run HUD: HP, XP + level, loop/stage/timer, skill levels, counters, controls hint.</summary>
public sealed class RunHud
{
    const string Hint = "WASD / left stick move · mouse / right stick camera · Esc / Start pause · F1 HUD · F2 skip stage · F3 +1 level";

    readonly Control _root;
    readonly ProgressBar _hp;
    readonly Label _hpText;
    readonly ProgressBar _xp;
    readonly Label _xpText;
    readonly Label _stage;
    readonly Label _skills;
    readonly Label _stats;
    readonly StringBuilder _sb = new();

    public bool Visible
    {
        get => _root.Visible;
        set => _root.Visible = value;
    }

    public RunHud(Node parent)
    {
        var layer = new CanvasLayer { Name = "Hud" };
        parent.AddChild(layer);
        _root = new Control { Name = "Root", MouseFilter = Control.MouseFilterEnum.Ignore };
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        layer.AddChild(_root);

        (_hp, _hpText) = Bar(new Color(0.85f, 0.2f, 0.25f));
        _hp.Position = new Vector2(16, 16);
        _hp.Size = new Vector2(340, 26);

        (_xp, _xpText) = Bar(new Color(0.3f, 0.6f, 1f));
        _xp.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        _xp.OffsetLeft = 16;
        _xp.OffsetRight = -16;
        _xp.OffsetTop = -38;
        _xp.OffsetBottom = -14;

        _stage = AddLabel(30, HorizontalAlignment.Center);
        _stage.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        _stage.OffsetTop = 12;

        _stats = AddLabel(16, HorizontalAlignment.Right);
        _stats.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        _stats.OffsetTop = 16;
        _stats.OffsetRight = -16;

        var hint = AddLabel(14, HorizontalAlignment.Left);
        hint.Position = new Vector2(16, 48);
        hint.Text = Hint;
        hint.Modulate = new Color(1, 1, 1, 0.7f);

        _skills = AddLabel(20, HorizontalAlignment.Left);
        _skills.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        _skills.OffsetLeft = 16;
        _skills.OffsetTop = -74;
        _skills.OffsetBottom = -44;
    }

    public void Update(RunSim sim, float fps)
    {
        var inv = CultureInfo.InvariantCulture;
        var p = sim.Progress;
        var c = sim.Config;

        _hp.MaxValue = sim.HeroMaxHp;
        _hp.Value = Math.Max(0f, sim.HeroHp);
        _hpText.Text = string.Create(inv, $"HP {MathF.Ceiling(Math.Max(0f, sim.HeroHp)):F0} / {sim.HeroMaxHp:F0}");

        _xp.MaxValue = p.XpToNext;
        _xp.Value = p.Xp;
        _xpText.Text = string.Create(inv, $"Level {p.Level}   XP {p.Xp:F0} / {p.XpToNext:F0}");

        var st = sim.Stages;
        int left = (int)MathF.Ceiling(st.TimeLeft);
        string timer = st.Current.IsTimed ? string.Create(inv, $" · {left / 60}:{left % 60:D2}") : "";
        _stage.Text = string.Create(inv, $"Loop {st.Loop} · {StageName(st.Current.Kind)}{timer}");

        _sb.Clear();
        for (int i = 0; i < c.Skills.Length; i++)
        {
            int level = p.SkillLevel(i);
            _sb.Append(c.Skills[i].Name).Append(' ').Append(level > 0 ? level.ToString(inv) : "–").Append("     ");
        }
        _sb.Append("skill cap ").Append(p.SkillCap.ToString(inv));
        if (p.BonusCount > 0) _sb.Append("   bonus +").Append(((p.DamageMultiplier - 1f) * 100f).ToString("0.#", inv)).Append("% damage");
        _skills.Text = _sb.ToString();

        _stats.Text = string.Create(inv,
            $"time {(int)sim.Time / 60}:{(int)sim.Time % 60:D2} · kills {sim.Kills} · enemies {sim.Enemies.Count} · {fps:F0} fps");
    }

    public static string StageName(StageKind kind) => kind switch
    {
        StageKind.Mobs => "Mobs",
        StageKind.Horde => "Horde",
        StageKind.Chest => "Chest",
        StageKind.Shop => "Shop",
        _ => kind.ToString(),
    };

    (ProgressBar, Label) Bar(Color fill)
    {
        var bar = new ProgressBar { ShowPercentage = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        bar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = fill });
        bar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.55f) });
        _root.AddChild(bar);
        var text = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            LabelSettings = new LabelSettings { FontSize = 15, OutlineSize = 4, OutlineColor = Colors.Black },
        };
        text.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        bar.AddChild(text);
        return (bar, text);
    }

    Label AddLabel(int size, HorizontalAlignment align)
    {
        var label = new Label
        {
            HorizontalAlignment = align,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            LabelSettings = new LabelSettings { FontSize = size, OutlineSize = 5, OutlineColor = Colors.Black },
        };
        _root.AddChild(label);
        return label;
    }
}
