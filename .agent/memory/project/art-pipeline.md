# Art pipeline
Updated: 2026-10-13
Related: [tooling](tooling.md), [status](status.md), [concept](../../../design/concept.md)

- Models are built by procedural bpy scripts run in the live Blender (community MCP execute code), then saved as `.blend`. No sculpting, no external assets.
- Style level (user, 2026-10-13): low-poly, flat shading, visible facets, not ultra-low. A smooth first mage (~110k tris) was rejected as "too round". The low-poly mage at ~5.5k tris was the accepted direction.
- Axes: character faces -Y, Z up, feet at z=0, metres. Her right hand is at -X.
- Techniques that worked:
  - Lofted rings: robe, sleeves, cape, neck. Alternating ring radii give low-poly pleats (skirt, cape hem).
  - Swept tubes (4–6 sides): fingers, staff, trims, belt. Hair locks use a 4-sided diamond cross-section with few segments.
  - Head: low-res UV sphere (20x14) bent by a shape function. Nose is a separate wedge mesh.
  - Eyes, lips, brows, lashes: decals ray-cast onto the head front. Eye colors (iris, pupil, highlight) come from a UV-distance color ramp with constant interpolation, not textures.
  - Cloth and hair vs body: push vertices out with `BVHTree.find_nearest` plus a margin, with smoothing between passes.
- Check progress with community `look` (`mode=angles`, `shading=material`). Final image with an official render (R9).
- Mage test: `art/characters/mage.blend`, untracked until Git LFS is decided. Its build script is in session scratchpad only, not in the repo.
