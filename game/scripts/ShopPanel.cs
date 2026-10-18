using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Godot;
using Vlg.Core.Config;
using Vlg.Core.Run;

namespace Vlg.Game;

/// <summary>
/// Shop stage screen (domain.md Shop): offers with Buy, Reroll, Continue, and the build: skill sockets (click a
/// modifier to move it to the stash), stash (click to socket), actives, usables, passives. Rebuilt after every action.
/// Socket choices go through <see cref="ChoicePanel"/> (drawn above this panel). Mouse, keys and gamepad focus.
/// </summary>
public sealed class ShopPanel
{
    readonly Control _root;
    readonly Label _title;
    readonly Label _credits;
    readonly Button _reroll;
    readonly GridContainer _offers;
    readonly VBoxContainer _build;
    readonly ChoicePanel _picker;
    RunSim _sim = null!;
    Action _onContinue = null!;

    public bool Visible => _root.Visible;

    public ShopPanel(Node parent, ChoicePanel picker)
    {
        _picker = picker;
        var layer = new CanvasLayer { Name = "ShopPanel", Layer = 9 };
        parent.AddChild(layer);
        _root = new Control { Name = "Root", Visible = false };
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        layer.AddChild(_root);
        var dim = new ColorRect { Color = new Color(0, 0, 0, 0.6f), MouseFilter = Control.MouseFilterEnum.Stop };
        dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(dim);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        foreach (var side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            margin.AddThemeConstantOverride(side, 40);
        _root.AddChild(margin);
        var panel = new PanelContainer();
        margin.AddChild(panel);
        var inner = new MarginContainer();
        foreach (var side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            inner.AddThemeConstantOverride(side, 18);
        panel.AddChild(inner);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 12);
        inner.AddChild(column);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 18);
        column.AddChild(header);
        _title = Label(header, 30);
        _title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _credits = Label(header, 24);
        _credits.Modulate = new Color(1f, 0.85f, 0.3f);
        _reroll = Button(header, "Reroll", () =>
        {
            _sim.Reroll();
            Refresh();
        });
        Button(header, "Continue", () => _onContinue());

        var body = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 18);
        column.AddChild(body);
        var offersScroll = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsStretchRatio = 1.6f };
        body.AddChild(offersScroll);
        _offers = new GridContainer { Columns = 2, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _offers.AddThemeConstantOverride("h_separation", 10);
        _offers.AddThemeConstantOverride("v_separation", 10);
        offersScroll.AddChild(_offers);
        var buildScroll = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        body.AddChild(buildScroll);
        _build = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _build.AddThemeConstantOverride("separation", 6);
        buildScroll.AddChild(_build);
    }

    public void Show(RunSim sim, Action onContinue)
    {
        _sim = sim;
        _onContinue = onContinue;
        _root.Visible = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        Refresh();
    }

    public void Hide() => _root.Visible = false;

    /// <summary>Asks where a stash modifier goes; any skill it fits with a free socket, or keep it in the stash.</summary>
    public static void AskSocket(RunSim sim, ChoicePanel picker, int stashIndex, Action done)
    {
        var inv = sim.Inventory;
        int item = inv.Stash[stashIndex];
        var labels = new List<string>();
        var icons = new List<Texture2D?>();
        var slots = new List<int>();
        for (int slot = 0; slot < sim.Loadout.Length; slot++)
        {
            if (!sim.CanSocket(item, slot)) continue;
            slots.Add(slot);
            labels.Add($"{sim.Skills.Form(slot)!.Name}  ({inv.Sockets(slot).Count}/{sim.SocketCount(slot)} sockets used)");
            icons.Add(Icons.Skill(sim.Loadout[slot].Id));
        }
        labels.Add("Keep in stash");
        icons.Add(Icons.Item(inv.Config.Items[item].Id));
        var it = inv.Config.Items[item];
        string body = slots.Count == 0
            ? $"{it.Text}\nNo unlocked skill with a free socket fits it right now (fits {string.Join(", ", it.Modifier!.Fits)})."
            : it.Text;
        picker.Show($"Socket {it.Name}", body, labels, i =>
        {
            if (i < slots.Count) sim.Socket(stashIndex, slots[i]);
            done();
        }, icons);
    }

    void Refresh()
    {
        var inv = _sim.Inventory;
        var shop = _sim.Shop;
        var inv0 = CultureInfo.InvariantCulture;
        _title.Text = $"Shop · Loop {_sim.Stages.Loop}  (fully healed)";
        _credits.Text = string.Create(inv0, $"{MathF.Floor(inv.Credits):0} credits");
        _reroll.Text = string.Create(inv0, $"Reroll ({shop.RerollCost:0})");
        _reroll.Disabled = shop.RerollCost > inv.Credits + 1e-4f;

        Clear(_offers);
        Button? first = null;
        for (int i = 0; i < shop.Offers.Count; i++)
        {
            int index = i;
            var o = shop.Offers[i];
            var it = inv.Config.Items[o.Item];
            bool room = inv.CanTake(o.Item);
            string state = o.Sold ? "SOLD" : !room ? "no free slot" : string.Create(inv0, $"{o.Price:0} cr");
            var b = Button(_offers, $"{state}  ·  {ItemText.Card(it)}", () => Buy(index));
            b.Icon = Icons.Item(it.Id);
            b.AddThemeConstantOverride("icon_max_width", 72);
            b.CustomMinimumSize = new Vector2(0, 96);
            b.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            b.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            b.Disabled = o.Sold || !room || o.Price > inv.Credits + 1e-4f;
            b.Modulate = ItemText.RarityColor(it.Rarity);
            if (!b.Disabled) first ??= b;
        }
        RefreshBuild();
        first?.CallDeferred(Control.MethodName.GrabFocus);
    }

    void Buy(int index)
    {
        var inv = _sim.Inventory;
        int item = _sim.Shop.Offers[index].Item;
        if (!_sim.Buy(index)) return;
        if (inv.Config.Items[item].Type == ItemType.Modifier)
        {
            AskSocket(_sim, _picker, inv.Stash.Count - 1, AfterPick);
            return;
        }
        Refresh();
    }

    void AfterPick()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        Refresh();
    }

    void RefreshBuild()
    {
        var inv = _sim.Inventory;
        var items = inv.Config.Items;
        Clear(_build);
        Label(_build, 22).Text = "Your build";
        Label(_build, 15).Text = "Click a socketed modifier to move it to the stash; click a stash item to socket it.";
        for (int slot = 0; slot < _sim.Loadout.Length; slot++)
        {
            int level = _sim.Progress.SkillLevel(slot);
            var form = _sim.Skills.Form(slot);
            int sockets = _sim.SocketCount(slot);
            var row = new HFlowContainer();
            row.AddThemeConstantOverride("h_separation", 6);
            _build.AddChild(row);
            Label(row, 17).Text = level == 0 ? $"{_sim.Loadout[slot].Name} (locked)" : $"{form?.Name ?? _sim.Loadout[slot].Name} {level}:";
            var socketed = inv.Sockets(slot);
            for (int k = 0; k < socketed.Count; k++)
            {
                int s = slot, i = k;
                var sb2 = Button(row, $"[{items[socketed[k]].Name}]", () =>
                {
                    _sim.Unsocket(s, i);
                    Refresh();
                });
                sb2.Icon = Icons.Item(items[socketed[k]].Id);
                sb2.AddThemeConstantOverride("icon_max_width", 28);
            }
            for (int k = socketed.Count; k < sockets; k++) Label(row, 17).Text = "[ empty ]";
        }

        if (inv.Stash.Count > 0)
        {
            Label(_build, 19).Text = "Stash";
            var row = new HFlowContainer();
            row.AddThemeConstantOverride("h_separation", 6);
            _build.AddChild(row);
            for (int k = 0; k < inv.Stash.Count; k++)
            {
                int i = k;
                var stashButton = Button(row, items[inv.Stash[k]].Name, () => AskSocket(_sim, _picker, i, AfterPick));
                stashButton.Icon = Icons.Item(items[inv.Stash[k]].Id);
                stashButton.AddThemeConstantOverride("icon_max_width", 28);
            }
        }

        var sb = new StringBuilder();
        sb.Append(string.Create(CultureInfo.InvariantCulture, $"Active ({inv.Actives.Count}/{inv.ActiveSlots}): "));
        sb.Append(inv.Actives.Count == 0 ? "–" : string.Join(", ", Names(inv.Actives, items)));
        sb.Append(string.Create(CultureInfo.InvariantCulture, $"\nUsable ({inv.Usables.Count}/{inv.UsableSlots}): "));
        sb.Append(inv.Usables.Count == 0 ? "–" : string.Join(", ", Names(inv.Usables, items)));
        sb.Append("\nPassive: ");
        var passives = new List<string>();
        for (int i = 0; i < items.Length; i++)
            if (inv.PassiveCount(i) > 0) passives.Add(inv.PassiveCount(i) > 1 ? $"{items[i].Name} x{inv.PassiveCount(i)}" : items[i].Name);
        sb.Append(passives.Count == 0 ? "–" : string.Join(", ", passives));
        var summary = Label(_build, 16);
        summary.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        summary.Text = sb.ToString();
    }

    static IEnumerable<string> Names(IReadOnlyList<int> list, ItemDef[] items)
    {
        foreach (int i in list) yield return items[i].Name;
    }

    static void Clear(Node node)
    {
        foreach (var child in node.GetChildren()) child.QueueFree();
    }

    static Label Label(Node parent, int size)
    {
        var l = new Label { LabelSettings = new LabelSettings { FontSize = size, OutlineSize = 3, OutlineColor = Colors.Black } };
        parent.AddChild(l);
        return l;
    }

    static Button Button(Node parent, string text, Action onPress)
    {
        var b = new Button { Text = text, Alignment = HorizontalAlignment.Left };
        b.AddThemeFontSizeOverride("font_size", 16);
        b.Pressed += onPress;
        parent.AddChild(b);
        return b;
    }
}
