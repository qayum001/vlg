# Status
Updated: 2026-10-18
Related: [overview](overview.md), [open questions](open-questions.md), [concept](../../../design/concept.md), [domain](../../../design/domain.md), [run](../architecture/run.md)

- Phase: gameplay prototype. First playable slice confirmed and merged to master (2026-10-17): hero, 4 placeholder skills with MOBA leveling, walkers, XP gems, 5-stage loop (Chest/Shop = placeholder pause panels), HP + death + restart ([run](../architecture/run.md)). On dev since 2026-10-18 the placeholder skills are replaced by Lowkey's kit.
- Done: genre, 2D/3D, camera, aiming, players, art approach (see `design/concept.md`).
- Game domain confirmed 2026-10-17 (`design/domain.md`): hub + storylines, stage loop, enemies, heroes, skills/branching, items, pickups, meta. Built via `grill-me` skill. Deferred items listed in the doc.
- Blender MCPs: both load natively in Code sessions and work against the same Blender (official 9876, community 9877). Exception: official screenshot tools return black, so visuals go through community `look` (see [tooling](tooling.md)).
- Modeling paused (user, 2026-10-14): agent-made models were not what the user wants; Meshy looked good but is paid. Toolkit, models and art files were removed (code is in git history before the removal commit). Since 2026-10-18 (R11): user models all humanoids; agent resumes the rest (items, props, VFX, non-humanoid monsters, ComfyUI icons).
- Content phase started 2026-10-18: first hero kit `design/heroes/lowkey.md` (bassist, phonk, beat-clock passive) + shared item pool `design/items.md` (25 items, mechanic skill tags). Both approved by the user 2026-10-18 ("keep as proposed").
- Implementation slices (2026-10-18): 1) base kit + beat clock + Drop (done, [skills](../architecture/skills.md)); 2) branches at skill level 5/10 (done); 3) ultimates (done); 4) items, credits, shop, chest; 5) assets (icons via ComfyUI, props, VFX). Hub (portals to levels, meta-upgrade NPC) queued after.
- Engine decided 2026-10-16: Godot 4.7 + C#, min spec 8 GB / 2 GB ([decision](../decisions/engine.md)).
- Godot project + horde stress test done 2026-10-16 ([stress test](../architecture/stress-test.md)), confirmed and merged to master 2026-10-17. Results in `design/engine.md` §7.
- Default renderer Mobile (2026-10-16).
- Next: kit slices 2-5 above, more enemy behaviors (runner, brute, ranged, exploder, horde swarmers), elites, runes, objectives, loadout pick. Balance of run.json is placeholder: later loops get easier for a moving player (headless sim, 2026-10-17).
