# Blender MCP setup
Date: 2026-10-13
Type: tooling
Commits: 8d6516e (MCP), 1d4adb3 (concept aiming)

## Summary
Installed and registered Blender MCP for agent-made art. Also folded the user's aiming note into the concept doc.

## Requirements
- User: "could you do it yourself? do it if you can" (after the setup guide).
- Answers: OK to install uv (winget) + mcp-for-blender (PyPI); launcher script wanted if the addon allows auto-connect.

## Details
- uv 0.13.0 via winget → aliases in WinGet\Links (already on PATH, no app restart needed for PATH).
- `uvx mcp-for-blender install-addon` with BLENDERMCP_ADDONS_DIR = Blender 5.2 addons dir (5.2 had no addons folder yet).
- Enabled `blender_mcp` headless + save_userpref; fresh headless instance confirms enabled.
- No launcher: addon auto-starts its server on GUI load by default (scene prop blendermcp_auto_start_server).
- `.mcp.json` server `blender`, DISABLE_TELEMETRY=true. Integrations default off.
- concept.md: user's inline note → mouse rotates camera, aimed weapons fire camera-forward; controls open item updated.

## Problems
- Blender GUI was running during setup (PID 34624). If it saves prefs on exit, the addon enable may be lost → re-enable in Preferences.

## Results
- Tests: none. Headless check `blender_mcp in prefs.addons` → True. Live socket not tested yet.

## Next
- User restarts Blender + new Claude session, approves `blender` server → agent builds a test model.
