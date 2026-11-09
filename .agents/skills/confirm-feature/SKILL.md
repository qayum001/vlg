---
name: confirm-feature
description: Merge dev into master after explicit user approval. Record Approved for merge, run the reviewer subagent, wait for the user's selection of fixes, merge, then record Confirmed only after the implementation reaches master.
---

# Approve -> review -> merge -> confirm (R5, R6, R8)

Only on explicit user approval in chat. Approval to merge and a completed merge are different states.

1. On `dev`, tree clean. Run full test suite; failure -> stop, report.
2. `git log master..dev`: everything listed gets merged. Map the user's approval to those features. Move covered entries from "Awaiting confirmation (dev)" to "Approved for merge (dev)", set `Approved for merge:` to the approval date, and keep `Confirmed: -`. Commit on dev: `docs: approve <features> for merge`. Contains work outside that approval -> ask: merge all, or first revert the unapproved feature commits per feature-pipeline "Rejected feature" (never devlog commits).
3. **Review** - `review-changes` skill on `master..dev`. R6 authorizes the reviewer subagent. Report findings, end the turn, wait for the user's pick. Keep this pause even when the review has no findings.
4. User picked fixes -> apply on dev via feature-pipeline steps 4-10 (devlog Type: fix, reference the review). User-visible change -> update test steps in features.md. Picked none -> skip.
   After the fixes, continue to step 5 without another review or confirmation; tests must pass.
5. Keep the approved entries in "Approved for merge (dev)" while merging the implementation. `git checkout master && git merge --no-ff dev -m "Merge dev: <features>"` (+ attribution line). Conflict -> abort, report; entries remain approved, never confirmed. Record the successful implementation merge hash.
6. `git checkout dev && git merge --ff-only master`. Only after the implementation is verified on master, move its entries to "Confirmed (master)" and set `Confirmed:` to the successful merge date. Preserve the approval date. Update memory `project/status.md` as needed. Commit on dev: `docs: confirm merged <features>`.
7. Devlog (Type: merge, implementation merge hash, merged feature commits, review: n findings / n fixed / n skipped) -> commit `devlog: ...` on dev.
8. Publish that confirmation/log bookkeeping through `git checkout master && git merge --ff-only dev`, then `git checkout dev`. All master changes still arrive by merging dev (R5); master and dev now have identical content. If this bookkeeping merge fails, report the successful implementation merge separately from the pending bookkeeping.
9. Report: implementation merge hash, confirmed features, and any remaining bookkeeping.
