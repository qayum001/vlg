# Launch

Play vlg from source. PowerShell, full paths: paste into any PowerShell window on this machine.

- Engine args (`--gpu-index`, `--rendering-method`) go before `--path`.
- Game args go after `--`. Full list: `LaunchOptions.Usage` in `src/Vlg.Core/Bench/LaunchOptions.cs`.
- Launching from the command line doesn't compile C#. After code changes, build first:

```powershell
dotnet build C:\Projects\Games\vlg\game\Vlg.csproj
```

## Start (hub)

The game now opens in the hub (The Crossroads): walk to the portal and press E / A to start a run, Setlist desk = loadout, The Fixer = meta upgrades. Add `-- --run` (or any run option below) to skip the hub.

## Game run

```powershell
& "C:\Projects\Games\Godot\engine\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe" --path C:\Projects\Games\vlg\game
```

Controls:

| Input | Action |
|---|---|
| WASD / left stick | Move (W = away from the camera) |
| Mouse / right stick | Orbit the camera (also aims Slap) |
| Click, 1–9, d-pad + A | Pick a level-up card |
| Esc / Start | Pause menu (Resume, Restart run, Quit) |
| Space / Y | Ultimate (once unlocked at hero level 10 and charged) |
| Q / E (R, F, G) · LB / RB (X) | Use usable item 1 / 2 (3, 4, 5) |
| M | Mute / unmute the beat |
| F1 | Hide / show HUD |
| F2 | Skip to the next stage (dev) |
| F3 | +1 hero level (dev) |
| F4 | Fill the ultimate charge (dev) |
| F5 | +100 credits (dev) |
| F11 | Fullscreen on / off |
| F12 | Screenshot → `%APPDATA%\Godot\app_userdata\vlg\screenshots\` |

## Stress test

```powershell
& "C:\Projects\Games\Godot\engine\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe" --path C:\Projects\Games\vlg\game -- --stress
```

Controls:

| Input | Action |
|---|---|
| WASD | Move (relative to the camera) |
| Mouse | Orbit the camera |
| Esc | Free / capture the mouse |
| 1–4 | Preset: very_low, low, medium, high |
| `[` `]` | Enemies −250 / +250 |
| `,` `.` | Overdraw layers −1 / +1 |
| `-` `=` | Aura particles −/+ |
| F1 | Hide / show HUD |
| F11 | Fullscreen on / off |
| F12 | Screenshot → `%APPDATA%\Godot\app_userdata\vlg\screenshots\` |

## Options

Work for both. Combine freely.

| Add | Where | Effect |
|---|---|---|
| `--gpu-index 1` | before `--path` | Vega 8 iGPU (min-spec test GPU). 0 = RTX 3070 (default). |
| `--rendering-method forward_plus` | before `--path` | Forward+ renderer. Default: `mobile`. |
| `--preset medium` | after `--` | Start quality preset: `very_low`, `low` (default), `medium`, `high`. |
| `--seed 7` | after `--` | Other random props and spawns. Default 1; same seed + same input = same run. |
| `--loadout vinyl,distortion,slap,cowbell` | after `--` | Game run only. Hero skills in slot order (first starts unlocked). Ids: `cowbell`, `low_end`, `speaker_stack`, `distortion`, `slap`, `vinyl`. Default: `cowbell,low_end,speaker_stack,slap`. |
| `--skill-level 5` | after `--` | Game run only (dev). Every loadout skill starts at this level, so you see them all at once. At 5 or 10 the game first asks for each skill's form. |
| `--ultimate encore` | after `--` | Game run only. Ultimate for the run: `sub_quake` (default), `encore`, `bass_cannon`. |
| `--ultimate-level 1` | after `--` | Game run only (dev). Ultimate starts unlocked at this level (1-3), no need to reach hero level 10. Press F4 to charge it. |
| `--stage 4` | after `--` | Game run only (dev). Start at that stage of loop 1: 2 = chest, 4 = shop. |
| `--credits 300` | after `--` | Game run only (dev). Starting credits. |
| `--items tesla_backpack,toy_drone,energy_drink` | after `--` | Game run only (dev). Items you start with (modifiers go to the stash). Ids: `design/items.md` names in snake_case, see `game/data/items.json`. |
| `--branch 2,0,1,1` | after `--` | Game run only (dev). Answers those form picks in order: first skill's level-5 form, its level-10 form, then the next skill. 0 = first option on the card. |

Example: default loadout at level 10 with Cowbell Rain + Cathedral, Earthquake + Aftershock, Boombox Drone + Bass Boosted, Sweep + Full Circle:

```powershell
& "C:\Projects\Games\Godot\engine\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe" --path C:\Projects\Games\vlg\game -- --skill-level 10 --branch 2,0,2,1,2,1,2,0
```

Example: straight into the shop with 300 credits:

```powershell
& "C:\Projects\Games\Godot\engine\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe" --path C:\Projects\Games\vlg\game -- --stage 4 --credits 300 --skill-level 5 --branch 0,0,0,0
```

Example: all six of Lowkey's skills at level 3:

```powershell
& "C:\Projects\Games\Godot\engine\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe" --path C:\Projects\Games\vlg\game -- --loadout cowbell,low_end,speaker_stack,distortion,slap,vinyl --skill-level 3
```

Example: game run on the Vega 8 at the medium preset:

```powershell
& "C:\Projects\Games\Godot\engine\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe" --gpu-index 1 --path C:\Projects\Games\vlg\game -- --preset medium
```
