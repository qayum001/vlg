# Features

Manual test guide.
- **Awaiting confirmation** — on `dev`, needs the user's manual test. Confirm → merged to `master`.
- **Confirmed** — merged to `master`.

Entry format:
```
### <Feature name>
Added: YYYY-MM-DD | Confirmed: YYYY-MM-DD
What: one or two lines.
How to test:
1. ...
Expected: ...
```

## Awaiting confirmation (dev)
### Blender modeling toolkit + mage v3
Added: 2026-10-13
What: `tools/blender/vlg3d` toolkit (geometry, rig-ready base body, clothing from body shells, face decals, silhouette matching vs reference images) and the mage rebuilt on it (`tools/blender/models/mage.py`).
How to test:
1. From the repo root run: `"C:\Program Files (x86)\Steam\steamapps\common\Blenderlender.exe" --background --factory-startup --python-exit-code 1 --python tools/blender/run_tests.py`
2. Open `art/characters/mage.blend`, switch the scene (top right) to `mage_v3`.
3. Optional rebuild, in Blender's Python console: `import sys; sys.path.insert(0, r"C:\Projects\Gameslg	oolslender"); import models.mage as m; m.build("MageV3", "mage3_")`
Expected: step 1 ends with `OK` (37 tests). Step 2 shows a flat-shaded low-poly mage in T-pose (~6.4k tris): black cape, hat, yellow hair, green eyes, staff in her right fist. The robe is one mesh, and a full body sits under it (hide the Robe to see it). Scene `Scene` still holds v2 for comparison.

## Confirmed (master)
_None._
