# Tooling (this machine)
Updated: 2026-10-13
Related: [overview](overview.md), [status](status.md)

## Blender
- Blender 5.2.2 LTS (Steam): `C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe`. Not on PATH. Headless: `blender.exe --background --python <script>`.
- Config: `%APPDATA%\Blender Foundation\Blender\5.2\`. Prefs: Allow Online Access = on (required by official addon). Custom `startup.blend` = factory scene + community port 9877.
- Prefs edited headless while the GUI runs DO get overwritten when the GUI quits (happened 2026-10-13). GUI running → change prefs inside it via MCP execute code + `bpy.ops.wm.save_userpref()`.

## Two Blender MCPs (user chose to keep both, 2026-10-13)
Different protocols, so different ports. Never put both on one port.
- **Official Blender Lab** (`projects.blender.org/lab/blender_mcp`): port 9876.
  - Server: Claude Desktop extension `ant.dir.gh.blender.blender-mcp` (`uv run blender-mcp`), port not configurable there → must keep 9876. Loads in Code sessions as `mcp__Blender__*` (capital B).
  - Addon: extension `bl_ext.user_default.mcp` v1.0.3, autostart on, binds localhost, refuses to start without online access.
  - Tools: execute code (return data via `result = {...}`), API/manual docs search, screenshots (area/window), render viewport/thumbnail to path, blendfile summaries.
  - Screenshot tools (window and area) return all-black images (2026-10-13; OpenGL, RTX 3070 Laptop, Blender window not in foreground). Unverified cause: window not in foreground. Use community `look` to see the scene.
  - `render_viewport_to_path` ignores the given directory: writes `%TEMP%\blender_<id>\blender_mcp\<basename>`, path returned in result. It renders through the scene camera and opens a `temp` render window in the GUI → close it: `wm.window_close()` under `temp_override(window=w)`.
- **Community "MCP for Blender"** (ahujasid, PyPI `mcp-for-blender`): port 9877.
  - Server: repo `.mcp.json` server `blender`, env `BLENDER_PORT=9877`, `DISABLE_TELEMETRY=true`. Loads in Code sessions as `mcp__blender__*` (lowercase). Addon v1.8, protocol 13, up to date; libraries/generators all off.
  - Addon: `5.2\scripts\addons\blender_mcp.py`, module `blender_mcp`. Port is a per-scene prop (`blendermcp_port`); new files get 9877 from startup.blend, old files keep 9876 → its autostart skips if 9876 is taken.
  - Tools: `look` (multi-angle rendered screenshots), execute code, scene info. Never use `generate_3d`, `search_assets`, `import_asset` (external assets/AI; art is agent-made only).
  - Upgrade Blender version → reinstall: `uvx mcp-for-blender install-addon` with `BLENDERMCP_ADDONS_DIR`, then enable.
- Both run arbitrary Python in the user's Blender, no auth on the socket. User should save their own work first.
- Test client without session tools: scratch script speaking MCP stdio to `uvx mcp-for-blender` worked (2026-10-13).

## Local image generation (ComfyUI)
- ComfyUI portable 0.39.0, installed and started by the user: `C:\Users\qayum\Downloads\ComfyUI_windows_portable_nvidia\ComfyUI_windows_portable\ComfyUI`. API at http://127.0.0.1:8188 (only while the user runs it). Model: `models\checkpoints\sd_xl_base_1.0.safetensors`.
- Drive it over HTTP: POST /prompt (API-format workflow), poll GET /history/<id>, outputs in `ComfyUI\output`, img2img inputs copied into `ComfyUI\input`. ~20 s per 1024² image on the RTX 3070.
- GPU trap: SDXL keeps ~5 GB VRAM. A Blender EEVEE render at the same time crashed Blender ("GPU has been disconnected", 2026-10-13). Call POST /free {"unload_models": true, "free_memory": true} after jobs. For reference renders, use headless Blender with Cycles on CPU.

## Other
- `uv` 0.13.0 via winget; aliases in `%LOCALAPPDATA%\Microsoft\WinGet\Links` (on PATH).
- `claude` CLI on PATH is broken (npm shim → missing claude.exe). Edit `.mcp.json` instead of `claude mcp add`.
- Available: node, python 3.13, dotnet, winget, git 2.45.
