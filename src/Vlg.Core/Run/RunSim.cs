using System;
using System.Collections.Generic;
using System.Numerics;
using Vlg.Core.Config;
using Vlg.Core.Jobs;
using Vlg.Core.Sim;

namespace Vlg.Core.Run;

/// <summary>
/// One level run: hero, stage loop, walkers, skills, XP gems, level-ups. Godot-free.
/// Step only clears last step's events (Hits, Deaths, HeroHurt) unless <see cref="Phase"/> is Playing;
/// the adapter answers LevelUp/Chest/Shop/Dead.
/// Ground plane: Vector2(x, y) = world (x, z).
/// </summary>
public sealed class RunSim
{
    public readonly RunConfig Config;
    public readonly EnemySwarm Enemies;
    public readonly ProjectilePool Projectiles;
    public readonly GemPool Gems;
    public readonly SpatialGrid Grid;
    public readonly PointBuffer Hits;
    public readonly PointBuffer Deaths;
    public readonly HeroProgress Progress;
    public readonly StageLoop Stages;
    public readonly SkillSystem Skills;

    public Vector2 HeroPosition { get; private set; }
    public float HeroHp { get; private set; }
    /// <summary>Hero took contact damage in the last step.</summary>
    public bool HeroHurt { get; private set; }
    public int Kills { get; private set; }
    /// <summary>Seconds of play (paused phases don't count).</summary>
    public float Time { get; private set; }

    readonly SwarmTuning _swarm;
    readonly WalkerSpawner _spawner = new();
    readonly WorkerPool? _workers;
    readonly int[] _contacts;
    XorShift32 _rng;

    /// <param name="workers">Shared pool for the enemy update; null = single thread. Same result either way.</param>
    public RunSim(RunConfig config, uint seed, WorkerPool? workers = null)
    {
        Config = config;
        _workers = workers;
        _rng = new XorShift32(seed);
        _swarm = config.Walker.ToSwarm(config.Hero.Radius);
        var s = config.Sim;
        Enemies = new EnemySwarm(s.EnemyCapacity);
        Projectiles = new ProjectilePool(s.ProjectileCapacity);
        Gems = new GemPool(s.GemCapacity);
        Grid = new SpatialGrid(s.GridDim, s.GridCellSize, s.EnemyCapacity);
        Hits = new PointBuffer(s.EnemyCapacity);
        // Every death drops a gem: never drop death events.
        Deaths = new PointBuffer(s.EnemyCapacity);
        Progress = new HeroProgress(config.Leveling, config.Skills.Length);
        Stages = new StageLoop(config.Stages);
        Skills = new SkillSystem(config.Skills, s.EnemyCapacity);
        _contacts = new int[s.EnemyCapacity];
        HeroHp = config.Hero.MaxHp;
        OnStageEntered();
    }

    public float HeroMaxHp => Config.Hero.MaxHp;

    public RunPhase Phase =>
        HeroHp <= 0f ? RunPhase.Dead
        : Progress.PendingLevelUps > 0 ? RunPhase.LevelUp
        : Stages.Current.Kind == StageKind.Chest ? RunPhase.Chest
        : Stages.Current.Kind == StageKind.Shop ? RunPhase.Shop
        : RunPhase.Playing;

    /// <summary>Valid only in the LevelUp phase. Reused list: read before the next call.</summary>
    public IReadOnlyList<LevelUpOption> LevelUpOptions() => Progress.Options();

    /// <param name="move">World-space ground direction, length &lt;= 1 (longer is normalized).</param>
    public void Step(float dt, Vector2 move)
    {
        Hits.Clear();
        Deaths.Clear();
        HeroHurt = false;
        if (Phase != RunPhase.Playing) return;

        dt = MathF.Min(dt, Config.Sim.MaxStepSec);
        Time += dt;
        float len2 = move.LengthSquared();
        if (len2 > 1f) move /= MathF.Sqrt(len2);
        HeroPosition += move * Config.Hero.Speed * dt;

        if (Stages.Update(dt)) OnStageEntered();
        if (!Stages.Current.IsTimed) return;

        var hero = HeroPosition;
        _spawner.Update(dt, Stages.Current, Stages.Loop, hero, Enemies, ref _rng, Config);
        Grid.Build(Enemies.Position, Enemies.Count, hero);
        Enemies.Update(dt, hero, Grid, _swarm, _workers);

        float enemyMaxSpeed = Config.Walker.Speed * EnemySwarm.MaxSpeedFactor;
        float pad = enemyMaxSpeed * dt;
        Skills.Update(dt, hero, Config.Hero.Radius, Progress, Enemies, Grid, Projectiles, Hits, Config.Walker.Radius, pad, ref _rng);
        Projectiles.Update(dt, Enemies, Grid, Hits, Config.Sim.ProjectileRadius + Config.Walker.Radius, enemyMaxSpeed);
        ApplyContactDamage(dt, pad);

        Kills += Enemies.RemoveDead(Deaths);
        for (int i = 0; i < Deaths.Count; i++) Gems.Spawn(Deaths.Items[i], Config.Walker.Xp);

        var p = Config.Pickups;
        Progress.AddXp(Gems.Update(dt, hero, Config.Hero.PickupRadius, p.MagnetSpeed, p.CollectRadius, p.DespawnRadius));
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

    /// <summary>Each living walker touching the hero deals contactDps (scaled per loop).</summary>
    void ApplyContactDamage(float dt, float pad)
    {
        float reach = Config.Hero.Radius + Config.Walker.Radius + Config.Hero.ContactMargin;
        float reach2 = reach * reach;
        int n = Grid.Query(HeroPosition, reach + pad, _contacts, Enemies.Position);
        int touching = 0;
        for (int k = 0; k < n; k++)
        {
            int j = _contacts[k];
            if (Enemies.Health[j] > 0f && Vector2.DistanceSquared(Enemies.Position[j], HeroPosition) <= reach2) touching++;
        }
        if (touching == 0) return;
        HeroHp -= touching * Config.Walker.ContactDps * Config.LoopScaling.Damage(Stages.Loop) * dt;
        HeroHurt = true;
    }

    void RequirePhase(RunPhase phase)
    {
        if (Phase != phase) throw new InvalidOperationException($"phase {Phase}, expected {phase}");
    }
}
