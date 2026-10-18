# Launch

Play vlg from source. PowerShell, full paths: paste into any PowerShell window on this machine.

- Engine args (`--gpu-index`, `--rendering-method`) go before `--path`.
- Game args go after `--`. Full list: `LaunchOptions.Usage` in `src/Vlg.Core/Bench/LaunchOptions.cs`.
- Launching from the command line doesn't compile C#. After code changes, build first:

```powershell
dotnet build C:\Projects\Games\vlg\game\Vlg.csproj
```

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
| M | Mute / unmute the beat |
| F1 | Hide / show HUD |
| F2 | Skip to the next stage (dev) |
| F3 | +1 hero level (dev) |
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
| `--skill-level 5` | after `--` | Game run only (dev). Every loadout skill starts at this level, so you see them all at once. |

Example: all six of Lowkey's skills at level 3:

```powershell
& "C:\Projects\Games\Godot\engine\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe" --path C:\Projects\Games\vlg\game -- --loadout cowbell,low_end,speaker_stack,distortion,slap,vinyl --skill-level 3
```

Example: game run on the Vega 8 at the medium preset:

```powershell
& "C:\Projects\Games\Godot\engine\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe" --gpu-index 1 --path C:\Projects\Games\vlg\game -- --preset medium
```
