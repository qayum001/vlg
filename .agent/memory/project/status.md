# Status
Updated: 2026-10-13
Related: [overview](overview.md), [open questions](open-questions.md), [concept](../../../design/concept.md)

- Phase: concept brainstorming. Agent environment done. No game code.
- Done: genre, 2D/3D, camera, aiming, players, art approach (see `design/concept.md`).
- Blender MCPs: both load natively in Code sessions and work against the same Blender (official 9876, community 9877). Exception: official screenshot tools return black, so visuals go through community `look` (see [tooling](tooling.md)).
- Art test: low-poly female mage (T-pose, staff) built and rendered, see [art pipeline](art-pipeline.md). Waiting for user feedback.
- AI 3D generators: tested, rejected (no external 3D services), see [AI 3D generators](ai-3d-generators.md).
- Modeling upgrade (2026-10-13): toolkit + rig-ready body + clothing + reference loop done, mage v3 built (features.md, awaiting confirmation). Reference images: Gemini is region-blocked in the built-in browser; user chose a local generator. ComfyUI portable cu126 + SDXL base are downloading to `C:\Tools\ComfyUI\_downloads` (slow, resumable curl loop). Next: unpack, generate front/side mage references, tune v3 against them with reference.compare.
- Next: theme/setting, engine choice (3D hordes + VFX → instancing matters).
