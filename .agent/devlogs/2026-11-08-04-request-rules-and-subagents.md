# Every-request rules and explicit subagent requests
Date: 2026-11-08
Type: tooling
Commits: 96c10fe, 05e8d1f

## Summary
Every user request now starts with a complete rules check. Subagents require an explicit user request.

## Requirements
- User asked to apply all project rules after every request and encode that in the pipeline.
- User then prohibited subagents unless explicitly requested.

## Details
- R14 added to rules.md, AGENTS.md, clarify and feature-pipeline.
- R15 added to rules.md, the shared guide and feature pipeline.
- Existing subagents were stopped when R15 was received.
- Both rule commits were integrated into dev immediately.

## Problems
- The supplied worktree was based on old master and hid newer dev rules/systems. Its obsolete Pyromancer work was stashed for reference, and the feature branch was reset to current dev before continuing.

## Results
- Documentation changes; no runtime behavior changed by these rule commits.

## Next
- Complete Pyromancer using current dev's established hero and ability systems, keeping the capsule placeholder under R11.
