# Items: credits, shop, chest, inventory
Updated: 2026-10-18
Related: [run](run.md), [skills](skills.md), [items design](../../../design/items.md), [domain](../../../design/domain.md)

Purpose: the shared item pool in a run (domain.md Build, Shop and chest).

## Where
- Data: `game/data/items.json` → `ItemsConfig` (credit drop, shop prices/weights/reroll, chest options, slots, maxSockets, 25 `ItemDef`). Each item has exactly one of `modifier` (`ModifierDef`: fits, scale/add/chainAdd, hex, poison), `active` (`ActiveDef` → `ToSkillDef`), `usable` (`UsableDef`), `passive` (`PassiveDef`). Actives are checked by the shared `FormRules` (same as hero forms).
- Core (`src/Vlg.Core/Run/`): `Inventory` (credits, passive copies + `PassiveTotals`, actives/usables lists, sockets per skill, stash; implements `IFormModifier`), `Shop` (offers, rerolls, chest options), `UsableSystem`, `OwnedActives` (`ISkillLevels` for the active SkillSystem). RunSim owns them plus `Coins` (a second GemPool) and `Actives` (SkillSystem without beat, one slot per pool active, level 1 when owned).
- Godot: `ShopPanel` (offers, reroll, build: sockets/stash/slots), chest and socket picker via `ChoicePanel` (`ShopPanel.AskSocket`), `ItemText`, coin renderer (`GemRenderer.Coins`), HUD items block, smoke/singularity visuals.

## Rules
- Credits: each kill drops a coin with `creditDropChance`; coins use XP magnet rules; Gold Tooth multiplies collected value.
- Shop (stage Shop, full heal first): `size` distinct offers by rarity weight; skips actives/usables already owned or without a free slot; reroll cost = base + perLoop*(loop-1) + step*n, resets per shop. Passives and modifiers can be bought again.
- Chest: `chestOptions` distinct rares under the same skip rule; `TakeChest` grants and continues; `ContinueStage` throws while options exist (no skip). Empty chest → continue allowed.
- Sockets: 0 locked, 1 unlocked, +1 per branch level reached, max `maxSockets`. Fit = modifier fits ∩ current form tags; one copy per skill. After every level-up pick RunSim revalidates sockets (branch tag changes / fewer sockets → stash). The branch card warns which modifiers would move.
- Modifiers apply after branch resolution (SkillSystem re-resolves when `Inventory.Version` changes). Hex = per-source on-hit vuln (`EnemySwarm.SetHitVuln`, synced on inventory change); Plague Flask = zone on kills credited to that slot (`PoisonSource`).
- Passives: damage, move speed, regen, pickup, life steal (from `EnemySwarm.DamageDealt` per step), armor (contact damage multiplier), credits, cooldown (RateMultiplier for skills and actives), ledger (+damage per N kills).
- Usables: presses queue into the next Step (`RunSim.UseItem`); Rush (rate/move bonus then crash), Smoke (enemies steer to the throw point), Holy Water (interval zone, undead factor always applies: all current enemies count as undead), Singularity (pull by topping up push, then pop).
- Sources: usables 22, actives 30+slot, poison 40.

## Dev / testing
- Keys Q E R F G / LB RB X use items; F5 +100 credits. Launch args `--stage n` (jump to stage n of loop 1: 2 = chest, 4 = shop), `--credits n`, `--items id,id`.

## Extend
- New item → items.json (+ new numbers in its part's record if needed). New usable effect → `UsableKind` + `UsableSystem.Use/Update`. New passive stat → `PassiveDef` + `PassiveTotals` + where RunSim applies it.
