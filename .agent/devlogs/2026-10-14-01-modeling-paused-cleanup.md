# Modeling paused, toolkit and art removed
Date: 2026-10-14
Type: revert
Commits: 6e8b957

## Summary
The user paused modeling: agent-made models (up to mage v4) were "not bad but still not what I want"; Meshy looked good but is paid. The modeling toolkit, models, art files and art notes were removed. Older devlogs stay (R3).

## Details
- Clarify answers: untracked files → Recycle Bin; code → one deletion commit; docs → also remove the art decisions added to design/concept.md. Kept: rule R9, the Blender/ComfyUI notes in tooling memory, the blender server in .mcp.json.
- Removed in 6e8b957: tools/blender (vlg3d toolkit, 39 tests, models/mage.py), the features.md entry, memory art-pipeline / ai-3d-generators / blender-toolkit, concept.md lines (low-poly level, no-external-3D confirmation, reference-images open item), .gitignore entries for Python caches and .blend backups.
- Restorable from git: everything up to 248c7ec (mage v4).
- Sent to the Recycle Bin: C:\Projects\Games\vlg\art (mage.blend with scenes v2/v3/v4, references, renders, ~45 MB) and C:\Tools\ComfyUI (partial ComfyUI download, ~408 MB). C:\Tools is left empty.
- Not touched: the user's ComfyUI install in Downloads (my reference images and renders remain in its input/output folders), Blender with its MCP add-ons, Blender's still-open session of mage.blend.

## Problems
- None.

## Results
- Tests: none (no code left in the repo).

## Next
- Back to concept: theme/setting, engine choice. The art approach is open again (concept still says agent-made art).
