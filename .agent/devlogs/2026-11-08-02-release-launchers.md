# Release launchers
Date: 2026-11-08
Type: feature
Commits: 3a91cfb

## Summary
Two double-click batch launchers for normal standalone release gameplay.

## Requirements
- Add a launch directory to the newer game checkout at C:\Projects\Games\vlg.
- One launcher builds and launches; the other uses the latest existing build.
- Preserve normal release startup without development or debug launch arguments.

## Details
- Shared PowerShell helper resolves the checkout from its own location.
- Build uses the Godot .NET release exporter with --quit and starts the GUI executable.
- Dated export directories preserve running games and previous successful builds.
- Only complete successful exports get a ready marker; latest launch skips failed exports.
- Added release instructions, manual checks, and repository tooling context.

## Problems
- None.

## Results
- Both .bat entry points opened responsive standalone vlg windows from a different working directory.
- Latest launch reused the completed executable without changing its modification time.
- Missing-build and missing-engine checks returned clear errors and exit code 1.
- Simulated export exit code 17 propagated through the batch wrapper as failure; the last good build remained selectable.
- Closed only the two game windows opened by the launcher smoke tests.
- dotnet test --project tests/Vlg.Core.Tests/Vlg.Core.Tests.csproj: 266 passed, 0 failed, 0 skipped.
- PowerShell parse and git diff --check passed.

## Next
- User can double-click either launcher; changes await manual confirmation on dev.
