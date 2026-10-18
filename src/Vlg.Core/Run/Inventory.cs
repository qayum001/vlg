using System;
using System.Collections.Generic;
using Vlg.Core.Config;

namespace Vlg.Core.Run;

/// <summary>
/// Run items (domain.md Build): credits, passive copies, active and usable slots (one of each item), modifiers in
/// skill sockets or the stash. Socket count per skill: 0 while locked, 1 when unlocked, +1 per branch level reached,
/// at most maxSockets. A modifier fits a skill whose current form shares a tag; one copy per skill.
/// Items are indices into <see cref="ItemsConfig.Items"/>. Socketed modifiers change forms via <see cref="Apply"/>.
/// </summary>
public sealed class Inventory : IFormModifier
{
    readonly ItemsConfig _c;
    readonly int[] _passiveCount;
    readonly List<int> _actives = new();
    readonly List<int> _usables = new();
    readonly List<int>[] _sockets;
    readonly List<int> _stash = new();
    static readonly SkillStats NoScale = SkillStats.Filled(1f);

    public float Credits { get; private set; }
    /// <summary>Bumps on every item or socket change.</summary>
    public int Version { get; private set; }
    public int ActiveSlots { get; }
    public int UsableSlots { get; }
    public PassiveTotals Totals { get; private set; } = PassiveTotals.None;

    public Inventory(ItemsConfig items, int skillCount)
    {
        _c = items;
        _passiveCount = new int[items.Items.Length];
        _sockets = new List<int>[skillCount];
        for (int i = 0; i < skillCount; i++) _sockets[i] = new List<int>();
        ActiveSlots = items.ActiveSlots;
        UsableSlots = items.UsableSlots;
    }

    public ItemsConfig Config => _c;
    public IReadOnlyList<int> Actives => _actives;
    public IReadOnlyList<int> Usables => _usables;
    public IReadOnlyList<int> Stash => _stash;
    public IReadOnlyList<int> Sockets(int slot) => _sockets[slot];
    public int PassiveCount(int item) => _passiveCount[item];
    public bool HasActive(int item) => _actives.Contains(item);
    public bool Owns(int item) => _passiveCount[item] > 0 || _actives.Contains(item) || _usables.Contains(item) || _stash.Contains(item)
        || Array.Exists(_sockets, s => s.Contains(item));

    public void AddCredits(float amount)
    {
        if (amount <= 0f) return;
        Credits += amount;
    }

    public bool Spend(float amount)
    {
        if (amount > Credits + 1e-4f) return false;
        Credits = MathF.Max(0f, Credits - amount);
        return true;
    }

    /// <summary>Active/usable: a free slot and not owned yet. Modifiers and passives always fit (stash, copies).</summary>
    public bool CanTake(int item) => _c.Items[item].Type switch
    {
        ItemType.Active => _actives.Count < ActiveSlots && !_actives.Contains(item),
        ItemType.Usable => _usables.Count < UsableSlots && !_usables.Contains(item),
        _ => true,
    };

    /// <summary>Adds the item; a modifier lands in the stash. Throws if <see cref="CanTake"/> is false.</summary>
    public void Grant(int item)
    {
        if (!CanTake(item)) throw new InvalidOperationException($"no room for item '{_c.Items[item].Id}'");
        switch (_c.Items[item].Type)
        {
            case ItemType.Passive: _passiveCount[item]++; break;
            case ItemType.Active: _actives.Add(item); break;
            case ItemType.Usable: _usables.Add(item); break;
            case ItemType.Modifier: _stash.Add(item); break;
        }
        Changed();
    }

    public int SocketCount(int skillLevel, IReadOnlyList<int> branchLevels)
    {
        if (skillLevel == 0) return 0;
        int n = 1;
        foreach (int b in branchLevels)
            if (skillLevel >= b) n++;
        return Math.Min(_c.MaxSockets, n);
    }

    /// <param name="form">The skill's current form (null = locked).</param>
    public bool CanSocket(int item, int slot, SkillForm? form, int socketCount)
    {
        var m = _c.Items[item].Modifier;
        return m != null && form != null && _sockets[slot].Count < socketCount && !_sockets[slot].Contains(item) && m.FitsAny(form.Tags);
    }

    public void Socket(int stashIndex, int slot, SkillForm? form, int socketCount)
    {
        int item = _stash[stashIndex];
        if (!CanSocket(item, slot, form, socketCount)) throw new InvalidOperationException($"'{_c.Items[item].Id}' doesn't fit slot {slot}");
        _stash.RemoveAt(stashIndex);
        _sockets[slot].Add(item);
        Changed();
    }

    public void Unsocket(int slot, int index)
    {
        _stash.Add(_sockets[slot][index]);
        _sockets[slot].RemoveAt(index);
        Changed();
    }

    /// <summary>Moves modifiers that no longer fit (tags changed by a branch, fewer sockets) to the stash. Returns the count moved.</summary>
    public int Revalidate(int slot, SkillForm? form, int socketCount)
    {
        var list = _sockets[slot];
        int moved = 0;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var m = _c.Items[list[i]].Modifier!;
            if (form != null && m.FitsAny(form.Tags) && i < socketCount) continue;
            _stash.Add(list[i]);
            list.RemoveAt(i);
            moved++;
        }
        if (moved > 0) Changed();
        return moved;
    }

    /// <summary>Socketed modifiers that would not fit a form with <paramref name="tags"/> (branch warning).</summary>
    public IEnumerable<int> WouldUnfit(int slot, SkillTag[] tags)
    {
        foreach (int item in _sockets[slot])
            if (!_c.Items[item].Modifier!.FitsAny(tags)) yield return item;
    }

    public SkillForm Apply(int slot, SkillForm form)
    {
        var list = _sockets[slot];
        if (list.Count == 0) return form;
        var stats = form.Stats;
        bool chain = Array.IndexOf(form.Tags, SkillTag.Chain) >= 0;
        foreach (int item in list)
        {
            var m = _c.Items[item].Modifier!;
            stats = stats.Apply(m.Scale, m.Add);
            if (chain) stats = stats.Apply(NoScale, m.ChainAdd);
        }
        return new SkillForm { Kind = form.Kind, Tags = form.Tags, Flags = form.Flags, Stats = stats, Name = form.Name };
    }

    /// <summary>Hex numbers of the modifiers in <paramref name="slot"/> (strongest wins).</summary>
    public (float Vuln, float Sec) Hex(int slot)
    {
        float vuln = 0f, sec = 0f;
        foreach (int item in _sockets[slot])
        {
            var m = _c.Items[item].Modifier!;
            vuln = MathF.Max(vuln, m.HexVuln);
            sec = MathF.Max(sec, m.HexSec);
        }
        return (vuln, sec);
    }

    /// <summary>The poison modifier socketed in <paramref name="slot"/>, or null.</summary>
    public ModifierDef? Poison(int slot)
    {
        foreach (int item in _sockets[slot])
            if (_c.Items[item].Modifier!.PoisonDamage > 0f) return _c.Items[item].Modifier;
        return null;
    }

    /// <summary>Extra damage fraction from ledger passives at <paramref name="kills"/>.</summary>
    public float LedgerBonus(int kills)
    {
        float bonus = 0f;
        for (int i = 0; i < _passiveCount.Length; i++)
        {
            var p = _c.Items[i].Passive;
            if (_passiveCount[i] == 0 || p == null || p.LedgerKills == 0) continue;
            bonus += _passiveCount[i] * (kills / p.LedgerKills) * p.LedgerDamage;
        }
        return bonus;
    }

    void Changed()
    {
        Version++;
        var t = PassiveTotals.None;
        for (int i = 0; i < _passiveCount.Length; i++)
        {
            int n = _passiveCount[i];
            var p = _c.Items[i].Passive;
            if (n == 0 || p == null) continue;
            t = t with
            {
                Damage = t.Damage + n * p.Damage,
                MoveSpeed = t.MoveSpeed + n * p.MoveSpeed,
                Regen = t.Regen + n * p.Regen,
                Pickup = t.Pickup + n * p.Pickup,
                LifeSteal = t.LifeSteal + n * p.LifeSteal,
                DamageTaken = t.DamageTaken * MathF.Pow(1f - p.Armor, n),
                Credits = t.Credits + n * p.Credits,
                CooldownMult = t.CooldownMult * MathF.Pow(1f - p.Cooldown, n),
            };
        }
        Totals = t;
    }
}
