# Automatic Codex project rules

One read-only `SessionStart` hook loads the complete current `AGENTS.md`, `rules.md`, `code-rules.md` and `.agent/memory/INDEX.md`. Codex fires this event on startup, resume, clear, compaction and fork. There is no vlg `UserPromptSubmit` hook.

## What the hook actually does

1. Read the hook's event and working directory from standard input.
2. Compare the session's common Git directory with this repository's. Other repositories get no output; linked worktrees are covered even when their checkout predates `AGENTS.md`.
3. Locate the live `dev` checkout and read the four files above, including instruction edits. If no dev checkout exists, read their committed `dev` versions instead.
4. Return one JSON `hookSpecificOutput.additionalContext` containing a source label and each complete file once. Codex places that text in the agent's context.

The hook does not run clarification, reviews, the feature pipeline, or any other workflow. It does not execute the rule text, edit project files, access the network, approve operations, or intercept ordinary prompts. R14 still tells the agent to check the current applicable instructions on every request; the hook's only job is supplying context at session lifecycle events.

Missing, empty or oversized required files produce a short diagnostic in `systemMessage` and `additionalContext`. The loader exits successfully and never returns a blocking decision, so requests to inspect or repair those files remain possible. The 64 KiB limit rejects the combined context instead of silently truncating rules. Successful output uses `additionalContextLimit: 0` within that cap.

## Install on this computer

Run from the existing `dev` checkout:

```powershell
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File tools/codex/install-project-rules.ps1
```

The installer configures one `SessionStart` entry in the active Codex home's `hooks.json` (normally `%USERPROFILE%\.codex\hooks.json`). It removes this loader's old `UserPromptSubmit` entry and duplicate session entries, preserving unrelated handlers and settings. It backs up the file before a change; repeating installation from the same checkout does not change the configuration or create another backup.

The definition stores the absolute loader path in this dev checkout, so keep it available. If Codex requests trust, inspect that definition in **Settings > Hooks**, **Review hooks** in the composer, or `/hooks` in the CLI. Codex skips untrusted hooks; hook definition changes may require review. Open a new session to check activation.

To remove the integration, delete this loader's `SessionStart` entry in `hooks.json`, preserving unrelated hooks. The normal repository `AGENTS.md` remains in place. Disabled/untrusted hooks and execution failures remain subject to Codex's behavior; loading instructions does not guarantee perfect model compliance.

## Inspect the returned context

This runs the same read-only loader and prints exactly the instruction text it supplies:

```powershell
$inputJson = @{ hook_event_name = 'SessionStart'; cwd = (Get-Location).Path; source = 'startup' } | ConvertTo-Json -Compress
$result = $inputJson | powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File tools/codex/load-project-rules.ps1 | ConvertFrom-Json
$result.hookSpecificOutput.additionalContext
```

## Validation

```powershell
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File tools/codex/test-project-rules.ps1
```

The 20 integration checks cover lifecycle output, no repeated prompt context, UTF-8, old worktrees, subdirectories, unrelated/non-repository directories, unsupported events, missing/empty/oversized rules and recovery, committed-dev fallback, plus installer preservation, deduplication and idempotence.

Official reference: https://learn.chatgpt.com/docs/hooks
