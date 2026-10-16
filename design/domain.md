# Game domain

Status: grilling (round 6)
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
- Loadout: each hero has a pool of 6 skills and 3 ultimates. Before a run the player picks 4 skills + 1 ultimate.
- Hero level-up (kills drop XP): the player upgrades one skill by one level, or unlocks a loadout skill not unlocked yet. Unlocking costs one level-up and puts the skill at level 1 (MOBA style). Each skill has its own growth per level (e.g. Blast: +1 projectile, +2% damage).
- MOBA-like level rules: the player can't max one skill only. Base: Dota's rule, adjusted for our higher max skill level.
- Max skill level 10. Branch points at skill level 5 and 10.
- Skill branching: at a branch point the player picks a new state for the skill. Options are designer-made and fixed per branch point. Socketed modifiers don't decide which options are offered.
  - Some options change the concept, some deepen the current form. Example, Lightning: base = chain lightning hits 10 enemies. Level 5 options: becomes AoE / stays chain, 10 -> 30 enemies per cast / becomes a damage aura.
  - Not every skill has concept-changing branches.
- Branching is a full tree: each choice opens its own next options. Skills are hero-tied, so the count per hero stays manageable. Worst case with 3 options per point: 3 + 9 = 12 forms per skill; "deepen" options reuse VFX and are cheaper.
- Ultimate: pressed by the player.
- After every skill and the ultimate are maxed (infinite mode): each level-up gives a small stat bonus (e.g. +2% damage), no limit.
- Item types:
  - Modifier item: goes into a skill socket, changes that skill (+1 projectile).
  - Active item: fires on its own, like a skill.
  - Usable item: pressed by the player, effect lasts ~3 s. Cooldown (20-60 s) starts on press. Reusable all run.
  - Passive item: global stat or effect (+15% damage).
- Modifiers: each skill has sockets. A modifier fits only skills with a matching tag. Modifiers can be swapped at the shop.
- Caps: active and usable items are counted separately. Start with 2 active + 2 usable slots; meta-progression unlocks +1 at a time, up to 5 + 5.
- Passive items: no cap.
- Assumed (not confirmed): chest gives rare items of any type; shop sells all item types, common to rare; skills never come from chest or shop; modifiers go into hero skills only, not active items.
- Input note: 5 usables on gamepad = 4 shoulder/trigger buttons (reachable while both sticks are used for move + camera) + 1 face button. Steam Deck also has 4 back buttons.

## Tree
Open decisions. `needs:` = decided first.

### Theme
- T5 Horde fantasy for later storylines. needs: storyline list
- T6 Power source: what skills and items are in-world (magic, weapons, mixed)

### Core entities
- E1 Heroes: how they differ besides the skill pool (stats, unique passive)
- E2 Enemy roster: tiers (basic, horde, elite, boss), behavior types
- E3 Pickups: XP, credits, health, magnets, others
- E4 Map NPCs: role (lore only, side tasks, trade)

### Build & evolution
- B2 Sockets per skill, how they grow
- B4 Synergy layer: elements, tags, set bonuses. needs: T6
- B5 Aimed vs automatic split: which skills use camera aim
- B10 Pick rules: reroll, banish, skip in chest and shop
- B17 MOBA rule: exact rule for max skill level 10
- B19 Ultimate charge: kills, time, damage
- B21 Ultimate unlock and leveling
- B22 Modifiers that stop fitting after a branch changes a skill's tags
- B23 Run start state: which skill is unlocked at hero level 1

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
