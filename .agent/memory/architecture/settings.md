# Settings and headless tests
Updated: 2026-11-08
Related: [run](run.md), [status](../project/status.md), [UI and CLI](../gotchas/godot-ui-and-cli.md)

- Run pause menu -> Settings -> master volume slider (0-100%, default 100, 0 mutes). Back/Esc/B/Start returns to pause; Resume resumes the simulation.
- `UserSettings` (Core) validates and serializes user preferences separately from strict gameplay tuning. Missing fields use defaults; unknown fields are accepted for compatibility.
- `GameSettings` initializes once per process from `user://settings.json`, applies the Master bus, and atomically replaces the file via a temporary file on every change. Invalid preferences warn and fall back to defaults; save failures keep the live volume change and show UI feedback.
- Boot and both scene roots initialize settings so direct scene launches also apply volume. The run still obtains graphics presets from stress.json; this settings screen is audio-only. No audio playback exists yet.
- `tools/test-game.ps1` is the game verification entry point: unit tests, build, import, then `game/tests/game_smoke.tscn` in headless Godot. All logs/preferences are isolated under a unique `build/game-tests/` directory. `-Godot` overrides the local engine path.
- `GameSmokeTests` drives the real run scene and checks UI/input, simulation pause, Master bus state, restart, saved settings, write errors, fresh-process reload and malformed-file fallback. Assertions throw and quit nonzero; the script also requires a pass marker to detect incomplete runs.
- Headless Godot's dummy display reports a 64x64 window even with `--resolution`; the test scene sets a virtual content size for layout/input checks. Synthetic mouse coordinates use `PushInput(..., inLocalCoords: true)`. Joypad input uses `Input.ParseInputEvent` to update global action state used by sliders.
- Smoke tests compile only in Debug; `tests/*` resources are excluded from exports. Game testing must follow R11: headless/CLI, no Computer Use.
