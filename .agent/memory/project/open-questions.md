# Open questions
Updated: 2026-10-16
Related: [overview](overview.md), [status](status.md), [concept](../../../design/concept.md)

Game design open items live in `design/concept.md` → Open. This file: project/tech items only. Remove an item once decided.
- Godot project setup ([engine decision](../decisions/engine.md)): Godot not installed (download needs user approval), project layout, test frameworks (pure C# logic via `dotnet test` + a NuGet framework; in-engine tests). Third-party → user approval + license record. On setup: fill `code-rules.md` engine section (layout, test commands).
- Git remote: none. `gh` CLI not installed. Pushing anywhere needs user approval.
- Git LFS for binary assets (models, textures, audio): decide before the first exported assets are committed.
