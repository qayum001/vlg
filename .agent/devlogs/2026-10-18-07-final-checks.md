# Final checks after hub and assets
Date: 2026-10-18
Type: fix
Commits: 26dd0bb

## Summary
Finished the open items from the handoff (2026-10-18-06): icon redraw review, save → run → hub checks, two fixes.

## Requirements
- User goal: "keep working till implement everything you said", then "continue" after the usage limit.

## Details
- Icon redraws reviewed: 6 fine; sneakers still showed brand-like side stripes, redrawn twice more (pair → six shoes → one shoe with color blocks). ComfyUI started for it and stopped after.
- Verified by launching: save with custom setlist + meta ranks → run uses the setlist and ultimate, max HP bonus applies, skill icon row shows; leaving the run banks (`runs` +1); hub reads meta credits/runs from the save.
- Found: HUD "HP 96 / 95" (current HP rounded up, max rounded to nearest) → max rounds up too.
- Found risk: a saved setlist with a removed skill would fail startup → `GameSession.Sanitize` falls back to defaults; checked with an outdated save in the hub.
- Removed the save file the smoke runs wrote, so the user starts fresh.

## Problems
- The E-key paths (portal, Fixer purchase, Setlist picker) can't be pressed by the agent; covered by core tests and code reading only.

## Results
- Tests: 266 passed. Smoke: run with saved setlist, hub with outdated save; no errors.

## Next
- User test of all awaiting features in features.md, then "merge".
