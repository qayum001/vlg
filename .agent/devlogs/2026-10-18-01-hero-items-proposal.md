# First hero kit + item pool proposal
Date: 2026-10-18
Type: decision
Commits: b3b0e56, 0e2a9bf

## Summary
User started the content phase: items, skills, later a playable hub menu. First step = design proposals for the first hero kit and a shared item pool; user reviews keep/adjust before any code or assets.

## Details
- User request: hero = modern-era girl bassist, modern phonk (not rock/metal). Agent designs her skills. Items + skills need assets (Blender, ComfyUI). Hub = playable zone with level portals and a meta-upgrade NPC, queued after skills/items.
- Clarify answers (all recommended): beat clock, automatic (skills fire on the beat, periodic Drop buff = passive); power source sound + gear with phonk look; items mixed worlds, ~24; humanoid enemies count as human-like (user models them).
- New rules: R11 user models hero, NPCs, all humanoids (agent: non-humanoid monsters, items, props, VFX, icons); R12 agent may launch ComfyUI and install nodes/models without asking (registry / official sources only).
- `design/concept.md` art line updated to the R11 split.
- `design/heroes/lowkey.md`: Mira "Lowkey", 6 skills (Cowbell, Low End, Speaker Stack, Distortion, Slap, Vinyl) with L5 branches (3 each) and L10 one-liners (2 each), 3 ultimates (Sub Quake cooldown, Encore kills, Bass Cannon drops), passive The Drop at 130 BPM.
- `design/items.md`: 9 mechanic skill tags, 25 items (8 modifiers, 4 active, 4 usable, 9 passive), rarity/price placeholders, stacking proposal, fit check vs her skills.

## Problems
- Blender MCP (`blender`) timed out and `agent_status` failed to connect at session start. Not needed for this step.

## Next
- User reviews both docs; apply keep/adjust.
- Then implementation via feature-pipeline (beat clock, new skill kinds, items, chest/shop content), then assets (icons in ComfyUI, props in Blender, VFX in Godot).
- Open: soundtrack at the beat tempo (placeholder generated loop), chest pool while rares are few, "undead" enemy tag.
