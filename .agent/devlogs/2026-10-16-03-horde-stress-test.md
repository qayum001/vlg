# Godot project + horde stress test
Date: 2026-10-16
Type: feature
Commits: 9f96cd9, f698131

## Summary
First Godot 4.7 .NET project. Horde stress test with play mode and automated benchmark ramp; measured limits on the Vega 8 iGPU (tier B / Deck stand-in).

## Requirements
- User: "start developing and testing limits"; Godot folder `C:\Projects\Games\Godot\engine`, may download latest.
- Clarify answers: download 4.7.2 .NET editor + export templates; xUnit for logic tests; full scope (enemies, projectiles, VFX, presets, automated ramp → CSV, Mobile + Forward+); playable (WASD + mouse orbit) + benchmark mode.
- Assumptions stated: layout game/ + src/Vlg.Core + tests; code-made placeholder art; measure on Vega 8 at 1920×1080 on AC; pass = p95 ≤ 16.7 ms; results to design/engine.md.

## Details
- Downloads SHA512-verified; Windows x86_64 templates installed to %APPDATA%\Godot\export_templates\4.7.2.stable.mono.
- Core: HordeSim (grid separation, weapons, projectiles, compaction, ring/area spawn), RampController, BenchLoad, LaunchOptions, strict JSON config. 47 xUnit tests.
- Godot: MultiMesh enemies with VAT shader (VERTEX_ID, RGBA16F), projectiles, GPUParticles bursts pool + aura, overdraw quads, lights, presets via QualityApplier, telemetry (RenderingServer GPU/CPU time, process memory), CSV writer.
- tools/bench.ps1 runs modes × renderers × presets on the release export.
- Results (Vega 8, low): enemies 3000 F+ / 4250 Mobile; overdraw 43 / ≥60 layers; particles 20k / 56k; medium 250 / 500 (enemy shadows); very_low ≥10k; Vulkan 1000 (pacing); RTX high 4250.

## Problems
- .NET 10 SDK refuses VSTest with xunit.v3 → global.json MTP opt-in (gotcha file).
- Export fails if the output folder doesn't exist; release console wrapper needs export_console_wrapper=2.
- `powershell -File` passes "a,b" as one string → bench.ps1 splits on commas. Game rejected the bad preset name loudly, as intended.
- One overdraw run was ended by an Esc/close at 34 layers; rerun reached the ramp max.

## Results
- Tests: `dotnet test --project tests/Vlg.Core.Tests/Vlg.Core.Tests.csproj` → 47 passed.
- Build: `dotnet build game/Vlg.csproj -c ExportRelease` → succeeded; release export ran all benchmarks.

## Next
- User: manual test (features.md), choose default renderer (agent recommends Mobile).
- Tier A / Steam Deck need real hardware runs of tools/bench.ps1.
