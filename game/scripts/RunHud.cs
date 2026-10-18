using System;
using System.Globalization;
using System.Text;
using Godot;
using Vlg.Core.Config;
using Vlg.Core.Run;

namespace Vlg.Game;

/// <summary>Run HUD: HP, XP + level, loop/stage/timer, skill levels, beat + Drop meter, counters, controls hint.</summary>
public sealed class RunHud
{
    const string Hint = "WASD / left stick move · mouse / right stick camera (aims Slap) · Space / Y ultimate · Q E / LB RB items · Esc / Start pause · M mute · F1 HUD · F2 skip stage · F3 +1 level · F4 charge ult · F5 +100 credits";
    static readonly string[] UseKeys = { "Q", "E", "R", "F", "G" };

    static readonly Color BeatOff = new(1f, 1f, 1f, 0.2f);
    static readonly Color BeatOn = new(0.3f, 0.95f, 1f);
    static readonly Color DropOn = new(1f, 0.3f, 0.75f);

    readonly Control _root;
    readonly ProgressBar _hp;
    readonly Label _hpText;
    readonly ProgressBar _xp;
    readonly Label _xpText;
    readonly Label _stage;
    readonly Label _skills;
    readonly Label _stats;
    readonly Label _items;
    readonly ColorRect[] _beats = new ColorRect[BeatClock.BeatsPerBar];
    readonly ProgressBar _cycle;
    readonly StyleBoxFlat _cycleFill;
    readonly Label _dropText;
    readonly ProgressBar _ult;
    readonly Label _ultText;
    readonly StyleBoxFlat _ultFill;
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

        // Form names get long ("Speaker Stack · Boombox Drone · Bass Boosted"): wrap upward over two lines.
        _skills = AddLabel(17, HorizontalAlignment.Left);
        _skills.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        _skills.OffsetLeft = 16;
        _skills.OffsetRight = -16;
        _skills.OffsetTop = -100;
        _skills.OffsetBottom = -44;
        _skills.VerticalAlignment = VerticalAlignment.Bottom;
        _skills.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        // Items: bottom right, above the ultimate bar.
        _items = AddLabel(16, HorizontalAlignment.Right);
        _items.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight);
        _items.OffsetLeft = -560;
        _items.OffsetRight = -16;
        _items.OffsetTop = -240;
        _items.OffsetBottom = -140;
        _items.VerticalAlignment = VerticalAlignment.Bottom;

        // Ultimate: bottom right, above the XP bar.
        _ultFill = new StyleBoxFlat { BgColor = new Color(1f, 0.85f, 0.3f) };
        _ult = new ProgressBar { ShowPercentage = false, MaxValue = 1, MouseFilter = Control.MouseFilterEnum.Ignore };
        _ult.AddThemeStyleboxOverride("fill", _ultFill);
        _ult.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.55f) });
        _ult.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight);
        _ult.OffsetLeft = -360;
        _ult.OffsetRight = -16;
        _ult.OffsetTop = -136;
        _ult.OffsetBottom = -108;
        _root.AddChild(_ult);
        _ultText = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            LabelSettings = new LabelSettings { FontSize = 15, OutlineSize = 4, OutlineColor = Colors.Black },
        };
        _ultText.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _ult.AddChild(_ultText);

        // Beat row under the stage line: 4 beat squares, cycle bar toward the next Drop, status text.
        var beatRow = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        beatRow.AddThemeConstantOverride("separation", 6);
        beatRow.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
        beatRow.OffsetTop = 74;
        beatRow.OffsetLeft = -170;
        beatRow.OffsetRight = 170;
        _root.AddChild(beatRow);
        for (int i = 0; i < _beats.Length; i++)
        {
            _beats[i] = new ColorRect { CustomMinimumSize = new Vector2(18, 18), Color = BeatOff, MouseFilter = Control.MouseFilterEnum.Ignore };
            beatRow.AddChild(_beats[i]);
        }
        _cycleFill = new StyleBoxFlat { BgColor = BeatOn };
        _cycle = new ProgressBar { ShowPercentage = false, CustomMinimumSize = new Vector2(150, 18), MaxValue = 1, MouseFilter = Control.MouseFilterEnum.Ignore };
        _cycle.AddThemeStyleboxOverride("fill", _cycleFill);
        _cycle.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.55f) });
        beatRow.AddChild(_cycle);
        _dropText = new Label
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            LabelSettings = new LabelSettings { FontSize = 18, OutlineSize = 5, OutlineColor = Colors.Black },
        };
        beatRow.AddChild(_dropText);
    }

    /// <summary>Every frame: the beat squares must not lag behind the groove.</summary>
    public void UpdateBeat(BeatClock beat)
    {
        bool drop = beat.InDrop;
        float pump = MathF.Exp(-beat.BeatPhase * 5f);
        for (int i = 0; i < _beats.Length; i++)
        {
            var on = drop ? DropOn : BeatOn;
            _beats[i].Color = i == beat.BeatInBar && beat.Beat >= 0 ? on.Lerp(Colors.White, 0.5f * pump) : BeatOff;
        }
        _cycle.Value = (beat.BeatInCycle + beat.BeatPhase) / beat.CycleBeats;
        _cycleFill.BgColor = drop ? DropOn : beat.InBuild ? BeatOn.Lerp(DropOn, beat.BuildProgress) : BeatOn;
        _dropText.Text = drop ? "DROP" : beat.InBuild ? "build-up" : "";
        _dropText.Modulate = drop ? DropOn : Colors.White;
    }

    public void Update(RunSim sim, float fps)
    {
        var inv = CultureInfo.InvariantCulture;
        var p = sim.Progress;

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
        for (int i = 0; i < sim.Loadout.Length; i++)
        {
            int level = p.SkillLevel(i);
            _sb.Append(sim.Skills.Form(i)?.Name ?? sim.Loadout[i].Name).Append(' ').Append(level > 0 ? level.ToString(inv) : "–").Append("     ");
        }
        _sb.Append("skill cap ").Append(p.SkillCap.ToString(inv));
        if (p.BonusCount > 0) _sb.Append("   bonus +").Append(((p.DamageMultiplier - 1f) * 100f).ToString("0.#", inv)).Append("% damage");
        _skills.Text = _sb.ToString();

        var u = sim.Ultimate;
        if (u.Level == 0)
        {
            _ult.Value = 0;
            _ultText.Text = $"{u.Def.Name}: unlocks at hero level {sim.Config.Leveling.UltimateLevels[0]}";
        }
        else
        {
            _ult.Value = u.Phase != UltimatePhase.Idle ? 1 : u.Charge / u.ChargeNeeded;
            _ultFill.BgColor = u.Ready ? new Color(1f, 0.3f, 0.75f) : new Color(1f, 0.85f, 0.3f);
            string state = u.Phase switch
            {
                UltimatePhase.Encore => "ENCORE!",
                UltimatePhase.Windup => "charging...",
                UltimatePhase.Firing => "FIRE!",
                _ => u.Ready ? "READY · Space / Y" : u.Def.Charge switch
                {
                    ChargeKind.Cooldown => string.Create(inv, $"{u.ChargeNeeded - u.Charge:0} s"),
                    ChargeKind.Kills => string.Create(inv, $"{u.Charge:0} / {u.ChargeNeeded:0} kills"),
                    _ => string.Create(inv, $"{u.Charge:0} / {u.ChargeNeeded:0} Drops"),
                },
            };
            _ultText.Text = string.Create(inv, $"{u.Def.Name} {u.Level} · {state}");
        }

        var invt = sim.Inventory;
        var defs = invt.Config.Items;
        _sb.Clear();
        _sb.Append(string.Create(inv, $"{MathF.Floor(invt.Credits):0} credits"));
        for (int i = 0; i < invt.Usables.Count; i++)
        {
            int item = invt.Usables[i];
            float cd = sim.Usables.CooldownLeft(item);
            _sb.Append('\n').Append(UseKeys[i]).Append("  ").Append(defs[item].Name)
                .Append(cd > 0f ? string.Create(inv, $"  {MathF.Ceiling(cd):0} s") : "  ready");
        }
        if (invt.Actives.Count > 0)
        {
            _sb.Append("\nactive: ");
            for (int i = 0; i < invt.Actives.Count; i++) _sb.Append(i > 0 ? ", " : "").Append(defs[invt.Actives[i]].Name);
        }
        _items.Text = _sb.ToString();

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
