# Godot UI, culling and export CLI
Updated: 2026-11-08
Related: [run](../architecture/run.md), [settings](../architecture/settings.md), [tooling](../project/tooling.md)

- `Control.SetAnchorsPreset(preset)` keeps the control's current rect (offsets recomputed). A label already in the tree with width 0 stays width 0 after `TopWide` → text sits at the left. Use `SetAnchorsAndOffsetsPreset` in code-built UI.
- Export templates refuse a scene path on the command line ("compiled without support for path overrides"). Editor binary accepts `--path game res://scenes/x.tscn`. Scene choice in exports goes through the boot scene + launch args (`--stress`).
- MultiMesh `CustomAabb` is in the instance's local space and culls the whole MultiMesh. A fixed box around the origin hides the entire horde once the camera leaves it (endless map, ~40 s of walking). Recenter it on the player.
- User requires headless/CLI tests without Computer Use (R11). Use `tools/test-game.ps1`; live test windows capture the user's mouse/keys and should not be used for automated verification. Headless input and viewport details: [settings](../architecture/settings.md).
