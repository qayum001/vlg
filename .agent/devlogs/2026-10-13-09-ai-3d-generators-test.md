# AI 3D generators browser test (mage)
Date: 2026-10-13
Type: investigation
Commits: d9e7c1b

## Summary
The user asked to try external AI 3D generators via the browser as a possible art source. I tested the same mage spec on Sloyd, Meshy and Hunyuan3D-2.1. Sloyd image-to-3D (run by the user) came closest to our low-poly style.

## Details
- Clarify answers: goal is a possible art source (the rule may change later), input is the text prompt plus our render, the user signs in to sites, output is a chat summary with screenshots.
- `design/concept.md`: AI generators marked "being evaluated". The Art rule stands until decided.
- Meshy 6 text-to-3D (20 credits): detailed untextured sculpt, standing pose, not low-poly.
- Hunyuan3D-2.1 HF Space image-to-3D: 310k faces in ~17 s. The silhouette is faithful. The texture step did not run.
- Sloyd image-to-3D (user, guest account): 40k faces, textured, close to our model, staff missing. It has polycount and T-pose settings.
- Details and per-tool notes: memory `project/ai-3d-generators.md`.

## Problems
- The built-in browser has no file upload, and localhost was unreachable. A retyped base64 image in a page script was corrupted.
- Fix: Windows clipboard plus Gradio's paste button. A simulated Ctrl+V did not paste on Meshy.
- Rodin (hyper3d.ai, hyperhuman.deemos.com) and the TRELLIS.2 app would not load in the built-in browser.
- The hidden pane renders 3D viewers blank or unlit, so the captures are low quality.
- The user's clipboard was overwritten with the input image.

## Results
- Tests: none (no code). Screenshots of 3 results sent to the user.

## Next
- Tripo (needs user sign-in), Meshy image-to-3D (user drags or pastes the image), Sloyd at 3k–5k polycount.
- Decide whether AI generators become an art source (license: free tiers are CC BY 4.0; Steam AI disclosure).
