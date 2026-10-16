# Game domain grilling
Date: 2026-10-17
Type: decision
Commits: fc230b2, 9445631, b0c33a3, a60fd91, 9604550, bf4197d, 65bcdb9, 09ebb84, 93d83f4, 13bbccc, daf7627, e5841df, 812693c

## Summary
User asked to brainstorm the game domain (entities, evolution, items, skills, mechanics) using a "grill-me" skill. Added the skill, ran 11 rounds, user confirmed the summary. Source of truth: `design/domain.md`.

## Details
- No grill-me skill in the user's account. Wrote `.claude/skills/grill-me/SKILL.md`, adapted from `grilling` in github.com/mattpocock/skills (MIT): rounds over the decision-tree frontier, numbered questions with a recommended answer, decisions committed after each round.
- Clarify answers: project skill; theme first; scope = entities, build/evolution, run structure, meta; decide as we go.
- Key decisions: hub between worlds + storylines (~5 objective levels, infinite mode, infinite-only level per storyline); first storyline undead plague; 5-stage loop (mobs/horde/chest/mobs/shop), ~30 min target, endless map.
- Skills: hero-tied, pool 6 + 3 ultimates, loadout 4 + 1; level-up unlocks or raises one skill; cap ceil(hero level / 3); max 10, branch points at 5 and 10 as a full tree; sockets 1 + 1 per branch.
- Items: modifier, active (auto), usable (pressed, 3 s), passive; slots 2 -> 5 via meta.
- Runes spawn by Dota's pseudo-random distribution, P(n) = C * n; C solved numerically (60 s average -> 0.000432), check reproduced Dota's 25% constant 0.084744.

## Problems
- User's answers often replaced or merged the offered options (usable items, ultimate charge per ultimate, healing from all sources). Readings were restated back each round for correction.
- Healing from 4 sources and 10-item shop flagged as balance/UX risks; kept as data to tune.

## Results
- Tests: none (design only).

## Next
- Deferred in `domain.md`: later storylines, in-world power source, synergy/elements, aimed skill split, humor.
- Start real game systems from the domain doc.
