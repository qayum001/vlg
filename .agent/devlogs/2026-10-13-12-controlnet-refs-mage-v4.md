# ControlNet T-pose references and mage v4
Date: 2026-10-13
Type: feature
Commits: c521a43, 248c7ec

## Summary
Set up a T-pose reference path for new characters: an OpenPose skeleton drawn from the base body joints plus ControlNet OpenPose (SDXL, xinsir) in local ComfyUI. Fixed reference masking for gradient backgrounds. Modeled mage v4 toward the user-picked reference (seed 505) with the silhouette loop: IoU 0.41 -> 0.58.

## Requirements
- User chose option 2 (new-character pipeline), then "505" as the design target.
- The user downloaded ControlNet (xinsir/controlnet-openpose-sdxl-1.0, 2.33 GB, Apache-2.0) and placed it as models/controlnet/controlnet-openpose-sdxl-xinsir.safetensors.

## Details
- Dark mannequin img2img: keeps the T-pose, but the design is ignored (thin arms, hoods, no hair).
- ControlNet txt2img (strength 1.0) plus emphasis weights and negatives (short skirt, two staffs, beard, teal cape, gradient background): 6 T-pose mages close to the spec.
- reference.load_mask: border flood fill through small color steps replaces corner-color thresholding. Gradient backdrops had masked as all-foreground. Regression test added.
- Mage v4: black dress + purple bell sleeves (material split at mid upper arm), wide belt, gold amulet, black knee boots, long purple open coat hanging from under the arms (asymmetric flare), bigger kinked hat tilted back so the brim clears the eyes, center-parted bangs + 6 front locks, thick staff. Kept from the spec: green eyes, cyan crystal.
- scene.push_out gets max_move (garment turn-in faces point inward and threw coat vertices far away).
- Scripts still in scratchpad: comfy_refs.py (txt2img/img2img/ControlNet), dump_pose.py + draw_pose.py, render_refs.py, score_refs.py, render_mannequin.py.

## Problems
- First v4 coat exploded into shards (wrong-normal pushes) → max_move + lower coat top.
- A bigger hat brim covered the eyes → tilt back + raise.
- Asymmetry tuning overshot once (0.588 → 0.573), settled at 0.58.

## Results
- Tests: run_tests.py → Ran 39 tests, OK.
- Render: art/characters/ref/mage_v4_render.png; side-by-side compare_505_v4.png.

## Next
- Visual gaps vs 505: stiff coat panels (needs folds, turned-back purple lapels), hair volume, thicker staff, bigger stylized eyes, belt/bodice details.
- Move the reference scripts into the repo (tools/refgen) with tests for the pose drawing.
