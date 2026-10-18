# Hero: Lowkey (bassist)

Status: proposal, waiting for user review (keep / adjust per part)
Updated: 2026-10-18
Base: `../domain.md` (Heroes, Build). Items and skill tags: `../items.md`.
Model: made by the user (R11). In game: capsule until then.

## Who
- **Mira "Lowkey"**, bassist from the 2020s. Plays electric bass in a drift-phonk duo: distorted 808, cowbell riffs, dark Memphis vocal chops, neon nights.
- Lore hook: mid-set at an underground parking-garage show, the drop hit so hard it tore open the crossroads. She's been looking for the exit (and her drummer) ever since.
- Starting hero, lives in the hub. Names are placeholders.
- Look of her skills: sound + gear. Bass waves, 808 hits, cowbell notes, summoned speakers and records. Palette purple / hot pink / cyan neon, drift smoke, VHS glitch on big hits.

## Base stats (vs current placeholder hero)
| Stat | Lowkey | Placeholder |
|---|---|---|
| HP | 90 | 100 |
| Move speed | 7.0 | 6.5 |
| Pickup radius | 4.5 m | 4 m |

Fast and a bit fragile.

## Unique passive: The Drop
- **Beat clock:** a global tempo, 130 BPM (data). 1 beat = 0.46 s, 1 bar = 4 beats = 1.85 s.
- **On the beat:** her skills keep normal cooldowns, but a ready skill waits for the next half-beat (max 0.23 s) before it fires. Everything she does hits in rhythm. Cooldown upgrades still count.
- **Drop:** every 8 bars (~15 s), 1 bar of build-up (riser VFX, screen pulse), then the Drop: 2 bars (~3.7 s) of +30% damage and +20% area for all her skills. The first beat of the Drop sends a shockwave around her (damage + knockback).
- Some branch forms below do extra things on the Drop (marked **Drop:**).
- The player never has to time anything. All automatic.
- Only her skills follow the beat. Items don't (they're shared by all heroes).

## Skills (pool of 6, pick 4 per run)
Cooldowns written in beats and bars. Numbers are placeholders. Branch types: *deepen* = same skill, stronger; *change* = new concept.
Tags decide which modifiers fit (`../items.md`). A branch that changes tags is marked; modifiers that stop fitting go to the stash (`domain.md`).

### 1. Cowbell (starter)
Tags: Projectile
The phonk cowbell riff: ringing notes fly at the nearest enemies.
- Numbers: 9 damage, every 2 beats, 1 note, range 14 m. Per level: +2 damage, +1 note every 2 levels (fired as a quick run of notes).
- L5 **Riff** (deepen): a 4-note riff every bar, notes pierce 2 enemies.
  - L10 **Solo**: 8-note riff, notes pierce 4.
  - L10 **Octave**: every note gets a twin: smaller, faster, higher pitch.
- L5 **Ricochet** (deepen, +Chain): notes bounce to 3 more enemies, −15% damage per bounce.
  - L10 **Pinball**: 6 bounces, no damage loss.
  - L10 **Echo Chamber**: every bounce rings a small burst around the target.
- L5 **Cowbell Rain** (change, Projectile → Area): giant cowbells fall from the sky on enemy clusters and ring a shockwave on impact.
  - L10 **Cathedral**: one huge bell per bar, stuns 1 s. **Drop:** a bell on every beat.
  - L10 **Hailstorm**: many small bells, falling twice as often.

### 2. Low End
Tags: Area
Every bar a sub-bass ring expands from her, hitting and pushing enemies back.
- Numbers: 6 damage, every bar, radius 7 m, knockback 1.5 m. Per level: +1.2 damage, +0.3 m radius.
- L5 **Sub Bass** (deepen): slower, wider ring. Hit enemies are slowed 40% for 1 s.
  - L10 **Infrasound**: hit enemies are stunned 0.5 s.
  - L10 **Pressure**: damage grows with distance from her (strongest at the edge).
- L5 **Double Time** (deepen): 2 rings per bar, 60% damage each.
  - L10 **Triplets**: 3 rings per bar.
  - L10 **Collapse**: each ring expands, then collapses back to her: hits twice and pulls enemies in.
- L5 **Earthquake** (change, Area → Zone): no ring. Each bar the ground cracks in a 6 m circle around her; the cracks stay 2 bars and hurt on every beat.
  - L10 **Fault Line**: cracks follow her path as a trail.
  - L10 **Aftershock**: every 4th bar the cracks erupt for big damage. **Drop:** they erupt on every bar.

### 3. Speaker Stack
Tags: Summon, Zone
Drops a speaker near enemy clusters. It pumps a damage pulse around itself on every beat for 4 bars.
- Numbers: 4 damage per pulse, radius 3 m, a new speaker every 4 bars, max 2. Per level: +0.8 damage, +1 max speaker every 3 levels.
- L5 **Wall of Sound** (deepen): speakers land 3 at a time in a ring around her, bigger pulses.
  - L10 **Festival Rig**: 6 speakers; every 4th pulse is a huge one.
  - L10 **Feedback Loop** (+Beam): speakers link with damaging beams between them.
- L5 **Subwoofer** (change): one big sub. Its pulse pulls enemies in, and it hits harder the more enemies are inside.
  - L10 **Black Hole Sub**: stronger pull; basic mobs at the center are crushed.
  - L10 **Blowout**: when it expires, the sub explodes for big area damage.
- L5 **Boombox Drone** (change, Zone → Projectile): a flying boombox follows her and shoots sound bolts on every beat.
  - L10 **Drone Squad**: 3 drones.
  - L10 **Bass Boosted**: bolts explode on hit.

### 4. Distortion
Tags: Aura
A fuzz field around her. Enemies inside take damage on every beat and get **Distorted**: +10% damage taken from everything.
- Numbers: 2.5 damage per beat, radius 2.8 m. Per level: +0.6 damage, +0.15 m radius.
- L5 **Overdrive** (deepen): double damage, −30% radius, Distorted becomes +20%.
  - L10 **Blown Speaker**: enemies that die inside explode for small area damage.
  - L10 **Clipping**: **Drop:** the field doubles its damage and radius.
- L5 **Fuzz Trail** (deepen, +Zone): she leaves fuzz puddles behind her; they last 3 s and Distort enemies too.
  - L10 **Static Storm** (+Chain): puddles zap nearby enemies with small lightning.
  - L10 **Tar Pit**: puddles slow enemies 50%.
- L5 **Noise Gate** (change, defensive): the field becomes a shield. It blocks 1 hit every 2 bars; each block blasts enemies around her.
  - L10 **Hard Limiter**: blocks 1 hit every bar.
  - L10 **Sidechain**: while the shield is up, all her skills deal +15% damage.

### 5. Slap (aimed)
Tags: Beam, Aimed
Slap-bass hit: a sound beam fires in the camera's direction every 2 beats and hits everything in a line.
- Numbers: 14 damage, every 2 beats, 16 m long, 1.2 m wide. Per level: +3 damage, +0.8 m length.
- L5 **Slap & Pop** (deepen): beams fire forward and backward.
  - L10 **Cross**: 4 directions.
  - L10 **Pop Shot**: the end of each beam explodes.
- L5 **Sustain** (deepen): one continuous beam. Damage on an enemy grows the longer it stays in the beam.
  - L10 **Wide Load**: beam width doubles.
  - L10 **Overtone**: the beam splits into 3 at its end.
- L5 **Sweep** (change, loses Aimed): automatic. Each bar the beam sweeps a 180° arc in front of her.
  - L10 **Full Circle**: 360° sweep.
  - L10 **Double Sweep**: two beams sweep in opposite directions.

### 6. Vinyl
Tags: Orbit
Spinning vinyl records orbit her and cut enemies.
- Numbers: 5 damage per hit, 2 records, orbit radius 3 m. Per level: +1 damage, +1 record every 2 levels, faster spin.
- L5 **Turntable** (deepen): +2 records, wider orbit.
  - L10 **Double Deck**: two rings spinning opposite ways.
  - L10 **Hot Wax**: records set enemies on fire (damage over time).
- L5 **Frisbee** (change, Orbit → Projectile): records fly out to far enemies and come back, hitting both ways.
  - L10 **Rebound** (+Chain): records bounce between enemies before they return.
  - L10 **Big Disc**: one huge disc that pierces everything.
- L5 **Scratch** (deepen, +Area): every bar the records reverse spin with a scratch burst that pulls nearby enemies into the orbit.
  - L10 **Beat Juggle**: scratch every 2 beats.
  - L10 **Spinback**: **Drop:** records fly outward to the screen edge and return.

## Ultimates (pool of 3, pick 1 per run)
Pressed by the player. Unlocks at hero level 10, upgrades at 20 and 30. Each uses a different charge.

| Ultimate | Charge | Effect | Level 2 / 3 |
|---|---|---|---|
| **Sub Quake** | Cooldown 60 s | One huge 808 hit. Shockwave over the whole screen: big damage + 2 s stun. | L2: double damage, 3 s stun. L3: the ground keeps shaking for 4 bars (damage on every beat). |
| **Encore** | Kills (1500, data) | The crowd wants more: for 8 bars the Drop stays on and her skills fire 50% faster. | L2: 12 bars. L3: every kill during Encore heals 1 HP. |
| **Bass Cannon** (aimed) | Drops (3 Drops) | 1 bar charge-up, then a giant beam in the camera direction for 2 bars; turn the camera to sweep it. Void-type: kills basic and horde mobs it touches; elites take 30% max HP, bosses 10%. | L2: wider; charges in 2 Drops. L3: lasts 3 bars, leaves a burning line. |

## Default loadout (until the loadout screen exists)
Cowbell (starter), Low End, Speaker Stack, Slap + Sub Quake.

## Open
- Music: the beat clock needs a track at the same tempo. Who makes the soundtrack is not decided. Placeholder: a simple generated loop (kick, 808, cowbell) at 130 BPM.
- Idea, not decided: tempo rises a bit each loop (skills fire faster as difficulty grows).
- All numbers are placeholders: tune in the prototype.

## Assets (after approval)
- Icons: 6 skills + 3 ultimates + passive (ComfyUI, R12). Branch forms reuse the skill icon with a mark.
- 3D props (Blender): cowbell, speaker, subwoofer, boombox drone, vinyl record, bass cannon muzzle.
- VFX (Godot shaders/particles): sound rings, beam, fuzz field, ground cracks, Drop build-up + shockwave.
