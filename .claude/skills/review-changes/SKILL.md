---
name: review-changes
description: Fresh-context review of vlg changes via the `reviewer` subagent — bugs, improvements, misleading logic, hallucinations, and the tests themselves. Runs only before merging dev to master (confirm-feature step 3), or when the user explicitly asks for a review. Report-only; fixes need the user's pick.
---

# Review changes (R6)

1. Range: `master..dev` by default (everything about to be merged). User asked for something else → that range.
2. Gather requirements for the range: devlog Requirements sections of the commits in range, plus their features.md entries.
3. Spawn subagent `reviewer`. Not available → `general-purpose` with `.claude/agents/reviewer.md` body as instructions. Give it:
   - the range and how to get the diff,
   - the requirements from step 2,
   - test commands.
   Don't pass your own reasoning or confidence — keep its view fresh.
4. Verify each finding yourself by reading the code: confirmed / rejected (with reason).
5. Report to the user, don't fix anything: confirmed findings (severity, file:line, problem, suggested fix), rejected ones with reason. End the turn; the user picks which to fix.
