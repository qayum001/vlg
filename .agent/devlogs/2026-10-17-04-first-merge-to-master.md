# First merge to master
Date: 2026-10-17
Type: merge
Commits: f698131, 5fab9fd, a46aaef, 21ea8c1 (horde stress test); 759cdad, 350a6ca, fc88797 (level run + review fixes); 0d2ccd9 (confirm)

## Summary
User said "merge" after the first playable run. First ever merge: master..dev held the whole project history (agent setup, concept/domain design docs, memory, devlogs, removed modeling toolkit, Godot project). Confirmed features: Level run (first gameplay slice), Horde stress test.

## Details
- User chose to merge everything, including the stress test (the run depends on its code). The modeling toolkit was already removed on dev (2026-10-14); its history comes along.
- Review (reviewer subagent, master..dev): 11 findings; fixes in `2026-10-17-03-review-fixes.md`.
- User decisions in review: keep worker threads = logical processors − 2; F2/F3 dev keys stay in release for now; fixed default seed (1) is fine.

## Problems
- None.

## Results
- Tests: `dotnet test --project tests/Vlg.Core.Tests/Vlg.Core.Tests.csproj` → 140 passed, 0 failed.
- Review: 11 findings, 10 fixed, 1 skipped (#4 thread count, user choice).

## Next
- Items (chest/shop), more enemy behaviors, elites, runes, objectives, loadout pick, balance pass.
