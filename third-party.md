# Third-party software

Every dependency, its license and whether it ships in the game. Add a row before adding the dependency (user approval first, see `code-rules.md`).

| Name | Version | License | Ships in game | Use | Approved |
|---|---|---|---|---|---|
| Godot Engine (.NET) | 4.7.2 | MIT | yes (export templates) | engine | 2026-10-16 |
| Godot.NET.Sdk / GodotSharp | 4.7.2 | MIT | yes (GodotSharp) | C# build + bindings | 2026-10-16 |
| .NET runtime | 8 | MIT | yes (bundled by Godot export) | C# runtime | 2026-10-16 |
| xunit.v3 | 4.0.2 | Apache-2.0 | no (tests only) | unit tests for `Vlg.Core` | 2026-10-16 |
| Stable Diffusion XL base 1.0 (Stability AI) | 1.0 | CreativeML Open RAIL++-M | outputs only (icons in `game/assets/icons/`), model not shipped | 2D icon generation via `tools/icons/make_icons.py` | 2026-10-18 (R12, user: "you can use comfy") |
| ComfyUI (portable) | 0.39.0 | GPL-3.0 | no (local tool) | runs SDXL for icons | 2026-10-18 (R12) |
| Blender | 5.2.2 | GPL-3.0-or-later | no (local tool; exported models are ours) | builds `game/assets/models/*.glb` via `tools/models/make_props.py` | 2026-10-13 (art approach) |
| OpenAI built-in image generation | service (2026-11-08) | OpenAI service terms (model not redistributed) | outputs only (five item icons) | text-to-image replacements; prompts in `tools/icons/text2image-prompts-2026-11-08.json` | 2026-11-08 (user requested generation and replacement) |

Godot's license requires its copyright notice in the shipped game (credits / about screen): https://godotengine.org/license

AI-generated content: most item and skill icons are made with SDXL (local, prompts in `tools/icons/make_icons.py`). Five item icons (Plague Flask, Clockwork Heart, Copper Coil, Grim Ledger, Pocket Singularity) were regenerated from text with OpenAI built-in image generation on 2026-11-08; prompts and descriptions are in `tools/icons/text2image-prompts-2026-11-08.json`. Steam requires an AI content disclosure on the store page (see `design/concept.md` Implications). Models and audio are procedural (Blender script, Python synthesis), not AI.
