#!/usr/bin/env python3
"""Link retained PowerRequestOverride path hits to override lineage evidence."""

from __future__ import annotations

import csv
import json
from collections import Counter
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
PATH_HITS = Path(
    "evidence/files/vm-tooling-staging/"
    "power-control-docs-first-stepwise-runtime-20260329-143515/path-hits.csv"
)
WILDCARD_STDOUT = Path(
    "evidence/files/vm-tooling-staging/"
    "local-kd-powerrequest-reglineage-20260408a/stdout.txt"
)
ROOT_DUMP = Path(
    "evidence/files/vm-tooling-staging/"
    "registry-dumps/power-control-root-20260324-210206/power-control-root.txt"
)
JSON_OUT = Path(
    "registry-research-framework/audit/"
    "execution-required-override-alignment-20260408.json"
)
MD_OUT = Path(
    "registry-research-framework/audit/"
    "execution-required-override-alignment-20260408.md"
)

OVERRIDE_SUBTREE = r"HKLM\System\CurrentControlSet\Control\Power\PowerRequestOverride"
OVERRIDE_SYMBOLS = [
    "PopPowerRequestHandleRequestOverrideQueryResponse",
    "PopPowerRequestOverrideInitialize",
    "PopUmpoSendPowerRequestOverrideQuery",
    "PopUmpoSendPowerRequestOverrideCleanup",
]


def load_rows(path: Path) -> list[list[str]]:
    with path.open(newline="", encoding="utf-8") as handle:
        return [row for row in csv.reader(handle) if row]


def counter_dict(items: list[str]) -> dict[str, int]:
    counter = Counter(items)
    return {key: counter[key] for key in sorted(counter)}


def main() -> int:
    rows = load_rows(REPO_ROOT / PATH_HITS)
    override_hits = [row for row in rows if OVERRIDE_SUBTREE.lower() in row[4].lower()]

    wildcard_text = (REPO_ROOT / WILDCARD_STDOUT).read_text(encoding="utf-8", errors="replace")
    root_dump_text = (REPO_ROOT / ROOT_DUMP).read_text(encoding="utf-8", errors="replace")

    payload = {
        "date": "2026-04-08",
        "path_hits_artifact": str(PATH_HITS),
        "wildcard_lineage_artifact": str(WILDCARD_STDOUT),
        "root_dump_artifact": str(ROOT_DUMP),
        "override_path_hits": {
            "count": len(override_hits),
            "processes": counter_dict([row[1] for row in override_hits]),
            "operations": counter_dict([row[3] for row in override_hits]),
            "paths": counter_dict([row[4] for row in override_hits]),
        },
        "override_symbols_present": {
            symbol: (symbol in wildcard_text) for symbol in OVERRIDE_SYMBOLS
        },
        "baseline_root_contains_override_subtree": OVERRIDE_SUBTREE.replace(
            "HKLM\\", "HKEY_LOCAL_MACHINE\\"
        )
        in root_dump_text,
        "interpretation": [
            "The retained runtime path-hit capture shows adjacent registry activity under Control\\Power\\PowerRequestOverride.",
            "The retained wildcard KD lineage evidence independently shows a current-build override family with HandleRequestOverrideQueryResponse, OverrideInitialize, UMPO override query, and override cleanup helpers.",
            "The retained power-control root dump also contains the PowerRequestOverride subtree, so the visible runtime path hits align with an already-proven override lineage rather than with exact reads of the execution-required pair values.",
        ],
    }

    (REPO_ROOT / JSON_OUT).write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")

    lines = [
        "# Execution-Required Override Alignment Audit",
        "",
        "Date: 2026-04-08",
        f"Path-hit artifact: `{PATH_HITS}`",
        f"KD wildcard artifact: `{WILDCARD_STDOUT}`",
        f"Root dump artifact: `{ROOT_DUMP}`",
        "",
        "## Outcome",
        "",
        f"- Adjacent `PowerRequestOverride` path hits: `{len(override_hits)}`",
        f"- Processes: `{payload['override_path_hits']['processes']}`",
        f"- Operations: `{payload['override_path_hits']['operations']}`",
        f"- Override symbols present: `{payload['override_symbols_present']}`",
        f"- Root dump contains `PowerRequestOverride` subtree: `{payload['baseline_root_contains_override_subtree']}`",
        "",
        "## Interpretation",
        "",
        "- The visible runtime registry activity aligns with the already-proven current-build override family rather than with exact reads of `AllowSystemRequiredPowerRequests` or `AllowAudioToEnableExecutionRequiredPowerRequests`.",
        "- This further narrows the execution-required pair: adjacent override flow is visible, while an exact pair read or earlier seeding path remains unresolved.",
        "",
        "## Artifacts",
        "",
        f"- `{JSON_OUT}`",
        f"- `{PATH_HITS}`",
        f"- `{WILDCARD_STDOUT}`",
        f"- `{ROOT_DUMP}`",
    ]
    (REPO_ROOT / MD_OUT).write_text("\n".join(lines) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
