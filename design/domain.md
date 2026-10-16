# Game domain

Status: grilling (round 11, final)
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
- Objectives run inside the loop; the loop never pauses. Each objective type has its own timing (e.g. portals and relics exist from the start, the boss arrives after loop N). Completing the objective ends the level with a short victory moment.
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
- Horde stage: higher density of basic mobs plus horde-only enemy types (small, fast, weak swarmers).
- Map: endless ground in every direction. Objectives (portals, relics, NPCs) spawn near the player. Level identity comes from ground, props, enemy set, objective and lore, not layout.
  - Implication: off-screen indicators are needed for objectives (see `concept.md` orientation aids).

### Enemies
- Starting behaviors (first storyline names): walker (zombie), runner (ghoul), brute (abomination), ranged (spitter), exploder (bloater). Plus horde-only swarmers.
- Behaviors must be extensible: new enemy types with new mechanics get added later without rework.
- Void vs tougher enemies: kills basic and horde mobs; elites and bosses take % max HP damage instead (placeholder: 30% elites, 10% bosses).
- Elites: bigger, glowing versions of normal enemies with 1 random affix (fast, shielded, splits on death, leaves poison). A few per mob stage from loop 2. Drop a big credit pile. Affixes are data, new ones added later.
- Bosses: in boss-objective levels the boss comes after loop N (e.g. 3). Infinite mode: a boss every 3 loops, each stronger. Other objective levels have no boss.
- Loop scaling: each loop enemy HP and damage +X%, spawn rate +Y%, and one new enemy type or elite affix joins. Numbers are data per level.

### NPCs
- Map NPCs spawn near the player during a run. They give lore plus an optional side task with a reward (e.g. "kill 3 elites in 60 s" for a free item or credits). Side tasks are data.

### Pickups and currencies
- Pickups: XP, credits, meta credits, runes, health (rare kill drop).
- Credits: in-run only, never converted to meta credits. Sources: kills (small), elites (big piles), NPC tasks.
- Meta credits: picked up during the run as their own drop (e.g. a kill drops one with 1% chance), plus rewards for objectives and loops survived.
- Runes: one-time pickup effect with a duration: magnet, haste, damage boost, heal, more later.
- Rune spawn: pseudo-random. Every tick there is a chance to spawn a rune near the player; the chance grows each tick and resets to the base after a spawn.
  - Function (Dota's pseudo-random distribution): `P(n) = min(1, C * n)`, n = ticks since the last rune. C comes from the target average interval.
  - Tick 1 s. C per target average: 30 s -> 0.001708, 45 s -> 0.000764, 60 s -> 0.000432, 90 s -> 0.000193.
  - Spread for 60 s: 80% of runes come 22-103 s after the previous one, 99% within 145 s. A flat 1/60 chance would give 7-138 s for the same 80%, and 1% of waits over 275 s.
- Healing: heal rune, HP regen and life steal as passive item stats, rare health drops from kills, full heal at the shop stage. Amounts are data.

### Shop and chest
- Shop: reroll for credits. No lock.
- Chest: 3 options, pick one. No reroll, no skip.

### Meta-progression
- One meta currency (meta credits). Spent in the hub on slot unlocks (2 -> 5 active and usable), stat upgrades and other upgrades.
- Permanent stat upgrades exist, small and capped (e.g. +1% damage per rank, max 10 ranks). Values are data. Most meta value comes from unlocks (slots, heroes, items).
- Infinite mode rewards: meta credits per loop survived, more for each loop; a best-loop record per level.
- Meta credits are kept fully on death and on win.
- Storyline structure: about 5 levels, unlocked one by one. Each cleared level gives a fixed unlock (an item enters the chest and shop pools, or a lore entry). Closing the storyline unlocks its hero and its infinite-only level.

### Heroes
- Heroes come from the storyline worlds. Closing a storyline unlocks a hero from that world (e.g. a plague doctor from the undead town).
- The starting hero lives in the hub.
- Each hero has: own pool of 6 skills + 3 ultimates, base stats (HP, speed, pickup radius), one unique passive (e.g. plague doctor: poison spreads to nearby enemies on kill).

### Build
- In-run power sources: XP level-ups, chest picks, shop items.
- Skills are tied to the hero. They fire on their own (a few are aimed with the camera).
- Loadout: each hero has a pool of 6 skills and 3 ultimates. Before a run the player picks 4 skills + 1 ultimate.
- Hero level-up (kills drop XP): the player upgrades one skill by one level, or unlocks a loadout skill not unlocked yet. Unlocking costs one level-up and puts the skill at level 1 (MOBA style). Each skill has its own growth per level (e.g. Blast: +1 projectile, +2% damage).
- Run start: hero level 1, first loadout skill already at level 1 (free).
- MOBA-like level rule: a skill's level can't be higher than ceil(hero level / 3). Skill level 5 is possible at hero level 13, level 10 at hero level 28. Hero levels 2-3 can only unlock new skills. The divisor (3) is a data value.
- Max skill level 10. Branch points at skill level 5 and 10. A full build is ~43 level-ups (4 skills x 10 + 3 ultimate levels).
- Skill branching: at a branch point the player picks a new state for the skill. Options are designer-made and fixed per branch point. Socketed modifiers don't decide which options are offered.
  - Some options change the concept, some deepen the current form. Example, Lightning: base = chain lightning hits 10 enemies. Level 5 options: becomes AoE / stays chain, 10 -> 30 enemies per cast / becomes a damage aura.
  - Not every skill has concept-changing branches.
- Branching is a full tree: each choice opens its own next options. Skills are hero-tied, so the count per hero stays manageable. Worst case with 3 options per point: 3 + 9 = 12 forms per skill; "deepen" options reuse VFX and are cheaper.
- Ultimate: pressed by the player. Unlocks at hero level 10, upgrades at 20 and 30 (max level 3). Each level is a bigger version; no branches.
- Ultimate charge differs per ultimate. First mechanics (more later):
  - Rage (one hero): charged by kills, 10K kills per charge. The number is data, tune it in the prototype.
  - Void: 1 min cooldown; on press, kills enemies on the whole map.
- After every skill and the ultimate are maxed (infinite mode): each level-up gives a small stat bonus (e.g. +2% damage), no limit.
- Item types:
  - Modifier item: goes into a skill socket, changes that skill (+1 projectile).
  - Active item: fires on its own, like a skill.
  - Usable item: pressed by the player, effect lasts ~3 s. Cooldown (20-60 s) starts on press. Reusable all run.
  - Passive item: global stat or effect (+15% damage).
- Modifiers: each skill has sockets. A modifier fits only skills with a matching tag. Modifiers can be swapped at the shop.
- Sockets: 1 when the skill unlocks, +1 at each branch point (max 3 at skill level 10). Late build: 12 modifier slots over 4 skills.
- Stash: modifiers that stop fitting after a branch changes a skill's tags go to a stash; the branch screen warns first. Re-socket at the next shop. New modifiers with no fitting socket also go to the stash.
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
- E6 Rune rules: target interval, how many on the map, lifetime

### Build & evolution
Skill system closed by the user (2026-10-16). Left for when real skills are designed:
- B4 Synergy layer: elements, tags, set bonuses. needs: T6
- B5 Aimed vs automatic split: which skills use camera aim

### Run structure
- B10 Shop reroll cost

### Meta-progression
- M4 Difficulty tiers after a storyline is closed

## Parked
- Leaderboards for infinite mode best loops.
- Cosmetic rewards for infinite mode milestone loops.
