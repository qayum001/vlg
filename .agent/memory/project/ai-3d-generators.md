# AI 3D generators (evaluation)
Updated: 2026-10-13
Related: [art pipeline](art-pipeline.md), [tooling](tooling.md), [concept](../../../design/concept.md)

Status: under evaluation as a possible art source (user, 2026-10-13). Until decided, the concept's "agent-made art only" rule stands, and the Blender MCP generators stay forbidden.

## First test (mage, same spec as the agent-made one)
- Sloyd (app.sloyd.ai): works as a guest, no login. Image-to-3D from our render came closest to our style. Output: 40k faces, 2048 texture, watertight, T-pose kept, but the staff was lost. Polycount can be picked (3k–500k), plus quad/tri, T-pose, auto-rig and animate options. Free outputs are CC BY 4.0.
- Meshy (meshy.ai): needs login, free 100 credits/month, 20 credits per generation, texture +20. Text-to-3D (Meshy 6) gave a detailed sculpt, not low-poly, not T-pose (T-pose and Private license are paid). Free outputs are CC BY 4.0.
- Hunyuan3D-2.1 (HF Space tencent/Hunyuan3D-2.1): no login. Image-to-3D shape in ~17 s, 310k faces, silhouette faithful (staff included). The texture step did not run (free GPU quota). Has a "Simplify Mesh" export to a target face count.
- Tripo: needs login, not tested yet. Rodin (hyper3d.ai) and the TRELLIS.2 app: would not load in the built-in browser.

## Findings
- Image input from our own render matches our style much better than text.
- Raw outputs are dense triangle meshes (40k–310k) vs our ~5.5k. They need decimation or retopology for the flat-shaded low-poly look. Sloyd's polycount setting is the closest fit.
- Free-tier licenses (CC BY 4.0) are public and need attribution. Commercial/private use needs paid plans. Steam requires disclosing AI-generated content.

## Browser mechanics
- The built-in browser can't attach local files and can't reach localhost. What works for image upload: put the image on the Windows clipboard (PowerShell `Clipboard.SetImage`), then click a page's own paste-from-clipboard button (Gradio has one). A simulated Ctrl+V does not paste. Otherwise the user drags the file in.
- Retyping long base64 in a page script corrupts it. Don't do that.
- The hidden browser pane doesn't draw 3D viewers properly (blank or unlit). Ask the user to show the pane (Ctrl+Shift+B) before capturing results.
