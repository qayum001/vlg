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
  - Server: Claude Desktop extension `ant.dir.gh.blender.blender-mcp` (`uv run blender-mcp`), port not configurable there → must keep 9876. Unverified: whether Desktop extensions load in Code sessions.
  - Addon: extension `bl_ext.user_default.mcp` v1.0.3, autostart on, binds localhost, refuses to start without online access.
  - Tools: execute code, API/manual docs search, screenshots (area/window), render viewport/thumbnail to path, blendfile summaries.
- **Community "MCP for Blender"** (ahujasid, PyPI `mcp-for-blender`): port 9877.
  - Server: repo `.mcp.json` server `blender`, env `BLENDER_PORT=9877`, `DISABLE_TELEMETRY=true`.
  - Addon: `5.2\scripts\addons\blender_mcp.py`, module `blender_mcp`. Port is a per-scene prop (`blendermcp_port`); new files get 9877 from startup.blend, old files keep 9876 → its autostart skips if 9876 is taken.
  - Tools: `look` (multi-angle rendered screenshots), execute code, scene info. Never use `generate_3d`, `search_assets`, `import_asset` (external assets/AI; art is agent-made only).
  - Upgrade Blender version → reinstall: `uvx mcp-for-blender install-addon` with `BLENDERMCP_ADDONS_DIR`, then enable.
- Both run arbitrary Python in the user's Blender, no auth on the socket. User should save their own work first.
- Test client without session tools: scratch script speaking MCP stdio to `uvx mcp-for-blender` worked (2026-10-13).

## Other
- `uv` 0.13.0 via winget; aliases in `%LOCALAPPDATA%\Microsoft\WinGet\Links` (on PATH).
- `claude` CLI on PATH is broken (npm shim → missing claude.exe). Edit `.mcp.json` instead of `claude mcp add`.
- Available: node, python 3.13, dotnet, winget, git 2.45.
