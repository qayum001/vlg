# Open questions
Updated: 2026-11-10
Related: [overview](overview.md), [status](status.md), [concept](../../../design/concept.md)

Game design open items live in `design/concept.md` → Open. This file: project/tech items only. Remove an item once decided.
- Tier A (4 GB / 1 GB) and Steam Deck numbers are untested; need real hardware (old laptop / Deck) running `tools/bench.ps1`.
- In-engine test framework (Godot side): not chosen. Pure logic uses xUnit ([engine decision](../decisions/engine.md)).
- Git LFS for binary assets (models, textures, audio): decide before big assets land. Small generated placeholder WAVs (`game/assets/audio/beat/`, ~0.5 MB) were committed to plain git 2026-10-18.
- Quality presets live in `stress.json` and the run reads them from there. Move to their own data file when a settings menu exists.
