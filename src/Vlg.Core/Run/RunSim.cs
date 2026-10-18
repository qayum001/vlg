using System;
using System.Collections.Generic;
using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Jobs;
using Vlg.Core.Sim;

namespace Vlg.Core.Run;

/// <summary>
/// One level run: hero, stage loop, walkers, skills, XP gems, level-ups. Godot-free.
/// Step only clears last step's events (Hits, Deaths, HeroHurt, Beams, Flashes) unless <see cref="Phase"/> is Playing;
/// the adapter answers LevelUp/Chest/Shop/Dead.
/// Ground plane: Vector2(x, y) = world (x, z).
/// </summary>
public sealed class RunSim
{
    /// <summary>Damage source ids (EnemySwarm.LastHit): hero skill slot i = HeroSkillSource + i.</summary>
    public const byte HeroSkillSource = 1;
    public const byte DropShockSource = 20;
    public const byte UltimateSource = 21;
    /// <summary>After a shield block, contact damage is ignored this long.</summary>
    public const float BlockSec = 0.5f;

    public readonly RunConfig Config;
    public readonly HeroDef Hero;
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
    public readonly SpatialGrid Grid;
    public readonly PointBuffer Hits;
    public readonly PointBuffer Deaths;
    /// <summary>Kill credit per death this step (same index as Deaths).</summary>
    public readonly byte[] DeathSources;
    public readonly HeroProgress Progress;
    public readonly StageLoop Stages;
    public readonly SkillSystem Skills;
    public readonly UltimateSystem Ultimate;

    public Vector2 HeroPosition { get; private set; }
    public float HeroHp { get; private set; }
    /// <summary>Hero took contact damage in the last step.</summary>
    public bool HeroHurt { get; private set; }
    public int Kills { get; private set; }
    /// <summary>Seconds of play (paused phases don't count).</summary>
    public float Time { get; private set; }

    readonly SwarmTuning _swarm;
    readonly SkillWorld _world;
    readonly WalkerSpawner _spawner = new();
    readonly WorkerPool? _workers;
    readonly int[] _contacts;
    float _blockTimer;
    bool _pressQueued;
    XorShift32 _rng;
    ViewFootprint _view;

    /// <summary>Until the adapter calls <see cref="SetView"/> (tests): yaw 0, 16:9.</summary>
    public const float DefaultAspect = 16f / 9f;

    /// <param name="workers">Shared pool for the enemy update; null = single thread. Same result either way.</param>
    /// <param name="loadout">Skill ids; null = the hero's default loadout.</param>
    /// <param name="ultimate">Ultimate id; null = the hero's default.</param>
    public RunSim(RunConfig config, HeroDef hero, uint seed, WorkerPool? workers = null, IReadOnlyList<string>? loadout = null,
        string? ultimate = null)
    {
        Config = config;
        Hero = hero;
        Loadout = hero.LoadoutSkills(loadout);
        _workers = workers;
        _rng = new XorShift32(seed);
        _swarm = config.Walker.ToSwarm(config.Hero.Radius);
        var s = config.Sim;
        Enemies = new EnemySwarm(s.EnemyCapacity);
        Projectiles = new ProjectilePool(s.ProjectileCapacity, s.ProjectileRadius, s.EnemyCapacity);
        Gems = new GemPool(s.GemCapacity);
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
        Skills = new SkillSystem(Loadout, _world, Beat, HeroSkillSource, seed ^ 0x5EEDu);
        Ultimate = new UltimateSystem(hero.UltimateById(ultimate), _world, Beat, UltimateSource);
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

    /// <summary>Dev shortcut: full ultimate charge (if unlocked).</summary>
    public void DevFillUltimate() => Ultimate.DevFill();

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
        HeroHurt = false;
        if (Phase != RunPhase.Playing) return;

        dt = MathF.Min(dt, Config.Sim.MaxStepSec);
        Time += dt;
        float len2 = move.LengthSquared();
        if (len2 > 1f) move /= MathF.Sqrt(len2);
        HeroPosition += move * Hero.Speed * dt;
        if (len2 > 1e-4f) _world.Facing = Vector2.Normalize(move);

        if (Stages.Update(dt)) OnStageEntered();
        if (!Stages.Current.IsTimed) return;
        Beat.Advance(dt);

        var hero = HeroPosition;
        _spawner.Update(dt, Stages.Current, Stages.Loop, hero, _view, Enemies, ref _rng, Config);
        Grid.Build(Enemies.Position, Enemies.Count, hero);
        Enemies.Update(dt, hero, Grid, _swarm, _workers);

        float enemyMaxSpeed = Config.Walker.Speed * EnemySwarm.MaxSpeedFactor + EnemySwarm.MaxPushSpeed;
        float pad = enemyMaxSpeed * dt;
        _world.Hero = hero;
        _world.QueryPad = pad;
        if (Beat.DropStarted)
        {
            var b = Hero.Beat;
            Rings.Spawn(new RingShot
            {
                Center = hero,
                MaxRadius = b.ShockRadius,
                Speed = b.ShockSpeed,
                Damage = b.ShockDamage * Progress.DamageMultiplier,
                Knockback = b.ShockKnockback,
                Source = DropShockSource,
            });
        }
        float er = Config.Walker.Radius;
        float dmgMul = Progress.DamageMultiplier;
        if (_pressQueued)
        {
            _pressQueued = false;
            Ultimate.Press(dmgMul);
        }
        Skills.RateMultiplier = Ultimate.Phase == UltimatePhase.Encore ? 1f + Ultimate.Stats.Speed : 1f;
        Skills.Update(dt, Progress, dmgMul);
        Ultimate.Update(dt, Progress.UltimateLevel, dmgMul);
        Strikes.Update(dt, Enemies, Grid, Hits, er, pad, Flashes);
        Zones.Update(dt, Beat.BeatTick, Enemies, Grid, Hits, er, pad, Beams);
        Rings.Update(dt, Enemies, Grid, Hits, er, pad);
        Projectiles.Update(dt, Enemies, Grid, Hits, er, enemyMaxSpeed, hero, Flashes);
        ApplyContactDamage(dt, pad);

        int killed = Enemies.RemoveDead(Deaths, DeathSources);
        Kills += killed;
        HeroHp = MathF.Min(HeroMaxHp, HeroHp + Ultimate.HealPerKill * killed);
        Ultimate.OnKills(killed);
        Skills.OnDeaths(Deaths);
        for (int i = 0; i < Deaths.Count; i++) Gems.Spawn(Deaths.Items[i], Config.Walker.Xp);

        var p = Config.Pickups;
        Progress.AddXp(Gems.Update(dt, hero, Hero.PickupRadius, p.MagnetSpeed, p.CollectRadius, p.DespawnRadius));
    }

    public void ChooseLevelUp(int index)
    {
        RequirePhase(RunPhase.LevelUp);
        Progress.Choose(index);
    }

    /// <summary>Leaves the Chest or Shop stage.</summary>
    public void ContinueStage()
    {
        if (Phase is not (RunPhase.Chest or RunPhase.Shop))
            throw new InvalidOperationException($"phase {Phase}: no stage to continue");
        Stages.Continue();
        OnStageEntered();
    }

    /// <summary>Dev shortcut for manual tests.</summary>
    public void DevSkipStage()
    {
        if (Phase == RunPhase.Dead) return;
        Stages.Skip();
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
        // domain.md Healing: full heal at the shop stage.
        if (Stages.Current.Kind == StageKind.Shop) HeroHp = HeroMaxHp;
    }

    /// <summary>
    /// Each living walker touching the hero deals contactDps (scaled per loop), unless a shield blocks:
    /// the block covers <see cref="BlockSec"/> of contact.
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
        HeroHp -= touching * Config.Walker.ContactDps * Config.LoopScaling.Damage(Stages.Loop) * dt;
        HeroHurt = true;
    }

    void RequirePhase(RunPhase phase)
    {
        if (Phase != phase) throw new InvalidOperationException($"phase {Phase}, expected {phase}");
    }
}
