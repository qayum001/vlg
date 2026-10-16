# Game concept

Status: brainstorming
Updated: 2026-10-16

## Decided
- **Genre:** survivors-like (Vampire Survivors style): automatic attacks, large hordes, level-up upgrade picks, run-based. The theme is not decided.
- **Dimension:** 3D.
- **Camera:** angled top-down perspective that follows the player (reference: Soulstone Survivors). Free orbit around the vertical axis through the player (yaw only). Mouse movement rotates the camera (right stick on gamepad). Tilt and distance are fixed and tuned by us, not by the player.
- **Aiming:** most weapons are automatic. Aimed weapons fire in the camera's facing direction, so rotating the camera is aiming. No aiming cursor.
- **Players:** single-player.
- **Visual direction:** colorful, with heavy particles and VFX. Stylized low-poly models; shaders and effects carry the look.
- **Art:** all assets are made by the agent (Blender via MCP and scripts) and reviewed/adjusted by the user. No external asset packs or AI 3D generators.
- **Platforms:** Windows + Steam Deck (Windows build through Proton). Linux/macOS native not planned.
- **Engine:** Godot 4.7 + C# (why: `engine.md`).
- **Min spec:** 8 GB RAM, 2 GB VRAM (GTX 750 Ti / GTX 1050 / Radeon Vega 8 class, about Steam Deck level). Target 60 fps on the low preset, 1080p output, render scale may drop on min spec. A "very low" preset tries to run on 4 GB / 1 GB but is not promised.

## Implications
- Movement is relative to the camera (W = away from the camera). Directions change when the camera orbits.
- Tall objects can hide enemies in some camera angles: keep the environment low, or fade objects that block the view.
- Enemies spawn outside the camera view, so spawning depends on the current camera rotation.
- Hundreds of 3D enemies plus heavy VFX need GPU instancing and pooling. This affects the engine choice.
- Steam asks developers to disclose AI-generated content that ships in the game. The store page will need this.

## Open
- Theme, setting, story.
- Controls: mouse sensitivity and smoothing for orbit; cursor shown only in menus and upgrade picks?
- Orientation aids during free orbit: compass, minimap, off-screen indicators?
- Run length, meta-progression, content scope, price.
