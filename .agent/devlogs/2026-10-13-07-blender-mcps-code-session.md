# Blender MCPs tested in a Code session
Date: 2026-10-13
Type: investigation
Commits: 099609e

## Summary
New Code session: both Blender MCPs load natively and hit the same live Blender. Everything works except the official screenshot tools, which return black images.

## Details
- Official (Desktop extension, 9876) → `mcp__Blender__*`. Community (`.mcp.json`, 9877) → `mcp__blender__*`.
- Community: `get_addon_status` OK (addon 1.8, protocol 13, up to date, libraries/generators off, telemetry off). `get_scene_info` OK. `look` OK (viewport image).
- Official: `get_objects_summary` OK. `execute_blender_code` OK (5.2.2 LTS, unsaved default scene). `render_viewport_to_path` OK.
- Scene: default Cube/Light/Camera, unsaved. Nothing modified.

## Problems
- Official `get_screenshot_of_window_as_image` and `get_screenshot_of_area_as_image(VIEW_3D)` return all-black PNGs. GPU backend OpenGL, RTX 3070 Laptop. Blender window not minimized but not in the foreground. Cause unverified.
- `render_viewport_to_path` ignored the scratchpad path, wrote to `%TEMP%\blender_a29204\blender_mcp\official_render.png`, and opened a `temp` render window in the GUI. Closed it via `wm.window_close()`.

## Results
- Tests: live tool calls on both servers → all OK except the official screenshots.

## Next
- Optional: retry official screenshots with Blender in the foreground.
- Back to concept: theme/setting, engine choice.
