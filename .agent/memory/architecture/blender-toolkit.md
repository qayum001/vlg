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
  - `reference`: world_tris → rasterize (numpy, ortho front/side/back) → normalize (bbox, height 256, centered) → iou. load_mask handles plain or transparent backgrounds and cleans stray components. compare() also tries the mirrored reference for side views. diff_image: gray both, red reference-only, blue model-only.
- Invariant: objects keep identity transforms. Geometry is written in world space, so BVH and rasterize work in world coords.
