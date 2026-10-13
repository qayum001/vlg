# Memory index
Current truth about vlg, agent-only. One line per file. How to maintain: `update-memory` skill.
Game design source of truth: `design/` (human-readable, shared with the user). Memory links to it, doesn't copy it.

## project
- [Overview](project/overview.md) — what vlg is, concept summary, art approach, engine not chosen
- [Status](project/status.md) — current phase, in-progress items, next steps
- [Open questions](project/open-questions.md) — undecided tech/project items (engine, remote, LFS)
- [Tooling](project/tooling.md) — Blender path/prefs, two Blender MCPs (ports, tools, forbidden tools), broken claude CLI

## gotchas
- [Shell escapes](gotchas/shell-escapes.md) — Bash heredoc collapses `\\`; write backslash text with Write/Edit
