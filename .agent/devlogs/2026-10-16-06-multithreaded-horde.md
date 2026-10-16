# Multi-threaded horde update
Date: 2026-10-16
Type: feature
Commits: 21ea8c1

## Summary
User: "can we fix that one thread issue?" Enemy update and GPU buffer fill now run on an allocation-free worker pool. RTX very_low went from 24 000 to ≥ 50 000 enemies (capacity cap); Vega low unchanged (GPU-bound).

## Requirements
- Told the user first: at the design budget (~1500 enemies) logic is ~0.6 ms, so this helps only big hordes on strong PCs. User confirmed the plan:
  - Parallelize enemy movement/separation + buffer fill with our own worker pool.
  - Threads = cores − 2. Same seed → identical results.
  - Done when the RTX very_low limit rises and Vega low doesn't regress.

## Details
- Profiled first (scratch console, 5800H, 24 000 enemies): enemy update 10.0 of 10.9 ms, buffer fill 0.36, projectiles 0.35.
- `Vlg.Core.Jobs.WorkerPool`: fixed background threads, SemaphoreSlim start + ManualResetEventSlim done, chunks via Interlocked, main thread works too, inline below parallelMinCount, worker exceptions rethrown on caller after all workers finish.
- EnemySwarm.Update split into Steer/Integrate range methods + cached job objects. EnemyRenderer FillJob. HordeSim takes optional pool.
- Data: sim.workerThreads -1, sim.parallelMinCount 2048. `--threads N`, `bench.ps1 -Threads`. HUD + CSV meta show threads.
- code-rules: parallel-loop rule (jobs write only own indices; threaded == single-thread test).

## Problems
- Tests caught `Dispose()` with 0 workers calling `SemaphoreSlim.Release(0)` (throws) → fixed.
- Allocation test flaked (24 B in 2 of 8 runs): one-time lazy lock object in ManualResetEventSlim when Wait first blocks. Test now allows < 256 B over 2000 runs (per-call allocation would be ≥ 48 KB).

## Results
- Tests: `dotnet test --project tests/Vlg.Core.Tests/Vlg.Core.Tests.csproj` → 72 passed, 8 runs in a row.
- Bench RTX very_low Mobile (ramp 4000..60 000): all pass; enemies capped at 50 000, p95 6.7 ms, sim 4.7 ms. At 24 000: sim 2.45 ms (was 10.1).
- Bench Vega low Mobile: best 4250 (same as before), sim at 4250 0.82 ms (was 1.92).

## Next
- If ever needed: off-screen culling (chunked MultiMeshes), parallel projectile hits with per-thread hit lists.
