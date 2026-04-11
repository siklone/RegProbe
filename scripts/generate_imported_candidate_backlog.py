#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
if str(REPO_ROOT / "scripts") not in sys.path:
    sys.path.insert(0, str(REPO_ROOT / "scripts"))

from external_evidence_import_lib import write_imported_candidate_backlog  # noqa: E402


def main() -> int:
    parser = argparse.ArgumentParser(description="Aggregate imported external candidate queues into a canonical research backlog.")
    parser.add_argument(
        "--imported-root",
        default=str(REPO_ROOT / "registry-research-framework" / "imported"),
        help="Root containing per-run imported candidate queues.",
    )
    parser.add_argument(
        "--output",
        default=str(REPO_ROOT / "research" / "imported-candidate-backlog.json"),
        help="Canonical output path for the aggregated imported candidate backlog.",
    )
    args = parser.parse_args()

    imported_root = Path(args.imported_root).resolve()
    output_path = Path(args.output).resolve()
    written = write_imported_candidate_backlog(imported_root, output_path)
    payload = {
        "imported_root": imported_root.as_posix(),
        "output": written.as_posix(),
    }
    print(json.dumps(payload, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
