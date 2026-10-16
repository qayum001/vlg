# Memory index
Current truth about vlg, agent-only. One line per file. How to maintain: `update-memory` skill.
Game design source of truth: `design/` (human-readable, shared with the user). Memory links to it, doesn't copy it.

## project
- [Overview](project/overview.md) — what vlg is, concept summary, art approach, engine, platforms, min spec
- [Status](project/status.md) — current phase, in-progress items, next steps
- [Open questions](project/open-questions.md) — undecided tech/project items (Godot setup, remote, LFS)
- [Tooling](project/tooling.md) — Godot path/templates/GPU indexes, .NET SDKs, Blender path/prefs, two Blender MCPs, Telegram notify MCP, broken claude CLI

## architecture
- [Stress test](architecture/stress-test.md) — horde sim (Vlg.Core) + Godot adapters, bench ramp/CSV, data file, VAT enemies

## decisions
- [Engine](decisions/engine.md) — Godot 4.7 + C#, min spec 8 GB / 2 GB, Unity fallback trigger, perf test on Vega 8

## gotchas
- [Shell escapes](gotchas/shell-escapes.md) — Bash heredoc collapses `\\`; write backslash text with Write/Edit
- [dotnet test + MTP](gotchas/dotnet-test-mtp.md) — .NET 10 SDK needs global.json test runner opt-in; `--project` syntax
