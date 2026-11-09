# vlg - agent guide

Game for Steam, built with Godot .NET and C#.

Read [rules.md](rules.md) and [.agent/memory/INDEX.md](.agent/memory/INDEX.md) before starting work. These instructions apply to every coding agent.

## Layout
- `rules.md` - user-declared rules. Always apply.
- `code-rules.md` - code conventions.
- `features.md` - features and manual test steps (awaiting on dev / confirmed on master).
- `LAUNCH.md` - release launchers, source commands and controls. Update when launch behavior or controls change.
- `design/` - game design docs, shared with the user. Source of truth for design; `concept.md` first.
- `.agent/memory/` - long-term project context. Entry: `INDEX.md`.
- `.agent/devlogs/` - append-only history.
- `.agents/skills/` - shared workflows. `.agents/agents/reviewer.md` - fresh-context reviewer brief.
- `CLAUDE.md` - compatibility import of this guide for Claude; shared instructions live here.

## Workflow
0. For every user request, follow-up and correction, perform the rules check in R14 before acting: read the current project rules, applicable workflows and design constraints, then apply them throughout the task. Recheck compliance before reporting completion.
1. Verify the branch and worktree (R5). Use `dev` for implementation, builds, launches and testing. If the current workspace is detached or on another branch, locate the existing `dev` worktree with `git worktree list` and work there. Do not treat a feature left only on a side branch as ready for the user to test from `dev`.
2. New request: use [clarify](.agents/skills/clarify/SKILL.md) (R4). Explicit `grill-me` / QuestionForms design interviews use [grill-me](.agents/skills/grill-me/SKILL.md); installation and connection details: [QuestionForms tooling](.agent/memory/project/questionforms.md).
3. Code or tooling change: use [feature-pipeline](.agents/skills/feature-pipeline/SKILL.md).
4. User confirms a merge to master: use [confirm-feature](.agents/skills/confirm-feature/SKILL.md). This includes the pre-merge review.
5. New user rule or preference: update `rules.md` and commit on `dev`.
6. New project facts: use [update-memory](.agents/skills/update-memory/SKILL.md). Notable events: use [add-devlog](.agents/skills/add-devlog/SKILL.md).

## Subagents
Never use subagents unless the user explicitly asks for them (R15). This overrides workflow steps that would otherwise spawn one; handle the work in the main agent until explicitly requested.
The main agent handles workflow setup and user coordination. Subagents follow the caller's brief, do not run the feature pipeline or commit, and put open questions in their report.

## Conventions
- All docs, memory and logs in English.
- Commit ongoing changes on `dev`. `master` changes only by merging `dev` after user confirmation.
- Never rewrite history on `master` or `dev`; undo with `git revert`.
