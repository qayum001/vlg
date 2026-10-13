---
name: add-devlog
description: Append a new dev log entry to .agent/devlogs/ in vlg. Use after every completed task, a merge to master, a revert, a notable bug or dead end, an important decision, or when a session ends with work in progress. Never edit existing logs.
---

# Dev log (R3)

Logs = history: what happened, when, why. Append-only.

## Rules
- Never edit or delete existing log files. Correction → new entry referencing the old file.
- One file per entry: `.agent/devlogs/YYYY-MM-DD-NN-slug.md`, NN = 01, 02, ... per day (check existing files for that day).
- 10–40 lines. Facts, not prose.
- Own commit: `devlog: <slug>`. Kept separate so reverting feature commits keeps the log.

## When
- Task done (feature, fix, refactor, tooling).
- Merge to master; feature rejected or reverted.
- Notable bug, failed approach, surprising engine/tool behavior.
- Important decision made with the user.
- Session ends mid-task (handoff: state + next step).

## Template
~~~
# <Title>
Date: YYYY-MM-DD
Type: feature | fix | refactor | tooling | decision | merge | revert | investigation | handoff
Commits: <short hashes, or "none">

## Summary
1–3 lines.

## Details
- what was done / found

## Problems
- dead ends, surprises; "None" if none

## Results
- Tests: <command> → <result>
- Review: <n findings, n fixed, n rejected>

## Next
- follow-ups, open questions
~~~
