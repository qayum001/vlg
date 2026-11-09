# QuestionForms interview tooling
Updated: 2026-11-10
Related: [tooling](tooling.md), [project interview workflow](../../../.agents/skills/grill-me/SKILL.md)

## Entry points
- Explicit invocation: `$grill-me <topic>` or an explicit request for a QuestionForms interview. Installation alone does not start an interview.
- Project entry: `.agents/skills/grill-me/SKILL.md`; it adds vlg design-document requirements to the installed QuestionForms skill.
- Read the installed `SKILL.md` before interviewing: `$CODEX_HOME/skills/grill-me/SKILL.md`, or `~/.codex/skills/grill-me/SKILL.md` when `CODEX_HOME` is unset.
- On this machine: `C:/Users/qayum/.codex/skills/grill-me/SKILL.md`. `C:/Users/qayum/.agents/skills/grill-me` is a junction to that installed directory. The installed UI metadata disables implicit invocation.
- Installed files: `SKILL.md`, `agents/openai.yaml`, `references/questionforms.md`, `scripts/questionforms.py`, `LICENSE`. They were downloaded individually from `https://raw.githubusercontent.com/qayum001/QuestionForms/main/skills/grill-me/`; no repository clone, archive or application source is needed.
- The project workflow and installed skill share the name `grill-me`; read both when interviewing in vlg. The project file supplies context and design recording, while the installed file owns QuestionForms operations.

## Connection
- Browser and HTTP API origin: `http://localhost:8000`. MCP endpoint: `http://localhost:8000/mcp`. No authentication or API key.
- Prefer connected QuestionForms MCP tools when available; otherwise use the installed `scripts/questionforms.py` bridge. It uses Python 3.10+ and the standard library. Resolve it from the installed skill directory, not the repository.
- Full operation names, arguments and response shapes: installed `references/questionforms.md`. Pass the supplied service origin with `--base-url` or `QUESTIONFORMS_BASE_URL`.
- Use the user's already-running local service. Do not start another application instance or Docker container. If unreachable, report the error and preserve known session/round IDs.
- Read-only connection check (PowerShell on this machine):

```powershell
'{"limit":1}' | python -B 'C:/Users/qayum/.codex/skills/grill-me/scripts/questionforms.py' list_sessions --base-url http://localhost:8000
```

## Interview state
- Keep the session ID and stable session response URL, plus each round's form ID. Reuse the same session for follow-ups; resume by reading it and its submitted answers before creating a new round.
- Questions are neutral and dependency-aware. Consume submitted `answers` and `free_text`; a chat acknowledgement or missing field is not an answer. Never submit on the user's behalf.
- Follow the installed skill's bounded waits and recovery rules. Do not automatically retry uncertain creation calls or choose an unrelated recent session.
- Record agreed game decisions in `design/` through the project workflow. Close the service session only after the user confirms the summary. Interview completion does not authorize implementation.
