#!/usr/bin/env python3
"""Canonical wrapper for custom registry value enriched-matrix generation.

The underlying implementation still lives in the historical operator96 module
so old artifact paths and imports remain reproducible. New contributor docs and
app command packs should point at this neutral entrypoint.
"""
from __future__ import annotations

import sys
from pathlib import Path


SCRIPT_DIR = Path(__file__).resolve().parent
if str(SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIR))

from generate_operator96_enriched_value_matrix import *  # noqa: F401,F403,E402
from generate_operator96_enriched_value_matrix import main as _legacy_main  # noqa: E402


if __name__ == "__main__":
    raise SystemExit(_legacy_main())
