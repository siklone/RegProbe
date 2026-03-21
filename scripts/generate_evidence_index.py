#!/usr/bin/env python3
from __future__ import annotations

import json
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[1]
RECORDS_DIR = REPO_ROOT / "Docs" / "tweaks" / "research" / "records"
OUTPUT_PATH = REPO_ROOT / "Docs" / "tweaks" / "research" / "evidence-index.json"


def pick(record: dict[str, Any], keys: list[str]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for key in keys:
        value = record.get(key)
        if value is not None:
            result[key] = value
    return result


def compact_allowed_values(target: dict[str, Any]) -> list[dict[str, Any]]:
    values: list[dict[str, Any]] = []
    for item in target.get("allowed_values", []) or []:
        values.append(
            pick(
                item,
                [
                    "state_kind",
                    "value",
                    "label",
                    "meaning",
                    "evidence_ids",
                ],
            )
        )
    return values


def compact_targets(record: dict[str, Any]) -> list[dict[str, Any]]:
    targets: list[dict[str, Any]] = []
    for target in record.get("setting", {}).get("targets", []) or []:
        compact = pick(
            target,
            [
                "target_id",
                "location_kind",
                "path",
                "value_name",
                "value_type",
                "notes",
            ],
        )
        compact["allowed_values"] = compact_allowed_values(target)
        targets.append(compact)
    return targets


def compact_windows_defaults(record: dict[str, Any]) -> list[dict[str, Any]]:
    defaults: list[dict[str, Any]] = []
    for item in record.get("windows_defaults", []) or []:
        compact = pick(item, ["label", "applies_to", "states", "evidence_ids"])
        defaults.append(compact)
    return defaults


def compact_profiles(record: dict[str, Any]) -> list[dict[str, Any]]:
    profiles: list[dict[str, Any]] = []
    for item in record.get("recommended_profiles", []) or []:
        compact = pick(
            item,
            [
                "profile_id",
                "label",
                "intended_for",
                "avoid_for",
                "states",
                "good_when",
                "bad_when",
                "tradeoffs",
                "apply_allowed",
                "notes",
                "evidence_ids",
            ],
        )
        profiles.append(compact)
    return profiles


def compact_evidence(record: dict[str, Any]) -> list[dict[str, Any]]:
    evidence: list[dict[str, Any]] = []
    for item in record.get("evidence", []) or []:
        evidence.append(
            pick(
                item,
                [
                    "evidence_id",
                    "kind",
                    "title",
                    "location",
                    "summary",
                    "strength",
                    "supports",
                ],
            )
        )
    return evidence


def compact_validation_proof(record: dict[str, Any]) -> dict[str, Any] | None:
    proof = record.get("validation_proof")
    if isinstance(proof, dict):
        return proof
    return None


def compact_decision(record: dict[str, Any]) -> dict[str, Any] | None:
    decision = record.get("decision")
    if isinstance(decision, dict):
        return pick(
            decision,
            [
                "confidence",
                "apply_allowed",
                "recommended_for_general_users",
                "restore_default_supported",
                "restore_previous_supported",
                "needs_vm_validation",
                "why",
                "blocking_issues",
            ],
        )
    return None


def compact_app_impl(record: dict[str, Any]) -> dict[str, Any] | None:
    impl = record.get("app_current_implementation")
    if isinstance(impl, dict):
        return pick(
            impl,
            [
                "status",
                "provider_source",
                "notes",
                "writes",
            ],
        )
    return None


def main() -> int:
    records: list[dict[str, Any]] = []
    status_counts: Counter[str] = Counter()
    evidence_counts: Counter[str] = Counter()
    validation_proof_missing = 0
    deprecated_without_validation_proof = 0

    for path in sorted(RECORDS_DIR.glob("*.json")):
        with path.open("r", encoding="utf-8") as handle:
            record = json.load(handle)

        status = record.get("record_status", "unknown")
        status_counts[status] += 1

        evidence = compact_evidence(record)
        if not evidence:
            evidence_counts["missing"] += 1
        else:
            evidence_counts["present"] += 1

        proof = compact_validation_proof(record)
        if proof is None:
            validation_proof_missing += 1
            if status == "deprecated":
                deprecated_without_validation_proof += 1

        records.append(
            {
                "record_id": record.get("record_id"),
                "tweak_id": record.get("tweak_id"),
                "record_status": status,
                "source_file": str(path.relative_to(REPO_ROOT)).replace("\\", "/"),
                "name": record.get("setting", {}).get("name"),
                "category": record.get("setting", {}).get("category"),
                "area": record.get("setting", {}).get("area"),
                "scope": record.get("setting", {}).get("scope"),
                "summary": record.get("summary"),
                "decision": compact_decision(record),
                "app_current_implementation": compact_app_impl(record),
                "targets": compact_targets(record),
                "windows_defaults": compact_windows_defaults(record),
                "recommended_profiles": compact_profiles(record),
                "evidence": evidence,
                "validation_proof": proof,
            }
        )

    output = {
        "schema_version": "1.0",
        "generated_utc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "summary": {
            "total_records": len(records),
            "validated": status_counts.get("validated", 0),
            "deprecated": status_counts.get("deprecated", 0),
            "review_required": status_counts.get("review-required", 0),
            "records_with_evidence": evidence_counts.get("present", 0),
            "records_without_evidence": evidence_counts.get("missing", 0),
            "records_missing_validation_proof": validation_proof_missing,
            "deprecated_missing_validation_proof": deprecated_without_validation_proof,
        },
        "records": sorted(records, key=lambda item: (item["record_status"], item["record_id"] or "")),
    }

    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    with OUTPUT_PATH.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(output, handle, ensure_ascii=False, indent=2)
        handle.write("\n")

    print(f"Wrote {OUTPUT_PATH}")
    print(
        "Summary: "
        f"{output['summary']['total_records']} records, "
        f"{output['summary']['validated']} validated, "
        f"{output['summary']['deprecated']} deprecated, "
        f"{output['summary']['review_required']} review-required."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
