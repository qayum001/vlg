# Hub, meta-progression, assets
Date: 2026-10-18
Type: handoff
Commits: ca95c97

## Summary
User goal "keep working till implement everything you said": hub + meta credits/upgrades, 35 icons (ComfyUI SDXL), 7 procedural props (headless Blender), fx shaders. Committed; usage limit reached before a final review pass.

## Requirements
- Original request: menu = playable zone with level portals and an NPC for meta upgrades; assets for items and skills (Blender, ComfyUI allowed, R12). No humanoid models (R11).
- Hub design decided without user answers (goal said don't pause): one endless portal, setlist desk, Fixer upgrades (see memory architecture/hub.md).

## Details
- See memory architecture/hub.md and architecture/assets.md.
- 7 icons were redrawn (patterns, light background, logo-like swoosh); the redraws were generated but not visually checked yet (build/shots/icons_redo.png).

## Results
- Tests: 265 passed. Smoke: hub screenshot, run with models + fx screenshot; no errors.

## Next
- Look at build/shots/icons_redo.png; redo any icon still off (tools/icons/make_icons.py <id>).
- Smoke the hub → portal → run → back-to-hub loop by hand (only parts were automated).
- ComfyUI was started by the agent and may still be running (port 8188).
