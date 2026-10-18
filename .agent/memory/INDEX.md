# Memory index
Current truth about vlg, agent-only. One line per file. How to maintain: `update-memory` skill.
Game design source of truth: `design/` (human-readable, shared with the user). Memory links to it, doesn't copy it.

## project
- [Overview](project/overview.md) — what vlg is, concept + domain summary, art approach, engine, platforms, min spec, user's design habits
- [Status](project/status.md) — current phase (first playable slice), in-progress items, next steps
- [Open questions](project/open-questions.md) — undecided tech/project items (Godot setup, remote, LFS)
- [Tooling](project/tooling.md) — Godot path/templates/GPU indexes, .NET SDKs, Blender path/prefs, two Blender MCPs, Telegram notify MCP, broken claude CLI

## architecture
- [Stress test](architecture/stress-test.md) — horde sim (Vlg.Core) + Godot adapters, bench ramp/CSV, data file, VAT enemies, `--stress` launch
- [Run](architecture/run.md) — playable level run: RunSim phases, MOBA skill leveling, stage loop, gems, run.json, boot scene
- [Skills](architecture/skills.md) — hero data file, beat clock + Drop, SkillSystem kinds, rings/speakers/beams, enemy statuses, damage sources, beat audio

## decisions
- [Engine](decisions/engine.md) — Godot 4.7 + C#, min spec 8 GB / 2 GB, Unity fallback trigger, perf test on Vega 8

## gotchas
- [Shell escapes](gotchas/shell-escapes.md) — Bash heredoc collapses `\\`; write backslash text with Write/Edit
- [dotnet test + MTP](gotchas/dotnet-test-mtp.md) — .NET 10 SDK needs global.json test runner opt-in; `--project` syntax
- [Godot UI + CLI](gotchas/godot-ui-and-cli.md) — SetAnchorsPreset keeps old rect; MultiMesh cull box must follow the player; exports refuse scene path arg; test windows catch user input
