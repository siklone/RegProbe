#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"
if str(SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_ROOT))

from research_v36_lib import check_mcp_readiness, load_promotion_gate_catalog  # noqa: E402


def main() -> int:
    parser = argparse.ArgumentParser(description="Check whether the RegProbe 3.6 surface is MCP-ready.")
    parser.add_argument("--emit-json", action="store_true", help="Emit JSON payload.")
    args = parser.parse_args()

    payload = check_mcp_readiness(load_promotion_gate_catalog())
    if args.emit_json:
        print(json.dumps(payload, ensure_ascii=False, indent=2))
    else:
        print(json.dumps({"status": payload["status"], "checks": payload["checks"]}, ensure_ascii=False, indent=2))
    return 0 if payload["status"] == "MCP_READY" else 1


if __name__ == "__main__":
    raise SystemExit(main())
