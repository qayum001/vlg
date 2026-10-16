# Status
Updated: 2026-10-16
Related: [overview](overview.md), [open questions](open-questions.md), [concept](../../../design/concept.md)

- Phase: tech prototype. Godot project exists (stress test only), no gameplay code yet.
- Done: genre, 2D/3D, camera, aiming, players, art approach (see `design/concept.md`).
- Blender MCPs: both load natively in Code sessions and work against the same Blender (official 9876, community 9877). Exception: official screenshot tools return black, so visuals go through community `look` (see [tooling](tooling.md)).
- Modeling paused (user, 2026-10-14): agent-made models were not what the user wants; Meshy looked good but is paid. Toolkit, models and art files were removed (code is in git history before the removal commit).
- Engine decided 2026-10-16: Godot 4.7 + C#, min spec 8 GB / 2 GB ([decision](../decisions/engine.md)).
- Godot project + horde stress test done 2026-10-16 ([stress test](../architecture/stress-test.md)), on dev, awaiting user test. Results in `design/engine.md` §7.
- Next: user picks default renderer; then real game systems (player, enemies, weapons, upgrades). Theme/setting still open.
