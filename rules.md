# Rules

User-declared rules. Apply to every agent and every task.
New rule → next number + date. Changed rule → edit in place, add `Changed:` date.

## R1. Comments are for the agent
Code comments are written for agents only. As simple as possible. No "friendly" or user-oriented explanations. Details: `code-rules.md`.
Added: 2026-10-13

## R2. Memory
Agent memory lives in `.agent/memory/` (in the repo, committed): a directory of small, well-separated markdown files that link to each other, entry point `INDEX.md`. Agent-only long-term context. Keep it small so any agent can load just what it needs. Update after every code change (`update-memory` skill).
Added: 2026-10-13

## R3. Dev logs
Dev logs live in `.agent/devlogs/` (in the repo, committed), separate from memory. Append-only: add new logs, never edit old ones (`add-devlog` skill).
Added: 2026-10-13

## R4. Clarify first
The user may drop details unintentionally. Every new request starts with clarifying questions (`clarify` skill). Skip only for answers to the agent's questions and short go-aheads ("yes", "merge", "continue").
Added: 2026-10-13

## R5. Git branches
At least two branches: `master` (confirmed work) and `dev` (all ongoing work). The agent commits every change to `dev` without asking. `master` changes only by merging `dev` after the user confirms. Keep `.gitignore` up to date.
Added: 2026-10-13

## R6. Code review
Per feature: write code → test, no review. Review runs only before merging `dev` to `master`, when the user says merge: a fresh-context reviewer subagent reviews everything being merged, including the tests, for bugs, improvements, misleading logic and hallucinations (`review-changes` skill). Findings are reported only; the user picks which to fix, then merge.
Added: 2026-10-13
Changed: 2026-10-13

## R7. Rules file
All user-declared rules, current and future, live in this file.
Added: 2026-10-13

## R8. Features list
`features.md` lists implemented features with manual test steps. A feature counts as confirmed once merged to `master`.
Added: 2026-10-13

## R9. Blender MCP roles
Seeing the scene (screenshots, viewport checks): community MCP `look` (`mcp__blender__*`). Renders: official MCP (`mcp__Blender__render_*`).
Added: 2026-10-13

## R10. Telegram notifications
Notify the user via Telegram (`agent_status` MCP, see memory `tooling.md`) only when they ask for it in that task.
Added: 2026-10-14
