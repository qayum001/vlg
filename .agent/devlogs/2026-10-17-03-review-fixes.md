# Pre-merge review fixes
Date: 2026-10-17
Type: fix
Commits: fc88797

## Summary
Fresh-context review of master..dev (whole project, first merge) found 11 issues. User picked all except #4 (worker threads = logical processors − 2 stays). Dev keys in release and the fixed seed are fine for now (user).

## Requirements
- Fix review findings 1–3 and 5–11; keep `ProcessorCount − 2`; then merge dev to master including the stress test.

## Details
- #1 major: horde, projectile, gem and blade MultiMesh cull boxes were fixed at ±250 m around the origin → everything vanished after ~40 s of walking straight (still simulated, so invisible walkers hurt). Now recentered on the hero in 50 m steps.
- #2 hurt flash refreshed while paused (HeroHurt only reset by Step) → gated on stepped frames.
- #3 ChoicePanel pick is one-shot; RunGame marks the panel stale after every pick, so chained level-ups and double inputs never act on stale cards.
- #5 vsync on by default; StressTest disables it at startup for the bench.
- #6 gems beyond `pickups.despawnRadius` (60 m, new data key) vanish; the pool filled after ~40 min before.
- #7 `StartupFailure.Quit` in Boot, RunGame, StressTest: bad args/preset/data log + exit code 1 instead of erroring every frame.
- #8 tests: loop-scaled contact damage, Dead > LevelUp, LevelUp > Chest, multi-level cap, gem despawn.
- #9–#11: features.md steps, cap panel text ("max 10"), RunSim doc comment, unused members removed.

## Problems
- None new. Verified #1 with a temporary auto-walk run (~450 m out at 70 s: walkers, bolts, blades still drawn).

## Results
- Tests: `dotnet test --project tests/Vlg.Core.Tests/Vlg.Core.Tests.csproj` → 140 passed, 0 failed.
- Export: `--preset bogus` exits in 2 s with code 1 and the error; `--bench enemies` 1 step OK (0.9 ms frames, vsync off).
- Review: 11 findings, 10 fixed, 1 skipped (#4, user choice).

## Next
- Merge dev → master (confirm-feature).
