# Assets: icons, props, effects, audio
Updated: 2026-10-18
Related: [skills](skills.md), [items](items.md), [tooling](../project/tooling.md)

Purpose: how the agent-made assets are produced and used. All regenerable from scripts in `tools/`.

## Icons (2D, AI)
- `tools/icons/make_icons.py` (stdlib, talks to local ComfyUI SDXL base): 35 icons → `game/assets/icons/skills/<id>.png` (6 skills, 3 ultimates, `the_drop`) and `game/assets/icons/items/<id>.png` (25 items), 256x256, fixed seed per id, skips existing files unless ids are named. Style: hand-painted, black background; skills add the neon phonk palette. Ends with POST /free (VRAM).
- Bad results: tweak that id's prompt (or SEED_SALT), delete the PNG, rerun with the id. Prompts that drew multiple objects (skull bottle) were rewritten.
- Used via `Icons.Skill/Item` in ChoicePanel cards (level-up, branch, chest, socket picker, hub setlist), shop offers/build, HUD icon rows. License + Steam AI disclosure: `third-party.md`.

## Props (3D, procedural)
- `tools/models/make_props.py`, run headless: `blender.exe --background --factory-startup --python tools/models/make_props.py` (doesn't touch the user's open Blender; Blender MCP was down this session). Exports `game/assets/models/{speaker,subwoofer,vinyl,cowbell,boombox,toy_drone,mortar_shell}.glb` with Principled materials (emission → glTF).
- `ModelMeshes.Get(name)` pulls the first mesh for MultiMesh use (MaterialOverride null keeps the model materials). SkillVisuals: speakers vs subwoofer (form with CrowdScaling), vinyl scaled by radius, cowbell for hero strikes vs mortar shell for item strikes (source >= ActiveSource), boombox for hero drones vs toy drone for item drones.
- Humanoids (hero, NPCs, humanoid enemies) are never modeled by the agent (R11).

## Effects (shaders)
- `game/shaders/fx_ring.gdshader` (soft ring: rings, pulses, flashes, strike marks), `fx_beam.gdshader` (core + scrolling stripes), `fx_zone.gdshader` (pattern from INSTANCE_CUSTOM.x: 0 fuzz, 1 Voronoi cracks, 2 bubbles; aura discs use the uniform). All on flat quads (PlaneMesh 2x2 = radius 1), additive, COLOR = instance tint/alpha. Zones MultiMesh uses custom data (stride 20).

## Audio
- `tools/audio/make_beat_samples.py` (pure synthesis) → `game/assets/audio/beat/*.wav`; played by `BeatAudio`.
