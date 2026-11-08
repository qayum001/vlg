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

Use `dev` for implementation, builds, exports, launches and manual test handoffs unless the user explicitly requests another branch. Check the current branch and `git worktree list` before acting. If the supplied workspace is detached or on another branch, use the existing `dev` worktree. A separate branch required by the host must start from current `dev`; integrate its completed feature into `dev` and verify it there before reporting it ready. Do not leave completed work only on a side branch or silently build from `master`.
Added: 2026-10-13
Changed: 2026-11-08

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

## R11. Humanoids are the user's
The agent never models the hero, NPCs or other human-like entities, humanoid enemies included (zombies, skeletons, ghouls). The user makes them; the game uses placeholders until then. The agent makes the rest: non-humanoid monsters, items, props, VFX, icons.
Added: 2026-10-18

## R12. ComfyUI
The agent may use ComfyUI for assets, launch it itself, and install ComfyUI modules (custom nodes, models) without asking. Sources: ComfyUI registry / Manager, official Hugging Face or maintainer pages.
Added: 2026-10-18

## R13. Shared agent workflow
The project rules and workflows apply to every coding agent. `AGENTS.md` is the shared entry point; skills and reviewer briefs live in `.agents/`. `CLAUDE.md` may import the shared guide for compatibility, but must not contain a separate set of project instructions.
Added: 2026-11-08

## R14. Apply all project rules to every request
After every user request, including follow-ups and corrections, read and apply all project rules before acting. Check `AGENTS.md`, all of `rules.md` and `code-rules.md`, the relevant shared workflows, and the applicable design rules. Verify the branch/worktree and current `dev` instructions first so an older checkout cannot hide newer rules. Reconcile the request with these rules, pass the applicable constraints to subagents, and check compliance again before reporting completion. Do not silently skip a rule; explain any conflict with a higher-priority instruction or an actual blocker.
Added: 2026-11-08
