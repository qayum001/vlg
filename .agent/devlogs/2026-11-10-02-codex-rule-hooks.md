# Automatic Codex project rules
Date: 2026-11-10
Type: tooling
Commits: c71e63d

## Summary
Prepared and installed read-only SessionStart and UserPromptSubmit hooks that supply the current dev rule files automatically.

## Requirements
- New Codex sessions must receive this project's rules without a reminder in each user prompt.
- Cover older linked worktrees and preserve existing project rules and unrelated settings.

## Details
- Loader and idempotent installer: tools/codex/. Setup and removal: tools/codex/README.md.
- Installed in C:/Users/qayum/.codex/hooks.json; Codex home AGENTS.md remains empty.
- Git common-directory comparison scopes output to this project; the live dev checkout supplies the four complete instruction files.
- Missing/empty/oversized required instructions stop matching sessions/prompts; the script caps context at 64 KiB.
- Codex 0.167.0-alpha.3.2 reports hooks enabled. Its mandatory trust review has not been completed; automatic execution remains pending user activation.

## Results
- Hook integration checks: 12 passed, including preservation/idempotence of installation.
- Real older-worktree invocation: all four current instruction files present, 13,798 UTF-8 bytes.
- dotnet test --project tests/Vlg.Core.Tests/Vlg.Core.Tests.csproj: 275 passed, 0 failed, 0 skipped.
- No gameplay code changed; no interactive game test or new game build was needed.

## Next
- User reviews and trusts both hook definitions in Codex Settings > Hooks, then opens a new vlg chat.
