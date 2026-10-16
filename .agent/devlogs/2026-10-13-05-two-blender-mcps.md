# Two Blender MCPs on separate ports
Date: 2026-10-13
Type: tooling
Commits: 1c524ba

## Summary
Community MCP tested end to end; user added the official Blender Lab MCP and chose to keep both. Split ports: official 9876, community 9877.

## Requirements
- User: "blender ready", then "I added blender connector" (Claude Desktop extension, Blender Lab).
- Answers: keep both; community → 9877; download + install official addon (mcp-1.0.3.zip, 17975 bytes, sha256 a7a9da81…0707); enable Blender online access.

## Details
- Session had no Blender tools (.mcp.json added after session start). Tested via scratch MCP stdio client: get_scene_info, execute_blender_code (low-poly test unit, collection vlg_test), look (rendered angles) → OK.
- Official server and addon both default to 9876 with a different protocol (JSON + \0 delimiter). Desktop extension exposes no port setting → official keeps 9876.
- Community port: .mcp.json BLENDER_PORT=9877 + startup.blend scene prop blendermcp_port=9877 (port is per-scene in that addon).
- Official addon installed via `blender --command extension install-file --repo user_default --enable`; reviewed code: binds localhost, no outbound network imports, autostart on.
- Blender pref Allow Online Access turned on (official addon refuses to start otherwise).

## Problems
- Test render: AgX view transform + no bloom washes out emissive colors; head too dark from light placement. Fine for a pipeline test.
- Blender GUI open during pref edits → may overwrite on quit.

## Results
- Tests: headless checks — both addons enabled, online access on, startup port 9877. Live port check pending Blender restart.

## Next
- User restarts Blender → verify 9876 (official) and 9877 (community). New session → check which MCP tools load.
