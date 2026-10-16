# Status
Updated: 2026-10-17
Related: [overview](overview.md), [open questions](open-questions.md), [concept](../../../design/concept.md), [domain](../../../design/domain.md), [run](../architecture/run.md)

- Phase: gameplay prototype. First playable slice on dev (2026-10-17), awaiting user test: hero, 4 auto skills (Bolt, Nova, Aura, Orbit) with MOBA leveling, walkers, XP gems, 5-stage loop (Chest/Shop = placeholder pause panels), HP + death + restart ([run](../architecture/run.md)).
- Done: genre, 2D/3D, camera, aiming, players, art approach (see `design/concept.md`).
- Game domain confirmed 2026-10-17 (`design/domain.md`): hub + storylines, stage loop, enemies, heroes, skills/branching, items, pickups, meta. Built via `grill-me` skill. Deferred items listed in the doc.
- Blender MCPs: both load natively in Code sessions and work against the same Blender (official 9876, community 9877). Exception: official screenshot tools return black, so visuals go through community `look` (see [tooling](tooling.md)).
- Modeling paused (user, 2026-10-14): agent-made models were not what the user wants; Meshy looked good but is paid. Toolkit, models and art files were removed (code is in git history before the removal commit).
- Engine decided 2026-10-16: Godot 4.7 + C#, min spec 8 GB / 2 GB ([decision](../decisions/engine.md)).
- Godot project + horde stress test done 2026-10-16 ([stress test](../architecture/stress-test.md)), on dev, awaiting user test. Results in `design/engine.md` §7.
- Default renderer Mobile (2026-10-16).
- Next (after user test of the slice): items (chest/shop content, 4 item types), more enemy behaviors (runner, brute, ranged, exploder, horde swarmers), elites, runes, objectives, loadout pick. Balance of run.json is placeholder: later loops get easier for a moving player (headless sim, 2026-10-17).
