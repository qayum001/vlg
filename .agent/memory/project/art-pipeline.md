# Art pipeline
Updated: 2026-10-13
Related: [tooling](tooling.md), [status](status.md), [concept](../../../design/concept.md)

- Models are built by procedural bpy scripts run in the live Blender (community MCP execute code), then saved as `.blend`. No sculpting, no external assets.
- Shared helpers live in the repo toolkit, see [Blender toolkit](../architecture/blender-toolkit.md). Model scripts should use it instead of copying helpers.
- Style level (user, 2026-10-13): low-poly, flat shading, visible facets, not ultra-low. A smooth first mage (~110k tris) was rejected as "too round". The low-poly mage at ~5.5k tris was the accepted direction.
- Axes: character faces -Y, Z up, feet at z=0, metres. Her right hand is at -X.
- Techniques that worked:
  - Lofted rings: robe, sleeves, cape, neck. Alternating ring radii give low-poly pleats (skirt, cape hem).
  - Swept tubes (4–6 sides): fingers, staff, trims, belt. Hair locks use a 4-sided diamond cross-section with few segments.
  - Head: low-res UV sphere (20x14) bent by a shape function. Nose is a separate wedge mesh.
  - Eyes, lips, brows, lashes: decals ray-cast onto the head front. Eye colors (iris, pupil, highlight) come from a UV-distance color ramp with constant interpolation, not textures.
  - Cloth and hair vs body: push vertices out with `BVHTree.find_nearest` plus a margin, with smoothing between passes.
- Reference images (SDXL, local), first findings 2026-10-13:
  - txt2img makes nice concept art but ignores T-pose and drifts colors. Silhouette IoU vs v3 is 0.1–0.45, so these are design ideas only.
  - img2img over our own ortho render (strength 0.55–0.68) keeps the T-pose and adds details (front seam, collar, hat band). IoU 0.83–0.96. Faces degrade above ~0.6.
  - img2img over a near-white mannequin turns into line art and drops the T-pose. Needs a darker mannequin or ControlNet OpenPose (not downloaded).
  - IoU at 256 px is dominated by 1–2 px edge misalignment on thin parts. TODO: add a shift/scale alignment search in reference.compare.
- ControlNet OpenPose (xinsir SDXL, in ComfyUI models/controlnet) + a T-pose skeleton drawn from the base body joints = T-pose references that follow the prompt. Use emphasis weights and negatives (short skirt, two staffs, beard, gradient background). The user picked seed 505 as the mage target.
- Mage v4 (`models/mage.py`, scene `mage_v4`) targets 505: black dress, purple open coat hanging from the arms, kinked hat, front locks. IoU vs 505 went 0.41 -> 0.58 by tuning the coat flare/asymmetry and the hat. Known gaps: stiff coat panels, thin hair volume, thin staff, small eyes.
- Check progress with community `look` (`mode=angles`, `shading=material`). Final image with an official render (R9).
- Mage: v2 (standalone script, lost with the session scratchpad) is saved in `art/characters/mage.blend` (untracked until Git LFS). v3 is built from the toolkit by `tools/blender/models/mage.py` (~6.4k tris): body-based robe, rig-ready body underneath.
