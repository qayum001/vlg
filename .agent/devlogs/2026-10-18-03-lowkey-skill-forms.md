# Lowkey skill forms (branches)
Date: 2026-10-18
Type: feature
Commits: da075de

## Summary
Slice 2: all 54 approved skill forms (3 at level 5, 2 follow-ups at level 10 per skill) with pick cards, new skill kinds, flags, pool upgrades and tests.

## Requirements
- Approved design `design/heroes/lowkey.md` ("keep as proposed"), domain.md branching rules: designer-made options per branch point, full tree, choice fixed for the run.
- Plan agreed with the user in this session: slice 2 after the base kit.

## Details
- Data: `branches` tree in lowkey.json; `leveling.branchLevels` [5, 10] in run.json; `zoneCapacity`.
- `SkillStats` reworked into a per-`Stat` float block (custom JSON converter, camelCase keys, unknown key throws). `BranchDef` scale/add/kind/tags/flags; `SkillDef.Resolve` → `SkillForm`.
- `HeroProgress`: branch pick queue (pruned when no options), `Version`, `PendingPicks`; `LevelUpKind.Branch` options with `Choice`.
- `SkillSystem`: forms cache keyed by progress version; new kinds BellDrop, Quake, Drone, Shield, Sweep, Boomerang; 24 `SkillFlag` behaviors.
- Pools: `ProjectileShot` (pierce, bounce, explode, burst, return, hit memory by enemy id), `RingShot` (slow, stun, collapse, distance damage), `ZonePool`, `StrikePool`, `FlashEvents`. Enemy burn + ramp heat. RunSim: shield block window, death sources, facing.
- Godot: form pick card, form names in level-up cards and HUD (wrapping), visuals for zones/bells/drones/flashes/shield, sounds for new kinds. Dev arg `--branch`.

## Problems
- `with` on the array-backed `SkillStats` mutated the shared array → test-order dependent failures (static test defs changed by later tests). Fixed: init accessors clone (copy-on-write) + regression test.
- First beam direction lost float exactness through angle→vector; first direction now uses the aim vector.
- Bash heredocs with certain contents failed to parse in this environment; edits moved to scratchpad Python files written with Write.

## Results
- Tests: `dotnet test --project tests/Vlg.Core.Tests/Vlg.Core.Tests.csproj` → 220 passed.
- Smoke: two form sets (`--skill-level 10 --branch ...`) windowed with screenshots, no errors; stress test 3 s exit 0.

## Next
- Slice 3: ultimates (Sub Quake, Encore, Bass Cannon). Then items.
