import unittest
from collections import Counter

from mathutils import Vector

from vlg3d import geom


def edge_uses(faces):
    # Undirected edge -> number of faces using it.
    c = Counter()
    for f in faces:
        for i in range(len(f)):
            a, b = f[i], f[(i + 1) % len(f)]
            c[(min(a, b), max(a, b))] += 1
    return c


def directed_dupes(faces):
    # Directed edges used twice = inconsistent winding.
    c = Counter()
    for f in faces:
        for i in range(len(f)):
            c[(f[i], f[(i + 1) % len(f)])] += 1
    return [e for e, n in c.items() if n > 1]


class SplineTest(unittest.TestCase):
    def test_passes_through_control_points(self):
        pts = [Vector((0, 0, 0)), Vector((1, 0, 0)), Vector((1, 1, 0)), Vector((2, 1, 1))]
        out = geom.spline(pts, seg=4)
        self.assertEqual(len(out), 3 * 4 + 1)
        for i, p in enumerate(pts):
            self.assertAlmostEqual((out[i * 4] - p).length, 0.0, places=6)


class RingTest(unittest.TestCase):
    def test_extents(self):
        r = geom.ring_z(1.0, 0.3, 0.2, 0.1, 16, cy=0.05)
        self.assertEqual(len(r), 16)
        self.assertAlmostEqual(max(p.x for p in r), 0.3, places=6)
        self.assertAlmostEqual(min(p.y for p in r), 0.05 - 0.2, places=6)
        self.assertAlmostEqual(max(p.y for p in r), 0.05 + 0.1, places=6)
        self.assertTrue(all(abs(p.z - 1.0) < 1e-9 for p in r))

    def test_disp_gets_index(self):
        r = geom.ring_z(0.0, 1.0, 1.0, 1.0, 8, disp=lambda p, t, k: p * (2.0 if k % 2 else 1.0))
        self.assertAlmostEqual(r[1].length, 2.0, places=6)
        self.assertAlmostEqual(r[2].length, 1.0, places=6)


class LoftTest(unittest.TestCase):
    def test_counts(self):
        rings = [geom.ring_z(z, 1, 1, 1, 8) for z in (0, 1, 2)]
        v, f = geom.loft(rings)
        self.assertEqual((len(v), len(f)), (24, 16))
        v, f = geom.loft(rings, closed=False)
        self.assertEqual(len(f), 14)


class SweepTest(unittest.TestCase):
    path = [Vector((0, 0, 0)), Vector((0.2, 0.1, 0.3)), Vector((0.3, 0.0, 0.7))]

    def test_capped_tube_is_closed_and_consistent(self):
        for caps in (('round', 'round'), ('flat', 'round'), ('flat', 'flat')):
            v, f = geom.sweep(self.path, [(0.05, 0.03)] * 3, n=6, caps=caps)
            uses = edge_uses(f)
            self.assertTrue(all(n == 2 for n in uses.values()), caps)
            self.assertEqual(directed_dupes(f), [], caps)
            self.assertEqual(len(v) - len(uses) + len(f), 2, caps)  # sphere topology

    def test_open_tube_has_two_boundaries(self):
        v, f = geom.sweep(self.path, [(0.05, 0.05)] * 3, n=5, caps=(None, None))
        uses = edge_uses(f)
        self.assertEqual(sum(1 for n in uses.values() if n == 1), 10)

    def test_radius_function(self):
        v, f = geom.sweep(self.path, lambda s: (0.1 * (1 - s) + 0.01, 0.05), n=4, caps=(None, None))
        first = v[:4]
        self.assertAlmostEqual(max((p - self.path[0]).length for p in first), 0.11, places=6)


class SweepLoopTest(unittest.TestCase):
    def test_torus_topology(self):
        path = geom.ring_z(0.5, 0.3, 0.3, 0.3, 12)
        v, f = geom.sweep_loop(path, 0.02, 0.03, n=4)
        uses = edge_uses(f)
        self.assertTrue(all(n == 2 for n in uses.values()))
        self.assertEqual(directed_dupes(f), [])
        self.assertEqual(len(v) - len(uses) + len(f), 0)


class MirrorTest(unittest.TestCase):
    def test_mirror_x(self):
        self.assertEqual(tuple(geom.mirror_x([Vector((1, 2, 3))])[0]), (-1, 2, 3))


if __name__ == "__main__":
    unittest.main()
