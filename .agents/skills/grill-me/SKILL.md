---
name: grill-me
description: Run an explicitly requested vlg design interview through QuestionForms and record agreed decisions in design/. Use when the user invokes grill-me, asks to be grilled, or requests a QuestionForms design interview.
---

# Grill me for vlg

Use the installed QuestionForms-adapted `grill-me` skill for the interview. This project workflow adds game context and design recording.

## Load the interview instructions
1. Read [QuestionForms tooling](../../../.agent/memory/project/questionforms.md) to resolve the installed skill directory, then read its `SKILL.md`. Read its `references/questionforms.md` when using the bundled Python bridge.
2. Follow that installed skill for neutral questions, service operations, submitted answers, session/round IDs, bounded waits, recovery, resume and final confirmation. Prefer connected QuestionForms MCP tools; otherwise use the installed bridge against the existing local service.
3. Load [concept](../../../design/concept.md), the area's design document when present, and the [memory index](../../../.agent/memory/INDEX.md). Do not ask settled decisions again. No interview starts until the user provides a topic.

## Record the game design
- Use one file per area, `design/<area>.md`, linked from `design/concept.md`. Keep Decided, Tree (open decisions and prerequisites), and Parked sections.
- Each round covers the current decision frontier, normally at most five questions, most foundational first. Ask one decision per question in short, direct English.
- Investigate facts from the environment yourself. Delegate only when the user explicitly requests subagents (R15).
- After a submitted round, record agreed decisions using the user's words for names and themes; add newly opened branches to Tree and out-of-scope ideas to Parked. Commit the design document on `dev` before the next round.
- Check decisions against the game concept, min-spec performance, art ownership, single-player scope and existing decisions. Explain conflicts and resolve them with the user. Humanoids remain the user's responsibility (R11).
- Keep the original QuestionForms session link throughout the interview. Written input may answer multiple questions or introduce new ideas; read it fully and clarify conflicts before recording a decision.
- When the frontier is resolved or explicitly deferred, ask for confirmation of the summary through the same session. After confirmation, close the session, update project memory with links to the design document, and add a dev log.
- This workflow does not authorize implementation or replace ordinary R4 clarification for other tasks.

The installed adaptation and its MIT license come from `qayum001/QuestionForms/skills/grill-me`, adapted from Matt Pocock's `grill-me` / `grilling` skills.
