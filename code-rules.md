# Code rules

Rules marked (R#) come from `rules.md`. The rest are agent defaults; the user may override.

## Comments (R1)
- Audience: agents. Write only what the code can't say: why, invariants, units, non-obvious constraints, links to memory/decisions.
- Short, plain. No restating the code, no tutorials, no "friendly" tone.
- No commented-out code (git has history).
- `TODO:` only with a concrete reason/condition.

## General
- Match surrounding code: naming, structure, idioms.
- Small focused units. No speculative abstractions, no unused code.
- Keep game logic separate from presentation so logic is unit-testable without the engine running.
- Prefer tuning values (numbers, balance) in data/config files over hardcoded constants.
- Fail loudly in dev builds. No silently swallowed errors.
- Per-frame code: avoid needless allocations and repeated lookups.

## Tests
- Every logic change gets automated tests.
- Bug fix: regression test that fails before the fix.
- A test must fail if the behavior it covers breaks. No tautologies, no asserting on mocks only.
- Report real test output. Never claim a pass without running.

## Dependencies and assets
- Ask the user before adding any third-party library, plugin, or asset.
- Check the license allows commercial release on Steam; record it (file TBD when the first one is added).

## Engine-specific
Engine not chosen. When chosen: add language style, project layout, test framework and commands here.
