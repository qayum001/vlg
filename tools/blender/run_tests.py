# Runs tools/blender/tests inside Blender. Command (from repo root):
#   "C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe" --background --factory-startup --python-exit-code 1 --python tools/blender/run_tests.py
import os
import sys
import unittest

here = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, here)
suite = unittest.defaultTestLoader.discover(os.path.join(here, "tests"), top_level_dir=here)
result = unittest.TextTestRunner(verbosity=2).run(suite)
if not result.wasSuccessful():
    # --python-exit-code turns this into a non-zero Blender exit code.
    raise RuntimeError("vlg3d tests failed")
