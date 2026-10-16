# Features

Manual test guide.
- **Awaiting confirmation** — on `dev`, needs the user's manual test. Confirm → merged to `master`.
- **Confirmed** — merged to `master`.

Entry format:
```
### <Feature name>
Added: YYYY-MM-DD | Confirmed: YYYY-MM-DD
What: one or two lines.
How to test:
1. ...
Expected: ...
```

## Awaiting confirmation (dev)

### Horde stress test
Added: 2026-10-16 | Confirmed: -
What: Godot 4.7 + C# test scene for measuring limits: GPU-animated placeholder enemies chasing the player, auto-fire projectiles, hit bursts, aura particles, spell lights, full-screen overdraw layers, 4 quality presets from `game/data/stress.json`. Play mode with live stats, plus an automated benchmark that ramps load and writes CSV. Results: `design/engine.md` → "Measured".
How to test:
1. Play from source: `Godot_v4.7.2-stable_mono_win64_console.exe --path game` (repo root). Mouse is captured; Esc frees it.
2. WASD moves (relative to camera), mouse orbits the camera. Enemies chase you, projectiles kill them, orange bursts on hits/deaths.
3. Keys 1–4 switch presets (very_low, low, medium, high): render resolution, shadows and enemy detail change. `[` `]` change enemy count by 250, `,` `.` add/remove full-screen overdraw layers, `-` `=` change aura particles. HUD shows FPS, frame/GPU/sim times, draw calls, memory.
4. Run on the built-in Vega 8: add `--gpu-index 1` before `--path`. HUD first line shows the renderer (default `mobile`) and GPU name. Forward+ for comparison: add `--rendering-method forward_plus`.
5. Benchmark (release export, see `code-rules.md` → Commands): `powershell -ExecutionPolicy Bypass -File tools\bench.ps1 -GpuIndex 1`. Each run goes fullscreen, ramps, quits by itself; CSVs land in `build\bench\<date>\`.
Expected: steady motion, no errors in the console. Benchmark CSVs have one row per step and end with `# best_passing=<value>`.

## Confirmed (master)
_None._
