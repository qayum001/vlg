using System;
using System.Collections.Generic;
using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Jobs;
using Vlg.Core.Sim;

namespace Vlg.Core.Run;

/// <summary>
/// One level run: hero, stage loop, walkers, skills, ultimate, items, XP gems, credits, level-ups. Godot-free.
/// Step only clears last step's events (Hits, Deaths, HeroHurt, Beams, Flashes) unless <see cref="Phase"/> is Playing;
/// the adapter answers LevelUp/Chest/Shop/Dead. Ultimate and usable presses are queued and applied inside the next
/// Step after the grid build.
/// Ground plane: Vector2(x, y) = world (x, z).
/// </summary>
public sealed class RunSim
{
    /// <summary>Damage source ids (EnemySwarm.LastHit): hero skill slot i = HeroSkillSource + i.</summary>
    public const byte HeroSkillSource = 1;
    public const byte DropShockSource = 20;
    public const byte UltimateSource = 21;
    public const byte UsableSource = 22;
    /// <summary>Active item slot i = ActiveSource + i.</summary>
    public const byte ActiveSource = 30;
    /// <summary>Poison puddles from Plague Flask kills.</summary>
    public const byte PoisonSource = 40;
    /// <summary>After a shield block, contact damage is ignored this long.</summary>
    public const float BlockSec = 0.5f;

    public readonly RunConfig Config;
    public readonly HeroDef Hero;
    public readonly ItemsConfig Items;
    /// <summary>Loadout skills in slot order.</summary>
    public readonly SkillDef[] Loadout;
    public readonly BeatClock Beat;
    public readonly RingPool Rings;
    public readonly SummonPool Summons;
    public readonly ZonePool Zones;
    public readonly StrikePool Strikes;
    public readonly BeamEvents Beams;
    public readonly FlashEvents Flashes;
    public readonly EnemySwarm Enemies;
    public readonly ProjectilePool Projectiles;
    public readonly GemPool Gems;
    /// <summary>Credit pickups; same magnet rules as XP gems.</summary>
    public readonly GemPool Coins;
    public readonly SpatialGrid Grid;
    public readonly PointBuffer Hits;
    public readonly PointBuffer Deaths;
    /// <summary>Kill credit per death this step (same index as Deaths).</summary>
    public readonly byte[] DeathSources;
    public readonly HeroProgress Progress;
    public readonly StageLoop Stages;
    public readonly SkillSystem Skills;
    public readonly UltimateSystem Ultimate;
    public readonly Inventory Inventory;
    public readonly Shop Shop;
    public readonly UsableSystem Usables;
    /// <summary>Owned active items, one slot per active item of the pool.</summary>
    public readonly SkillSystem Actives;
    public readonly OwnedActives OwnedActives;

    public Vector2 HeroPosition { get; private set; }
    public float HeroHp { get; private set; }
    /// <summary>Hero took contact damage in the last step.</summary>
    public bool HeroHurt { get; private set; }
    public int Kills { get; private set; }
    /// <summary>Seconds of play (paused phases don't count).</summary>
    public float Time { get; private set; }
    /// <summary>Rare items offered by the current chest (empty outside the Chest phase).</summary>
    public IReadOnlyList<int> ChestOffer => _chest;

    readonly SwarmTuning _swarm;
    readonly SkillWorld _world;
    readonly WalkerSpawner _spawner = new();
    readonly WorkerPool? _workers;
    readonly int[] _contacts;
    readonly bool[] _useQueued = new bool[ItemsConfig.MaxSlots];
    List<int> _chest = new();
    int _hexVersion = -1;
    float _blockTimer;
    bool _pressQueued;
    XorShift32 _rng;
    ViewFootprint _view;

    /// <summary>Until the adapter calls <see cref="SetView"/> (tests): yaw 0, 16:9.</summary>
    public const float DefaultAspect = 16f / 9f;

    /// <param name="workers">Shared pool for the enemy update; null = single thread. Same result either way.</param>
    /// <param name="loadout">Skill ids; null = the hero's default loadout.</param>
    /// <param name="ultimate">Ultimate id; null = the hero's default.</param>
    public RunSim(RunConfig config, HeroDef hero, ItemsConfig items, uint seed, WorkerPool? workers = null,
        IReadOnlyList<string>? loadout = null, string? ultimate = null)
    {
        Config = config;
        Hero = hero;
        Items = items;
        Loadout = hero.LoadoutSkills(loadout);
        _workers = workers;
        _rng = new XorShift32(seed);
        _swarm = config.Walker.ToSwarm(config.Hero.Radius);
        var s = config.Sim;
        Enemies = new EnemySwarm(s.EnemyCapacity);
        Projectiles = new ProjectilePool(s.ProjectileCapacity, s.ProjectileRadius, s.EnemyCapacity);
        Gems = new GemPool(s.GemCapacity);
        Coins = new GemPool(s.GemCapacity);
        Grid = new SpatialGrid(s.GridDim, s.GridCellSize, s.EnemyCapacity);
        Hits = new PointBuffer(s.EnemyCapacity);
        // Every death drops a gem: never drop death events.
        Deaths = new PointBuffer(s.EnemyCapacity);
        DeathSources = new byte[s.EnemyCapacity];
        Rings = new RingPool(s.RingCapacity, s.EnemyCapacity);
        Summons = new SummonPool(RunConfig.MaxSkills * RunConfig.MaxSummonsPerSkill);
        Zones = new ZonePool(s.ZoneCapacity, s.EnemyCapacity);
        Strikes = new StrikePool(RunConfig.MaxSkills * RunConfig.MaxSummonsPerSkill * 2, s.EnemyCapacity);
        Beams = new BeamEvents(256);
        Flashes = new FlashEvents(128);
        Beat = new BeatClock(hero.Beat);
        Progress = new HeroProgress(config.Leveling, Loadout.Length, Loadout, hasUltimate: true);
        Stages = new StageLoop(config.Stages);
        _world = new SkillWorld(Enemies, Grid, Projectiles, Rings, Summons, Zones, Strikes, Beams, Flashes, Hits,
            config.Hero.Radius, config.Walker.Radius);
        Inventory = new Inventory(items, Loadout.Length);
        Shop = new Shop(items, seed ^ 0x5409u);
        Skills = new SkillSystem(Loadout, _world, Beat, HeroSkillSource, seed ^ 0x5EEDu) { Modifier = Inventory };
        Ultimate = new UltimateSystem(hero.UltimateById(ultimate), _world, Beat, UltimateSource);
        OwnedActives = new OwnedActives(Inventory);
        Actives = new SkillSystem(OwnedActives.Defs, _world, null, ActiveSource, seed ^ 0xAC71u);
        Usables = new UsableSystem(Inventory, _world, UsableSource);
        _contacts = new int[s.EnemyCapacity];
        HeroHp = hero.MaxHp;
        SetView(0f, DefaultAspect);
        OnStageEntered();
    }

    public float HeroMaxHp => Hero.MaxHp;

    /// <summary>Unit camera forward on the ground: aimed skills fire along it.</summary>
    public Vector2 Aim => _world.Aim;

    /// <summary>Ground area the camera sees, relative to the hero. Spawns and recycles land outside it.</summary>
    public ViewFootprint View => _view;

    /// <summary>Level-up bonus x passive damage x ledger.</summary>
    public float DamageMultiplier => Progress.DamageMultiplier * (1f + Inventory.Totals.Damage + Inventory.LedgerBonus(Kills));

    public float HeroSpeed => Hero.Speed * (1f + Inventory.Totals.MoveSpeed) * Usables.MoveMultiplier;

    /// <summary>Camera yaw (CameraMath convention) and viewport width / height; call when either changes.</summary>
    public void SetView(float yaw, float aspect)
    {
        var c = Config.Camera;
        _view = ViewFootprint.Compute(c.TiltDeg, c.Distance, c.FovDeg, aspect, yaw);
        _world.Aim = CameraMath.Forward(yaw);
    }

    public RunPhase Phase =>
        HeroHp <= 0f ? RunPhase.Dead
        : Progress.HasPendingPick ? RunPhase.LevelUp
        : Stages.Current.Kind == StageKind.Chest ? RunPhase.Chest
        : Stages.Current.Kind == StageKind.Shop ? RunPhase.Shop
        : RunPhase.Playing;

    /// <summary>
    /// Queues the ultimate for the next Step (it needs a fresh grid). False = not playing or not ready.
    /// </summary>
    public bool PressUltimate()
    {
        if (Phase != RunPhase.Playing || !Ultimate.Ready || _pressQueued) return false;
        _pressQueued = true;
        return true;
    }

    /// <summary>Queues the usable in <paramref name="slot"/> for the next Step. False = not playing, empty or cooling down.</summary>
    public bool UseItem(int slot)
    {
        if (Phase != RunPhase.Playing || (uint)slot >= (uint)Inventory.Usables.Count) return false;
        if (Usables.CooldownLeft(Inventory.Usables[slot]) > 0f) return false;
        _useQueued[slot] = true;
        return true;
    }

    /// <summary>Dev shortcut: full ultimate charge (if unlocked).</summary>
    public void DevFillUltimate() => Ultimate.DevFill();

    /// <summary>Dev shortcut: credits.</summary>
    public void DevAddCredits(float amount) => Inventory.AddCredits(amount);

    /// <summary>Dev shortcut: grant an item by id (ignores price).</summary>
    public void DevGrant(string id)
    {
        Inventory.Grant(Items.IndexOf(id));
        SyncItems();
    }

    /// <summary>Valid only in the LevelUp phase. Reused list: read before the next call.</summary>
    public IReadOnlyList<LevelUpOption> LevelUpOptions() => Progress.Options();

    /// <param name="move">World-space ground direction, length &lt;= 1 (longer is normalized).</param>
    public void Step(float dt, Vector2 move)
    {
        Hits.Clear();
        Deaths.Clear();
        Beams.Clear();
        Flashes.Clear();
        Ultimate.BeginStep();
        Usables.BeginStep();
        HeroHurt = false;
        if (Phase != RunPhase.Playing) return;

        dt = MathF.Min(dt, Config.Sim.MaxStepSec);
        Time += dt;
        float len2 = move.LengthSquared();
        if (len2 > 1f) move /= MathF.Sqrt(len2);
        HeroPosition += move * HeroSpeed * dt;
        if (len2 > 1e-4f) _world.Facing = Vector2.Normalize(move);

        if (Stages.Update(dt)) OnStageEntered();
        if (!Stages.Current.IsTimed) return;
        Beat.Advance(dt);
        SyncItems();

        var hero = HeroPosition;
        _spawner.Update(dt, Stages.Current, Stages.Loop, hero, _view, Enemies, ref _rng, Config);
        Grid.Build(Enemies.Position, Enemies.Count, hero);
        Enemies.Update(dt, Usables.SmokeActive ? Usables.SmokePoint : hero, Grid, _swarm, _workers);
        Enemies.ResetDamageDealt();

        float enemyMaxSpeed = Config.Walker.Speed * EnemySwarm.MaxSpeedFactor + EnemySwarm.MaxPushSpeed;
        float pad = enemyMaxSpeed * dt;
        _world.Hero = hero;
        _world.QueryPad = pad;
        float dmgMul = DamageMultiplier;
        if (Beat.DropStarted)
        {
            var b = Hero.Beat;
            Rings.Spawn(new RingShot
            {
                Center = hero,
                MaxRadius = b.ShockRadius,
                Speed = b.ShockSpeed,
                Damage = b.ShockDamage * dmgMul,
                Knockback = b.ShockKnockback,
                Source = DropShockSource,
            });
        }
        float er = Config.Walker.Radius;
        if (_pressQueued)
        {
            _pressQueued = false;
            Ultimate.Press(dmgMul);
        }
        for (int i = 0; i < _useQueued.Length; i++)
        {
            if (!_useQueued[i]) continue;
            _useQueued[i] = false;
            Usables.Use(i, dmgMul);
        }
        float cooldownRate = Usables.RateMultiplier / Inventory.Totals.CooldownMult;
        Skills.RateMultiplier = cooldownRate * (Ultimate.Phase == UltimatePhase.Encore ? 1f + Ultimate.Stats.Speed : 1f);
        Actives.RateMultiplier = cooldownRate;
        Skills.Update(dt, Progress, dmgMul);
        Actives.Update(dt, OwnedActives, dmgMul);
        Ultimate.Update(dt, Progress.UltimateLevel, dmgMul);
        Usables.Update(dt, dmgMul);
        Strikes.Update(dt, Enemies, Grid, Hits, er, pad, Flashes);
        Zones.Update(dt, Beat.BeatTick, Enemies, Grid, Hits, er, pad, Beams);
        Rings.Update(dt, Enemies, Grid, Hits, er, pad);
        Projectiles.Update(dt, Enemies, Grid, Hits, er, enemyMaxSpeed, hero, Flashes);
        ApplyContactDamage(dt, pad);

        var t = Inventory.Totals;
        HeroHp = MathF.Min(HeroMaxHp, HeroHp + t.Regen * dt + t.LifeSteal * Enemies.DamageDealt);
        int killed = Enemies.RemoveDead(Deaths, DeathSources);
        Kills += killed;
        HeroHp = MathF.Min(HeroMaxHp, HeroHp + Ultimate.HealPerKill * killed);
        Ultimate.OnKills(killed);
        Skills.OnDeaths(Deaths);
        OnDeathsItems();

        var p = Config.Pickups;
        float pickup = Hero.PickupRadius * (1f + t.Pickup);
        Progress.AddXp(Gems.Update(dt, hero, pickup, p.MagnetSpeed, p.CollectRadius, p.DespawnRadius));
        Inventory.AddCredits(Coins.Update(dt, hero, pickup, p.MagnetSpeed, p.CollectRadius, p.DespawnRadius) * (1f + t.Credits));
    }

    public void ChooseLevelUp(int index)
    {
        RequirePhase(RunPhase.LevelUp);
        Progress.Choose(index);
        Skills.Refresh(Progress);
        for (int slot = 0; slot < Loadout.Length; slot++) Inventory.Revalidate(slot, Skills.Form(slot), SocketCount(slot));
    }

    /// <summary>Leaves the Shop stage, or the Chest stage once nothing is left to take.</summary>
    public void ContinueStage()
    {
        if (Phase is not (RunPhase.Chest or RunPhase.Shop))
            throw new InvalidOperationException($"phase {Phase}: no stage to continue");
        if (Phase == RunPhase.Chest && _chest.Count > 0)
            throw new InvalidOperationException("chest: take an item first (domain.md: no skip)");
        Stages.Continue();
        OnStageEntered();
    }

    // ---- Shop and chest ----

    public bool Buy(int offer)
    {
        RequirePhase(RunPhase.Shop);
        bool ok = Shop.Buy(offer, Inventory);
        if (ok) SyncItems();
        return ok;
    }

    public bool Reroll()
    {
        RequirePhase(RunPhase.Shop);
        return Shop.Reroll(Inventory);
    }

    /// <summary>Takes chest option <paramref name="index"/> and continues to the next stage.</summary>
    public void TakeChest(int index)
    {
        RequirePhase(RunPhase.Chest);
        Inventory.Grant(_chest[index]);
        _chest.Clear();
        SyncItems();
        Stages.Continue();
        OnStageEntered();
    }

    public int SocketCount(int slot) => Inventory.SocketCount(Progress.SkillLevel(slot), Progress.BranchLevels);

    public bool CanSocket(int item, int slot)
    {
        Skills.Refresh(Progress);
        return Inventory.CanSocket(item, slot, Skills.Form(slot), SocketCount(slot));
    }

    /// <summary>Stash item → skill socket (any phase but Dead; domain.md: re-socket at the shop).</summary>
    public void Socket(int stashIndex, int slot)
    {
        Skills.Refresh(Progress);
        Inventory.Socket(stashIndex, slot, Skills.Form(slot), SocketCount(slot));
        SyncItems();
    }

    public void Unsocket(int slot, int index)
    {
        Inventory.Unsocket(slot, index);
        SyncItems();
    }

    /// <summary>Dev shortcut for manual tests.</summary>
    public void DevSkipStage()
    {
        if (Phase == RunPhase.Dead) return;
        _chest.Clear();
        Stages.Skip();
        OnStageEntered();
    }

    /// <summary>Dev shortcut: jump to stage <paramref name="index"/> of the current loop (e.g. straight into the shop).</summary>
    public void DevJumpToStage(int index)
    {
        if ((uint)index >= (uint)Config.Stages.Length) throw new ArgumentOutOfRangeException(nameof(index), $"0..{Config.Stages.Length - 1}");
        while (Stages.Index != index)
        {
            _chest.Clear();
            Stages.Skip();
        }
        OnStageEntered();
    }

    /// <summary>Dev shortcut for manual tests.</summary>
    public void DevGainLevel()
    {
        if (Phase == RunPhase.Dead) return;
        Progress.DevGainLevel();
    }

    void OnStageEntered()
    {
        var kind = Stages.Current.Kind;
        // domain.md Healing: full heal at the shop stage.
        if (kind == StageKind.Shop)
        {
            HeroHp = HeroMaxHp;
            Shop.Open(Stages.Loop, Inventory);
        }
        _chest = kind == StageKind.Chest ? Shop.ChestOptions(Inventory) : new List<int>();
    }

    /// <summary>Hex modifiers → per-source on-hit vuln, re-applied when the inventory changes.</summary>
    void SyncItems()
    {
        if (_hexVersion == Inventory.Version) return;
        _hexVersion = Inventory.Version;
        for (int slot = 0; slot < Loadout.Length; slot++)
        {
            var (vuln, sec) = Inventory.Hex(slot);
            Enemies.SetHitVuln((byte)(HeroSkillSource + slot), vuln, sec);
        }
    }

    /// <summary>Credit coins (chance per kill) and Plague Flask puddles for kills by skills holding one.</summary>
    void OnDeathsItems()
    {
        for (int i = 0; i < Deaths.Count; i++)
        {
            var at = Deaths.Items[i];
            Gems.Spawn(at, Config.Walker.Xp);
            if (_rng.NextFloat() < Items.CreditDropChance) Coins.Spawn(at, Items.CreditValue);
            int slot = DeathSources[i] - HeroSkillSource;
            if ((uint)slot >= (uint)Loadout.Length) continue;
            var poison = Inventory.Poison(slot);
            if (poison == null) continue;
            Zones.Spawn(new ZoneShot
            {
                Center = at,
                Radius = poison.PoisonRadius,
                LifeSec = poison.PoisonSec,
                Damage = poison.PoisonDamage * DamageMultiplier,
                Interval = poison.PoisonInterval,
                Source = PoisonSource,
                Slot = -1,
            });
        }
    }

    /// <summary>
    /// Each living walker touching the hero deals contactDps (scaled per loop, x passive armor), unless a shield
    /// blocks: the block covers <see cref="BlockSec"/> of contact.
    /// </summary>
    void ApplyContactDamage(float dt, float pad)
    {
        _blockTimer = MathF.Max(0f, _blockTimer - dt);
        float reach = Config.Hero.Radius + Config.Walker.Radius + Config.Hero.ContactMargin;
        float reach2 = reach * reach;
        int n = Grid.Query(HeroPosition, reach + pad, _contacts, Enemies.Position);
        int touching = 0;
        for (int k = 0; k < n; k++)
        {
            int j = _contacts[k];
            if (Enemies.Health[j] > 0f && Vector2.DistanceSquared(Enemies.Position[j], HeroPosition) <= reach2) touching++;
        }
        if (touching == 0 || _blockTimer > 0f) return;
        if (Skills.TryBlock())
        {
            _blockTimer = BlockSec;
            return;
        }
        HeroHp -= touching * Config.Walker.ContactDps * Config.LoopScaling.Damage(Stages.Loop) * Inventory.Totals.DamageTaken * dt;
        HeroHurt = true;
    }

    void RequirePhase(RunPhase phase)
    {
        if (Phase != phase) throw new InvalidOperationException($"phase {Phase}, expected {phase}");
    }
}
