# Merge current dev into master
Date: 2026-11-10
Type: merge
Commits: 85b6ea4..4830d8c, 7a191d9

## Summary
Merge all current dev work into master on the user's explicit request. Confirm the nine pending feature entries.

## Details
- Includes the run camera, Lowkey kit/forms/ultimates, items/shop/chest, hub/meta/assets, regenerated item icons, release launchers, Pyromancer and saved hero selection.
- Includes the existing shared AGENTS.md entry point and project rules already committed on dev.
- The user cancelled the separate Codex-home instruction-loader setup; its pending write was cancelled and the original empty global AGENTS.md was preserved.
- The separate audio-settings branch is not part of dev and is not included.

## Problems
- None during automated validation.

## Results
- Tests: dotnet test --project tests/Vlg.Core.Tests/Vlg.Core.Tests.csproj -> 275 passed, 0 failed, 0 skipped.
- Build: dotnet build game/Vlg.csproj -> succeeded, 0 warnings, 0 errors.
- Review: no subagent review performed (R15); the user explicitly requested the immediate merge, so the review/selection pause was skipped.
- Interactive gameplay was not tested during this merge.

## Next
- Continue ongoing work on dev.
