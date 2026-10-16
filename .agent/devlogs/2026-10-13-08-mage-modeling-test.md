# Mage modeling test (first agent-made character)
Date: 2026-10-13
Type: investigation
Commits: b921ae3, c446d85, 56fd519

## Summary
Built a female mage in T-pose via procedural bpy scripts in the live Blender. The first version was smooth and the user rejected it as too round. Rebuilt it low-poly (~5.5k tris, flat shading) and saved it to `art/characters/mage.blend` (untracked).

## Details
- User spec: female mage, T-pose, black cape, mage hat, yellow hair, green eyes, staff in her right hand. Clarify answers: heroic proportions (~7 heads), purple robe dress with gold trim, long staff with glowing gem, repo file not committed until Git LFS.
- New rule R9: community MCP for screenshots (`look`), official MCP for renders.
- v1: smooth, ~110k tris (head 96x72 sphere, gaussian face features, 20 hair clumps + hair sheet).
- User feedback mid-build: "too round", keep low poly, but not ~100-face models. Added to `design/concept.md`.
- v2 (kept): flat shading, ~5.5k tris, 75 objects. Ring/sweep lofts, wedge nose, decal eyes with ramp shader, diamond-section hair locks, pleats from alternating ring radii.
- Cape and hair pushed out of the body with BVH nearest-point checks.

## Problems
- Solidify material_offset: the cape's offset shell came out on the outside, so the outer face showed the lining color. Fixed by swapping material slots.
- v1 hair: thin strips looked stringy, and the hair sheet formed a ruff at the nape. Chunky low-poly locks fixed both.
- A chest cord between the cape clasps looked like a bent wire, so it was removed.
- EEVEE render shows some shadow speckle on the hair. The viewport is clean.

## Results
- Tests: visual checks via `look` (front/3-4/back/side, close-ups) and an official render. No automated tests (no code in repo).

## Next
- User feedback on the mage.
- Decide whether to keep build scripts in the repo (currently scratchpad only).
- Git LFS before committing `.blend` files.
