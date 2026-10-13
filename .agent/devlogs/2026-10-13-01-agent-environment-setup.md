# Agent environment setup
Date: 2026-10-13
Type: tooling
Commits: fad4556, f3d9a63, 826942f

## Summary
Set up the dev process for vlg before any game code: rules, skills, reviewer agent, memory, devlogs, git branches.

## Details
- User answers: engine not decided; memory + devlogs in repo; clarify on every new request (skip only answers/go-aheads); review by fresh reviewer agent.
- rules.md R1–R8 from the user's first message. code-rules.md: comment rule + agent defaults, engine section deferred.
- Skills: clarify, feature-pipeline, review-changes, update-memory, add-devlog, confirm-feature. Agent: reviewer.
- UserPromptSubmit hook echoes a one-line workflow reminder each prompt.
- CLAUDE.md imports rules.md and memory INDEX.md.
- Bootstrap exception: empty root commit `fad4556` made directly on master (no other way to create the branch), then dev branched from it.
- `.gitattributes` set to `eol=lf` — user's global `core.autocrlf=true` caused CRLF warnings.
- Harness memory folder holds only a pointer to the in-repo memory.

## Problems
- Started reading C:\Projects\Games\Unknown as possible context; user stopped it — unrelated project. Recorded in memory overview.

## Results
- Tests: none (docs/config). settings.json parsed with node; hook command executed OK.
- Review: 12 findings (3 major, 9 minor), all valid; 10 fixed in files, 2 (bootstrap, initial devlog) done via commit sequence.

## Next
- User confirms setup → merge dev to master.
- User picks engine + concept → engine section in code-rules.md, .gitignore, project skeleton.
