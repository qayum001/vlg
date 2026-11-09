# Rule audit fixes and hook recovery
Date: 2026-11-10
Type: fix
Commits: 8688a58, fe0ba92, 97fff86

## Summary
Applied the user's rule-audit selections on dev and migrated the installed hook to context loading only.

## Requirements
- Fix hook recovery; hook only supplies rules to context, without repeated per-prompt work.
- Review authorization overrides the general subagent restriction; retain thorough clarification through all needed rounds.
- Add Approved for merge; Confirmed only after implementation reaches master. Preserve the review pause even with no findings.
- Return development to dev and refresh stale project memory. Skip audit items 7 and 10; retain rules accepted under item 5.

## Details
- SessionStart covers startup/resume/clear/compact/fork and loads four complete dev instruction files once per event; UserPromptSubmit now returns nothing.
- Missing, empty and oversized rules return bounded diagnostics, never blocking decisions. Repair requests can proceed.
- Installer removes this loader's duplicate/session-prompt entries, preserves unrelated hooks, backs up changed config, and is idempotent.
- Local hooks.json migrated to one SessionStart handler and no vlg prompt handler. SessionStart definition stayed identical; no trust settings were modified.
- Updated R4/R6/R8/R15, shared workflows, features and memory for current master content, heroes, remote, assets and hooks. Preserved QuestionForms additions.
- Saved requested correction note in Codex ad-hoc memory. Actual loader preview: build/codex/project-rules-context.md (ignored), 15485 UTF-8 bytes.
- Implementation and tests used the existing dev checkout at C:\Projects\Games\vlg.

## Problems
- Original loader regression reproduced its blocking continue field when rules were missing.
- Expanded installer fixture exposed PowerShell scalar-array unwrapping with existing hooks; fixed by explicit array capture.
- Normalized an embedded carriage return introduced while adding a memory cross-link.

## Results
- tools/codex/test-project-rules.ps1: 20 integration checks passed, including recovery and migration preservation/idempotence.
- dotnet test --project tests/Vlg.Core.Tests/Vlg.Core.Tests.csproj: 275 passed, 0 failed/skipped.
- Real linked-worktree loader: all four current files present exactly once; ordinary prompt output empty.
- Updated memory links/lengths and git diff --check passed. Game build/launch not applicable to these tooling/doc changes.

## Next
- Await user testing/merge approval; required pre-merge reviewer and result pause remain. No merge or push performed for this task.
