---
name: clarify
description: First step for every new user request in vlg (task, feature, fix, change, rule, research). Builds clarifying questions from a fixed checklist before any work starts. Skip only for answers to the agent's own questions and short go-aheads ("yes", "merge", "continue").
---

# Clarify

R4: the user may drop details unintentionally. Ask before acting.

## Procedure
0. Apply the every-request rules check (R14) before deciding what to ask or do, including on follow-ups and corrections.
1. Extract explicit requirements from the request. Check `rules.md` and memory `INDEX.md` for related context.
2. Run the checklists below. Mark each item: given / inferable (code, memory, rules) / unknown.
3. Ask about unknowns that change what gets built. Each question gets a recommended default.
   - AskUserQuestion, max 4 per call. More → most important first, rest in a second round.
   - Don't ask what the repo or memory already answers.
4. List the assumptions taken for "inferable" items so the user can correct them.
5. End the turn with the questions. Nothing unknown → assumptions + one confirm question. No work before the reply.
6. Answers that state a general way of working → `rules.md`. Project facts → memory (`update-memory`). Commit these on dev as their own `docs:` commit before starting the task.

## Always
- Goal: what outcome, why?
- Scope: what's in, what's explicitly out?
- Done: what does the user see when testing manually? Acceptance criteria.
- Conflicts: clashes with `rules.md`, existing features, memory decisions?
- Priority: interrupt current work or queue? Unconfirmed work on dev?

## By request type
Gameplay feature
- Input: keyboard+mouse, gamepad, Steam Deck?
- Feedback: visuals, audio, UI text?
- Edge cases: fails, repeats, interrupted, spammed?
- Numbers: concrete values or placeholders? Tunable via data?
- Persistence: saved between sessions?
- Assets: placeholders ok?
- Interaction with existing systems.

UI
- Where/when shown, navigation (mouse + gamepad), resolutions/aspect ratios, text ready for localization?

Bug fix
- Repro steps, expected vs actual, frequency, since when, logs/screenshots?

Refactor / tech
- Behavior must stay identical? What may change? Performance targets?

Content / data
- Format, who edits it, validation?

Process / rules / tooling
- Applies to past work too? Scope (all agents, code only, docs)?

Steam / release
- Platforms (Windows/Linux/macOS/Deck), Steamworks features (achievements, cloud saves, overlay), store requirements?
