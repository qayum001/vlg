# Codex rule hook
Updated: 2026-11-10
Related: [tooling](tooling.md), [status](status.md), [hook documentation](../../../tools/codex/README.md)

- Project loader: `tools/codex/load-project-rules.ps1`; installer and isolated integration checks are beside it.
- Local definition: `%USERPROFILE%\.codex\hooks.json` (or the active `CODEX_HOME`), with an absolute loader path in the existing `dev` checkout. Reinstall if that path moves.
- One `SessionStart` handler supplies AGENTS.md, rules.md, code-rules.md and the memory index on startup/resume/clear/compact/fork. No UserPromptSubmit handler or prompt interception.
- Repository matching uses the common Git directory, covering older linked worktrees. Content comes from live dev, or committed dev if no dev checkout exists.
- Missing/empty/oversized rules yield diagnostics, not blocking decisions. The hook only supplies context; agents select and apply relevant workflows themselves.
- Installer removes this loader's legacy prompt handler/duplicate session entries, backs up changed config, and preserves unrelated hooks. Existing Codex trust controls remain in force.
