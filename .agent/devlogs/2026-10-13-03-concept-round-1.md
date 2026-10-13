# Concept brainstorm, round 1
Date: 2026-10-13
Type: decision
Commits: ddf5856

## Summary
First concept decisions: 3D survivors-like with an orbiting angled top-down camera, single-player, all art by the agent.

## Requirements
- User: survivors-like ("vampire like"), colorful, leaning 3D for particles/VFX; agent makes models (3D) or drawings (2D).
- Answers: survivors-like (theme open); art agent-only via Blender, user reviews, no artist; Soulstone Survivors camera + free yaw orbit around the player; fixed tilt + zoom; mostly automatic attacks plus some aimed weapons; single-player; user installs Blender MCP and wants a guide.

## Details
- New `design/concept.md` (decided / implications / open). Memory links to it instead of copying.
- Found Blender 5.2.2 LTS via Steam. Package renamed `blender-mcp` → `mcp-for-blender`.
- `claude` CLI shim broken → MCP registration via project `.mcp.json`.
- Agent assumption accepted silently: "art is your work only" = no external packs, no AI 3D generators.

## Problems
- None.

## Results
- Tests: none (docs).

## Next
- User finishes MCP setup → `.mcp.json` + test model.
- Open: theme, engine, orbit vs aim controls, orientation aids.
