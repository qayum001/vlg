# Features

Manual test guide.
- **Awaiting confirmation** — implemented on `dev`, awaiting user approval.
- **Approved for merge** — the user authorized the merge; the implementation has not reached `master` yet.
- **Confirmed** — the implementation has successfully reached `master`. Historical entries can also be mirrored on `dev`.

Entry format:
```
### <Feature name>
Added: YYYY-MM-DD | Approved for merge: YYYY-MM-DD or - | Confirmed: YYYY-MM-DD or -
What: one or two lines.
How to test:
1. ...
Expected: ...
```

## Awaiting confirmation (dev)

### QuestionForms design interviews
Added: 2026-11-10 | Confirmed: -
What: the project `grill-me` workflow uses the installed QuestionForms skill to collect browser answers and records agreed game decisions in `design/`. Agent setup and connection details are linked from `AGENTS.md` and the memory index.
How to test:
1. With the existing local QuestionForms service running, invoke `$grill-me` with a vlg design topic.
2. Open the returned session link and submit a round, including a written answer. Check that the agent reads the submission and records the agreed decisions on `dev`.
3. Follow-up rounds should appear under that same session link and ask only decisions whose prerequisites are settled.
4. Confirm the final summary; the agent should close that session and update project memory and the dev log.
Expected: questions and answers use QuestionForms through connected MCP tools or the installed Python bridge. Pending forms remain unanswered until submitted; no extra service instance starts. Full interview testing awaits a user-provided topic.

### Automatic Codex project rules
Added: 2026-11-10 | Confirmed: -
What: a read-only Codex hook supplies current dev instructions at session start/resume/compaction and each user prompt, including sessions in older linked worktrees. Installed locally through `tools/codex/install-project-rules.ps1`; one-time hook trust is required.
How to test:
1. Review and trust both vlg loader entries in Codex Settings > Hooks, then open a new vlg chat with an ordinary project request.
2. Inspect hook activity: the SessionStart and UserPromptSubmit handlers should supply the complete current AGENTS.md, rules.md, code-rules.md and memory index without a reminder in the prompt.
3. Start a chat in an unrelated repository: the loader supplies no vlg instructions.
Expected: project instructions arrive automatically. The loader reports missing/oversized rules instead of truncating them. Setup, limitations and removal: `tools/codex/README.md`.

## Approved for merge (dev)

None.

## Confirmed (master)

### Pyromancer and hub hero selection
Added: 2026-11-08 | Confirmed: 2026-11-10
What: Pyromancer is available during development alongside Lowkey. Six skills, three ultimates, Ember Heart kill healing, full rank 5/10 branch trees, second-based cooldowns, and fire effects. Hero selection is saved and uses the existing setlist, item and progression systems.
How to test:
1. Launch the normal hub. Press H or interact with the Change hero stand near the starting point. Pick Pyromancer; the capsule turns orange and the HUD shows his name. Use Setlist to choose four of his six skills and one of three ultimates, then enter the portal.
2. Magic Bolts are blue. Unlock Fireball, Flame Aura and Flame Orbit: fireballs explode, an orange aura pulses and flames orbit the capsule. Try Flame Nova and Meteor through Setlist: an expanding ring pushes enemies; marked impacts precede falling meteors.
3. Use F3 to gain levels. Rank 5 and 10 skills offer named branch paths. Hero levels 10/20/30 offer ultimate rank picks. F4 fills ultimate charge; Space / Y casts Inferno, Meteor Shower or Phoenix. Check burning ground, repeated delayed meteors and Phoenix's capped healing.
4. Return to the hub, change to Lowkey and enter the portal. Lowkey keeps his existing music, Drop and abilities. Restart the game: the selected hero persists. Changing hero resets the setlist to that hero's defaults; selecting the same hero preserves it.
5. Direct development launch: `-- --hero pyromancer --skill-level 5 --branch 0,0,0,0 --ultimate phoenix --ultimate-level 3`. Pause: spell effects, charge and stage time stop. Pyromancer has no beat HUD or beat soundtrack.
Expected: both heroes are available, setlists never cross between heroes, no missing-resource errors, and the hero remains a capsule placeholder (R11). Balance is provisional. Full kit and ability-authoring notes: `design/heroes/pyromancer.md`.

### Release launchers
Added: 2026-11-08 | Confirmed: 2026-11-10
What: two batch files in `launch/`: build and launch a standalone Windows release, or launch the newest completed release without rebuilding. Both use normal hub startup with no dev/debug launch arguments.
How to test:
1. Double-click `launch/build-and-launch.bat`: it exports the current code, then opens the hub.
2. Close the game and double-click `launch/launch-latest.bat`: the same completed build opens without importing or compiling.
3. Keep a game open and run the build launcher again: it creates a separate dated build and leaves the running game intact.
Expected: successful launches use `build/release/<timestamp>/vlg.exe`. Failed exports do not replace the latest completed build. If no completed build exists, the latest launcher tells you to run the build launcher.

### Five item icons: painterly 3D text-to-image
Added: 2026-11-08 | Confirmed: 2026-11-10
What: Plague Flask, Clockwork Heart, Copper Coil, Grim Ledger and Pocket Singularity now use Arcane-inspired painterly 3D icons generated from their written descriptions with OpenAI's built-in image tool. No reference images were supplied. Native generated resolution is retained; no resizing.
How to test:
1. Launch the dev game with `-- --run --stage 4 --items plague_flask,clockwork_heart,copper_coil,grim_ledger,pocket_singularity`.
2. Inspect the five item icons in the shop build/stash/socket panels, then Continue and inspect the HUD. Inspect their cards when offered in a chest or shop.
Expected: each item shows the correct new image, without clipping, stretching or missing textures. All five PNGs are 1254 x 1254. Prompts and source descriptions: `tools/icons/text2image-prompts-2026-11-08.json`.

### Hub, meta-progression, icons, props, effects
Added: 2026-10-18 | Confirmed: 2026-11-10
What: the game opens in The Crossroads hub. Portal = start the endless Undead Town run. Setlist desk = pick 4 skills + 1 ultimate (saved). The Fixer (capsule placeholder) = meta upgrades: slots, damage, HP, pickup, start credits. Runs drop rare violet meta orbs and pay meta credits per finished loop, kept on death (save: `%APPDATA%\Godotpp_userdatalg\save.json`). Also: 35 icons on cards, shop and HUD; 3D props (speaker, subwoofer, vinyl, cowbell, boombox, toy drone, mortar shell); new ring/beam/zone effects.
How to test:
1. Play from source without args: the hub opens. Walk to The Fixer, press E, buy with F5 meta credits. Walk to Setlist, press E, pick 4 skills and an ultimate. Walk into the portal, press E: the run starts with that setlist.
2. In the run, Esc → "Back to hub": meta credits earned are added. The death panel offers the same.
3. Level-up cards, chest, shop and HUD show icons. Speakers, records, falling cowbells and drones are models; rings, beams and ground zones glow.
Expected: no errors; upgrades and setlist survive a restart of the game.

### Items: credits, shop, chest, 25 items
Added: 2026-10-18 | Confirmed: 2026-11-10
What: the item pool from `design/items.md` is in the game (`game/data/items.json`):
- **Credits:** some kills drop gold coins, picked up like XP. The count is on the HUD, bottom right.
- **Shop** (after each loop, full heal): 10 offers, Buy, Reroll (costs more each time, resets each shop), and your build. Modifiers go into skill sockets: 1 per unlocked skill, +1 at skill levels 5 and 10. A modifier only fits a skill that shares one of its tags. Ones that don't fit wait in the stash.
- **Chest** (after the horde): pick 1 of 3 rare items. It can't be skipped.
- **Items:** active items fire on their own. Usable items are bound to Q / E (gamepad LB / RB). Passives stack when bought again.
How to test:
1. Shop right away: `-- --stage 4 --credits 300 --skill-level 5 --branch 0,0,0,0`. You see 10 offers with price, rarity color, type, effect and flavor line. Buy a passive: credits go down, it shows under "Passive", and the offer says SOLD. Buy a modifier: a picker asks which skill to put it in, listing only skills it fits that have a free socket. Clicking a socketed modifier moves it to the stash; clicking a stash item asks again. Reroll: new offers, and the price goes up by 5. Continue: back to the run.
2. Chest: `-- --stage 2`. 3 rare items. Pick one; a modifier asks for a socket, then the run goes on.
3. Items in play: `-- --items tesla_backpack,toy_drone,energy_drink,holy_water --skill-level 3`. Expect: a cyan drone shooting next to you, lightning chains, Q = Energy Drink (faster skills, then a short slow crash), E = Holy Water (a yellow burning pool where the camera looks). The HUD lists usables with their cooldowns and the active items. Old Mortar and Haunted Lantern: `-- --items old_mortar,haunted_lantern` (only 2 actives fit at once).
4. Other usables: `-- --items smoke_bomb,pocket_singularity`. Q: a grey cloud stays where you threw it and walkers go there instead of chasing you. E: a dark orb ahead pulls walkers in for 3 s, then pops.
5. Normal play: kill walkers, gather coins, F2 to the shop and spend them. F5 gives +100 credits.
6. Branch warning: socket Hex Needle into Low End (`--items hex_needle --loadout low_end,cowbell --skill-level 5`, socket it in the shop with `--stage 4`, then F3 up to a form pick). The Earthquake card says it moves Hex Needle to the stash.
Expected: no errors in the console. You can't buy without credits, can't own two of the same active or usable, and can't overfill slots. Passive effects show (faster move with sneakers, regen, etc.). Credits stay between shops.

### Lowkey ultimates
Added: 2026-10-18 | Confirmed: 2026-11-10
What: Lowkey's 3 ultimates from `design/heroes/lowkey.md`. Each run has 1 of them (default Sub Quake, `--ultimate` picks another). At hero level 10 a level-up card offers "Unlock ultimate", and levels 20 and 30 offer upgrades. You fire it with Space (gamepad Y) once it's charged. Each one charges differently:
- **Sub Quake:** 60 s cooldown. One 808 hit over the whole screen that damages and stuns. At level 3 the ground keeps shaking.
- **Encore:** charged by 1500 kills. The Drop stays on and skills fire 50% faster. At level 3 you heal on kills.
- **Bass Cannon:** charged by 3 Drops (2 from level 2). A 1-bar charge-up, then a giant beam where the camera looks, killing everything it touches. At level 3 it leaves a burning line.
How to test:
1. Normal play: the bottom-right bar says "Sub Quake: unlocks at hero level 10". Use F3 up to level 10: one card is "Unlock ultimate: Sub Quake".
2. Quick: `-- --ultimate-level 1` (Sub Quake), then F4 fills the charge, and the bar turns pink with "READY". Press Space: white flash, boom, a huge ring, and walkers on screen get hit and freeze for 2 s. The bar counts down 60 s.
3. `-- --ultimate encore --ultimate-level 3`, F4, Space: the HUD shows "ENCORE!", the Drop stays on (pink pulse, bigger skills), and skills fire faster for 12 bars.
4. `-- --ultimate bass_cannon --ultimate-level 3`, F4, Space: a thin line shows where the camera looks for 1 bar, then a wide beam kills every walker it touches. Turn the camera to sweep it. Orange burning circles stay along the line.
5. Without F4: Bass Cannon fills by 1 per Drop (the HUD counts "x / 3 Drops"). Encore counts kills.
Expected: no errors in the console. Space does nothing while locked, charging or already running. The ultimate never fires during a card or the pause menu. Upgrade cards appear only at hero levels 10, 20 and 30.

### Lowkey skill forms (branches at skill level 5 and 10)
Added: 2026-10-18 | Confirmed: 2026-11-10
What: every Lowkey skill now changes form. At skill level 5 you pick 1 of 3 forms, and at level 10 1 of 2 follow-ups of that form: 54 forms in total, as in `design/heroes/lowkey.md`. Some forms deepen the skill (more notes, pierce, bounces, extra rings). Others change what it is: falling cowbells, ground cracks, a flying boombox, a shield, a sweeping beam, records thrown like frisbees. The HUD and the level-up cards show the form name ("Cowbell · Ricochet · Pinball").
How to test:
1. Normal play: raise a skill to level 5 (F3 for levels). Right after the "4 → 5" card, a "Cowbell: choose a form" card shows 3 options with a one-line description each. At level 10 it shows 2 follow-ups of the chosen form.
2. Quick look at many forms (`LAUNCH.md` → Options): `-- --skill-level 10 --branch 2,0,2,1,2,1,2,0` gives Cowbell Rain + Cathedral (big golden bells drop on groups and stun), Earthquake + Aftershock (orange crack circles that hurt on every beat, with an eruption every 4th bar), Boombox Drone + Bass Boosted (a cyan box floating next to you, shooting exploding bolts on every beat), and Sweep + Full Circle (the beam spins around you each bar).
3. Second set: `-- --loadout distortion,vinyl,slap,speaker_stack --skill-level 10 --branch 1,0,1,0,0,1,1,1` gives Fuzz Trail + Static Storm (walk around: purple puddles behind you zap nearby walkers), Frisbee + Rebound (records fly out, bounce between walkers, come back), Slap & Pop + Pop Shot (beams forward and back, ends explode), and Subwoofer + Blowout (one big speaker pulls walkers in, then explodes when it expires).
4. Try Noise Gate (`--loadout distortion,cowbell --skill-level 5 --branch 2`): a cyan bubble around you. The first walker that touches you sets off a blast, and you take no damage for a moment. The bubble comes back 2 bars later.
5. Try any other combination. The last digit of each pair picks the level-10 form; skills listed in `--loadout` are asked in that order.
Expected: no errors in the console, every form plays as its card text says, and form choices never repeat for the same skill. A wrong `--branch` count stops the game with an error.

### Lowkey base kit + The Drop (beat clock)
Added: 2026-10-18 | Confirmed: 2026-11-10
What: the placeholder skills are gone. The hero is now Lowkey (`game/data/heroes/lowkey.json`, design: `design/heroes/lowkey.md`) with 90 HP and speed 7. She has a pool of 6 skills, and 4 of them are in the loadout. Her skills fire on a 130 BPM beat. Every 8 bars a 1-bar build-up leads into a 2-bar Drop (+30% damage, +20% area) that starts with a shockwave. A placeholder groove plays on the beat (kick, clap, hats, riser, drop boom, plus a sound for each skill cast). Skill branches, ultimates and items come next.
How to test:
1. Build, then play from source (`LAUNCH.md`). Default loadout: Cowbell (unlocked), Low End, Speaker Stack, Slap.
2. Listen and watch the HUD under the stage line: 4 beat squares light up in time with the kick. The bar next to them fills toward the next Drop. It shows "build-up" (rising pink tint and riser sound), then "DROP" (pink flash, boom, a white shockwave ring that pushes walkers away; the hero glows pink on each beat).
3. Cowbell: notes fly at the nearest walker in time with the beat, each with a cowbell hit that walks a short riff. Level-ups unlock the others:
   - Low End: a pink ring every bar that pushes walkers back.
   - Speaker Stack: purple boxes land on walker groups and pulse cyan on every beat.
   - Slap: a pink beam in the direction the camera looks (turn the camera to aim).
4. To see all 6 at once, run with `-- --loadout cowbell,low_end,speaker_stack,distortion,slap,vinyl --skill-level 5`:
   - Distortion: purple field around you; walkers inside take more damage from everything.
   - Vinyl: black records with pink labels circle you.
5. M mutes / unmutes the beat. Level-up cards describe each skill with its numbers; cooldowns are in beats.
Expected: no errors in the console. Skills fire on the beat, never between beats. The groove and the HUD squares stay in sync, stop while a card or the pause menu is open, and continue in time. During the Drop, the skill visuals get bigger and walkers die faster. A wrong id in `--loadout` stops the game with an error that lists the valid ids.

### Run camera: lower and farther, off-screen spawning
Added: 2026-10-17 | Confirmed: 2026-11-10
What: the run camera tilts 40° below the horizon (was 55°) and sits 28 m from the hero (was 22 m). Values are in `game/data/run.json` → `camera`. Walkers now spawn just outside the visible screen area in every direction, following camera rotation and screen shape. The stress test camera is unchanged.
How to test:
1. Play from source: `Godot_v4.7.2-stable_mono_win64_console.exe --path game`.
2. The view is flatter and wider: you see much more ground ahead, and the hero and walkers look smaller.
3. Watch the screen edges for a minute while turning the camera with the mouse. Walkers walk in from outside the screen; none appear out of nothing inside the view. Walkers from the bottom edge (behind you) arrive sooner than those from the top.
4. Walk far in one direction: props and shadows cover the whole view, up to the top corners.
5. Optional: change `tiltDeg` / `distance` in `run.json` and restart. A tilt that shows the horizon (below fovDeg / 2 + 5) or a too-small `despawnRadius` stops the game with an error naming the key.
Expected: no errors in the console, no walkers popping in on screen, the stress test (`-- --stress`) still looks as before.

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
