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

from research_v36_lib import (  # noqa: E402
    canonical_bundle_projection,
    documentation_quality_projection,
    load_audit_entries,
    load_full_evidence_bundle,
    load_promotion_gate_map,
    load_records,
    validate_canonical_bundle,
)


def build_validation_summary(
    records: list[dict] | None = None,
    audit_map: dict[str, dict] | None = None,
    gate_map: dict[str, dict] | None = None,
    full_evidence_loader=None,
) -> dict[str, object]:
    audit_map = audit_map or load_audit_entries()
    gate_map = gate_map or load_promotion_gate_map()
    records = records or load_records()
    full_evidence_loader = full_evidence_loader or load_full_evidence_bundle
    details: list[dict[str, object]] = []

    invalid_count = 0
    undocumented_count = 0
    blocked_count = 0
    missing_docs_count = 0

    for record in records:
        if str(record.get("record_status") or "").strip().lower() == "deprecated":
            continue
        candidate_id = str(record.get("record_id") or record.get("tweak_id") or "")
        audit = audit_map.get(candidate_id, {})
        gate = gate_map.get(candidate_id, {})
        full_evidence = full_evidence_loader(candidate_id)
        bundle = canonical_bundle_projection(record, audit, full_evidence)
        schema_errors = validate_canonical_bundle(bundle)
        docs = documentation_quality_projection(record, full_evidence, gate)
        blocked = str(gate.get("promotion_state") or "") == "blocked"
        undocumented = "missing-validation-proof" in (docs.get("documentation_issues") or []) or "missing-source-enrichment" in (docs.get("documentation_issues") or [])
        missing_docs = not bool(docs.get("documentation_quality_pass"))

        invalid_count += int(bool(schema_errors))
        undocumented_count += int(undocumented)
        blocked_count += int(blocked)
        missing_docs_count += int(missing_docs)

        if schema_errors or undocumented or blocked or missing_docs:
            details.append(
                {
                    "candidate_id": candidate_id,
                    "promotion_state": gate.get("promotion_state"),
                    "schema_errors": schema_errors,
                    "documentation_issues": docs.get("documentation_issues") or [],
                    "undocumented": undocumented,
                    "missing_docs": missing_docs,
                    "blocked": blocked,
                }
            )

    return {
        "invalid_count": invalid_count,
        "undocumented_count": undocumented_count,
        "blocked_count": blocked_count,
        "missing_docs_count": missing_docs_count,
        "details": details,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate RegProbe candidate batches for schema/docs/gate issues.")
    parser.add_argument("--undocumented", action="store_true", help="Only show undocumented candidates.")
    parser.add_argument("--invalid", action="store_true", help="Only show schema-invalid candidates.")
    parser.add_argument("--blocked-state", action="store_true", help="Only show promotion_state=blocked candidates.")
    parser.add_argument("--missing-docs", action="store_true", help="Only show documentation-quality failures.")
    parser.add_argument("--emit-json", action="store_true", help="Emit JSON payload.")
    args = parser.parse_args()

    payload = build_validation_summary()
    filters_enabled = any((args.undocumented, args.invalid, args.blocked_state, args.missing_docs))
    if filters_enabled:
        filtered = []
        for detail in payload["details"]:
            include = False
            include |= args.undocumented and bool(detail["undocumented"])
            include |= args.invalid and bool(detail["schema_errors"])
            include |= args.blocked_state and bool(detail["blocked"])
            include |= args.missing_docs and bool(detail["missing_docs"])
            if include:
                filtered.append(detail)
        payload = {
            **payload,
            "details": filtered,
        }

    if args.emit_json:
        print(json.dumps(payload, ensure_ascii=False, indent=2))
    else:
        print(json.dumps(
            {
                "invalid_count": payload["invalid_count"],
                "undocumented_count": payload["undocumented_count"],
                "blocked_count": payload["blocked_count"],
                "missing_docs_count": payload["missing_docs_count"],
                "detail_count": len(payload["details"]),
            },
            ensure_ascii=False,
            indent=2,
        ))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
