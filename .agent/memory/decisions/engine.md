# Decision: engine Godot 4.7 + C#, min spec 8 GB / 2 GB
Updated: 2026-10-16
Related: [overview](../project/overview.md), [open questions](../project/open-questions.md), [analysis](../../../design/engine.md)

- Decided by user 2026-10-16. Full comparison and budget numbers: `design/engine.md` (don't copy here).
- Engine: Godot 4.7.x .NET build (4.7.2 stable at decision time). Language: C# only; GDScript too slow for horde loops.
- Min spec: 8 GB RAM, 2 GB VRAM, GTX 750 Ti / GTX 1050 / Vega 8 class (≈ Steam Deck). 60 fps low preset, 1080p output, render scale may drop. 4 GB / 1 GB = "very low" preset stretch goal only.
- Why Godot: agent does all work → text scenes/resources, headless tests and exports, pure-logic C# testable with `dotnet test`; free (MIT); no external assets so Unity Asset Store gives nothing; min-spec wall is GPU, so Burst/VFX Graph advantages matter little.
- Rejected: Unity 6 (YAML scenes + .meta hard for agent, license activation for batch mode, official MCP beta needs subscription).
- Fallback trigger → Unity: horde stress test misses targets on Vega 8 iGPU and profiling blames the engine, not our code; or design moves to 3000+ visible enemies.
- Stress test 2026-10-16 (Vega 8, low preset, release): 3000 enemies Forward+ / 4250 Mobile at 60 fps; design budget ~1500 visible enemies on low. Enemy real-time shadows ~3× per-enemy cost → off up to medium. Details `design/engine.md` §7. Unity fallback trigger not hit.
- Windows rendering driver: D3D12 (Vulkan had bad frame pacing on the hybrid laptop).
- Renderer: Mobile measured faster in every test (+40% enemies, 2.8× particles). Default renderer not decided yet (user).
- Tests: pure C# logic → xUnit (user, 2026-10-16). In-engine test framework: not chosen.
- Benchmarks run on release export builds (export templates installed), not the editor binary.
- Horde rules (any engine): instanced draws + baked vertex animation, own collision grid, pools, no per-frame allocations, per-preset budgets. Details: `design/engine.md` §3.
- Perf testing: dev laptop Vega 8 iGPU = tier B / Deck stand-in. Never judge perf on the RTX 3070.
