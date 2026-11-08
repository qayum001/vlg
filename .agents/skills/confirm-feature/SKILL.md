---
name: confirm-feature
description: Merge dev into master in vlg after the user explicitly says to ("merge", "confirm", "approved"). Runs the subagent review first, user picks fixes, then moves entries to "Confirmed" in features.md and merges. Never merge without explicit user confirmation.
---

# Confirm → master (R5, R6, R8)

Only on explicit user confirmation in chat.

1. On `dev`, tree clean. Run full test suite; failure → stop, report.
2. `git log master..dev`: everything listed gets merged. Contains unconfirmed work → ask: merge all, or first revert the unconfirmed feature commits per feature-pipeline "Rejected feature" (never devlog commits).
3. **Review** — `review-changes` skill on `master..dev`. Report findings, end the turn, wait for the user's pick.
4. User picked fixes → apply on dev via feature-pipeline steps 4–10 (devlog Type: fix, reference the review). User-visible change → update test steps in features.md. Picked none → skip.
   After the fixes, continue to step 5 without another review or confirmation; tests must pass.
5. `features.md`: move confirmed entries to "Confirmed (master)", set `Confirmed:` date. Memory: update `project/status.md` if needed. Commit on dev: `docs: confirm <features>`.
6. Devlog (Type: merge, Commits: merged feature commits, review: n findings / n fixed / n skipped) → commit `devlog: ...` on dev.
7. `git checkout master && git merge --no-ff dev -m "Merge dev: <features>"` (+ attribution line). Conflict → abort, report.
8. `git checkout dev`. master and dev now have identical content.
9. Report: merge commit hash, confirmed features.
