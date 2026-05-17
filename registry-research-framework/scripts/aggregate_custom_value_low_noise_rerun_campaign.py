#!/usr/bin/env python3
"""Canonical wrapper for custom registry value low-noise aggregate generation."""
from __future__ import annotations

import sys
from pathlib import Path


SCRIPT_DIR = Path(__file__).resolve().parent
if str(SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIR))

from aggregate_operator96_low_noise_rerun_campaign import *  # noqa: F401,F403,E402
from aggregate_operator96_low_noise_rerun_campaign import main as _legacy_main  # noqa: E402


if __name__ == "__main__":
    raise SystemExit(_legacy_main())
