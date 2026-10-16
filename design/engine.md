# Engine choice and min-spec budget

Status: decided 2026-10-16 — Godot 4.7 + C#, min spec 8 GB RAM + 2 GB VRAM
Updated: 2026-10-16

Inputs (from `concept.md`): 3D survivors-like, hundreds of enemies, heavy particles/VFX, stylized low-poly, single-player. Platforms: Windows + Steam Deck. Min spec: 4 GB RAM + 1 GB VRAM, 60 fps on the low preset, 1080p output (render scale may drop). The agent writes all code and builds all assets; the user has not used either engine.

## 1. Short answer
- **Engine: Godot 4.7 with C#** (recommended). Unity 6 is the fallback if the horde stress test fails.
- **Min spec: raise it to 8 GB RAM + 2 GB VRAM** (GTX 750 Ti / GTX 1050 / Radeon Vega 8 class, about Steam Deck level). Keep a "very low" preset that tries to run on 4 GB / 1 GB, but don't promise it on the store page.
- With 4 GB / 1 GB, the limit is **GPU speed**, not memory size. The thing that breaks first is the concept's main feature: heavy transparent VFX.
- **Measured 2026-10-16 (§7):** on a Vega 8 (tier B stand-in), the low preset holds 3000 (Forward+) / 4250 (Mobile) enemies in the world at 60 fps. The screen itself shows at most ~1000–1200 at this camera distance. Design budget on low: a full screen of enemies + ~1500 total in the world.

## 2. What hardware "4 GB RAM + 1 GB VRAM" really means
VRAM size does not tell speed. Real machines with 1 GB VRAM in 2026 are old:

| Tier | Example hardware | GPU speed (FP32) | Memory bandwidth |
|---|---|---|---|
| A: current min spec (4 GB / 1 GB) | Intel UHD 620 / HD 520 laptop (shared memory), GT 730, GTX 650 1 GB, Radeon HD 7750 1 GB. CPU: 2 cores / 4 threads, ~2.4 GHz (Core i3-7100U) | 0.4–0.8 TFLOPS | 15–80 GB/s (laptops often ~20–38) |
| B: proposed min spec (8 GB / 2 GB) | GTX 750 Ti, GTX 1050 2 GB, RX 550, Ryzen Vega 8 iGPU. CPU: 4 cores (i5-6500, Ryzen 3 3200G) | 1.2–2.0 TFLOPS | 50–112 GB/s |
| Steam Deck | 4-core Zen 2, RDNA2 GPU, 16 GB shared, 1280×800 | 1.6 TFLOPS | 88 GB/s |
| User's dev laptop | Ryzen 7 5800H, RTX 3070 Laptop, 32 GB | ~15 TFLOPS (RTX) / ~2 TFLOPS (built-in Vega 8) | 448 / ~51 GB/s |

- Tier B is ~2.5–3× faster than tier A. The Deck is inside tier B.
- The dev laptop's RTX is ~20–40× faster than tier A. Never judge performance on it. Its built-in **Vega 8** is a good tier B / Deck stand-in (Windows Settings → Display → Graphics → "Power saving" GPU for the game).
- Tier A cannot be measured on this laptop. Only a real old machine can confirm tier A numbers.

### Who has this hardware (Steam survey, September 2026)
| RAM | Share | VRAM | Share |
|---|---|---|---|
| 4 GB or less | 1.17% | 1 GB or less | 4.04% |
| 8 GB | 6.47% | 2 GB | 3.69% |
| 16 GB or more | ~89% | 4 GB or more | ~90% |

- Raising min spec from 4 GB to 8 GB RAM drops ~1.2% of Steam users. Raising VRAM to 2 GB drops ~4% more on paper; the real number is lower, because integrated GPUs often report only 512 MB and fall into that bucket.

## 3. Computing limits per tier
**These are engineering estimates for planning, not measurements.** Tier B measurements are in §7 and replace these where they overlap. Frame budget at 60 fps = 16.6 ms.

### Memory
| | Tier A: 4 GB / 1 GB | Tier B: 8 GB / 2 GB |
|---|---|---|
| Used by Windows + Steam + antivirus | ~2.0–2.8 GB | ~3 GB |
| Left for the game (RAM) | **~1.2–1.5 GB total.** On a laptop with integrated graphics the "VRAM" comes out of these same 4 GB, so it is CPU and GPU data together | ≤ 3 GB RAM + separate VRAM |
| VRAM usable by game | ~700 MB (Windows desktop + drivers keep the rest) | ~1.6 GB |
| Render targets (screen buffers, shadow map) | ~60–90 MB (internal res ~1344×756) | ~120–180 MB (1080p) |
| Texture budget | ~200–300 MB | ~600–800 MB |
| Audio in memory | ~50–80 MB (music streamed) | ~150 MB |
| Paging risk | High. Old laptops often have HDDs, paging = multi-second freezes | Low |

Memory size alone is fine for a stylized low-poly game: a 1024×1024 compressed texture is ~1.3–2.7 MB, an enemy mesh is tens of KB. VRAM size is not the wall on either tier.

### GPU (the real limit)
| | Tier A | Tier B (≈ Deck) |
|---|---|---|
| Internal resolution | ~67–70% of 1080p (~1344×756), upscaled. Looks soft | 1080p native (Deck: 800p native) |
| Triangles on screen, incl. shadow pass | ~300–500k | ~1–1.5M |
| Animated enemies visible (instanced, GPU-animated) | **~300–500** | ~1000–2000 |
| Enemy model detail (near / far) | 500–1000 / 200–300 triangles | 1500–3000 / ~500 triangles |
| Projectiles visible | ~500 | ~2000 |
| Transparent VFX "overdraw" (screen-sized layers per frame) | **~3–5** | ~10–15 |
| Live particles (depends on size) | ~3k–10k | ~30k–50k |
| Dynamic lights from spells | ~4–8 visible, no shadows | ~16–32 |
| Shadows | Player + big props only, 1 map 1024–2048. Enemies use fake blob shadows | 2 cascades 2048, enemies cast shadows |
| Post effects | Tone mapping + cheap bloom (quarter res) + FXAA | + SMAA/TAA, color grading, light ambient occlusion |

- Transparent particles are the most expensive thing on tier A. Each big glowing sprite redraws pixels that are already drawn. Tier A has bandwidth for only ~3–5 full-screen layers of that per frame. A screen full of big spell effects goes past that fast.
- On tier A the "colorful, VFX-heavy" look means: fewer and smaller particles, more **solid glowing low-poly meshes** (cheap, no transparency), and a particle-count multiplier on the low preset (×0.25–0.5).

### CPU
| | Tier A (2 cores / 4 threads) | Tier B (4 cores) |
|---|---|---|
| Game logic budget | ~5 ms | ~5 ms |
| Enemies the logic can update | ~1000–1500 | ~3000+ |
| Rendering submit (draw calls) | ≤ ~500 | ≤ ~1500 |

- The CPU is not the first wall if the code is written for hordes. On tier A the GPU runs out first.

### Rules that apply on any engine and any tier
- Enemies are **not** separate engine objects with physics and skeleton animation. One instanced draw per enemy type, animation baked into textures (vertex animation, baked in Blender), logic in plain arrays.
- Enemy-enemy and bullet-enemy collision use our own grid, not the physics engine. Physics only for player vs level.
- Pools for everything spawned. No per-frame memory allocation (C# garbage collector pauses = stutter).
- A "budget" system caps live enemies, projectiles and particles per quality preset.

## 4. Unity vs Godot for this project
Current versions: Godot 4.7.2 (stable, August 2026). Unity 6.3 LTS (December 2025, supported to December 2027); newer 6.x update releases exist.

| | Godot 4.7 (C#) | Unity 6 (C#) |
|---|---|---|
| Cost | Free, MIT license, no revenue limit, no splash screen | Free (Personal) while revenue + funding < $200k in the last 12 months, then a paid plan per seat. Splash screen optional in Unity 6 |
| Agent works without the editor | **Yes.** Scenes, resources, settings are plain text (`.tscn`, `.tres`, `project.godot`). Agent can create and review them, diffs are readable | Partly. Scenes/prefabs are YAML with GUID links and `.meta` files; editing them by hand breaks easily. Most work goes through the editor or an MCP bridge (official one is beta and needs a Unity Cloud project + subscription; free community ones exist) |
| Tests and builds from command line | Headless editor runs tests and exports. Game logic in a plain C# library can be tested with `dotnet test` in seconds, without the engine | Batch mode runs tests and builds, but needs a Unity account and an activated license on the machine; slow editor start / script reload |
| Editor size | ~150 MB, starts in seconds | Several GB, Hub + account, slower imports |
| Horde rendering | MultiMesh + vertex animation shader (documented approach). Enough for our counts | GPU instancing / BatchRendererGroup / GPU Resident Drawer. More mature, more examples |
| Horde logic (CPU) | C# (.NET), plain arrays. Fine up to a few thousand enemies. No built-in multi-core SIMD compiler | **Burst + Jobs**: multi-core, very fast; matters at 3000+ enemies |
| VFX tools | GPUParticles3D + particle shaders. No node-based VFX editor | **VFX Graph** (node-based GPU particles) + Shader Graph. Strongest point of Unity |
| Old GPU support | Main renderers need Vulkan 1.0 or Direct3D 12 (Intel HD 510+ / GTX 600+ / Radeon HD 7700+). Older GPUs fall back to a simpler OpenGL renderer | Direct3D 11 runs on almost everything |
| Steam Deck | Windows build via Proton, or a native Linux build (one click) | Same |
| Steam API | GodotSteam or Steamworks.NET | Steamworks.NET / Facepunch.Steamworks |
| Survivors-likes made with it | Brotato, Halls of Torment (both 2D/2.5D) | Megabonk (3D), Vampire Survivors (current version) |
| Learning for the user | Smaller, simpler editor | More tutorials, bigger editor |

### Why Godot wins here
1. **The agent does all the work.** Text scenes + headless tests + `dotnet test` on pure logic = the agent can build, test and review almost everything without clicking in an editor. This is the biggest difference for this project.
2. **The min-spec wall is the GPU.** Unity's big advantages (Burst for 10k enemies, VFX Graph for millions of particles) give little on tier A/B hardware, where the GPU caps us at hundreds to ~2000 enemies anyway.
3. **No external assets** (concept rule), so the Unity Asset Store doesn't help us.
4. Free forever, no license on the build machine, no revenue thresholds.

### What we give up with Godot (risks)
- No VFX Graph. Effects are particle nodes + shaders written by the agent (text, so this suits the agent anyway).
- Fewer 3D survivors-like precedents. Unknowns are found early by the stress test.
- GDScript is too slow for horde loops, so the project uses **C#**. Godot's C# build adds ~70 MB to the download.
- Renderer to pick by measurement: "Mobile" (cheaper, smaller color buffer, good for low end) or "Forward+" (more lights). Both run on desktop.

### When Unity is the better pick
- The stress test misses targets on the Vega 8 (tier B stand-in) and profiling shows engine limits, not our code.
- Design moves to 3000+ enemies on screen, or the user wants to author VFX by hand in a node editor.

## 5. Next step after the decision
First prototype = **horde stress test** in the chosen engine: N GPU-animated enemies, projectiles, particles, quality presets. Measure frame time and memory on the Vega 8 iGPU (tier B / Deck stand-in). Tier A numbers stay estimates unless an old test laptop is available (e.g. Core i3 7th gen, UHD 620, 4 GB).

## 6. Decision (user, 2026-10-16)
1. Engine: **Godot 4.7 + C#**.
2. Min spec: **8 GB RAM + 2 GB VRAM** (tier B, ≈ Steam Deck). Tier A (4 GB / 1 GB) only as a "very low" preset stretch goal, not on the store page.

## 7. Measured (horde stress test, 2026-10-16)
These replace the §3 estimates where they overlap. The stress test is in `features.md`, and the raw CSVs come from `tools/bench.ps1`.

### Setup
- Godot 4.7.2 .NET **release export**, Direct3D 12, 1920×1080 exclusive fullscreen, vsync off.
- GPU: **Radeon Vega 8 iGPU** in the dev laptop's Ryzen 7 5800H (~2 TFLOPS, shared DDR4 ~51 GB/s). That's a bit above Steam Deck compute with less bandwidth, and above a GTX 750 Ti. CPU work runs on the 5800H. Laptop on AC, performance mode.
- **Pass** = 95% of frames ≤ 16.7 ms (60 fps), measured for 3 s after 1.5 s warmup. A ramp stops after 2 failing steps in a row.
- Enemies: placeholder monster, GPU-animated (baked vertex animation), one instanced draw. Spawned around the player, then they crowd in. Live projectiles = half the enemy count, plus hit/death bursts.
- **Counts are enemies in the world, not on screen.** The camera (22 m, 55° tilt, 45° FOV) sees ~860 m² of ground, which fits ~1000–1200 enemies at their 0.9 m spacing. Higher counts fill the screen and extend past it. Off-screen enemies still cost simulation and vertex work, because the test draws the whole horde without culling.
- Presets (`game/data/stress.json`):
  - **very_low**: 60% resolution, 280-triangle enemies, no shadows.
  - **low**: 75% resolution, 760 triangles, sun shadows on props only, FXAA, glow, 4 lights, 400 aura particles.
  - **medium**: 100% resolution, enemies cast shadows (2 cascades), 8 lights, 1000 aura particles.
  - **high**: medium + TAA, SSAO, 2700-triangle enemies.

### Results, Vega 8 (tier B / Deck stand-in)
| Test (other load at preset values) | Forward+ renderer | Mobile renderer |
|---|---|---|
| Enemies, low preset | **3000** | **4250** |
| Enemies, very_low preset | ≥ 10 000 (ramp max) | ≥ 10 000 (9.5 ms p95) |
| Enemies, medium preset | 250 | 500 |
| Full-screen additive layers (500 enemies, low) | 43 | ≥ 60 (ramp max) |
| GPU particles, ~0.6 m soft additive (500 enemies, low) | 20 000 | 56 000 |
| Enemies, low, **Vulkan** instead of D3D12 | 1000 | – |

RTX 3070 Laptop, Mobile unless noted:
- **High** preset: **Forward+ 4250** enemies (34 M triangles per frame), **Mobile 6750**. Mobile skips TAA and SSAO, which the high preset asks for on Forward+, so part of that gap is those effects. Either way, Mobile doesn't cap fast GPUs.
- **very_low** preset: **24 000 enemies + 12 000 projectiles**. The GPU took only 3.2 ms; the limit was the **CPU**: single-threaded game logic took 10 ms at 24 000 (~0.4 µs per enemy). Multi-threading that loop is the lever if a design ever needs more.

Other numbers:
- **Game logic (CPU)**: 1.3 ms for 3000 enemies + 1500 projectiles; 4.2 ms at 10 000. Single thread on the 5800H. Not the limit.
- **Draw calls**: ~85 regardless of enemy count, so instancing works.
- **Memory**: process working set 430–630 MB, video memory 130–210 MB. Far below 8 GB / 2 GB, and even below 4 GB / 1 GB.

### What this means
1. **Min-spec target is reachable with a big margin.** On the low preset the Vega 8 holds 3000–4250 enemies in the world with projectiles and VFX at 60 fps, so a fully packed screen (~1000–1200) fits easily. A real GTX 750 Ti / GTX 1050 is slower, and real art, gameplay and UI cost more. Design budget on low: **a full screen of enemies, ~1500 total in the world** (about 50% headroom).
2. **Enemy shadows are the most expensive setting.** Medium (enemies cast shadows, native 1080p) drops to 250–500 enemies, about 3× the per-enemy cost of low. Keep real-time enemy shadows off up to medium; use fake blob shadows instead.
3. **The Mobile renderer is faster on this GPU in every test**: +40% enemies, +40% overdraw, ~2.8× particles. It lacks Forward+ extras (SSAO, SSR, SDFGI, volumetric fog, TAA) and lights only 8 omni lights per mesh. The concept's stylized look doesn't need those. → **Decided (user, 2026-10-16): Mobile is the default renderer.** Presets fall back on Mobile: TAA → FXAA, SSAO off. Switching back is one line in `project.godot`.
4. **Transparent overdraw is not the wall I estimated** (§3 said 10–15 layers on tier B; measured 43+). Particle *count* costs more than fill: 20 000 (Forward+) vs 56 000 (Mobile).
5. **Keep Direct3D 12 on Windows.** Vulkan used less GPU time but presented frames unevenly on this hybrid laptop, so it passed only 1000 enemies.
6. **Tier A (4 GB / 1 GB) looks possible on very_low.** very_low passes 10 000 enemies here. A 3–4× slower tier A GPU could still hold ~2000 on very_low. This is an extrapolation, not tested; it needs an old test laptop.
7. **Steam Deck** (800p native, about Vega 8 class) should land near the low-preset numbers. Not tested; the Windows build runs through Proton.

## Sources
- Steam Hardware & Software Survey, September 2026: https://store.steampowered.com/hwsurvey/
- Godot system requirements: https://docs.godotengine.org/en/stable/about/system_requirements.html
- Godot download (4.7.2): https://godotengine.org/download/windows/
- Godot vertex animation for thousands of instances: https://docs.godotengine.org/en/4.4/tutorials/performance/vertex_animation/animating_thousands_of_fish.html
- Unity system requirements: https://docs.unity3d.com/Manual/system-requirements.html
- Unity 6 release support (6.3 LTS): https://unity.com/releases/unity-6/support
- Unity Personal terms: https://unity.com/products/unity-personal
- Unity MCP (beta): https://unity.com/blog/unity-ai-mcp-how-to-get-started
- Steam Deck compatibility review: https://partner.steamgames.com/doc/steamdeck/compat
- Engines of Halls of Torment / Megabonk: https://en.wikipedia.org/wiki/Halls_of_Torment, https://en.wikipedia.org/wiki/Megabonk
