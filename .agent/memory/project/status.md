# Status
Updated: 2026-11-10
Related: [overview](overview.md), [open questions](open-questions.md), [concept](../../../design/concept.md), [domain](../../../design/domain.md), [run](../architecture/run.md), [hub](../architecture/hub.md), [skills](../architecture/skills.md), [assets](../architecture/assets.md), [Codex hook](codex-hooks.md)

- Phase: gameplay prototype. The first playable run and horde stress test are on master; performance results are in `design/engine.md` section 7.
- Confirmed on master: camera orbit/aiming, Lowkey's six-skill pool with branches and three ultimates, 25 items with shop/chest/credits, The Crossroads hub and persistent meta progression, generated icons/props/effects, five replacement item icons, release launchers, Pyromancer and saved hub hero selection. See `features.md` for authoritative feature states and manual checks.
- Tooling confirmed on master: automatic Codex instruction loading and its recovery fix, QuestionForms design interviews, and clarified review authorization / merge-status workflow.
- Domain and design source of truth: `design/concept.md`, `design/domain.md`, `design/heroes/lowkey.md`, `design/heroes/pyromancer.md`, `design/items.md`. Memory records implementation context, not duplicate designs.
- User supplies humanoid models; both heroes and The Fixer still use placeholders. Asset production details: [assets](../architecture/assets.md).
- Engine: Godot 4.7 + C#, Mobile renderer; minimum target 8 GB RAM / 2 GB VRAM. See [engine decision](../decisions/engine.md).
- Still deferred: additional enemy behaviors, elites, runes, storyline levels/objectives and broader content. The implemented portal starts an endless run; do not describe hub, skill branches, ultimates or loadout selection as queued.
- Balance remains provisional; no new content priority was selected during the rules audit.
