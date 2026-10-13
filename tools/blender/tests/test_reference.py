import os
import tempfile
import unittest

import bpy
import numpy as np
from mathutils import Vector

from vlg3d import geom, reference, scene


def box(b, name, sx, sy, sz, center=(0, 0, 0)):
    c = Vector(center)
    xs, ys, zs = (-sx / 2, sx / 2), (-sy / 2, sy / 2), (-sz / 2, sz / 2)
    v = [c + Vector((x, y, z)) for x in xs for y in ys for z in zs]
    f = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
    return b.mesh(name, v, f, [b.mats.get("g") or b.mat("g", (0.5, 0.5, 0.5))])


def save_png(rgba, path):
    H, W, _ = rgba.shape
    img = bpy.data.images.new("t_ref_png", W, H, alpha=True)
    img.pixels.foreach_set(rgba[::-1].astype(np.float32).ravel())
    img.filepath_raw = path
    img.file_format = 'PNG'
    img.save()
    bpy.data.images.remove(img)


class RasterTest(unittest.TestCase):
    def setUp(self):
        self.b = scene.Builder("TestRef", "t_ref_")
        self.b.clear()

    def tearDown(self):
        self.b.clear()
        c = bpy.data.collections.get("TestRef")
        if c:
            bpy.data.collections.remove(c)

    def test_box_silhouettes_match_dimensions(self):
        ob = box(self.b, "Box", 1.0, 0.5, 2.0)
        tris = reference.world_tris([ob])
        self.assertEqual(tris.shape, (12, 3, 3))
        front = reference.rasterize(tris, 'front', res=200, margin=0)
        ys, xs = np.nonzero(front)
        self.assertAlmostEqual((xs.max() - xs.min() + 1) / (ys.max() - ys.min() + 1), 0.5, delta=0.02)
        self.assertGreater(front.mean(), 0.97)  # box fills its own bounding rectangle
        side = reference.rasterize(tris, 'side', res=200, margin=0)
        ys, xs = np.nonzero(side)
        self.assertAlmostEqual((xs.max() - xs.min() + 1) / (ys.max() - ys.min() + 1), 0.25, delta=0.02)

    def test_side_view_orientation(self):
        # A nose block at -Y (front) must appear on the image's left in the side view.
        body = box(self.b, "Body", 0.4, 0.4, 2.0)
        nose = box(self.b, "Nose", 0.1, 0.3, 0.2, center=(0, -0.35, 0.5))
        m = reference.rasterize(reference.world_tris([body, nose]), 'side', res=200)
        cols = np.nonzero(m.any(0))[0]
        rows_with_nose = np.nonzero(m[:, cols.min()])[0]
        self.assertLess(rows_with_nose.mean(), m.shape[0] / 2)  # nose is above the middle (z=0.5 > 0)

    def test_compare_against_own_render_scores_high(self):
        body = box(self.b, "Body", 0.4, 0.3, 1.8, center=(0, 0, 0.9))
        arm = box(self.b, "Arm", 1.6, 0.1, 0.1, center=(0, 0, 1.4))
        m = reference.rasterize(reference.world_tris([body, arm]), 'front', res=300)
        with tempfile.TemporaryDirectory() as d:
            path = os.path.join(d, "ref.png")
            img = np.ones(m.shape + (4,))
            img[m] = (0.1, 0.1, 0.4, 1.0)
            pad = np.ones((m.shape[0] + 60, m.shape[1] + 200, 4))
            pad[30:30 + m.shape[0], 120:120 + m.shape[1]] = img  # off-center, different canvas
            save_png(pad, path)
            score, _, _ = reference.compare([body, arm], 'front', path)
            self.assertGreater(score, 0.95)
            arm.location.z = -0.8  # arm moved to the hips: shape differs
            bpy.context.view_layer.update()
            worse, model, ref = reference.compare([body, arm], 'front', path)
            self.assertLess(worse, score - 0.1)
            out = os.path.join(d, "diff.png")
            reference.diff_image(model, ref, out)
            self.assertTrue(os.path.getsize(out) > 0)


class MaskTest(unittest.TestCase):
    def test_iou(self):
        a = np.zeros((10, 10), bool)
        a[2:6, 2:6] = True
        self.assertEqual(reference.iou(a, a), 1.0)
        b = np.zeros_like(a)
        b[7:9, 7:9] = True
        self.assertEqual(reference.iou(a, b), 0.0)

    def test_normalize_is_scale_and_position_invariant(self):
        a = np.zeros((100, 100), bool)
        a[10:50, 20:30] = True
        a[20:25, 10:40] = True
        b = np.zeros((400, 300), bool)
        b[100:260, 140:180] = True
        b[140:160, 100:220] = True
        self.assertGreater(reference.iou(reference.normalize(a, 128), reference.normalize(b, 128)), 0.95)

    def test_clean_drops_small_components(self):
        m = np.zeros((50, 50), bool)
        m[5:45, 5:30] = True
        m[2:4, 45:47] = True
        c = reference.clean(m, min_frac=0.02)
        self.assertFalse(c[2:4, 45:47].any())
        self.assertTrue(c[5:45, 5:30].all())

    def test_load_mask_plain_and_transparent_background(self):
        with tempfile.TemporaryDirectory() as d:
            img = np.ones((40, 60, 4))
            img[10:30, 20:35] = (0.2, 0.6, 0.2, 1.0)
            p1 = os.path.join(d, "plain.png")
            save_png(img, p1)
            m = reference.load_mask(p1)
            self.assertEqual(m.sum(), 20 * 15)
            self.assertTrue(m[10:30, 20:35].all())
            img2 = np.zeros((40, 60, 4))
            img2[5:15, 5:15] = (1, 1, 1, 1)  # white figure on transparent background
            p2 = os.path.join(d, "alpha.png")
            save_png(img2, p2)
            self.assertEqual(reference.load_mask(p2).sum(), 100)


if __name__ == "__main__":
    unittest.main()
