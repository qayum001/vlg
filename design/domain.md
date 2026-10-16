# Game domain

Status: grilling (round 3)
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

### Build
- In-run power sources: XP level-ups (kills drop XP, each level-up = pick 1 of 3 small upgrades), chest picks, shop items.
- Skills attack on their own (a few are aimed with the camera). Skills take modifiers.
- Item types:
  - Modifier item: changes a skill (+1 projectile).
  - Active item: works like a skill.
  - Passive item: global stat or effect (+15% damage).

## Tree
Open decisions. `needs:` = decided first.

### Theme
- T4 Hero fantasy: who the heroes are
- T5 Horde fantasy for later storylines. needs: storyline list
- T6 Power source: what skills and items are in-world (magic, weapons, mixed). needs: T4

### Core entities
- E1 Heroes: how many, how they differ (start skill, stats, unique passive). needs: T4
- E2 Enemy roster: tiers (basic, horde, elite, boss), behavior types
- E3 Pickups: XP, credits, health, magnets, others
- E4 Map NPCs: role (lore only, side tasks, trade)

### Build & evolution
- B2 Slot limits: skills, active items, passive items, sockets per skill. needs: B8, B9
- B3 What evolves and how. needs: B9
- B4 Synergy layer: elements, tags, set bonuses. needs: T6, B9
- B5 Aimed vs automatic split: which skills use camera aim
- B7 Sources: what level-ups, chests and the shop each offer
- B8 Active item vs skill: what makes them different
- B9 Modifier rules: sockets or automatic, tag compatibility, swapping
- B10 Pick rules: reroll, banish, skip. needs: B7

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
