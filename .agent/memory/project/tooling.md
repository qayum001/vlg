# Tooling (this machine)
Updated: 2026-10-13
Related: [overview](overview.md), [open questions](open-questions.md)

- Blender 5.2.2 LTS (Steam): `C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe`. Not on PATH. Headless: `blender.exe --background --python <script>`.
- Blender MCP: package `mcp-for-blender` (formerly `blender-mcp`, repo ahujasid/blender-mcp), addon panel "MCP for Blender", socket port 9876. User sets it up (2026-10-13). Integrations (Poly Haven, Sketchfab, Hyper3D, Hunyuan, Poly Pizza) must stay off: art is agent-made only.
- `uv`/`uvx`: not installed until the user does the MCP setup.
- `claude` CLI on PATH is broken (npm shim → missing claude.exe). Don't rely on `claude mcp add`; use project `.mcp.json`.
- Available: node, python 3.13, dotnet, winget, git 2.45.
