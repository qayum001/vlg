# Master volume settings and headless game tests
Date: 2026-11-08
Type: feature
Commits: 3bc1d34, 240a314

## Summary
Added pause-menu Settings with a saved master volume slider and a reusable headless game verification command.

## Requirements
- Add an audio volume bar to settings; add a settings mechanism if missing.
- User chose one master slider.
- User requested game testing without Computer Use and asked to save that preference.

## Details
- Master volume 0-100%, live Master bus control, mute at 0, persistent JSON preferences, invalid-file fallback and visible save errors.
- Settings returns to pause with Back/Esc/B/Start; the simulation remains paused.
- `tools/test-game.ps1`: unit suite, build, import and three isolated headless Godot phases exercising real UI/input/audio/saving/restart/reload.
- Added R11 and a Codex memory note for the user's headless-testing preference. Test resources are excluded from exports; the test class compiles only in Debug.
- Work was committed on `codex/audio-settings`: this worktree started detached and dev was already checked out elsewhere.

## Problems
- Sandboxed NuGet audit, MTP named pipes and Godot .NET initialization failed; verification succeeded outside the sandbox.
- A Computer Use attempt was stopped by the user. All subsequent verification used headless/CLI tests.
- Headless Godot reports a 64x64 dummy window; tests set virtual content size. Synthetic joypad events must update global input state through `Input.ParseInputEvent`.

## Results
- `./tools/test-game.ps1`: 152 unit tests passed; settings 50 checks, reload 3 checks, malformed-file fallback 3 checks passed. Expected warnings cover deliberately injected save/load failures.
- Debug and ExportRelease game builds: 0 warnings, 0 errors.
- `git diff --check`: clean.

## Next
- Feature is awaiting confirmation; no merge to master requested.
- The game still has no audio playback/assets; future playback uses the configured Master bus.
