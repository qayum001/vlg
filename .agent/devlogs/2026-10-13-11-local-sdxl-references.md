# Local SDXL reference images (first run)
Date: 2026-10-13
Type: investigation
Commits: none (memory commit follows)

## Summary
The user set up ComfyUI 0.39.0 + SDXL base themselves (my download was too slow, ~0.2 MB/s) and stopped my download. I generated mage references through the local API and scored them with the reference loop. img2img over our own ortho render works for T-pose references. txt2img and mannequin img2img don't keep the T-pose.

## Details
- txt2img (4 seeds, front): stylized concept art, arms down, color drift, one multi-view sheet. IoU vs v3 0.11–0.45.
- img2img from the v3 ortho front render (CPU Cycles, headless, flattened on white): strength 0.55 → IoU 0.96/0.87, strength 0.68 → 0.85/0.83. Adds a front seam/panels, collar, hat band. Faces degrade, and one seed grew a beard-like hair shape.
- img2img from a gray mannequin render (0.80/0.88): the render was overexposed (near white), so the outputs are line-art sketches, mostly not in T-pose.
- Scripts (scratchpad, not in the repo yet): comfy_refs.py (txt2img/img2img via /prompt, /history, then /free), render_refs.py, render_mannequin.py, score_refs.py.
- Outputs: `art/characters/ref/gen/` (untracked).

## Problems
- Blender GUI crashed ("GPU has been disconnected") when an EEVEE render started while ComfyUI held ~5 GB VRAM. Fix: headless Cycles CPU renders and /free after Comfy jobs. Saved mage.blend (22:10) intact.
- IoU differences between near-identical shapes come mostly from 1–2 px misalignment on thin parts.

## Results
- Tests: none run (no repo code changed).

## Next
- User picks a direction: tune v3 toward an img2img reference, try a darker mannequin, or download ControlNet OpenPose for SDXL for new characters.
- Toolkit: alignment search in reference.compare. Move comfy_refs.py into the repo if the workflow is kept.
- User restarts Blender (crashed).
