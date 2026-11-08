# Five painterly 3D item icons from text
Date: 2026-11-08
Type: feature
Commits: 9fbd0d3

## Summary
Replaced five item icons on dev using OpenAI built-in text-to-image generation and the existing written item descriptions.

## Requirements
- User: generate and replace five assets when text descriptions exist, continuing the Arcane-inspired animation style and solid 3D appearance.
- Latest correction: "do not resize yet". Native generated files retained without resizing.
- Selected: Plague Flask, Clockwork Heart, Copper Coil, Grim Ledger, Pocket Singularity.

## Details
- Sources: design/items.md, game/data/items.json, original ITEMS descriptions in tools/icons/make_icons.py.
- Five separate generation calls, no reference images. Each PNG is 1254 x 1254 and copied byte-for-byte over its existing item path.
- Full prompt set and source descriptions: tools/icons/text2image-prompts-2026-11-08.json.
- Updated features.md and third-party.md. Existing texture resource paths and import settings remain valid.

## Problems
- None affecting the assets.

## Results
- Checks: five PNGs decoded; source/destination SHA256 hashes match; git diff --check passed.
- Godot 4.7.2 --headless --path game --import: exit 0, replacements imported without errors.
- Logic tests not rerun: no code or gameplay data changed. In-game visual review remains manual.
- Account-wide weekly meter displayed 7% before and immediately after generation; precision does not establish zero usage or a per-image cost.

## Next
- User inspects the five icons in shop/build/HUD and confirms the art direction before more replacements.

