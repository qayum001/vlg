# vlg — agent guide

New game, target: release on Steam. Engine not chosen yet (see memory).

@rules.md
@.agent/memory/INDEX.md

## Layout
- `rules.md` — user-declared rules. Always apply.
- `code-rules.md` — code conventions.
- `features.md` — features + manual test steps (awaiting on dev / confirmed on master).
- `.agent/memory/` — long-term memory, current truth. Entry: `INDEX.md`.
- `.agent/devlogs/` — append-only history.
- `.claude/skills/` — workflows. `.claude/agents/reviewer.md` — fresh-context reviewer.

## Workflow
1. New request → `clarify` skill first (R4).
2. Any code change → `feature-pipeline` skill (order and commit contents defined there).
3. User says merge → `confirm-feature` skill (subagent review → user picks fixes → merge to master).
4. User states a new rule or preference about how to work → add to `rules.md` (next number, date), commit on dev.
5. New project facts / decisions → `update-memory` skill. Notable events → `add-devlog` skill.

## Subagents
Workflow steps 1–5 belong to the main agent handling user messages. Subagents follow the caller's brief, don't run the pipeline, don't commit, and put open questions in their report.

## Conventions
- All docs, memory, logs in English.
- Never commit to `master` directly. Never rewrite history on `master` or `dev`; undo with `git revert`.
