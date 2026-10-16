---
name: grill-me
description: Relentless design interview in vlg. Use when the user asks to be grilled, to stress-test a plan, or to brainstorm a design area (theme, entities, systems, progression) until every decision is made. Rounds of numbered questions with recommended answers; decisions land in design/.
---

# Grill me

Adapted from `grilling` in github.com/mattpocock/skills (MIT).

Goal: shared understanding of a design, nothing silently assumed. The design is a decision tree; resolve it branch by branch.

## Procedure
1. Load context: `design/concept.md`, the area's doc in `design/` if it exists, memory `INDEX.md`. Decided items are not asked again.
2. Doc: one file per area, `design/<area>.md`, linked from `concept.md`. Sections: Decided, Tree (open decisions + what each depends on), Parked.
3. Round = the frontier: open decisions whose prerequisites are all decided. Max ~5 per round, most foundational first. The rest waits.
4. Question format:
   ~~~
   **Q1. <title>** <question, 1-2 lines>
   a) ...  b) ...  c) ...
   ➡️ b) <one-line why>
   ~~~
   Word it so "yes" accepts every recommendation. User answers per number ("Q2 c", "Q3 <own idea>") or "yes".
5. Facts vs decisions: facts (code, engine limits, perf budget, how reference games do it) → look up yourself (WebSearch, subagent if large). Ask the user only for decisions.
6. After each reply:
   - Record decisions in the doc (user's words for names and themes).
   - Answer conflicts with an earlier decision, `concept.md`, min spec or art approach → say so once with the trade-off, then accept the user's call.
   - Answers that open new branches → add to Tree.
   - Ideas outside the session scope → Parked.
   - Commit the doc on dev (`docs:`), then send the next round.
7. Done: frontier empty, Tree empty or only items the user chose to defer. Send a short summary of the tree and ask the user to confirm shared understanding. No implementation in this skill.
8. After confirmation: project facts → `update-memory` (link the doc, don't copy it), session → `add-devlog`.

## Rules
- One decision per question. No compound questions.
- Every question has a recommendation with a reason.
- Short, direct English (user is non-native). Free-text answers are common: read them fully, they may answer several questions or add new ones.
- Check new ideas against constraints: agent-made low-poly art, hundreds of enemies on min spec, single-player, small-team content scope.
