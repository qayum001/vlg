---
name: confirm-feature
description: Merge dev into master in vlg after the user explicitly confirms tested work ("confirm", "merge", "approved"). Moves entries from "Awaiting confirmation" to "Confirmed" in features.md. Never merge without explicit user confirmation.
---

# Confirm → master (R5, R8)

Only on explicit user confirmation in chat.

1. On `dev`, tree clean. Run full test suite; failure → stop, report.
2. `git log master..dev`: everything listed gets merged. Contains unconfirmed work → ask: merge all, or first revert the unconfirmed feature commits per feature-pipeline "Rejected feature" (never devlog commits).
3. `features.md`: move confirmed entries to "Confirmed (master)", set `Confirmed:` date. Memory: update `project/status.md` if needed. Commit on dev: `docs: confirm <features>`.
4. Devlog (Type: merge, Commits: the merged feature commits) → commit `devlog: ...` on dev.
5. `git checkout master && git merge --no-ff dev -m "Merge dev: <features>"` (+ attribution line). Conflict → abort, report.
6. `git checkout dev`. master and dev now have identical content.
7. Report: merge commit hash, confirmed features.
