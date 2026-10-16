# Telegram notify MCP added
Date: 2026-10-14
Type: tooling
Commits: 0dce874

## Summary
User's Telegram status bot registered as MCP `agent_status` in .mcp.json. Rule R10: notify only when the user asks.

## Details
- User said "MCP on localhost:9000". Port 9000 (docker `notificationsbot-status-bot-1`) is only an HTTP API (/health, /status); MCP paths 404. The real MCP is stdio `mcp_server.py` in `C:\Projects\Backend\Notifications bot`, run with its .venv python.
- Stdio handshake tested from another cwd: initialize + tools/list OK, tool `send_agent_status`. No test message sent.

## Problems
- None.

## Results
- Loads after session restart.
