# Level run (first gameplay slice)
Updated: 2026-10-17
Related: [stress test](stress-test.md), [status](../project/status.md), [domain](../../../design/domain.md)

Purpose: playable run from `design/domain.md`: hero, 4 auto skills with MOBA leveling, walkers, XP gems, stage loop, death.

## Where
- Logic (Godot-free, tested): `src/Vlg.Core/Run/` — `RunSim` (orchestrator, phases), `HeroProgress` + `LevelRule` (XP, level-up picks, cap), `StageLoop`, `WalkerSpawner`, `SkillSystem`, `GemPool`.
- Config: `src/Vlg.Core/Config/RunConfig.cs` + section records; data `game/data/run.json` (strict parse, `ConfigJson` options shared with stress.json).
- Engine side: `game/scripts/RunGame.cs` (scene root `scenes/run.tscn`), `RunHud`, `ChoicePanel` (modal picks), `GemRenderer`, `SkillVisuals` (aura disc, orbit blades). Reuses `EnemyRenderer`, `ProjectileRenderer`, `VfxSystem` (bursts only), `OrbitCamera`, `Arena.Follow` (endless ground).
- Main scene `scenes/boot.tscn` (`Boot.cs`): run by default, stress test for `--stress`/`--bench` (export templates refuse a scene path arg).

## Data flow per step (`RunSim.Step`, only in phase Playing)
move hero → `StageLoop.Update` (entering Shop = full heal; Chest/Shop stop the step) → spawn + recycle outrun walkers → grid build → swarm update → skills (Bolt/Nova spawn projectiles; Aura/Orbit damage on tick) → projectiles → contact damage → remove dead → gems at deaths → gems collected → XP → pending level-ups.

## Rules (domain.md)
- Run start: hero level 1, loadout slot 0 at level 1. Each level-up = 1 pick: unlock (→1) or +1, skill level ≤ ceil(hero level / levelDivisor), max `maxSkillLevel`. Nothing pickable → +`maxedBonusDamage` damage bonus.
- Cap uses the current hero level, so a multi-level jump lets every pending pick see the final level.
- Phase priority: Dead > LevelUp > Chest/Shop > Playing. Adapter answers via `ChooseLevelUp` / `ContinueStage`.
- Skill stats at level L = base + perLevel × (L−1); `count` floored (`SkillStats.Amount`). Stat meaning per kind: `SkillStats.cs`.
- Loop scaling linear: 1 + perLoop × (loop−1); spawn multiplier scales rate and alive cap.

## Non-obvious
- Loadout = `skills` array order in run.json (no loadout screen yet). Options shown in slot order, keys 1..9.
- Bolt with no target in range stays ready (timer 0) and fires the frame one enters range.
- Cooldown overshoot carries (`timer += cooldown`), so the average rate holds at any frame rate.
- Hits/Deaths/HeroHurt cleared at the start of every Step call, even when paused. RunGame doesn't call Step while paused, so it gates bursts and hurt flash on "stepped this frame".
- Endless map: renderer MultiMesh cull boxes (500 m) are recentered on the hero in 50 m steps (`SetCullCenter`), else everything vanishes ~250 m from the origin. Gems left > `pickups.despawnRadius` behind vanish (pool never fills).
- ChoicePanel picks are one-shot per Show; after a pick RunGame marks the panel stale so SyncPanel always rebuilds (chained level-ups, double inputs).
- Vsync on for play (project default); StressTest turns it off at startup. Scene roots catch _Ready errors → `StartupFailure.Quit` (log + exit code 1).
- Quality presets still read from `stress.json` (`--preset`, default low). Spell lights/aura/overdraw layers forced off.
- Seed = `--seed` (default 1): runs are deterministic for the same input.
- Dev keys: F2 skip stage, F3 +1 level (`RunSim.DevSkipStage/DevGainLevel`).

## Extend
- New skill kind → `SkillKind` + case in `SkillSystem.Update` + validation in `RunConfig.ValidateSkill` + `RunGame.SkillText`.
- New tuning value → record property + run.json + validation.
- Items, chest/shop content, elites, other enemy types: not started (Chest/Shop are placeholder panels).
