# Lowkey base kit + beat clock
Date: 2026-10-18
Type: feature
Commits: ef28fe6

## Summary
Slice 1 of the approved kit: Lowkey replaces the placeholder hero skills. 6-skill pool, 4-skill loadout, beat clock with the Drop, enemy statuses, placeholder visuals and a synthesized groove.

## Requirements
- User approved `design/heroes/lowkey.md` and `design/items.md` as proposed ("Keep ... Start implementing").
- Clarify answers (same session): beat clock automatic (skills on the beat, Drop = passive); sound + gear visuals with phonk palette; her kit replaces Bolt/Nova/Aura/Orbit; 1-2 aimed skills.
- Plan told to the user: slices 1) base kit + beat, 2) branches, 3) ultimates, 4) items/shop/chest, 5) assets.

## Details
- `HeroDef` from `game/data/heroes/lowkey.json`, validated against RunConfig. run.json keeps only body (radius, contactMargin); walker gets `pushDecay`, sim gets `ringCapacity`.
- `BeatClock`: ticks on half-beats/beats from sim time; cooldown/duration in beats; skills wait for half-beat ticks; overshoot carries.
- `SkillSystem` rewritten around `SkillWorld`; kinds Projectile, Ring, Speaker, Aura (+vuln), Beam (camera aim), Orbit. `RingPool`, `SummonPool`, `BeamEvents`.
- `EnemySwarm`: Id, slow, stun, vuln, push (capped 25 m/s), LastHit, DamageDealt, `Damage()` helper; query pads include push speed.
- Godot: `SkillVisuals` rewrite (MultiMesh with instance colors), `BeatAudio` (samples from `tools/audio/make_beat_samples.py`), `BeatOverlay`, beat row in `RunHud`.
- Speaker numbers changed to make max 2 meaningful: new speaker every 2 bars, lasts 4 bars (design doc updated).
- Dev args `--loadout`, `--skill-level`; key M mutes the beat. Export filter includes `data/heroes/*.json`.

## Problems
- Bash heredocs broke twice (quote/backslash handling, see gotcha shell-escapes): files written with Write instead; one LAUNCH.md path got a `\v` and was fixed.
- Speaker pulse ran before placement in the first version, so a speaker placed on a beat waited a beat; pulse moved after Fire.

## Results
- Tests: `dotnet test --project tests/Vlg.Core.Tests/Vlg.Core.Tests.csproj` → 177 passed.
- Smoke: game run windowed with all 6 skills (`--skill-level 3/5`), screenshots in build/shots: rings, aura, speakers, vinyls, beat HUD, Drop shock visible; no errors, exit 0.

## Next
- Slice 2: branch picks at skill level 5 and 10.
