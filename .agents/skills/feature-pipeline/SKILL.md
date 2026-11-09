---
name: feature-pipeline
description: Mandatory pipeline for any code change in vlg (feature, bug fix, refactor, tests, tooling code). Clarify → implement on dev → test → memory → features.md → commit → devlog → report. No review here; subagent review runs only before merge to master (confirm-feature).
---

# Feature pipeline

Steps in order. Step not applicable → say why in the report.

0. **Rules check (every request, R14)** - Before acting on each user request, follow-up or correction, verify the branch and worktree against current `dev`; read `AGENTS.md`, all of `rules.md` and `code-rules.md`, the applicable shared workflows and design rules. Reconcile the request with every applicable rule. Carry these constraints into the plan and subagent briefs. Before the final report, check the changes, validation, branch handoff, assets and documentation against the same rules. Explain any higher-priority conflict or actual blocker instead of silently skipping a rule. This check also applies to requests that do not change code.
1. **Clarify** — `clarify` skill. No code before requirements are clear.
2. **Context** - Read `AGENTS.md`, memory `INDEX.md` and relevant files, `rules.md`, `code-rules.md`. Check the branch and `git worktree list`; work in the `dev` checkout, including when the supplied workspace is detached or on another branch (R5). Inspect existing changes and preserve unrelated work.
3. **Plan** — non-trivial work: short plan (files, approach, tests). Plan changes scope → confirm with user.
4. **Implement** — follow `code-rules.md`. New generated files (build output, caches, engine metadata, secrets) → `.gitignore`.
5. **Test** — add/update automated tests; bug fix: failing regression test first. Run the full suite; run the game/build to smoke test when possible. Keep the real output for the report.
6. **Memory** — `update-memory` skill.
7. **Dev handoff** - If host-required isolation used a side branch, integrate the completed feature into `dev` and run the relevant checks there before calling it ready. Builds, exports and launches for the user use `dev` unless the user requested otherwise (R5).
8. **features.md** — user-visible change → entry under "Awaiting confirmation" with manual test steps.
9. **Commit** — on `dev`: code + tests + memory + features.md together, so a revert stays consistent. Check `git status` for unexpected files first.
   Message: `<type>: <summary>` (feat|fix|refactor|test|docs|chore), body: what/why, attribution line last.
10. **Devlog** — `add-devlog` skill (incl. Requirements section), separate commit.
11. **Report** — what changed, test results, manual test steps. Ask: test it, then "merge" → `confirm-feature` (runs the subagent review first), or reject → revert.

No per-feature review (R6). Review happens once, before merge to master.
Research and implementation subagents require an explicit user request (R15). The fresh-context reviewer in the R6 pre-merge workflow or an explicitly requested review is authorized by the review exception, without a separate subagent request.

## Rejected feature
`git revert <feature commits>` on dev (not devlog commits, no history rewrite). Remove its features.md entry and fix memory in the same or a follow-up commit. Devlog entry (Type: revert).
Already on master → same revert on dev, then reach master via `confirm-feature` after the user OKs it.

## Stopping mid-task
Blocked or waiting on the user: commit WIP on dev (`wip: ...`) only if it builds and tests pass; otherwise leave uncommitted and say so. Session ending mid-task → devlog (Type: handoff).
