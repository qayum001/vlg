# Game domain

Status: grilling (round 4)
Updated: 2026-10-16
Scope: theme, core entities, build & evolution, run structure, meta-progression.
Base: `concept.md` (genre, camera, art, platforms are decided there).

## Decided

### Theme
- Not tied to one theme or culture. Enemies are common monsters many cultures and genres share: zombies, demons and similar.
- Frame: a hub between worlds (crossroads / limbo). The hero picks a storyline there; NPCs live there. Each storyline can be any world or era. The hub can start as a menu screen.
- First storyline (placeholder, can change): undead plague. A cursed town with zombies, skeletons and ghouls.
- Tone: darker, with Dead Cells / Deadpool style humor. Low priority, not designed now.
- Lore delivery: item descriptions, level lore, NPCs on the map.

### Storylines and levels
- Content is split into storylines. Each storyline has its own levels.
- A level is passed when the player does something during the run (level objective).
- Level objectives come from a small set: kill a boss, destroy spawn portals, find relics, protect an NPC, survive N loops. Start with 2-3 types (boss, portals, relics).
- Storylines unlock items and heroes.
- Every level also has an infinite mode.
- Each closed storyline opens one infinite-only level.
- Storylines carry the lore.

### Run structure
- A level runs as a loop of stages, repeated until the player dies (infinite) or the objective is done (story):
  1. Basic mobs, 2 min
  2. Horde, 1 min
  3. Chest: rare loot, choose 1 of 3
  4. Basic mobs, 2 min
  5. Shop: 10 items, paid with credits earned during the run
- Durations and counts are placeholders for tuning. Shop size: compare 10 vs 6 in the prototype.
- Target: a run lasts about 30 min (about 6 loops). Difficulty must keep rising per loop.

### Heroes
- Heroes come from the storyline worlds. Closing a storyline unlocks a hero from that world (e.g. a plague doctor from the undead town).
- The starting hero lives in the hub.

### Build
- In-run power sources: XP level-ups, chest picks, shop items.
- Skills are tied to the hero. They fire on their own (a few are aimed with the camera).
- Hero level-up (kills drop XP): the player picks one skill to upgrade by one level. Each skill has its own growth per level (e.g. Blast: +1 projectile, +2% damage).
- Skill branching: every 5 skill levels the skill reaches a branch point and the player picks a form. Each branch changes how the skill looks and behaves, not only numbers.
- Item types:
  - Modifier item: goes into a skill socket, changes that skill (+1 projectile).
  - Active item: fires on its own, like a skill.
  - Usable item: pressed by the player, works for ~3 s.
  - Passive item: global stat or effect (+15% damage).
- Modifiers: each skill has sockets. A modifier fits only skills with a matching tag. Modifiers can be swapped at the shop.
- Caps: 5 active items, 5 usable items, counted separately. The caps are a meta-progression lever.
- Assumed (not confirmed): chest gives rare items of any type; shop sells all item types, common to rare; skills never come from chest or shop; modifiers go into hero skills only, not active items.
- Input note: 5 usables on gamepad = 4 shoulder/trigger buttons (reachable while both sticks are used for move + camera) + 1 face button. Steam Deck also has 4 back buttons.

## Tree
Open decisions. `needs:` = decided first.

### Theme
- T5 Horde fantasy for later storylines. needs: storyline list
- T6 Power source: what skills and items are in-world (magic, weapons, mixed)

### Core entities
- E1 Heroes: how they differ (skill set, stats, unique passive). needs: B14
- E2 Enemy roster: tiers (basic, horde, elite, boss), behavior types
- E3 Pickups: XP, credits, health, magnets, others
- E4 Map NPCs: role (lore only, side tasks, trade)

### Build & evolution
- B2 Sockets per skill, how they grow. needs: B14
- B4 Synergy layer: elements, tags, set bonuses. needs: T6
- B5 Aimed vs automatic split: which skills use camera aim
- B10 Pick rules: reroll, banish, skip in chest and shop
- B11 Usable items: what "~3 s" means, how they recharge
- B12 Slot caps: start values, how meta raises them
- B13 Passive items: capped or not
- B14 Hero skills: how many per hero, when each becomes available
- B15 Branch options: who or what decides the forms offered at a branch point
- B16 Branch structure and max skill level (full tree vs stacking layers). needs: B15

### Run structure
- R2 Map per level: size, bounds, layout
- R4 Bosses and elites: where they appear in the loop. needs: E2, R8
- R5 Credits: sources, carry over between runs or not
- R7 Loop scaling: what changes each loop (enemy HP, count, new types). needs: E2
- R8 How objectives fit the loop: when they show up, does the loop keep running

### Meta-progression
- M1 Meta currency besides storyline unlocks: yes/no, what it buys
- M2 Permanent stat upgrades: yes/no, how strong. needs: M1
- M3 Unlock conditions in detail: which level unlocks what. needs: E1
- M4 Difficulty tiers after a storyline is closed
- M5 Infinite mode rewards: what the player gets. needs: M1

## Parked
(none yet)
