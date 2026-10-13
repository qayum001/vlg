---
name: review-changes
description: Fresh-context review of new changes in vlg via the `reviewer` subagent — bugs, improvements, misleading logic, hallucinations, and the tests themselves. Use after code is written and tested (feature-pipeline step 6), or when the user asks for a review.
---

# Review changes (R6)

1. Define the range: uncommitted (`git diff HEAD` + untracked files from `git status --short -uall`), `master..dev`, or specific commits.
2. Spawn subagent `reviewer`. Not available → `general-purpose` with `.claude/agents/reviewer.md` body as instructions. Give it:
   - the range and how to get the diff,
   - the clarified requirements (user's request + answers, verbatim where possible),
   - test commands used.
   Don't pass your own reasoning or confidence — keep its view fresh.
3. Triage each finding yourself by reading the code: confirmed → fix; wrong → note why; out of scope → tell the user.
4. Re-run tests after fixes. Substantial fixes → another review round. Max 2 rounds, then report leftovers to the user.
5. Report: findings count, fixed, rejected with reason.
