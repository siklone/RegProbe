#!/usr/bin/env python3
"""Audit retained stepwise boot-trace evidence for HiberFileSizePercent."""

from __future__ import annotations

import csv
import json
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SOURCE_CSV = Path(
    "evidence/files/vm-tooling-staging/"
    "power-control-docs-first-stepwise-runtime-20260329-143515/path-hits.csv"
)
SOURCE_SUMMARY = Path(
    "evidence/files/vm-tooling-staging/"
    "power-control-docs-first-stepwise-runtime-20260329-143515/summary.json"
)
JSON_OUT = Path(
    "registry-research-framework/audit/"
    "hiber-file-size-percent-stepwise-runtime-audit-20260408.json"
)
MD_OUT = Path(
    "registry-research-framework/audit/"
    "hiber-file-size-percent-stepwise-runtime-audit-20260408.md"
)

TARGET_PATH = r"HKLM\System\CurrentControlSet\Control\Power\HiberFileSizePercent"


def load_rows(path: Path) -> list[list[str]]:
    with path.open(newline="", encoding="utf-8-sig") as handle:
        return [row for row in csv.reader(handle) if row]


def main() -> int:
    rows = load_rows(REPO_ROOT / SOURCE_CSV)
    with (REPO_ROOT / SOURCE_SUMMARY).open("r", encoding="utf-8-sig") as handle:
        summary = json.load(handle)

    hit_rows = [row for row in rows if len(row) >= 7 and row[4] == TARGET_PATH and row[3] == "RegQueryValue"]
    success_rows = [row for row in hit_rows if row[5] == "SUCCESS"]

    payload = {
        "date": "2026-04-08",
        "target_record_id": "power.control.hiber-file-size-percent",
        "source_artifacts": {
            "path_hits_csv": str(SOURCE_CSV),
            "stepwise_summary": str(SOURCE_SUMMARY),
            "docs_note": "Docs/power/power.md:149",
        },
        "stepwise_probe_status": summary.get("status"),
        "snapshot_name": summary.get("snapshot_name"),
        "boot_cycle": summary.get("boot_cycle"),
        "shell_before_healthy": ((summary.get("shell_before") or {}).get("shell_healthy")),
        "shell_after_healthy": ((summary.get("shell_after") or {}).get("shell_healthy")),
        "query_rows": [
            {
                "time": row[0],
                "process": row[1],
                "pid": row[2],
                "operation": row[3],
                "path": row[4],
                "result": row[5],
                "detail": row[6],
            }
            for row in hit_rows
        ],
        "success_count": len(success_rows),
        "successful_exact_read": bool(success_rows),
        "ida_symbol_note": "Docs/power/power.md:149 preserves the IDA-derived symbol note `PopHiberFileSizePercent` for the same value name.",
        "interpretation": [
            "The retained docs-first stepwise Procmon boot trace contains an exact RegQueryValue SUCCESS for HKLM\\System\\CurrentControlSet\\Control\\Power\\HiberFileSizePercent.",
            "The hit comes from smss.exe during the rebooted boot cycle on RegProbe-Baseline-Clean-20260329.",
            "The same repo power notes preserve the IDA-derived internal symbol name PopHiberFileSizePercent, giving this candidate a reviewable static naming layer in addition to the runtime read.",
        ],
    }

    (REPO_ROOT / JSON_OUT).write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")

    md = [
        "# HiberFileSizePercent Stepwise Runtime Audit",
        "",
        "Date: 2026-04-08",
        "",
        "## Scope",
        "",
        "Re-audit the retained docs-first stepwise boot trace for `power.control.hiber-file-size-percent` and preserve the exact boot-time read in a machine-readable form.",
        "",
        "## Source Artifacts",
        "",
        f"- `{SOURCE_CSV}`",
        f"- `{SOURCE_SUMMARY}`",
        "- `Docs/power/power.md:149`",
        "",
        "## Findings",
        "",
        f"- Stepwise probe status: `{summary.get('status')}`",
        f"- Snapshot: `{summary.get('snapshot_name')}`",
        f"- Boot stop mode: `{((summary.get('boot_cycle') or {}).get('stop_mode'))}`",
        f"- Shell healthy before: `{((summary.get('shell_before') or {}).get('shell_healthy'))}`",
        f"- Shell healthy after: `{((summary.get('shell_after') or {}).get('shell_healthy'))}`",
        f"- Exact `RegQueryValue` rows for `HiberFileSizePercent`: `{len(hit_rows)}`",
        f"- Successful exact reads: `{len(success_rows)}`",
        "",
    ]
    for row in success_rows:
        md.extend(
            [
                "### Successful Exact Read",
                "",
                f"- Time: `{row[0]}`",
                f"- Process: `{row[1]}` (PID `{row[2]}`)",
                f"- Operation: `{row[3]}`",
                f"- Path: `{row[4]}`",
                f"- Result: `{row[5]}`",
                f"- Detail: `{row[6]}`",
                "",
            ]
        )
    md.extend(
        [
            "## Interpretation",
            "",
            "- The retained docs-first stepwise Procmon boot trace contains an exact `RegQueryValue SUCCESS` for `HKLM\\System\\CurrentControlSet\\Control\\Power\\HiberFileSizePercent`.",
            "- The hit comes from `smss.exe` during the rebooted boot cycle on `RegProbe-Baseline-Clean-20260329`.",
            "- `Docs/power/power.md:149` also preserves the IDA-derived symbol note `PopHiberFileSizePercent`, which gives this candidate a static decompilation-derived naming layer alongside the runtime read.",
            "",
            "## Artifacts",
            "",
            f"- `{JSON_OUT}`",
            f"- `{SOURCE_CSV}`",
            f"- `{SOURCE_SUMMARY}`",
        ]
    )
    (REPO_ROOT / MD_OUT).write_text("\n".join(md) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
