#!/usr/bin/env python3
"""Audit retained PowerRequestOverride subtree runtime evidence."""

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
ROOT_DUMP = Path(
    "evidence/files/vm-tooling-staging/registry-dumps/"
    "power-control-root-20260324-210206/power-control-root.txt"
)
KD_STDOUT = Path(
    "evidence/files/vm-tooling-staging/"
    "local-kd-powerrequest-reglineage-20260408a/stdout.txt"
)
JSON_OUT = Path(
    "registry-research-framework/audit/"
    "power-request-override-runtime-audit-20260408.json"
)
MD_OUT = Path(
    "registry-research-framework/audit/"
    "power-request-override-runtime-audit-20260408.md"
)

OVERRIDE_ROOT = r"HKLM\System\CurrentControlSet\Control\Power\PowerRequestOverride"
KD_SYMBOLS = [
    "PopPowerRequestHandleRequestOverrideQueryResponse",
    "PopPowerRequestOverrideInitialize",
    "PopUmpoSendPowerRequestOverrideQuery",
    "PopUmpoSendPowerRequestOverrideCleanup",
]


def format_counter(counter: Counter[str]) -> dict[str, int]:
    return {key: counter[key] for key in sorted(counter)}


def load_path_hits(path: Path) -> list[dict[str, str]]:
    with path.open(newline="", encoding="utf-8-sig") as handle:
        return [row for row in csv.DictReader(handle)]


def main() -> int:
    path_rows = load_path_hits(REPO_ROOT / PATH_HITS)
    override_hits = [row for row in path_rows if OVERRIDE_ROOT.lower() in row["Path"].lower()]

    root_text = (REPO_ROOT / ROOT_DUMP).read_text(encoding="utf-8-sig")
    kd_text = (REPO_ROOT / KD_STDOUT).read_text(encoding="utf-8-sig")

    subtree_present = OVERRIDE_ROOT.replace("System", "SYSTEM") in root_text
    kd_symbols_found = [name for name in KD_SYMBOLS if name in kd_text]

    payload = {
        "title": "PowerRequestOverride subtree runtime audit",
        "generated_utc": "2026-04-08T19:05:00Z",
        "target_path": r"HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride",
        "source_artifacts": {
            "path_hits": str(PATH_HITS),
            "root_dump": str(ROOT_DUMP),
            "kd_stdout": str(KD_STDOUT),
        },
        "root_subtree_present": subtree_present,
        "runtime_hits": {
            "count": len(override_hits),
            "operations": format_counter(Counter(row["Operation"] for row in override_hits)),
            "processes": format_counter(Counter(row["Process Name"] for row in override_hits)),
            "paths": format_counter(Counter(row["Path"] for row in override_hits)),
            "results": format_counter(Counter(row["Result"] for row in override_hits)),
            "sample_rows": [
                {
                    "time": row["Time of Day"],
                    "process": row["Process Name"],
                    "pid": row["PID"],
                    "operation": row["Operation"],
                    "path": row["Path"],
                    "result": row["Result"],
                    "detail": row.get("Detail", ""),
                }
                for row in override_hits[:8]
            ],
        },
        "kd_override_symbols": kd_symbols_found,
        "interpretation": [
            "The retained root dump proves that Control\\Power\\PowerRequestOverride exists as a persisted subtree.",
            "The retained current-build path-hits capture shows repeated svchost.exe access to the subtree root plus Driver, Process, and Service leaves.",
            "The retained wildcard KD lineage exposes an override family around response handling, initialization, and UMPO override query/cleanup.",
            "This is enough to open a dedicated runtime-backed subtree draft even though exact leaf values and a bounded Ghidra path are still unresolved.",
        ],
    }

    (REPO_ROOT / JSON_OUT).write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")

    md = [
        "# PowerRequestOverride Runtime Audit",
        "",
        "Date: 2026-04-08",
        f"Target path: `{payload['target_path']}`",
        "",
        "## Outcome",
        "",
        f"- Root subtree present in retained dump: `{subtree_present}`",
        f"- Runtime hits under subtree: `{len(override_hits)}`",
        f"- Processes: `{payload['runtime_hits']['processes']}`",
        f"- Operations: `{payload['runtime_hits']['operations']}`",
        f"- Results: `{payload['runtime_hits']['results']}`",
        f"- KD override symbols found: `{kd_symbols_found}`",
        "",
        "## Interpretation",
        "",
        "- The retained root dump proves that `Control\\Power\\PowerRequestOverride` exists as a persisted subtree.",
        "- The retained current-build runtime trace shows repeated `svchost.exe` access to the subtree root plus the `Driver`, `Process`, and `Service` leaves.",
        "- The retained wildcard KD lineage exposes an override family around response handling, initialization, and UMPO override query / cleanup.",
        "- Exact leaf values and a bounded Ghidra path are still unresolved, so this remains a draft subtree lane rather than an app-ready tweak.",
        "",
        "## Artifacts",
        "",
        f"- `{PATH_HITS}`",
        f"- `{ROOT_DUMP}`",
        f"- `{KD_STDOUT}`",
        f"- `{JSON_OUT}`",
    ]
    (REPO_ROOT / MD_OUT).write_text("\n".join(md) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
