# Memory index
Current truth about vlg, agent-only. One line per file. How to maintain: `update-memory` skill.
Project entry point: `AGENTS.md`; shared workflows: `.agents/skills/`.
Game design source of truth: `design/` (human-readable, shared with the user). Memory links to it, doesn't copy it.

## project
- [Overview](project/overview.md) — what vlg is, concept + domain summary, art approach, engine, platforms, min spec, user's design habits
- [Status](project/status.md) — current phase (first playable slice), in-progress items, next steps
- [Open questions](project/open-questions.md) — undecided tech/project items (Godot setup, remote, LFS)
- [Tooling](project/tooling.md) — Godot path/templates/GPU indexes, .NET SDKs, Blender path/prefs, two Blender MCPs, Telegram notify MCP, broken claude CLI
- [QuestionForms](project/questionforms.md) - installed grill-me entry point, local MCP/API, Python bridge and interview resume
- [Release launchers](project/release-launchers.md) - standalone release build and latest-build batch files

## architecture
- [Stress test](architecture/stress-test.md) — horde sim (Vlg.Core) + Godot adapters, bench ramp/CSV, data file, VAT enemies, `--stress` launch
- [Run](architecture/run.md) — playable level run: RunSim phases, MOBA skill leveling, stage loop, gems, run.json, boot scene
- [Skills](architecture/skills.md) — hero data file, beat clock + Drop, SkillSystem kinds + branch forms, ultimates, pools, enemy statuses, damage sources, beat audio
- [Hub](architecture/hub.md) — The Crossroads hub, meta credits/upgrades, save file, boot flow (hub default, --run)
- [Assets](architecture/assets.md) — icon generator (ComfyUI), Blender prop script, fx shaders, beat samples
- [Items](architecture/items.md) — items.json, inventory/sockets/stash, shop + chest rules, usables, passives, credits, dev args

## decisions
- [Engine](decisions/engine.md) — Godot 4.7 + C#, min spec 8 GB / 2 GB, Unity fallback trigger, perf test on Vega 8

## gotchas
- [Shell escapes](gotchas/shell-escapes.md) — Bash heredoc collapses `\\`; write backslash text with Write/Edit
- [dotnet test + MTP](gotchas/dotnet-test-mtp.md) — .NET 10 SDK needs global.json test runner opt-in; `--project` syntax
- [Godot UI + CLI](gotchas/godot-ui-and-cli.md) — SetAnchorsPreset keeps old rect; MultiMesh cull box must follow the player; exports refuse scene path arg; test windows catch user input
