# Mobile as default renderer
Date: 2026-10-16
Type: decision
Commits: 5fab9fd

## Summary
User asked whether Mobile caps PC performance; if not, keep Mobile and run one RTX 3070 test. RTX high preset: Mobile 6750 enemies vs Forward+ 4250 → no cap. Mobile is now the default.

## Requirements
- Keep Mobile if it doesn't cap PC performance; one benchmark on the RTX 3070 (earlier runs were Vega 8, except one Forward+ RTX run).

## Details
- `tools/bench.ps1 -GpuIndex 0 -Modes enemies -Renderers mobile -Presets high` → best passing 6750 (Forward+ same preset: 4250). Mobile skips TAA/SSAO, part of the gap.
- `project.godot`: rendering_method = mobile, feature tag Mobile.
- `Vlg.Core.Config.RendererSupport`: TAA → FXAA and SSAO off when not Forward+; QualityApplier uses it (was printing "TAA is only available when using the Forward+ renderer").
- design/engine.md §7, decision memory, status, features.md updated.

## Problems
- None

## Results
- Tests: `dotnet test --project tests/Vlg.Core.Tests/Vlg.Core.Tests.csproj` → 54 passed.
- Release export + smoke run on high preset: "Forward Mobile" renderer, no TAA warning, screenshot OK.

## Next
- Real game systems; tier A / Deck runs need real hardware.
