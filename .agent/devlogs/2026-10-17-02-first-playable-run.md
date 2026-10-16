# First playable run
Date: 2026-10-17
Type: feature
Commits: 759cdad, 350a6ca

## Summary
First gameplay slice from `design/domain.md`: hero, 4 auto skills with MOBA leveling, walkers, XP gems, 5-stage loop, HP/death. Playable from the main scene; stress test moved behind `--stress`.

## Requirements
- Started from another session's brief: hero moving relative to the camera, auto skill with per-level growth, XP + level-up (unlock or raise one skill, cap ceil(hero level / 3)), walkers on the horde sim, stage loop timer, all tunable via data. Done = short playable run, tests pass, features.md steps.
- Clarify answers (user): 4 simple skills (Bolt, Nova, Aura, Orbit) so the level rule is testable; hero has HP, walkers deal contact damage, death screen + restart; Chest and Shop = placeholder pause panels (Shop full heal); `Assests/` (user's HeroModel.blend) ignored in git, hero stays a capsule.
- Assumptions stated, not objected: new run scene as main, stress test stays; one data file `game/data/run.json`; level-up pauses with cards (mouse, keys 1-4, gamepad); horde = walkers at higher density (no swarmers); no items/elites/runes/branches/ultimate/hub; English HUD; no audio, no saves.

## Details
- Core: `src/Vlg.Core/Run/` (RunSim, HeroProgress, LevelRule, StageLoop, WalkerSpawner, SkillSystem, GemPool) + run config records. 63 new tests.
- Reuse refactors in stress code: `SwarmTuning` for EnemySwarm, per-projectile damage, `RandomPoints`, `ConfigJson`. Arena props now a periodic 3x3 tile (~same count) so `Arena.Follow` gives endless ground.
- Godot: RunGame, RunHud, ChoicePanel, GemRenderer, SkillVisuals, Boot main scene.
- Added dev keys F2 (skip stage), F3 (+1 level) for manual testing.
- Placeholder balance checked with a headless scratch sim: standing still dies ~2 min; a moving bot reaches level 12-23 at 20 min and later loops get easier. Tuning left for the prototype.

## Problems
- Export templates refuse a scene path arg ("compiled without support for path overrides"), so `bench.ps1` could not pick the stress scene. Fixed with the Boot scene routing on `--stress`/`--bench`.
- `Control.SetAnchorsPreset` kept a zero-width label: stage title drawn at the left. Use `SetAnchorsAndOffsetsPreset`.
- One smoke run showed a level-up picked with no panel visible. Diagnostic logging in a clean run showed no auto-pick; the camera yaw also changed between runs, so the user's live input reached the captured test window.
- Bash heredoc backslash trap hit twice (csproj path, memory edit); fixed with Write/Edit (known gotcha).

## Results
- Tests: `dotnet test --project tests/Vlg.Core.Tests/Vlg.Core.Tests.csproj` → 135 passed, 0 failed.
- `dotnet build game/Vlg.csproj` → succeeded, 0 warnings.
- Smoke: source run screenshots (HUD, level-up panel, aura + orbit visuals); release export runs the game run by default and `--bench enemies` (1 step) wrote a CSV with `# best_passing=250`.

## Next
- User plays the slice (features.md → Level run), then "merge" → confirm-feature.
- Next systems: items (chest/shop), more enemy behaviors + swarmers, elites, runes, objectives, loadout pick, balance pass.
