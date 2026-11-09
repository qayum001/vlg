# Lowkey ultimates
Date: 2026-10-18
Type: feature
Commits: c41c207

## Summary
Slice 3: the 3 approved ultimates with charge kinds, level-up unlock/upgrade cards, input, HUD, effects and tests.

## Requirements
- `design/heroes/lowkey.md` Ultimates table (approved "keep as proposed"); domain.md: pressed, unlock at hero level 10, upgrades at 20/30, charge differs per ultimate, no branches.

## Details
- Data: `ultimates` + `ultimate` in lowkey.json, `leveling.ultimateLevels` in run.json. New stats `tick`, `heal`, `charge`.
- `HeroProgress` ultimate card (`LevelUpKind.Ultimate`), `UltimateLevel`, `DevSetUltimate`.
- `UltimateSystem` with phases Idle/Encore/Windup/Firing; `BeatClock.ForceDrop`; `SkillSystem.RateMultiplier`; RunSim queues presses into the next step (grid is stale between steps), heals per kill in Encore L3.
- Godot: Space / Y, F4 dev fill, ultimate bar (charge text, READY, phase), white overlay flash, sounds, cards with per-level text.

## Problems
- Test pitfalls only: a queued press applies on the next step (tests stepped once before checking); a hero dying from a test enemy froze the Encore timer.

## Results
- Tests: `dotnet test --project tests/Vlg.Core.Tests/Vlg.Core.Tests.csproj` → 230 passed.
- Smoke: run with `--ultimate bass_cannon --ultimate-level 2 --skill-level 3`, screenshot shows the ultimate bar; no errors.

## Next
- Slice 4: items, credits, shop, chest (design/items.md).
