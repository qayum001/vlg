# Blender toolkit (vlg3d)
Updated: 2026-10-13
Related: [art pipeline](../project/art-pipeline.md), [tooling](../project/tooling.md)

- Location: `tools/blender/vlg3d/` (package), tests in `tools/blender/tests/`. Runs only inside Blender's Python (bpy, mathutils, numpy bundled). No third-party deps.
- Tests (headless, ~1 s): `"C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe" --background --factory-startup --python-exit-code 1 --python tools/blender/run_tests.py`. Exit code 1 on failure (verified).
- Use from the live Blender (MCP execute code): `sys.path.insert(0, r"C:\Projects\Games\vlg\tools\blender")`, then `importlib.reload` the modules after edits.
- Modules:
  - `geom`: pure geometry (spline, frames, ring_z, loft, fan, sweep, sweep_loop, mirror_x). Returns (verts, faces). Winding is consistent; caps make closed manifold tubes.
  - `scene`: `Builder(coll, prefix)` with mat/ramp_mat/mesh/tube/loop/light/clear. Datablock names start with the prefix so `clear()` can find them. Meshes are flat-shaded. Also orient_axis/orient_path/face_dir (fix normals), trees/push_out (BVH collision, needs outward normals), ray_front/back_y, tri_count.
  - `decals`: grid/strip/almond. Flat meshes ray-cast onto a head from the front (eyes, brows, lips).
  - `body`: `BodyParams` (height, heads, shoulder/hip half-widths, per-joint radii) -> `skeleton()` -> `build_body()`. Skin modifier + Catmull-Clark level 1, applied, then symmetrize +X onto -X, seam weld, recalc normals. Result: one closed, mirror-symmetric island (~1.6k tris) with a ring at each joint. Joints are stored as JSON in `ob["vlg_joints"]` (`body.joints(ob)`). `build_head()`/`head_shape()`/`head_point()` give a separate low-res head (~0.5k tris) sized from head height.
    - Traps: radii are post-subsurf half-widths (RADIUS_GAIN=1.1 calibrates them, guarded by a test). Arm chains map radius to (y, z), vertical chains to (x, y). Leg hip/thigh rx must stay < hip_half, or the legs cross x=0 and symmetrize pinches non-manifold edges at the crotch.
  - `clothing`: shell(body, offset, keep_planes) = copy, inflate along normals, bisect with planes (keeps (p-co).no <= 0). Then boundary_loops/pick_loop/orient_loop (CCW around 'z' or 'x'), grow(bm, loop, rings) bridges new rings, cap, away_from_bones + to_object (consistent winding, flipped to face away from the bones). Clothes reuse body topology, which is rig-friendly.
  - `reference`: world_tris → rasterize (numpy, ortho front/side/back) → normalize (bbox, height 256, centered) → iou. load_mask: transparent border -> alpha; else flood_background() from the border via small neighbor color steps (handles gradients and soft ground shadows; generated refs have them), subsampled to <=512 px, then clean(). compare() also tries the mirrored reference for side views. diff_image: gray both, red reference-only, blue model-only.
- Models: `tools/blender/models/<name>.py` with `build(coll, prefix)`. Mage v3 = `models/mage.py`. Build it in live Blender: reload modules in dependency order (geom, scene, body, decals, clothing, models.*), or the stale `scene` breaks `from .scene import orient`.
- Body `fist=('R',)` gives a one-sided fist. Symmetrize is skipped then, so cm-level skin asymmetry remains (still one closed island, tested).
- Invariant: objects keep identity transforms. Geometry is written in world space, so BVH and rasterize work in world coords.
