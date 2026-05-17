#!/usr/bin/env python3
"""Canonical wrapper for custom registry value app-surface review.

The implementation delegates to the historical operator96 module because the
checked-in audit artifacts intentionally keep those filenames for compatibility.
New surfaces should use this neutral script name.
"""
from __future__ import annotations

import sys
from pathlib import Path


SCRIPT_DIR = Path(__file__).resolve().parent
if str(SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIR))

from generate_operator96_app_surface_review import *  # noqa: F401,F403,E402
from generate_operator96_app_surface_review import main as _legacy_main  # noqa: E402


if __name__ == "__main__":
    raise SystemExit(_legacy_main())
