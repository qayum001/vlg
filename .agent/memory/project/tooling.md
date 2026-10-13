# Tooling (this machine)
Updated: 2026-10-13
Related: [overview](overview.md), [status](status.md)

- Blender 5.2.2 LTS (Steam): `C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe`. Not on PATH. Headless: `blender.exe --background --python <script>`.
- Blender MCP: package `mcp-for-blender` (repo ahujasid/blender-mcp). Registered in repo `.mcp.json` as server `blender`, `DISABLE_TELEMETRY=true`.
  - Addon: `%APPDATA%\Blender Foundation\Blender\5.2\scripts\addons\blender_mcp.py`, module `blender_mcp`, enabled in 5.2 userprefs.
  - Server auto-starts when Blender GUI opens (scene prop `blendermcp_auto_start_server`, default on). Port 9876. Check: `Test-NetConnection localhost -Port 9876`.
  - Asset integrations (Poly Haven, Sketchfab, Hyper3D, Hunyuan, Poly Pizza) default off — keep off: art is agent-made only. Addon content telemetry default off.
  - `execute_blender_code` runs arbitrary Python in the user's Blender — tell the user to save their own work first.
  - Blender upgrade (new version folder) → addon must be reinstalled: `uvx mcp-for-blender install-addon` with `BLENDERMCP_ADDONS_DIR` set to the new version's `scripts\addons`, then enable.
- `uv` 0.13.0 via winget; `uv`/`uvx` aliases in `%LOCALAPPDATA%\Microsoft\WinGet\Links` (on PATH).
- `claude` CLI on PATH is broken (npm shim → missing claude.exe). Don't rely on `claude mcp add`; edit `.mcp.json`.
- Available: node, python 3.13, dotnet, winget, git 2.45.
