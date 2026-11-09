# Level run
Updated: 2026-10-18
Related: [stress test](stress-test.md), [skills](skills.md), [status](../project/status.md), [domain](../../../design/domain.md)

Purpose: playable run from `design/domain.md`: hero, loadout skills with MOBA leveling, walkers, XP gems, stage loop, death. Skills, beat clock and statuses: [skills](skills.md).

## Where
- Logic (Godot-free, tested): `src/Vlg.Core/Run/` — `RunSim` (orchestrator, phases; `RunSim(config, hero, seed, workers, loadout)`), `HeroProgress` + `LevelRule` (XP, level-up picks, cap), `StageLoop`, `WalkerSpawner`, `GemPool`, skills ([skills](skills.md)).
- Config: `src/Vlg.Core/Config/RunConfig.cs` + section records; data `game/data/run.json` (strict parse, `ConfigJson` options shared with stress.json and hero files). Hero: `game/data/heroes/lowkey.json`.
- Engine side: `game/scripts/RunGame.cs` (scene root `scenes/run.tscn`), `RunHud`, `ChoicePanel` (modal picks), `GemRenderer`, `SkillVisuals`, `BeatAudio`, `BeatOverlay`. Reuses `EnemyRenderer`, `ProjectileRenderer`, `VfxSystem` (bursts only), `OrbitCamera`, `Arena.Follow` (endless ground).
- Main scene `scenes/boot.tscn` (`Boot.cs`): run by default, stress test for `--stress`/`--bench` (export templates refuse a scene path arg).

## Data flow per step (`RunSim.Step`, only in phase Playing)
(adapter: `SetView(yaw, aspect)` → view + aim) → move hero → `StageLoop.Update` (entering Shop = full heal; Chest/Shop stop the step) → beat advance → spawn + recycle outrun walkers just off-screen → grid build → swarm update (statuses) → Drop shock ring → skills → rings → projectiles → contact damage → remove dead → gems at deaths → gems collected → XP → pending level-ups. Query pad = (walker speed × 1.5 + MaxPushSpeed) × dt.

## Rules (domain.md)
- Run start: hero level 1, loadout slot 0 at level 1. Each level-up = 1 pick: unlock (→1) or +1, skill level ≤ ceil(hero level / levelDivisor), max `maxSkillLevel`. Nothing pickable → +`maxedBonusDamage` damage bonus.
- Cap uses the current hero level, so a multi-level jump lets every pending pick see the final level.
- Phase priority: Dead > LevelUp > Chest/Shop > Playing. Adapter answers via `ChooseLevelUp` / `ContinueStage`.
- Skill stats at level L = base + perLevel × (L−1); `count` floored (`SkillStats.Amount`). Stat meaning per kind: `SkillStats.cs`.
- Loop scaling linear: 1 + perLoop × (loop−1); spawn multiplier scales rate and alive cap.

## Non-obvious
- Loadout = hero `loadout` (or `--loadout`), no loadout screen yet. Options shown in slot order, keys 1..9.
- A skill with no target (projectile) or no room (speaker cap) stays ready (timer 0) and retries.
- Hits/Deaths/HeroHurt/Beams cleared at the start of every Step call, even when paused. RunGame doesn't call Step while paused, so it gates bursts and hurt flash on "stepped this frame".
- Camera: `run.json` `camera` (tilt 40° below horizon, 28 m, FOV 45°, user 2026-10-17). `OrbitCamera` takes values as params; stress test keeps its own fixed 55°/22 m for bench comparability.
- Spawning follows the view (concept.md): `ViewFootprint` = ground quad the camera sees (yaw + aspect); spawn = random direction, edge + `viewMargin` + [0, `band`) → ~18 m behind the hero, ~41-60 m ahead at 16:9. Config validation: both despawn radii must exceed the farthest visible point at 21:9 (`RunConfig.MaxAspect`); tilt must keep the top edge ≥ 5° below the horizon.
- Props: run uses 5x5 tiles (props within 120 m), sun shadow distance from `camera.shadowDistance`.
- Endless map: renderer MultiMesh cull boxes (500 m) are recentered on the hero in 50 m steps (`SetCullCenter`), else everything vanishes ~250 m from the origin. Gems left > `pickups.despawnRadius` behind vanish (pool never fills).
- ChoicePanel picks are one-shot per Show; after a pick RunGame marks the panel stale so SyncPanel always rebuilds (chained level-ups, double inputs).
- Vsync on for play (project default); StressTest turns it off at startup. Scene roots catch _Ready errors → `StartupFailure.Quit` (log + exit code 1).
- Quality presets still read from `stress.json` (`--preset`, default low). Spell lights/aura/overdraw layers forced off.
- Seed = `--seed` (default 1): runs are deterministic for the same input.
- Dev keys: F2 skip stage, F3 +1 level (`RunSim.DevSkipStage/DevGainLevel`).

## Extend
- New skill kind: see [skills](skills.md) → Extend.
- New tuning value → record property + run.json + validation.
- Items, chest/shop content, elites, other enemy types: not started (Chest/Shop are placeholder panels).
