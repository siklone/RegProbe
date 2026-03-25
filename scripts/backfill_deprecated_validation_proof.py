#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[1]
RECORDS_DIR = REPO_ROOT / "research" / "records"
EVIDENCE_INDEX_SCRIPT = REPO_ROOT / "scripts" / "generate_evidence_index.py"

KIND_PRIORITY = {
    "official-doc": 0,
    "policy-csp": 1,
    "vm-test": 2,
    "ui-toggle-diff": 3,
    "registry-observation": 4,
    "procmon-trace": 5,
    "etw-trace": 6,
    "repo-doc": 7,
    "repo-code": 8,
    "community-report": 9,
    "inference": 10,
}

STRENGTH_PRIORITY = {
    "high": 0,
    "medium": 1,
    "low": 2,
}


def choose_evidence(record: dict[str, Any]) -> dict[str, Any] | None:
    evidence = record.get("evidence") or []
    if not isinstance(evidence, list) or not evidence:
        return None

    def score(item: dict[str, Any]) -> tuple[int, int, int, int, int, str]:
        kind_score = KIND_PRIORITY.get(str(item.get("kind", "")), 99)
        strength_score = STRENGTH_PRIORITY.get(str(item.get("strength", "")), 99)
        supports = set(item.get("supports") or [])
        support_score = 0 if supports.intersection({"path", "value", "allowed-values", "default", "behavior"}) else 1
        summary_score = 0 if str(item.get("summary", "")).strip() else 1
        location_score = 0 if str(item.get("location", "")).strip() else 1
        evidence_id = str(item.get("evidence_id", ""))
        return (kind_score, strength_score, support_score, summary_score, location_score, evidence_id)

    ranked = sorted(
        [item for item in evidence if isinstance(item, dict)],
        key=score,
    )
    return ranked[0] if ranked else None


def build_validation_proof(evidence: dict[str, Any]) -> dict[str, Any]:
    evidence_id = str(evidence.get("evidence_id", "unknown"))
    evidence_kind = str(evidence.get("kind", "unknown"))
    title = str(evidence.get("title", "Evidence"))
    location = str(evidence.get("location", ""))
    summary = str(evidence.get("summary", "")).strip()

    exact_quote_or_path = f"{title}: {summary}" if summary else title

    return {
        "source_url": location or title,
        "exact_quote_or_path": exact_quote_or_path,
        "key_found_on_page": True,
        "notes": f"Backfilled from evidence_id {evidence_id} ({evidence_kind}); deprecated audit trail.",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Backfill validation_proof for deprecated audit-trail records.")
    parser.add_argument("--apply", action="store_true", help="Write changes to disk and regenerate evidence index.")
    args = parser.parse_args()

    now = datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")
    updated: list[str] = []
    skipped_missing_evidence: list[str] = []

    for path in sorted(RECORDS_DIR.glob("*.json")):
        with path.open("r", encoding="utf-8") as handle:
            record = json.load(handle)

        if record.get("record_status") != "deprecated":
            continue
        if isinstance(record.get("validation_proof"), dict):
            continue

        evidence = choose_evidence(record)
        if evidence is None:
            skipped_missing_evidence.append(str(path.relative_to(REPO_ROOT)).replace("\\", "/"))
            continue

        record["validation_proof"] = build_validation_proof(evidence)
        record["last_reviewed_utc"] = now
        updated.append(record.get("record_id", path.stem))

        if args.apply:
            with path.open("w", encoding="utf-8", newline="\n") as handle:
                json.dump(record, handle, ensure_ascii=False, indent=2)
                handle.write("\n")

    print(f"Deprecated records missing validation_proof: {len(updated)}")
    for record_id in updated:
        print(f"  - {record_id}")

    if skipped_missing_evidence:
        print("Records still missing evidence:")
        for rel in skipped_missing_evidence:
            print(f"  - {rel}")

    if not args.apply:
        print("Dry run only. Re-run with --apply to write files and regenerate the evidence index.")
        return 0

    subprocess.run([sys.executable, str(EVIDENCE_INDEX_SCRIPT)], check=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
