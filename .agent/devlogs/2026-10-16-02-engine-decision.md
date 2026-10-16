# Engine decision: Godot 4.7 + C#, min spec 8 GB / 2 GB
Date: 2026-10-16
Type: decision
Commits: 072fa58

## Summary
User accepted both recommendations from `2026-10-16-01-engine-minspec-analysis.md`: Godot 4.7 + C#, min spec raised from 4 GB RAM / 1 GB VRAM to 8 GB / 2 GB (≈ Steam Deck).

## Details
- `design/engine.md` status → decided; `design/concept.md` Decided: engine, min spec (4 GB / 1 GB = "very low" stretch preset, not promised).
- New memory `decisions/engine.md` (why, rejected Unity, fallback trigger, perf test on Vega 8 iGPU).
- `code-rules.md` engine section: C# only, naming, logic in Godot-free C# library tested via `dotnet test`, horde rules, hot-path allocation rules. Layout/test commands deferred to project setup.
- `.gitignore`: `.godot/`, `/android/`, `bin/`, `obj/`, `mono_crash.*.json`.
- Tooling memory: .NET SDK 8/9/10 present, git-lfs 3.5.1 present, Godot not installed.

## Problems
- None

## Results
- Tests: none (docs only).

## Next
- Godot project setup: download Godot 4.7.2 .NET (needs approval), choose test frameworks (NuGet → approval + license record), Git LFS decision, then horde stress test prototype.
