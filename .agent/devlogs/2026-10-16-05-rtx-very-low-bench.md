# RTX 3070 very_low benchmark + ramp overrides
Date: 2026-10-16
Type: investigation
Commits: a46aaef

## Summary
User: "run lowest preset + 3070". very_low had already hit the 10 000 ramp max on Vega 8, so ramp override flags and higher capacities were added first. RTX 3070 very_low (Mobile): 24 000 enemies at 60 fps, CPU-bound.

## Requirements
- Run the very_low preset on the RTX 3070 (default renderer Mobile). Agent assumption: find the real limit, not "≥ 10 000".

## Details
- LaunchOptions `--ramp-start/--ramp-step/--ramp-max` + `ApplyRamp`; bench.ps1 `-RampStart/-RampStep/-RampMax`; CSV meta `ramp`.
- stress.json capacities: enemies 12 000 → 50 000, projectiles 8000 → 25 000, gridDim 160 → 256 (crowd at 24 000 is ~90 m wide).
- Result: 24 000 pass (p95 16.3), 26 000 fail. GPU 3.2 ms, sim 10.1 ms → single-thread logic is the limit (~0.4 µs per enemy incl. projectiles).
- Correction found: the test camera sees ~860 m² of ground, which fits ~1000–1200 enemies at 0.9 m spacing. Earlier "visible enemies" wording was wrong; counts are enemies in the world. engine.md + memory fixed. Design budget on low: full screen, ~1500 total.

## Problems
- Own mistake: called horde counts "visible" before computing the camera footprint.

## Results
- Tests: `dotnet test --project tests/Vlg.Core.Tests/Vlg.Core.Tests.csproj` → 58 passed.
- Release export rebuilt; bench run completed and quit by itself.

## Next
- If a design ever wants >10k enemies on strong PCs: multi-thread the separation loop and cull off-screen instances (chunked MultiMeshes).
