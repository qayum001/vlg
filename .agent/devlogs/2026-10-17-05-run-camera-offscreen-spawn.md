# Run camera lower and farther, off-screen spawning
Date: 2026-10-17
Type: feature
Commits: 94e5722

## Summary
After playing the merged run, the user asked for a lower, farther camera. Tilt 55° → 40° below the horizon, distance 22 → 28 m. Spawning now follows the camera view so walkers never pop in on screen.

## Requirements
- User: "make the camera view angle lower, closer to the ground, -15 degrees; place the camera further".
- Clarify answers: lower by 15° → 40° below the horizon; 28 m distance.
- Assumptions stated in the report: camera values in run.json; stress test camera unchanged (bench comparability); spawn change needed because the new view reaches ~54 m (16:9) past the old 30-36 m spawn ring; concept.md already says spawning depends on camera rotation.

## Details
- `CameraSettings` in run.json; `OrbitCamera` takes tilt, distance, fov and follow rate as params.
- `ViewFootprint` (Core): the ground quad hit by the 4 screen-corner rays, rotated by yaw. `WalkerSpawner` spawns and recycles at edge + viewMargin (6) + [0, band 6). That is about 18 m behind the hero and 41-60 m ahead.
- Validation: the top edge must be ≥ 5° below the horizon; walker and gem despawn radii (80 m) must exceed the farthest visible point at 21:9.
- The run uses 5x5 prop tiles and an 80 m sun shadow distance.

## Problems
- None. Headless balance sim is about the same as before: standing still dies at ~2 min; moving bots reach level 19-21 at 20 min.

## Results
- Tests: `dotnet test --project tests/Vlg.Core.Tests/Vlg.Core.Tests.csproj` → 149 passed, 0 failed.
- Source run screenshot at 15 s: lower view, walkers entering from the edges, props cover the view.

## Next
- User checks the feel; tilt/distance can be tuned in run.json without code.
