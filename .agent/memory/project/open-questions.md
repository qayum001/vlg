# Open questions
Updated: 2026-11-08
Related: [overview](overview.md), [status](status.md), [concept](../../../design/concept.md)

Game design open items live in `design/concept.md` → Open. This file: project/tech items only. Remove an item once decided.
- Tier A (4 GB / 1 GB) and Steam Deck numbers are untested; need real hardware (old laptop / Deck) running `tools/bench.ps1`.
- In-engine test framework (Godot side): not chosen. Pure logic uses xUnit ([engine decision](../decisions/engine.md)).
- Git remote: none. `gh` CLI not installed. Pushing anywhere needs user approval.
- Git LFS for binary assets (models, textures, audio): decide before the first exported assets are committed.
- Quality presets live in `stress.json` and the run reads them from there. Move to their own data file when graphics settings are added (the current settings screen is audio-only).
