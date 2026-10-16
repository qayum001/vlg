# Horde stress test
Updated: 2026-10-16
Related: [engine decision](../decisions/engine.md), [tooling](../project/tooling.md), [results](../../../design/engine.md)

Purpose: measure enemy / overdraw / particle limits per GPU and preset. Also the seed of the real horde code.

## Where
- Logic (Godot-free, tested): `src/Vlg.Core/Sim/` (HordeSim, EnemySwarm, ProjectilePool, SpatialGrid, WeaponDirector, CameraMath), `src/Vlg.Core/Bench/` (RampController, BenchLoad, LaunchOptions, BenchCsv, BenchPath), `src/Vlg.Core/Config/` (StressConfig + records).
- Engine side: `game/scripts/` — `StressTest.cs` (scene root, wires everything), renderers, `VfxSystem`, `Arena`, `OrbitCamera`, `QualityApplier`, `Telemetry`, `BenchRun`, `PlaceholderMeshes`.
- Data: `game/data/stress.json` (sim tuning, bench ramps, presets very_low/low/medium/high). Strict parse: unknown/missing keys throw.
- Shaders: `game/shaders/enemy_vat.gdshader` (VAT), `overdraw_layer.gdshader`, `ground.gdshader`.

## Data flow per frame
`HordeSim.Step`: grid build → enemies seek + separate (2-pass, order-independent) → weapons top up projectiles → projectiles move/hit (grid query padded by enemy movement) → dead enemies compacted (swap-remove) → refill to target (Ring = off-screen, Area = on-screen for bench).
Then `EnemyRenderer`/`ProjectileRenderer` write the whole MultiMesh buffer (`RenderingServer.MultimeshSetBuffer`), `VfxSystem` spawns bursts from `Hits`/`Deaths` (capped per preset).

## Non-obvious
- Ground plane: `System.Numerics.Vector2(x, y)` = world `(x, z)`. Yaw 0 looks toward -Z (`CameraMath`).
- Enemy mesh is flat-shaded triangle soup so `VERTEX_ID` indexes the VAT texture (RGBA16F, rows = frames, then normals). LOD via index buffers would keep this valid.
- MultiMesh buffer must be exactly instance_count × stride, so capacity rounds to 512 and the whole buffer uploads each frame.
- Overdraw layers = camera-attached full-screen additive quads; 1 layer = every pixel blended once.
- Whole horde is one MultiMesh, no per-instance culling: off-screen enemies still cost vertex + sim. Screen fits ~1000–1200 enemies at the test camera.
- Bench ramp can be overridden per run: `--ramp-start/--ramp-step/--ramp-max` (LaunchOptions.ApplyRamp), `tools/bench.ps1 -RampStart/-RampStep/-RampMax`. Capacities in stress.json: 50 000 enemies, 25 000 projectiles, grid 256².
- Bench frame time = wall clock between `_Process` calls (vsync off). GPU/CPU render time from `RenderingServer.ViewportGetMeasuredRenderTime*` (previous frame).
- Godot `.uid` files next to scripts/shaders are committed.

## Extend
- New tuning value → record property (required) + `stress.json` + test still parses.
- New bench mode → `BenchMode`, `BenchLoad.For/RampFor`, ramp in `BenchSettings`, `tools/bench.ps1` default list.
