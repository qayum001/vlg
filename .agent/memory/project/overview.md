# Overview
Updated: 2026-10-17
Related: [status](status.md), [open questions](open-questions.md), [concept](../../../design/concept.md), [domain](../../../design/domain.md), [engine](../decisions/engine.md)

- vlg: new game, goal: publish on Steam. "vlg" is the repo/folder name; real title unknown.
- Concept (source of truth: `design/concept.md`): 3D survivors-like, angled top-down camera with free yaw orbit, single-player, colorful VFX-heavy low-poly look.
- Domain (source of truth: `design/domain.md`): hub between worlds, storylines of ~5 objective levels + infinite mode, looped stages (mobs/horde/chest/mobs/shop) on endless ground, hero-tied skills with MOBA-style leveling and branch trees, 4 item types, meta credits.
- User thinks in MOBA (Dota) terms; hasn't played Vampire Survivors. Answers design questions with own variants, often merging options.
- All art made by the agent (Blender), user reviews. No external assets or AI 3D generators.
- Engine/language: Godot 4.7 + C# ([decision](../decisions/engine.md)).
- Platforms: Windows + Steam Deck. Min spec: 8 GB RAM / 2 GB VRAM (≈ Deck), 60 fps low preset.
- Repo: `C:\Projects\Games\vlg`, local git only, branches `master` + `dev`. `Assests/` (sic) = user's local Blender files (e.g. HeroModel.blend), git-ignored by user choice (2026-10-17); hero in game stays a capsule.
- User writes in English (non-native); short direct questions work well. User often answers with free text instead of offered options — read it carefully.
- User has used neither Unity nor Godot (2026-10-16).
