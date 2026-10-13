# Silhouette matching: model meshes vs reference images in orthographic views.
# Masks are numpy bool arrays (H, W), row 0 = top. Scores are IoU after normalize() (scale/position invariant).
from collections import deque

import bpy
import numpy as np

# view -> (image-right axis, image-up axis) in world coords.
VIEWS = {
    'front': ((1.0, 0.0, 0.0), (0.0, 0.0, 1.0)),   # seen from -Y
    'side': ((0.0, 1.0, 0.0), (0.0, 0.0, 1.0)),    # seen from +X (her left); she faces image-left
    'back': ((-1.0, 0.0, 0.0), (0.0, 0.0, 1.0)),   # seen from +Y
}


def world_tris(objs):
    # (T, 3, 3) world-space triangles of the evaluated meshes (modifiers applied).
    dg = bpy.context.evaluated_depsgraph_get()
    out = []
    for o in objs:
        if o.type != 'MESH':
            continue
        e = o.evaluated_get(dg)
        m = e.to_mesh()
        m.calc_loop_triangles()
        co = np.empty(len(m.vertices) * 3)
        m.vertices.foreach_get('co', co)
        co = co.reshape(-1, 3)
        mw = np.array(e.matrix_world)
        co = co @ mw[:3, :3].T + mw[:3, 3]
        idx = np.empty(len(m.loop_triangles) * 3, dtype=np.int64)
        m.loop_triangles.foreach_get('vertices', idx)
        out.append(co[idx.reshape(-1, 3)])
        e.to_mesh_clear()
    return np.concatenate(out) if out else np.zeros((0, 3, 3))


def rasterize(tris, view, res=512, margin=4):
    # Fills triangles projected on the view plane; the larger extent maps to res - 2*margin pixels.
    u_ax, v_ax = (np.array(a) for a in VIEWS[view])
    U = tris @ u_ax
    Vv = tris @ v_ax
    umin, umax, vmin, vmax = U.min(), U.max(), Vv.min(), Vv.max()
    scale = (res - 2 * margin) / max(umax - umin, vmax - vmin)
    W = int(np.ceil((umax - umin) * scale)) + 2 * margin
    H = int(np.ceil((vmax - vmin) * scale)) + 2 * margin
    px = (U - umin) * scale + margin
    py = (vmax - Vv) * scale + margin
    mask = np.zeros((H, W), bool)
    for t in range(len(tris)):
        x, y = px[t], py[t]
        d = (y[1] - y[2]) * (x[0] - x[2]) + (x[2] - x[1]) * (y[0] - y[2])
        if abs(d) < 1e-12:
            continue
        x0, x1 = max(int(x.min()), 0), min(int(np.ceil(x.max())), W - 1)
        y0, y1 = max(int(y.min()), 0), min(int(np.ceil(y.max())), H - 1)
        gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        a = ((y[1] - y[2]) * (gx - x[2]) + (x[2] - x[1]) * (gy - y[2])) / d
        b = ((y[2] - y[0]) * (gx - x[2]) + (x[0] - x[2]) * (gy - y[2])) / d
        inside = (a >= -1e-9) & (b >= -1e-9) & (a + b <= 1 + 1e-9)
        mask[y0:y1 + 1, x0:x1 + 1] |= inside
    return mask


def clean(mask, min_frac=0.02):
    # Drops 4-connected components smaller than min_frac of the largest one (labels, stray marks).
    H, W = mask.shape
    label = np.zeros((H, W), np.int32)
    sizes = [0]
    for sy, sx in zip(*np.nonzero(mask)):
        if label[sy, sx]:
            continue
        lab = len(sizes)
        label[sy, sx] = lab
        q = deque([(sy, sx)])
        n = 0
        while q:
            y, x = q.popleft()
            n += 1
            for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                if 0 <= ny < H and 0 <= nx < W and mask[ny, nx] and not label[ny, nx]:
                    label[ny, nx] = lab
                    q.append((ny, nx))
        sizes.append(n)
    if len(sizes) == 1:
        return mask.copy()
    sizes = np.array(sizes)
    keep = sizes >= sizes.max() * min_frac
    keep[0] = False
    return keep[label]


def flood_background(rgb, step_tol=0.035):
    # Background = 4-connected region reached from the image border through small neighbor-to-neighbor color
    # steps. Follows gradients and soft ground shadows; stops at the figure's hard edges.
    H, W, _ = rgb.shape
    rgb = rgb.tolist()  # list indexing is ~10x faster than numpy scalar access in this loop
    bg = np.zeros((H, W), bool)
    q = deque()
    for y in range(H):
        for x in (0, W - 1):
            if not bg[y, x]:
                bg[y, x] = True
                q.append((y, x))
    for x in range(W):
        for y in (0, H - 1):
            if not bg[y, x]:
                bg[y, x] = True
                q.append((y, x))
    tol2 = step_tol * step_tol
    while q:
        y, x = q.popleft()
        c0, c1, c2 = rgb[y][x]
        for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if 0 <= ny < H and 0 <= nx < W and not bg[ny, nx]:
                n0, n1, n2 = rgb[ny][nx]
                if (n0 - c0) ** 2 + (n1 - c1) ** 2 + (n2 - c2) ** 2 < tol2:
                    bg[ny, nx] = True
                    q.append((ny, nx))
    return bg


def load_mask(path, step_tol=0.035, min_frac=0.02, max_side=512):
    # Foreground = alpha if the border is transparent, else everything flood_background() doesn't reach.
    # Images are subsampled to <= max_side px first (masks get normalized to 256 px anyway; keeps the flood fast).
    img = bpy.data.images.load(path, check_existing=False)
    try:
        w, h = img.size
        px = np.empty(w * h * 4, np.float32)
        img.pixels.foreach_get(px)
    finally:
        bpy.data.images.remove(img)
    px = px.reshape(h, w, 4)[::-1]
    k = max(1, int(np.ceil(max(w, h) / max_side)))
    px = px[::k, ::k]
    rgb, alpha = px[..., :3], px[..., 3]
    border_alpha = np.concatenate([alpha[0], alpha[-1], alpha[:, 0], alpha[:, -1]])
    if border_alpha.min() < 0.5:
        mask = alpha > 0.5
    else:
        mask = ~flood_background(rgb, step_tol)
    return clean(mask, min_frac)


def normalize(mask, H=256):
    # Crops to the bounding box, scales to height H (nearest), centers horizontally on a (H, 2H) canvas.
    ys, xs = np.nonzero(mask)
    if len(ys) == 0:
        raise ValueError("empty mask")
    crop = mask[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    ch, cw = crop.shape
    s = H / ch
    nw = max(1, int(round(cw * s)))
    rows = np.minimum((np.arange(H) / s).astype(int), ch - 1)
    cols = np.minimum((np.arange(nw) / s).astype(int), cw - 1)
    scaled = crop[rows][:, cols]
    Wc = 2 * H
    out = np.zeros((H, Wc), bool)
    x0 = (Wc - nw) // 2
    src0 = max(0, -x0)
    dst0 = max(0, x0)
    n = min(nw - src0, Wc - dst0)
    out[:, dst0:dst0 + n] = scaled[:, src0:src0 + n]
    return out


def iou(a, b):
    union = np.logical_or(a, b).sum()
    return float(np.logical_and(a, b).sum() / union) if union else 1.0


def compare(objs, view, ref_path, res=512, H=256):
    # Returns (score, model_mask, ref_mask), both normalized. Side views also try the mirrored reference.
    model = normalize(rasterize(world_tris(objs), view, res), H)
    raw = load_mask(ref_path)
    ref = normalize(raw, H)
    score = iou(model, ref)
    if view == 'side':
        mref = normalize(raw[:, ::-1], H)
        ms = iou(model, mref)
        if ms > score:
            score, ref = ms, mref
    return score, model, ref


def diff_image(model, ref, path):
    # PNG: gray = both, red = reference only (model missing), blue = model only (extra), white = neither.
    H, W = model.shape
    rgb = np.ones((H, W, 3), np.float32)
    rgb[model & ref] = (0.55, 0.55, 0.55)
    rgb[ref & ~model] = (0.95, 0.2, 0.2)
    rgb[model & ~ref] = (0.2, 0.4, 0.95)
    rgba = np.concatenate([rgb, np.ones((H, W, 1), np.float32)], -1)[::-1]
    img = bpy.data.images.new("vlg3d_diff", W, H, alpha=False)
    try:
        img.pixels.foreach_set(rgba.ravel())
        img.filepath_raw = path
        img.file_format = 'PNG'
        img.save()
    finally:
        bpy.data.images.remove(img)
