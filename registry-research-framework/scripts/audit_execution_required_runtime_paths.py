#!/usr/bin/env python3
"""Audit retained execution-required runtime path hits."""

from __future__ import annotations

import csv
import json
from collections import Counter
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SOURCE = Path(
    "evidence/files/vm-tooling-staging/"
    "power-control-docs-first-stepwise-runtime-20260329-143515/path-hits.csv"
)
JSON_OUT = Path(
    "registry-research-framework/audit/"
    "execution-required-runtime-path-audit-20260408.json"
)
MD_OUT = Path(
    "registry-research-framework/audit/"
    "execution-required-runtime-path-audit-20260408.md"
)

PAIR_VALUE_NAMES = [
    "AllowSystemRequiredPowerRequests",
    "AllowAudioToEnableExecutionRequiredPowerRequests",
]
OVERRIDE_SUBTREE = r"HKLM\System\CurrentControlSet\Control\Power\PowerRequestOverride"


def load_rows(path: Path) -> list[list[str]]:
    with path.open(newline="", encoding="utf-8") as handle:
        return [row for row in csv.reader(handle) if row]


def format_counter(counter: Counter[str]) -> dict[str, int]:
    return {key: counter[key] for key in sorted(counter)}


def main() -> int:
    source_path = REPO_ROOT / SOURCE
    rows = load_rows(source_path)

    exact_hits = {}
    for value_name in PAIR_VALUE_NAMES:
        matches = [row for row in rows if value_name.lower() in row[4].lower()]
        exact_hits[value_name] = {
            "count": len(matches),
            "operations": format_counter(Counter(row[3] for row in matches)),
            "processes": format_counter(Counter(row[1] for row in matches)),
            "paths": format_counter(Counter(row[4] for row in matches)),
        }

    override_hits = [row for row in rows if OVERRIDE_SUBTREE.lower() in row[4].lower()]
    payload = {
        "date": "2026-04-08",
        "source_artifact": str(SOURCE),
        "row_count": len(rows),
        "exact_pair_hits": exact_hits,
        "adjacent_override_hits": {
            "count": len(override_hits),
            "operations": format_counter(Counter(row[3] for row in override_hits)),
            "processes": format_counter(Counter(row[1] for row in override_hits)),
            "paths": format_counter(Counter(row[4] for row in override_hits)),
            "sample_rows": [
                {
                    "time": row[0],
                    "process": row[1],
                    "pid": row[2],
                    "operation": row[3],
                    "path": row[4],
                    "result": row[5],
                    "detail": row[6] if len(row) > 6 else "",
                }
                for row in override_hits[:6]
            ],
        },
        "interpretation": [
            "The retained docs-first stepwise runtime path-hit capture contains zero exact path hits for both execution-required pair members.",
            "The same capture contains repeated adjacent runtime accesses under Control\\Power\\PowerRequestOverride by svchost.exe.",
            "Within the retained runtime registry trace layer, visible activity is currently narrowed to the adjacent override subtree rather than exact reads of the execution-required pair values.",
        ],
    }

    (REPO_ROOT / JSON_OUT).write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")

    md = [
        "# Execution-Required Runtime Path Audit",
        "",
        "Date: 2026-04-08",
        f"Source artifact: `{SOURCE}`",
        "",
        "## Outcome",
        "",
        f"- Parsed rows: `{len(rows)}`",
        f"- `AllowSystemRequiredPowerRequests` exact path hits: `{exact_hits['AllowSystemRequiredPowerRequests']['count']}`",
        f"- `AllowAudioToEnableExecutionRequiredPowerRequests` exact path hits: `{exact_hits['AllowAudioToEnableExecutionRequiredPowerRequests']['count']}`",
        f"- Adjacent `PowerRequestOverride` subtree hits: `{len(override_hits)}`",
        f"- Adjacent subtree processes: `{payload['adjacent_override_hits']['processes']}`",
        f"- Adjacent subtree operations: `{payload['adjacent_override_hits']['operations']}`",
        "",
        "## Interpretation",
        "",
        "- The retained docs-first stepwise runtime path-hit capture contains zero exact path hits for both execution-required pair members.",
        "- The same capture contains repeated adjacent registry activity under `HKLM\\System\\CurrentControlSet\\Control\\Power\\PowerRequestOverride`.",
        "- Visible runtime registry activity is therefore narrowed to the adjacent override subtree rather than exact reads of the pair values.",
        "",
        "## Artifacts",
        "",
        f"- `{JSON_OUT}`",
        f"- `{SOURCE}`",
    ]
    (REPO_ROOT / MD_OUT).write_text("\n".join(md) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
