# Subagent review moved to pre-merge only
Date: 2026-10-13
Type: tooling
Commits: bab3b36

## Summary
Code review by reviewer subagent now runs only before merging dev to master, not per feature.

## Requirements
- User: "review with subagent should be applied only before merging to master branch".
- Clarify answers: per feature → no review at all; review runs when user says "merge"; findings → report only, user picks fixes, then merge.

## Details
- rules.md R6 edited in place (Changed: 2026-10-13).
- feature-pipeline: review step removed, steps renumbered 1–10.
- confirm-feature: step 3 review on master..dev → user picks → fixes via pipeline steps 4–9 → merge without another review/confirm.
- review-changes: report-only, requirements gathered from devlog Requirements sections + features.md.
- add-devlog template: new Requirements section, since the merge-time reviewer may run in a later session.

## Problems
- None.

## Results
- Tests: none (docs/config). Grep for stale step/review references: clean.

## Next
- Pending: user "merge" → first pre-merge review over all setup commits.
