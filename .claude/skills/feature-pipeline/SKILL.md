---
name: feature-pipeline
description: Mandatory pipeline for any code change in vlg (feature, bug fix, refactor, tests, tooling code). Clarify → implement on dev → test → memory → features.md → commit → devlog → report. No review here; subagent review runs only before merge to master (confirm-feature).
---

# Feature pipeline

Steps in order. Step not applicable → say why in the report.

1. **Clarify** — `clarify` skill. No code before requirements are clear.
2. **Context** — memory `INDEX.md` + relevant files, `rules.md`, `code-rules.md`. `git status`: must be on `dev`, tree clean (leftovers → ask).
3. **Plan** — non-trivial work: short plan (files, approach, tests). Plan changes scope → confirm with user.
4. **Implement** — follow `code-rules.md`. New generated files (build output, caches, engine metadata, secrets) → `.gitignore`.
5. **Test** — add/update automated tests; bug fix: failing regression test first. Run the full suite; run the game/build to smoke test when possible. Keep the real output for the report.
6. **Memory** — `update-memory` skill.
7. **features.md** — user-visible change → entry under "Awaiting confirmation" with manual test steps.
8. **Commit** — on `dev`: code + tests + memory + features.md together, so a revert stays consistent. Check `git status` for unexpected files first.
   Message: `<type>: <summary>` (feat|fix|refactor|test|docs|chore), body: what/why, attribution line last.
9. **Devlog** — `add-devlog` skill (incl. Requirements section), separate commit.
10. **Report** — what changed, test results, manual test steps. Ask: test it, then "merge" → `confirm-feature` (runs the subagent review first), or reject → revert.

No per-feature review (R6). Review happens once, before merge to master.

## Rejected feature
`git revert <feature commits>` on dev (not devlog commits, no history rewrite). Remove its features.md entry and fix memory in the same or a follow-up commit. Devlog entry (Type: revert).
Already on master → same revert on dev, then reach master via `confirm-feature` after the user OKs it.

## Stopping mid-task
Blocked or waiting on the user: commit WIP on dev (`wip: ...`) only if it builds and tests pass; otherwise leave uncommitted and say so. Session ending mid-task → devlog (Type: handoff).
