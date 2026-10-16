# Game domain

Status: grilling (round 2)
Updated: 2026-10-16
Scope: theme, core entities, build & evolution, run structure, meta-progression.
Base: `concept.md` (genre, camera, art, platforms are decided there).

## Decided

### Theme
- Not tied to one theme or culture. Enemies are common monsters many cultures and genres share: zombies, demons and similar.
- First theme can be picked at random and changed later.
- Tone: darker, with Dead Cells / Deadpool style humor. Low priority, not designed now.
- Lore delivery: item descriptions, level lore, NPCs on the map.

### Storylines and levels
- Content is split into storylines. Each storyline has its own levels.
- A level is passed when the player does something during the run (level objective).
- Storylines unlock items and heroes.
- Every level also has an infinite mode.
- An infinite-only level opens after a storyline is closed.
- Storylines carry the lore.

### Run structure
- A level runs as a loop of stages, repeated until the player dies (infinite) or the objective is done (story):
  1. Basic mobs, 2 min
  2. Horde, 1 min
  3. Chest: rare loot, choose 1 of 3
  4. Basic mobs, 2 min
  5. Shop: 10 items, paid with credits earned during the run
- Durations and counts are placeholders for tuning.
- Target: a run lasts about 30 min (about 6 loops). Difficulty must keep rising per loop.

### Build
- In-run power sources so far: chest picks, shop items.

## Tree
Open decisions. `needs:` = decided first.

### Theme
- T4 Hero fantasy: who the player is. needs: T7
- T5 Horde fantasy per storyline. needs: T8
- T6 Power source: what skills and items are in-world. needs: T7
- T7 Frame that ties storylines together
- T8 First storyline theme (placeholder)

### Core entities
- E1 Heroes: how many, how they differ. needs: T4, B1
- E2 Enemy roster: tiers (basic, horde, elite, boss), behavior types. needs: T5
- E3 Pickups: XP, credits, health, magnets, others. needs: B6
- E4 Map NPCs: role (lore only, objectives, trade). needs: R6

### Build & evolution
- B1 What a build is made of (skills, items, modifiers)
- B2 Slot limits and pick rules (reroll, banish, skip). needs: B1
- B3 What evolves and how. needs: B1
- B4 Synergy layer: elements, tags, set bonuses. needs: T6, B1
- B5 Aimed vs automatic split: which skills use camera aim. needs: B1
- B6 XP level-ups in addition to chest and shop

### Run structure
- R2 Map per level: size, bounds, layout. needs: T8
- R4 Bosses and elites: where they appear in the loop. needs: E2, R6
- R5 Credits: sources, carry over between runs or not. needs: B1
- R6 Level objective types
- R7 Loop scaling: what changes each loop (enemy HP, count, new types). needs: E2

### Meta-progression
- M1 Meta currency besides storyline unlocks: yes/no, what it buys
- M2 Permanent stat upgrades: yes/no, how strong. needs: M1
- M3 Unlock conditions in detail: which level unlocks what. needs: E1, B1, R6
- M4 Difficulty tiers after a storyline is closed
- M5 Infinite mode rewards: what the player gets. needs: M1

## Parked
(none yet)
