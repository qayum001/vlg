# Code rules

Rules marked (R#) come from `rules.md`. The rest are agent defaults; the user may override.

## Comments (R1)
- Audience: agents. Write only what the code can't say: why, invariants, units, non-obvious constraints, links to memory/decisions.
- Short, plain. No restating the code, no tutorials, no "friendly" tone.
- No commented-out code (git has history).
- `TODO:` only with a concrete reason/condition.

## General
- Match surrounding code: naming, structure, idioms.
- Small focused units. No speculative abstractions, no unused code.
- Keep game logic separate from presentation so logic is unit-testable without the engine running.
- Prefer tuning values (numbers, balance) in data/config files over hardcoded constants.
- Fail loudly in dev builds. No silently swallowed errors.
- Per-frame code: avoid needless allocations and repeated lookups.

## Tests
- Every logic change gets automated tests.
- Bug fix: regression test that fails before the fix.
- A test must fail if the behavior it covers breaks. No tautologies, no asserting on mocks only.
- Report real test output. Never claim a pass without running.

## Dependencies and assets
- Ask the user before adding any third-party library, plugin, or asset.
- Check the license allows commercial release on Steam; record it in `third-party.md`.

## Engine-specific
Godot 4.7.x .NET build, C# only (decision: `.agent/memory/decisions/engine.md`).
- No GDScript. Hot loops must be C#.
- C# naming: PascalCase types/methods/properties/constants, camelCase locals/parameters, `_camelCase` private fields. One public type per file, file name = type name.
- Game rules live in a plain C# library with no Godot reference, unit-tested with `dotnet test`. Godot nodes are thin adapters: input, rendering, audio.
- Hordes (enemies, projectiles, pickups): no Node per entity. Struct arrays + MultiMesh/RenderingServer, own spatial grid for collisions, pools. Budgets per quality preset from data.
- Parallel loops: `Vlg.Core.Jobs.WorkerPool` with cached `IRangeJob` objects. A job writes only data of its own indices; writes to other entities (damage, pickups) stay single-thread. Each parallel system gets a test that threaded == single-thread results.
- Hot paths: no LINQ, closures, boxing, string building or `new` of reference types per frame.

Layout:
- `game/`: Godot project (`project.godot`, `Vlg.csproj`, `Vlg.sln`). `scenes/` (snake_case `.tscn`), `scripts/` (C#, file = class), `shaders/`, `data/` (JSON tuning, exported via `include_filter`).
- `src/Vlg.Core/`: Godot-free logic (net8.0). `tests/Vlg.Core.Tests/`: xUnit v3 (`/global.json` opts into Microsoft.Testing.Platform for the .NET 10 SDK).
- `tools/`: scripts. `build/`: exports, benchmark CSVs, screenshots (ignored).
- Commit Godot `.uid` sidecar files. `.godot/` is ignored.

Commands (repo root; `G` = Godot console exe, path in memory `project/tooling.md`):
- Tests: `dotnet test --project tests/Vlg.Core.Tests/Vlg.Core.Tests.csproj`
- Build game C#: `dotnet build game/Vlg.csproj`
- Import after adding resources/shaders: `"$G" --headless --path game --import`
- Run from source (debug): `"$G" --path game -- <game args>`; game args: `LaunchOptions.Usage`. Main scene `boot.tscn` opens the game run; `--stress` or `--bench` opens the stress test (exports refuse a scene path arg).
- Export release: `"$G" --headless --path game --export-release "Windows Desktop" ../build/stress/vlg.exe`
- Benchmarks on the export: `tools/bench.ps1`. Performance numbers only from release exports, never from the editor binary.
