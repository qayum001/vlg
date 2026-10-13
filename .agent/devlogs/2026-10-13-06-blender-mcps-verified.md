# Both Blender MCPs verified live
Date: 2026-10-13
Type: fix
Commits: b4271ea

## Summary
After Blender restart only 9877 worked: the old GUI instance saved its prefs on quit and undid the headless pref edits. Re-applied from inside the live Blender; both ports now answer.

## Details
- Saved prefs after restart: online access off, official addon disabled. startup.blend (separate file) kept port 9877.
- Fix: via community MCP (9877) execute code in live Blender: use_online_access=True, enable bl_ext.user_default.mcp, save_userpref.
- 9876 probe (official protocol) → "official ok 5.2.2 LTS". 9877 get_scene_info → OK.

## Problems
- Predicted overwrite happened. Rule now in memory tooling.md: GUI running → edit prefs inside it.

## Results
- Tests: live probes on both ports OK.

## Next
- User starts a new Code session in vlg → approve "blender" server, check if the official Desktop extension's tools appear.
