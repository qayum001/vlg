# Modeling toolkit, rig-ready body, reference loop, mage v3
Date: 2026-10-13
Type: feature
Commits: 1b4a8f0, afed3f7, bdd32ba, b91c77d, 8116411, a5640a6

## Summary
After rejecting external 3D generators, the user asked to improve agent modeling. Built the `vlg3d` Blender toolkit (geometry, builder, decals, silhouette matching, rig-ready base body, body-based clothing) with 37 headless tests, and rebuilt the mage on it (v3, ~6.4k tris). Reference images are still pending: Gemini is region-blocked, and the local SDXL download is slow.

## Requirements
- No external 3D generation services (decision recorded in concept.md).
- Build all three: reference-matching loop, reusable toolkit in the repo, rig-ready base body. First test: redo the mage.
- Reference images: start with an online generator and download a local one. Figma credits declined; Gemini via the user's Google account, but it is region-blocked in the built-in browser. User chose: wait for local SDXL.
- Downloads approved: ComfyUI_windows_portable_nvidia_cu126.7z (1.77 GB, GitHub Comfy-Org v0.39.0) and sd_xl_base_1.0.safetensors (6.94 GB, Stability AI on Hugging Face) to C:\Tools\ComfyUI.

## Details
- `tools/blender/vlg3d`: geom, scene (Builder), decals, reference (numpy rasterizer, mask load, normalize, IoU, diff PNG), body (skin modifier, subsurf, symmetrize), clothing (shell, cut loops, grow).
- `tools/blender/models/mage.py`: v3. The robe is one mesh grown from the body (bodice + sleeves + skirt + bell cuffs). Boots come from body feet. Face, cape, hair, hat and staff are ported from v2.
- art/characters/mage.blend now holds v2 (scene "Scene") and v3 (scene "mage_v3"). Untracked.
- Reference loop demo: v3 vs v2 silhouettes, front IoU 0.81, side 0.90. The diff shows v3's smaller bell cuffs and wider cape.

## Problems
- Skin modifier hulls were asymmetric (3.7 cm) → symmetrize. That pinched the crotch (non-manifold) because the legs crossed x=0 → hip/thigh rx < hip_half. Both are caught by tests.
- One-sided fist vs symmetrize: symmetrize is skipped when fist is set.
- The robe neckline cut below the shoulder tops made an off-shoulder look → cut at neck - 0.03.
- Live Blender module reload order: a stale `scene` broke `clothing` imports.
- The Bash heredoc collapsed `\\` and mangled Windows paths in features.md (fixed; gotcha in memory).
- Downloads drop (curl 56) → resumable retry loop, ~0.5 MB/s.

## Results
- Tests: `blender --background --factory-startup --python-exit-code 1 --python tools/blender/run_tests.py` → Ran 37 tests, OK. A failing script gives exit code 1 (verified).

## Next
- Unpack ComfyUI, finish the SDXL download, generate front and side mage references, tune v3 with reference.compare.
- User manual test of the features.md entry, then merge via confirm-feature.
