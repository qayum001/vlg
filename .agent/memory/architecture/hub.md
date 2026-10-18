# Hub and meta-progression
Updated: 2026-10-18
Related: [run](run.md), [items](items.md), [skills](skills.md), [domain](../../../design/domain.md)

Purpose: The Crossroads hub (domain.md hub between worlds) and meta credits / upgrades that persist between runs.

## Where
- Data: `game/data/meta.json` → `MetaConfig` (meta orb drop chance/value, loop rewards, `upgrades` = `MetaUpgradeDef`: kind, perRank, maxRank, costBase + costStep*rank).
- Core: `Vlg.Core.Meta.MetaSave` (save JSON: meta credits, ranks, loadout, ultimate, best loop, runs; lenient parse), `MetaShop` (costs, buy, `Bonuses()` → `Run.MetaBonuses`). RunSim takes `MetaConfig? meta` + `MetaBonuses` (slots, damage, max HP, pickup, start credits), drops meta orbs (`MetaOrbs` GemPool), pays loop rewards when a loop completes (`MetaCreditsEarned`).
- Godot: `GameSession` (static; loads meta.json + `user://save.json` = `%APPDATA%\Godot\app_userdata\vlg\save.json`, `Persist()`), `HubGame` (scenes/hub.tscn), `Boot` picks hub / run / stress. RunGame reads loadout/ultimate from the save unless launch args override, banks once per run (`AddRun`) on hub/restart/quit/exit.

## Rules
- Boot: hub by default; `--run` or any run dev arg (`LaunchOptions.WantsRun`: loadout, skill-level, branch, ultimate, ultimate-level, stage, credits, items) opens the run; `--stress`/`--bench` the stress test.
- Hub: plaza radius 22 m (props cleared via `Arena(propClearRadius)`), E / gamepad A at a spot: Portal (Undead Town endless run), Setlist desk (4 distinct skills in order, slot 1 starts unlocked, then 1 ultimate; saved), The Fixer (meta upgrades; capsule placeholder, R11). F5 +100 meta credits (dev).
- Meta credits are kept on death (domain.md); dev runs started straight from launch args also bank into the real save.
- Decisions taken without user input (goal "implement everything"): single endless portal (no storyline levels/objectives exist yet), upgrades = slots + small capped stats + start credits, numbers placeholders.

## Extend
- New upgrade kind → `MetaKind` + `MetaShop.Bonuses` + where RunSim applies it. New hub spot → `HubGame.Add*` + `_spots`.
