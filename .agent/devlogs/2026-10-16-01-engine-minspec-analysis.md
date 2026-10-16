# Engine choice + min-spec budget analysis
Date: 2026-10-16
Type: investigation
Commits: a899a9d, 03e7681

## Summary
User asked to choose Unity vs Godot for the 3D survivors-like and to know compute limits at 4 GB RAM + 1 GB VRAM. Wrote `design/engine.md`; decision pending.

## Details
- Clarify answers: platforms Windows + Steam Deck; min spec 60 fps low preset, 1080p output, render scale may drop; user knows neither engine; paper analysis now, stress test after the decision.
- Researched current state: Godot 4.7.2 stable (2026-08-18), Unity 6.3 LTS (to 2027-12), Unity Personal $200k cap, Unity MCP beta needs Unity Cloud + subscription, Steam survey 2026-09 (4 GB RAM 1.15%, ≤1 GB VRAM 4.04%).
- Dev laptop: Ryzen 7 5800H (Vega 8 iGPU), RTX 3070 Laptop, 32 GB. Vega 8 = tier B / Deck stand-in.
- Conclusion: at 4 GB / 1 GB the wall is GPU throughput (0.4–0.8 TFLOPS, transparent VFX overdraw ~3–5 screen layers, ~300–500 visible enemies), not memory size. Whole game ≤ ~1.2–1.5 GB incl. shared iGPU memory.
- Recommendation: Godot 4.7 + C# (text scenes, headless tests/exports, `dotnet test` on pure logic, free). Min spec → 8 GB / 2 GB (≈ Deck), keep a "very low" preset for 4 GB / 1 GB.

## Problems
- Tier A (4 GB / 1 GB) cannot be measured on the dev laptop; numbers are estimates.
- Third-party spec sites for Megabonk/Soulstone gave junk/conflicting data; not used.

## Results
- Tests: none (docs only).

## Next
- User decision: engine, min spec. Then `code-rules.md` engine section, `.gitignore`, decision memory file, horde stress test prototype.
