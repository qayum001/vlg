# Godot UI, culling and export CLI
Updated: 2026-10-17
Related: [run](../architecture/run.md), [tooling](../project/tooling.md)

- `Control.SetAnchorsPreset(preset)` keeps the control's current rect (offsets recomputed). A label already in the tree with width 0 stays width 0 after `TopWide` → text sits at the left. Use `SetAnchorsAndOffsetsPreset` in code-built UI.
- Export templates refuse a scene path on the command line ("compiled without support for path overrides"). Editor binary accepts `--path game res://scenes/x.tscn`. Scene choice in exports goes through the boot scene + launch args (`--stress`).
- MultiMesh `CustomAabb` is in the instance's local space and culls the whole MultiMesh. A fixed box around the origin hides the entire horde once the camera leaves it (endless map, ~40 s of walking). Recenter it on the player.
- Test windows from `--path game` grab focus and capture the mouse: the user's live mouse/keys reach the game (camera yaw changed between identical runs; a level-up pick with no panel input logged was most likely a stray key/click). Keep agent smoke runs short and log picks before blaming code.
