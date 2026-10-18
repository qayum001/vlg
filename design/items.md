# Items: first pool

Status: proposal, waiting for user review (keep / adjust per item)
Updated: 2026-10-18
Base: `domain.md` (Build → item types, Shop and chest). First hero: `heroes/lowkey.md`.

## Theme
- Shared by all heroes, so not tied to one hero's world. Loot from many worlds and eras that ended up at the crossroads: cursed relics, modern gadgets, old weapons, sci-fi junk.
- Each item has one flavor line (lore delivery, dark humor).
- The first storyline (undead town) can add its own items later through level unlocks.

## Skill tags
Mechanic tags only (elements and set bonuses stay deferred). Every skill has 1-3 tags; a modifier fits a skill when they share at least one tag.

| Tag | Means |
|---|---|
| Projectile | fires objects that travel |
| Area | instant burst in a radius |
| Aura | constant field around the hero |
| Orbit | objects circling the hero |
| Summon | placed or following objects with their own life |
| Beam | damage along a line |
| Chain | jumps between enemies |
| Zone | ground effect that lasts |
| Aimed | fires in the camera direction |

## Rarity and price (placeholders)
| Rarity | Price (credits) | Where |
|---|---|---|
| Common | 15 | shop |
| Uncommon | 30 | shop |
| Rare | 60 | shop, chest (chest offers rare only) |

## Stacking (proposal)
- Passive: can be bought again, effects add up.
- Modifier: copies allowed, but not two of the same in one skill.
- Active, usable: one of each; they take a slot (start 2 + 2).

## Modifiers (8): go into a skill socket
| Item | Rarity | Fits tags | Effect | Flavor |
|---|---|---|---|---|
| Bell Shard | Common | Area, Aura, Zone | +25% radius | From a church bell that rang once too often. |
| Hourglass Sand | Common | Summon, Zone | +50% duration | Sand from a desert where time is sold by the pound. |
| Watchtower Lens | Common | Beam, Aimed | +40% length, +15% damage | The guard saw everything coming. Except that. |
| Split Arrowhead | Uncommon | Projectile, Orbit | +1 projectile or orbiting object, −10% damage each | Elven work. The elves want it back. |
| Hex Needle | Uncommon | Orbit, Aura, Area | Hits apply Hex: +10% damage taken for 3 s | Grandma's knitting was never just knitting. |
| Overclock Chip | Uncommon | Projectile, Area, Beam, Chain, Summon, Orbit | −15% cooldown | Warranty void. So is the user. |
| Copper Coil | Rare | Projectile, Chain | Chain skills: +2 jumps. Projectiles: jump to 1 more enemy on hit | Still humming. Don't lick it. |
| Plague Flask | Rare | Area, Zone, Aura | Kills leave a poison puddle for 2 s | Smells like the undead town. Everything does. |

## Active items (4): fire on their own
| Item | Rarity | Effect | Flavor |
|---|---|---|---|
| Haunted Lantern | Common | Every 3 s a wisp flies out, homes on an enemy and pops | The ghost inside pays no rent. |
| Toy Drone | Uncommon | Hovers next to you, shoots the nearest enemy twice per second | Batteries not included. Nobody knows what it runs on. |
| Old Mortar | Rare | Every 4 s lobs a shell at the densest enemy group, big explosion | Found in a trench. The war ended. The mortar didn't hear. |
| Tesla Backpack | Rare | Every 3 s lightning chains through 5 enemies | Hair styling included. |

## Usable items (4): pressed, effect ~3 s, cooldown starts on press
| Item | Rarity | Cooldown | Effect | Flavor |
|---|---|---|---|---|
| Energy Drink | Common | 40 s | 3 s: skills +40% faster, move +30%. Then 2 s crash: move −20% | Contains 400% of the daily value of regret. |
| Smoke Bomb | Uncommon | 30 s | 3 s: enemies lose track of you, move +30% | Ninjas vanish. You mostly cough. |
| Holy Water | Rare | 25 s | Thrown in the camera direction: 3 s burning pool, double damage to undead | Blessed by a priest. Bottled by a gas station. |
| Pocket Singularity | Rare | 50 s | Pulls every enemy within 15 m to one point in front of you for 3 s, then pops | Do not fold. |

## Passive items (9): global stats and effects
| Item | Rarity | Effect | Flavor |
|---|---|---|---|
| Whetstone | Common | +8% damage | Sharpens anything. Even jokes. |
| Knockoff Sneakers | Common | +8% move speed | Bought in an alley. Run before he wants them back. |
| Herbal Tea | Common | +0.5 HP/s regen | Grandma's recipe. Same grandma as the needle. |
| Horseshoe Magnet | Common | +30% pickup radius | Lucky and attractive. |
| Vampire Tooth | Uncommon | 1.5% life steal | The rest of the vampire is still looking for it. |
| Knight's Pauldron | Uncommon | −8% damage taken | One shoulder of a very brave knight. |
| Gold Tooth | Uncommon | +20% credits from kills | Pried from the richest zombie in town. |
| Clockwork Heart | Rare | −8% cooldowns (skills and active items) | Ticks. Loudly. At night. |
| Grim Ledger | Rare | Every 200 kills: +1% damage for this run, no cap | Someone has to keep the count. |

25 items: 9 common, 8 uncommon, 8 rare.

## Fit check with Lowkey
| Skill | Tags | Modifiers that fit |
|---|---|---|
| Cowbell | Projectile | Split Arrowhead, Overclock Chip, Copper Coil |
| Low End | Area | Bell Shard, Hex Needle, Overclock Chip, Plague Flask |
| Speaker Stack | Summon, Zone | Bell Shard, Hourglass Sand, Overclock Chip, Plague Flask |
| Distortion | Aura | Bell Shard, Hex Needle, Plague Flask |
| Slap | Beam, Aimed | Watchtower Lens, Overclock Chip |
| Vinyl | Orbit | Split Arrowhead, Hex Needle, Overclock Chip |

## Open
- Items don't level up. Is buying a passive again enough for growth? (proposal: yes)
- Should the chest also offer uncommon items while the pool is small? 8 rares cover 3 options per chest.
- "Undead" as an enemy tag (Holy Water). Every enemy in the first storyline would have it.

## Assets (after approval)
- Icons for all 25 items (ComfyUI, R12).
- 3D models (Blender) for items visible in the world: lantern + wisp, toy drone, mortar + shell, tesla backpack sparks, holy water flask, smoke bomb.
- VFX: poison puddle, lightning chain, singularity, smoke, holy fire.
