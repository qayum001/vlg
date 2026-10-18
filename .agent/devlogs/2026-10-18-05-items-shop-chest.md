# Items, credits, shop and chest
Date: 2026-10-18
Type: feature
Commits: 3625b4f

## Summary
Slice 4: the approved 25-item pool with credits, shop, chest, sockets/stash, active items, usables and passives.

## Requirements
- `design/items.md` approved "keep as proposed" (pool, tags, rarity prices, stacking proposal), domain.md Shop and chest + Build (sockets, stash, slots 2 + 2, modifiers swapped at the shop, chest no skip/no reroll).

## Details
- Data `game/data/items.json` → `ItemsConfig` / `ItemDef` (modifier/active/usable/passive parts), shared `FormRules` for active validation; new stat `blast`, new kind `Chain`, base `flags` on SkillDef.
- `Inventory` (IFormModifier), `Shop` (offers, reroll, chest options), `UsableSystem`, `OwnedActives` (ISkillLevels); SkillSystem now takes `ISkillLevels` + optional modifier.
- RunSim: coins pool, passives applied (damage, speed, regen, life steal, armor, credits, cooldown, ledger), hex via per-source on-hit vuln, poison puddles, queued usable presses, shop/chest API, socket revalidation after level-up picks, dev jumps/grants.
- Godot: ShopPanel, chest/socket pickers, branch-card stash warning, HUD items block, coins, smoke/singularity visuals, item drones, item zone colors, sounds. Dev args `--stage`, `--credits`, `--items`; F5 credits.
- design/items.md "Open" turned into recorded decisions (stacking, rare-only chest, all current enemies undead, Copper Coil rule).

## Problems
- Old chest test assumed skipping; changed to the no-skip rule.
- Probe for "0 kills" in a screenshot: not a bug (seed 3 spawns far; actives verified firing headless).

## Results
- Tests: `dotnet test --project tests/Vlg.Core.Tests/Vlg.Core.Tests.csproj` → 257 passed.
- Smoke: shop (`--stage 4 --credits 300`), chest (`--stage 2`), items run (`--items ...`) windowed with screenshots, no errors.

## Next
- Assets (icons, props, VFX), hub/menu. Balance is untested (placeholders).
