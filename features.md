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
### Blender modeling toolkit + mage v4
Added: 2026-10-13
What: `tools/blender/vlg3d` toolkit (geometry, rig-ready base body, clothing from body shells, face decals, silhouette matching vs reference images) and the mage rebuilt on it (`tools/blender/models/mage.py`, now v4 modeled toward generated reference 505).
How to test:
1. From the repo root run: `"C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe" --background --factory-startup --python-exit-code 1 --python tools/blender/run_tests.py`
2. Open `art/characters/mage.blend`, switch the scene (top right) to `mage_v4`.
3. Optional rebuild, in Blender's Python console: `import sys; sys.path.insert(0, r"C:\Projects\Games\vlg\tools\blender"); import models.mage as m; m.build("MageV4", "mage4_")`
Expected: step 1 ends with `OK` (39 tests). Step 2 shows a flat-shaded low-poly mage in T-pose (~6.4k tris): black hat with a kinked tip, long yellow hair, green eyes, black dress with a belt and gold amulet, long purple open coat, purple bell sleeves, black boots, staff with a cyan crystal in her right fist. A full body sits under the clothes. Scenes `Scene` (v2) and `mage_v3` stay for comparison.

## Confirmed (master)
_None._
