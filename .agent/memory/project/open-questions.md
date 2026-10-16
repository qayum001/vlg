# Open questions
Updated: 2026-10-16
Related: [overview](overview.md), [status](status.md), [concept](../../../design/concept.md)

Game design open items live in `design/concept.md` → Open. This file: project/tech items only. Remove an item once decided.
- Default renderer: Mobile (faster, fewer features) vs Forward+. Measured data in `design/engine.md` §7; agent recommends Mobile.
- Tier A (4 GB / 1 GB) and Steam Deck numbers are untested; need real hardware (old laptop / Deck) running `tools/bench.ps1`.
- In-engine test framework (Godot side): not chosen. Pure logic uses xUnit ([engine decision](../decisions/engine.md)).
- Git remote: none. `gh` CLI not installed. Pushing anywhere needs user approval.
- Git LFS for binary assets (models, textures, audio): decide before the first exported assets are committed.
