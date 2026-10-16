# Telegram MCP moved to Docker
Date: 2026-10-14
Type: tooling
Commits: f97647a

## Summary
User moved the bot's MCP server into the Docker image. .mcp.json now runs it via `docker compose exec -T status-bot`. Test message sent and delivered ("Status sent.").

## Details
- Host .venv removed by the user; old .mcp.json entry would have failed.
- Attachments limited to the bot project dir (read-only /workspace mount).

## Problems
- Git Bash path mangling on `docker exec ... ls /app` → use MSYS_NO_PATHCONV=1.

## Results
- Tool loads after session restart.
