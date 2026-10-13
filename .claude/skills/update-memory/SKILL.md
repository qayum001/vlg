---
name: update-memory
description: How to update vlg long-term agent memory in .agent/memory/. Use after any code change (feature-pipeline step 7), after decisions, when learning non-obvious project facts or traps, or when a memory file turns out stale or wrong.
---

# Update memory (R2)

Memory = current truth about the project, agent-only. Not history (devlogs), not rules (`rules.md`), not test steps (`features.md`).

## Layout
~~~
.agent/memory/
  INDEX.md        one line per file: "- [Title](path) — hook". Loaded every session via CLAUDE.md.
  project/        overview, status, open questions
  architecture/   one file per system: purpose, location, key types, data flow, how to extend
  decisions/      one file per decision: context, choice, why, rejected alternatives
  gotchas/        traps: engine quirks, tooling issues, things that broke before
~~~
Create a folder when its first file appears. New category → add here and in INDEX.

## File format
~~~
# Title
Updated: YYYY-MM-DD
Related: [name](../dir/file.md), ...

- facts as bullets
~~~
Relative markdown links. Link related files both ways.

## Store
- What an agent can't get quickly from code: where things live, how systems connect, why it's built this way, traps.
- Current status, next steps.
- User's decisions and preferences about the game.

## Don't store
- What code or git log already says clearly; long code snippets; session chatter; rules; temporary state.
- Speculation. If unavoidable, prefix `Unverified:`.

## Procedure
1. List what changed or was learned.
2. Read `INDEX.md`, find the covering files.
3. Prefer updating an existing file. Edit facts in place (memory is not append-only). Delete stale facts.
4. New file → INDEX line. Renamed/removed → fix INDEX and every link (grep the path).
5. Bump `Updated:`. Status changed → `project/status.md`.
6. Commit together with the related code change, or as its own `docs:` commit when no code change is involved.

## Size limits
- One topic per file, ~60 lines max. Bigger → split.
- INDEX ~80 lines max. Bigger → merge or tighten files.
- Delete outdated facts instead of adding caveats.
