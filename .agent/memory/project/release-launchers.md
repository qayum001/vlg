# Release launchers
Updated: 2026-11-08
Related: [tooling](tooling.md)

- `launch/build-and-launch.bat` invokes `launch/launch.ps1 -Build`; `launch/launch-latest.bat` invokes it without the switch. Both start the exported GUI executable without game or debug arguments. User instructions: `LAUNCH.md`.
- Exports are independent dated directories in `build/release/`; write `release-ready.txt` only after a successful release export with executable and game assembly present. Latest launch selects the newest completed directory, skipping failed exports. Do not replace files used by a running game.
- Paths resolve from the launcher's location, not the caller's directory. Build uses the existing Godot .NET installation, overridable with `VLG_GODOT_EXE`, and `--headless --export-release "Windows Desktop" ... --quit`. Export performs C# publish. Existing builds need neither the editor nor SDK.
