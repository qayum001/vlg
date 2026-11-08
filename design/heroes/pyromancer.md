# Pyromancer

Added: 2026-11-08. Male fire mage; working name: Pyromancer.

Follows the hero and ability rules in [domain.md](../domain.md): six automatic skills, three manual ultimates, a pre-run selection of four skills plus one ultimate, and branches at skill ranks 5 and 10. Ultimate ranks cost level-up picks from hero levels 10, 20 and 30. Skills cap at rank 10 and at ceil(hero level / 3).

## Development access

Pyromancer is available immediately in The Crossroads. Press H or interact with the Change hero stand before entering the level portal. Both Pyromancer and Lowkey remain available. Changing hero restores that hero's default setlist; the selected hero and setlist are saved between sessions. The Setlist desk selects four distinct skills and one ultimate from the selected hero.

The hero remains a colored capsule placeholder. The user supplies humanoid models (R11).

## Stats and passive

- Base HP: 90; speed: 6.5 m/s; pickup radius: 4 m.
- Ember Heart: each kill restores 0.25 HP, capped at maximum HP. It cannot revive a dead hero.
- Spell cooldowns use seconds. Pyromancer has no beat gating, Drop bonuses or musical beat soundtrack.

## Skills

| Spell | Behavior | Rank 5 paths |
| --- | --- | --- |
| Magic Bolts | Blue arcane projectiles automatically fire at nearby enemies. | Rapid Missiles / Empowered Bolts |
| Fireball | Orange projectiles explode on impact; splash uses the existing explosion rules. | Wildfire / Furnace Shot |
| Flame Aura | Repeated fire damage around the hero. | Blazing Circle / Searing Heat |
| Flame Orbit | Orbiting flames hit enemies and leave burns. | Ember Swarm / Searing Brands |
| Flame Nova | An expanding fire ring damages and pushes enemies away. | Blasting Wave / Burning Horizon |
| Meteor | Marks enemy groups, then drops meteors after a short delay. | Falling Stars / Deep Impact |

Each rank 5 path has two distinct rank 10 continuations. These deepen damage, area, projectile count, burns or cast rate. Numeric values and the full branch trees live in `game/data/heroes/pyromancer.json`.

Default setlist: Magic Bolts, Fireball, Flame Aura, Flame Orbit; ultimate: Inferno. The first selected skill starts unlocked.

## Ultimates

Press Space / gamepad Y when charged. Each has three ranks, no branches, and charges only during active play.

| Ultimate | Behavior |
| --- | --- |
| Inferno | Blast nearby enemies and leave a wide patch of burning ground. |
| Meteor Shower | Repeatedly mark nearby enemies and drop meteors onto their positions. A cast with no target preserves its charge. |
| Phoenix | A fire eruption damages nearby enemies and restores health once, capped at maximum HP. |

## Adding abilities

Use the established hero data schema (`HeroDef`, `SkillDef`, `BranchDef`, `UltimateDef`). Reuse existing skill kinds, tags, flags and pooled effects when they express the intended behavior. Validate every reachable branch form and add tests for new gameplay behavior. A new kind also needs config validation, simulation behavior, player-facing descriptions and visuals. Keep tuning in the hero JSON, simulation logic in `Vlg.Core`, and Godot nodes as presentation/input adapters.
