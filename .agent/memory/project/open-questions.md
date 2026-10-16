# Open questions
Updated: 2026-10-16
Related: [overview](overview.md), [status](status.md), [concept](../../../design/concept.md)

Game design open items live in `design/concept.md` → Open. This file: project/tech items only. Remove an item once decided.
- Engine + language + min spec. Analysis and recommendation (Godot 4.7 + C#; min spec 8 GB / 2 GB) in `design/engine.md`, waiting for the user. On decision: `code-rules.md` engine section (incl. test framework + commands), `.gitignore`.
- Git remote: none. `gh` CLI not installed. Pushing anywhere needs user approval.
- Git LFS for binary assets (models, textures, audio): decide before the first exported assets are committed.
