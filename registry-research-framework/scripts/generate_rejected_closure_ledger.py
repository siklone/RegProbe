#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"
if str(SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_ROOT))

from research_v36_lib import PROMOTION_GATES_PATH, list_records, load_json, write_json  # noqa: E402

AUDIT_ROOT = REPO_ROOT / "registry-research-framework" / "audit"
JSON_OUTPUT = AUDIT_ROOT / "rejected-closure-ledger.json"
MARKDOWN_OUTPUT = AUDIT_ROOT / "rejected-closure-ledger.md"


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def evidence_refs(record: dict[str, Any]) -> list[dict[str, str]]:
    refs: list[dict[str, str]] = []
    for item in record.get("evidence") or []:
        if not isinstance(item, dict):
            continue
        evidence_id = str(item.get("evidence_id") or item.get("title") or "").strip()
        location = str(item.get("location") or item.get("url") or "").strip()
        kind = str(item.get("kind") or "").strip()
        if not evidence_id and not location:
            continue
        refs.append(
            {
                "evidence_id": evidence_id,
                "kind": kind,
                "location": location,
            }
        )
    return refs


def first_sentence(value: str, max_length: int = 140) -> str:
    text = " ".join(str(value or "").split())
    if len(text) <= max_length:
        return text
    return text[: max_length - 1].rstrip() + "..."


def markdown_cell(value: str) -> str:
    return str(value or "").replace("|", "\\|").replace("\n", " ").replace("`", "\\`")


def build_ledger(
    gate_payload: dict[str, Any],
    records_by_id: dict[str, dict[str, Any]],
    *,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or utc_now_iso()
    items: list[dict[str, Any]] = []
    closure_status_counts: Counter[str] = Counter()
    closure_kind_counts: Counter[str] = Counter()

    for entry in gate_payload.get("entries") or []:
        if str(entry.get("promotion_state") or "") != "rejected":
            continue
        record_id = str(entry.get("record_id") or entry.get("candidate_id") or entry.get("tweak_id") or "")
        record = records_by_id.get(record_id, {})
        closure = entry.get("rejection_closure") if isinstance(entry.get("rejection_closure"), dict) else {}
        closure_status = str(entry.get("closure_status") or closure.get("status") or "unclassified-rejected")
        closure_kind = str(entry.get("closure_kind") or closure.get("kind") or "unclassified")
        closure_status_counts[closure_status] += 1
        closure_kind_counts[closure_kind] += 1

        items.append(
            {
                "record_id": record_id,
                "tweak_id": str(entry.get("tweak_id") or record.get("tweak_id") or record_id),
                "closure_status": closure_status,
                "closure_kind": closure_kind,
                "closure_reason": str(entry.get("closure_reason") or closure.get("reason") or ""),
                "closure_blocker": str(closure.get("closure_blocker") or (entry.get("promotion_blockers") or [""])[0]),
                "promotion_disposition": entry.get("promotion_disposition"),
                "promotion_blockers": list(entry.get("promotion_blockers") or []),
                "superseded_blockers": list(closure.get("superseded_blockers") or []),
                "next_missing_layer": entry.get("next_missing_layer"),
                "confidence": closure.get("confidence") or (entry.get("documentation_status") or {}).get("confidence"),
                "evidence_count": int(closure.get("evidence_count") or (entry.get("evidence_status") or {}).get("evidence_count") or 0),
                "evidence_refs": evidence_refs(record),
                "source_file": str(record.get("_source_file") or ""),
            }
        )

    items.sort(key=lambda item: (str(item.get("closure_status")), str(item.get("closure_kind")), str(item.get("record_id"))))
    total = len(items)
    evidence_backed = int(closure_status_counts.get("evidence-backed-rejected") or 0)
    deprecated = int(closure_status_counts.get("deprecated-record") or 0)
    unclassified = sum(
        count
        for status, count in closure_status_counts.items()
        if str(status).startswith("unclassified")
    )

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "summary": {
            "total_rejected": total,
            "evidence_backed_rejected": evidence_backed,
            "deprecated_records": deprecated,
            "unclassified_rejected": unclassified,
            "closure_status_counts": dict(sorted(closure_status_counts.items())),
            "closure_kind_counts": dict(sorted(closure_kind_counts.items())),
            "all_rejected_have_closure": total > 0 and unclassified == 0,
        },
        "items": items,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    summary = payload.get("summary") or {}
    lines = [
        "# Rejected Closure Ledger",
        "",
        f"Generated: `{payload.get('generated_utc')}`",
        "",
        "Rejected records are not treated as active evidence gaps here. Each row records the closure lane that explains why the tweak is not promoted.",
        "",
        "| Metric | Value |",
        "|---|---:|",
        f"| Total rejected | {int(summary.get('total_rejected') or 0)} |",
        f"| Evidence-backed rejected | {int(summary.get('evidence_backed_rejected') or 0)} |",
        f"| Deprecated records | {int(summary.get('deprecated_records') or 0)} |",
        f"| Unclassified rejected | {int(summary.get('unclassified_rejected') or 0)} |",
        "",
        "## Closure Status Counts",
        "",
        "| Status | Count |",
        "|---|---:|",
    ]
    for status, count in (summary.get("closure_status_counts") or {}).items():
        lines.append(f"| `{status}` | {count} |")

    lines.extend(
        [
            "",
            "## Rejected Records",
            "",
            "| Record | Closure | Evidence | Superseded blockers | Reason |",
            "|---|---|---:|---|---|",
        ]
    )
    for item in payload.get("items") or []:
        record_id = markdown_cell(str(item.get("record_id") or ""))
        closure = f"`{markdown_cell(str(item.get('closure_status') or ''))}` / `{markdown_cell(str(item.get('closure_kind') or ''))}`"
        evidence_count = int(item.get("evidence_count") or 0)
        superseded = ", ".join(f"`{markdown_cell(str(value))}`" for value in (item.get("superseded_blockers") or [])[:3])
        if not superseded:
            superseded = "n/a"
        reason = markdown_cell(first_sentence(str(item.get("closure_reason") or "")))
        lines.append(f"| `{record_id}` | {closure} | {evidence_count} | {superseded} | {reason} |")

    lines.append("")
    return "\n".join(lines)


def records_by_id() -> dict[str, dict[str, Any]]:
    result: dict[str, dict[str, Any]] = {}
    for path in list_records():
        record = load_json(path)
        record_id = str(record.get("record_id") or record.get("tweak_id") or "")
        if record_id:
            record["_source_file"] = str(path.relative_to(REPO_ROOT)).replace("\\", "/")
            result[record_id] = record
    return result


def write_outputs(payload: dict[str, Any]) -> None:
    write_json(JSON_OUTPUT, payload)
    MARKDOWN_OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    MARKDOWN_OUTPUT.write_text(render_markdown(payload), encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate the rejected promotion closure ledger.")
    parser.add_argument("--emit-json", action="store_true", help="Print summary JSON.")
    args = parser.parse_args()

    payload = build_ledger(load_json(PROMOTION_GATES_PATH), records_by_id())
    write_outputs(payload)

    if args.emit_json:
        print(json.dumps(payload["summary"], ensure_ascii=False, indent=2))
    else:
        print(f"Wrote {JSON_OUTPUT}")
        print(f"Wrote {MARKDOWN_OUTPUT}")
        print(json.dumps(payload["summary"], ensure_ascii=False, indent=2))
    return 0 if payload.get("summary", {}).get("all_rejected_have_closure") else 1


if __name__ == "__main__":
    raise SystemExit(main())
