# Shared agent workflows and dev handoffs
Date: 2026-11-08
Type: tooling
Commits: c9c3363

## Summary
Renamed .claude to .agents and made AGENTS.md the shared project guide.
Clarified the existing requirement to use dev for ongoing work and gameplay test handoffs.

## Details
- R5 now explicitly covers implementation, builds, exports, launches and test handoffs. Agents locate the dev worktree before acting and integrate completed host-isolated work into dev before calling it ready.
- R13 records that project workflows apply to every coding agent.
- Migrated all seven skills and the reviewer brief; updated live path references and feature-pipeline step references.
- CLAUDE.md imports AGENTS.md for compatibility. Removed the Claude-only echo hook; its reminder is part of the shared guide.
- Added the shared personal-settings ignore and retained the old tool-specific personal-settings ignore.
- Historical devlogs keep their original paths.

## Problems
- The dev requirement already existed in R5, but the audio-settings feature had been left on a separate branch. The updated entry point makes the required handoff explicit.

## Results
- skill-creator quick_validate.py passed for all seven migrated skills.
- All AGENTS.md links and the reviewer brief resolve; active skill files contain no old .claude references.
- The old directory is absent. Git recognizes the workflow files as renames.
- git diff --check and staged diff checks passed.
- Documentation and instruction changes only; game tests and launch smoke tests were not repeated.

## Next
- Future feature work and test handoffs use dev according to R5.
- Audio-settings integration remains separate work.
