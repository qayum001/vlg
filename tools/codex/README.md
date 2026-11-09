# Automatic Codex project rules

Codex hooks load the complete current `AGENTS.md`, `rules.md`, `code-rules.md` and `.agent/memory/INDEX.md` before a session starts/resumes/compacts and before each submitted prompt. The loader identifies this repository by its common Git directory, so linked worktrees are covered even when their checkout predates `AGENTS.md`. Other repositories receive no context.

The loader reads the live `dev` checkout, including instruction edits. If there is no `dev` checkout, it reads the committed `dev` files and labels that source. Relevant workflow and design documents remain selected according to the request and the loaded project rules.

## Install on this computer

Run from the existing `dev` checkout:

```powershell
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File tools/codex/install-project-rules.ps1
```

This adds `SessionStart` and `UserPromptSubmit` entries to the active Codex home `hooks.json` (normally `%USERPROFILE%\.codex\hooks.json`). Existing hooks are preserved and backed up before changes. Repeating installation from the same checkout does not duplicate entries. The command stores the absolute loader path in this `dev` checkout, so keep it available.

Then review and trust both definitions in Codex **Settings > Hooks**, **Review hooks** in the composer, or `/hooks` in the CLI. Codex skips untrusted hooks. Open a new session after activation. Changes to a definition require another review.

The hook only reads Git metadata and the four instruction files. It does not execute their contents, edit files, access the network, or approve tool operations. A missing/empty required file or output larger than 64 KiB stops a matching session/prompt with an explanation. The handler uses `additionalContextLimit: 0` within this explicit cap so Codex receives the complete rule text rather than an abbreviated preview. Hook execution failures and untrusted/disabled hooks are still subject to Codex's own behavior; loading instructions does not guarantee perfect model compliance.

To remove the integration, delete these two loader entries in the Codex home `hooks.json`, preserving unrelated hooks. The normal repository `AGENTS.md` remains in place.

## Validation

```powershell
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File tools/codex/test-project-rules.ps1
```

The integration checks cover session/prompt output, UTF-8, old worktrees, subdirectories, unrelated repositories, non-repository directories, unsupported events, missing/oversized rules, committed-dev fallback, and installer preservation/idempotence.

Official reference: https://learn.chatgpt.com/docs/hooks
