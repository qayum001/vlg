# Features

Manual test guide.
- **Awaiting confirmation** — on `dev`, needs the user's manual test. Confirm → merged to `master`.
- **Confirmed** — merged to `master`.

Entry format:
```
### <Feature name>
Added: YYYY-MM-DD | Confirmed: YYYY-MM-DD
What: one or two lines.
How to test:
1. ...
Expected: ...
```

## Awaiting confirmation (dev)

### Run camera: lower and farther, off-screen spawning
Added: 2026-10-17 | Confirmed: -
What: the run camera tilts 40° below the horizon (was 55°) and sits 28 m from the hero (was 22 m). Values are in `game/data/run.json` → `camera`. Walkers now spawn just outside the visible screen area in every direction, following camera rotation and screen shape. The stress test camera is unchanged.
How to test:
1. Play from source: `Godot_v4.7.2-stable_mono_win64_console.exe --path game`.
2. The view is flatter and wider: you see much more ground ahead, and the hero and walkers look smaller.
3. Watch the screen edges for a minute while turning the camera with the mouse. Walkers walk in from outside the screen; none appear out of nothing inside the view. Walkers from the bottom edge (behind you) arrive sooner than those from the top.
4. Walk far in one direction: props and shadows cover the whole view, up to the top corners.
5. Optional: change `tiltDeg` / `distance` in `run.json` and restart. A tilt that shows the horizon (below fovDeg / 2 + 5) or a too-small `despawnRadius` stops the game with an error naming the key.
Expected: no errors in the console, no walkers popping in on screen, the stress test (`-- --stress`) still looks as before.

## Confirmed (master)

### Level run (first gameplay slice)
Added: 2026-10-17 | Confirmed: 2026-10-17
What: playable run from `design/domain.md`. Capsule hero moves relative to the camera. 4 auto skills (Bolt, Nova, Aura, Orbit) with MOBA leveling: unlock or +1 per level-up, skill level ≤ ceil(hero level / 3). Walkers chase and hurt the hero. Kills drop XP gems. Stage loop: Mobs 2 min → Horde 1 min → Chest → Mobs 2 min → Shop; each loop is harder. HP, death and restart. All numbers in `game/data/run.json` (placeholders).
How to test:
1. Play from source (repo root): `Godot_v4.7.2-stable_mono_win64_console.exe --path game`. The game run starts (mouse is captured).
2. WASD / left stick moves relative to the camera (W = away from it); mouse / right stick orbits. Bolt fires at the nearest walker by itself. Walkers die, drop green gems, and gems fly to you when you get close. The XP bar at the bottom fills.
3. First level-up: the game pauses and shows 3 cards: Unlock Nova / Aura / Orbit, each with its stats. No "Bolt 1 → 2" option yet (cap is 1 at hero levels 2–3). Pick with a click, keys 1–3, or gamepad (d-pad + A).
4. Hero level 4: upgrade options to level 2 appear. The HUD skill row shows each skill level and the current cap. Unlocked skills show up: Nova = ring of bolts, Aura = purple disc that pulses per tick, Orbit = orange blades circling you.
5. Top center shows `Loop N · Stage · m:ss`. F2 skips to the next stage. Horde = many more walkers. Chest = pause panel "items come later" → Continue. Shop = full heal + panel → Continue → Loop 2 (walkers tougher, faster spawns).
6. F3 gives a level instantly (one more pick). Use it to check the cap: a skill can reach level 5 at hero level 13, level 10 at hero level 28. Once all 4 skills are at 10 (after the hero level 40 pick), every later level-up offers only "+2% damage to all skills".
7. Stand in a crowd: the hero flashes red and HP drops. At 0 HP: "You died" panel with loop, time, level, kills → Restart run starts over.
8. Esc / Start: pause menu (Resume, Restart run, Quit). F1 hides the HUD, F11 fullscreen, F12 screenshot.
Expected: no errors in the console. At Bolt level 1, bolts only fire when a walker is within 14 m (+0.5 m per Bolt level). Walk far in one direction for a minute or more: ground, props, walkers, bolts and gems never disappear. Gems left more than 60 m behind vanish. Esc during a level-up card does nothing; pick first.

### Horde stress test
Added: 2026-10-16 | Confirmed: 2026-10-17
What: Godot 4.7 + C# test scene for measuring limits: GPU-animated placeholder enemies chasing the player, auto-fire projectiles, hit bursts, aura particles, spell lights, full-screen overdraw layers, 4 quality presets from `game/data/stress.json`. Play mode with live stats, plus an automated benchmark that ramps load and writes CSV. Results: `design/engine.md` → "Measured".
How to test:
1. Play from source: `Godot_v4.7.2-stable_mono_win64_console.exe --path game -- --stress` (repo root; without `--stress` the game run opens). Mouse is captured; Esc frees it.
2. WASD moves (relative to camera), mouse orbits the camera. Enemies chase you, projectiles kill them, orange bursts on hits/deaths.
3. Keys 1–4 switch presets (very_low, low, medium, high): render resolution, shadows and enemy detail change. `[` `]` change enemy count by 250, `,` `.` add/remove full-screen overdraw layers, `-` `=` change aura particles. HUD shows FPS, frame/GPU/sim times, draw calls, memory.
4. Run on the built-in Vega 8: add `--gpu-index 1` before `--path`. HUD first line shows the renderer (default `mobile`) and GPU name. Forward+ for comparison: add `--rendering-method forward_plus`.
5. Benchmark (release export, see `code-rules.md` → Commands): `powershell -ExecutionPolicy Bypass -File tools\bench.ps1 -GpuIndex 1`. Each run goes fullscreen, ramps, quits by itself; CSVs land in `build\bench\<date>\`.
Expected: steady motion, no errors in the console. Benchmark CSVs have one row per step and end with `# best_passing=<value>`.
