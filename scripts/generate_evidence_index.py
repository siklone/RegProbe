#!/usr/bin/env python3
from __future__ import annotations

import json
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from evidence_class_lib import build_class_entry, load_overrides, load_provenance_map as load_provenance_entries
from research_path_lib import REPO_ROOT, RESEARCH_ROOT, V31_EVIDENCE_ROOT, normalize_reference, normalize_reference_text

RECORDS_DIR = RESEARCH_ROOT / "records"
PROVENANCE_PATH = REPO_ROOT / "Docs" / "tweaks" / "tweak-provenance.json"
OVERRIDES_PATH = RESEARCH_ROOT / "evidence-class-overrides.json"
OUTPUT_PATH = RESEARCH_ROOT / "evidence-index.json"
REDACTED_USER = "<USER>"
HOME_PATH = str(Path.home())
HOME_PATH_FWD = HOME_PATH.replace("\\", "/")
USER_PATH_REPLACEMENTS = {
    HOME_PATH: HOME_PATH.replace(Path.home().name, REDACTED_USER),
    HOME_PATH_FWD: HOME_PATH_FWD.replace(Path.home().name, REDACTED_USER),
}


def sanitize_text(value: Any) -> Any:
    if isinstance(value, str):
        text = value
        for source, replacement in USER_PATH_REPLACEMENTS.items():
            text = text.replace(source, replacement)
        return text
    return value


def sanitize_value(value: Any) -> Any:
    if isinstance(value, dict):
        return {key: sanitize_value(item) for key, item in value.items()}
    if isinstance(value, list):
        return [sanitize_value(item) for item in value]
    return sanitize_text(value)


def pick(record: dict[str, Any], keys: list[str]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for key in keys:
        value = record.get(key)
        if value is not None:
            result[key] = sanitize_value(value)
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
        if isinstance(compact.get("notes"), str):
            compact["notes"] = normalize_reference_text(str(compact["notes"]), title=str(compact.get("target_id") or "Target"))
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
        if isinstance(compact.get("notes"), str):
            compact["notes"] = normalize_reference_text(str(compact["notes"]), title=str(compact.get("profile_id") or "Profile"))
        profiles.append(compact)
    return profiles


def has_nohuto_lineage(provenance_entry: dict[str, Any] | None) -> bool:
    if not provenance_entry:
        return False
    for item in provenance_entry.get("References", []) or []:
        if str(item.get("Kind") or "").strip().lower() == "nohuto":
            return True
    return False


def evidence_origin(kind: Any) -> str:
    mapping = {
        "official-doc": "Microsoft official doc",
        "policy-csp": "Microsoft policy CSP",
        "troubleshoot-doc": "Microsoft support doc",
        "repo-code": "Current repo code",
        "repo-doc": "Current repo docs",
        "procmon-trace": "VM Procmon trace",
        "runtime-diff": "VM runtime diff",
        "vm-test": "VM test / probe",
        "registry-observation": "VM registry observation",
        "decompilation": "Our Ghidra decompilation",
        "decompiled-pseudocode": "Nohuto upstream pseudocode",
    }
    return mapping.get(str(kind), "unspecified")


def compact_evidence(record: dict[str, Any], provenance_entry: dict[str, Any] | None) -> list[dict[str, Any]]:
    evidence: list[dict[str, Any]] = []
    includes_nohuto = has_nohuto_lineage(provenance_entry)
    for item in record.get("evidence", []) or []:
        compact = pick(
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
        title = str(compact.get("title") or "")
        location = str(compact.get("location") or "")
        is_nohuto_decomp = compact.get("kind") == "decompilation" and (
            includes_nohuto or "nohuto" in title.lower() or "nohuto" in location.lower()
        )
        origin = evidence_origin(compact.get("kind"))
        if is_nohuto_decomp:
            origin = "Nohuto's and our Ghidra decompilation"
        compact["origin"] = origin
        if isinstance(compact.get("location"), str):
            compact["location"] = normalize_reference_text(str(compact["location"]), title=str(compact.get("title") or "Evidence"))
        if is_nohuto_decomp:
            title = str(compact.get("title") or "Decompilation")
            if not title.startswith("Nohuto's and our Ghidra decompilation"):
                compact["title"] = f"Nohuto's and our Ghidra decompilation - {title}"
        evidence.append(compact)
    return evidence


def compact_reference(item: dict[str, Any]) -> dict[str, Any]:
    compact = pick(item, ["Kind", "Title", "Url", "Summary"])
    if isinstance(compact.get("Url"), str):
        compact["Url"] = normalize_reference(str(compact["Url"]), title=str(compact.get("Title") or "Reference"))
    return compact


def compact_provenance(record: dict[str, Any], provenance_map: dict[str, dict[str, Any]]) -> dict[str, Any] | None:
    entry = provenance_map.get(str(record.get("record_id") or record.get("tweak_id") or ""))
    if not entry:
        return None

    references = [compact_reference(item) for item in entry.get("References", []) or []]
    nohuto_references = [item for item in references if item.get("Kind") == "nohuto"]
    internals_references = [item for item in references if item.get("Kind") == "internals"]
    other_references = [item for item in references if item.get("Kind") not in {"nohuto", "internals"}]

    return sanitize_value(
        {
            "coverage_state": entry.get("CoverageState"),
            "has_nohuto_evidence": entry.get("HasNohutoEvidence"),
            "has_windows_internals_context": entry.get("HasWindowsInternalsContext"),
            "needs_review": entry.get("NeedsReview"),
            "source_repositories": entry.get("SourceRepositories"),
            "matched_tokens": entry.get("MatchedTokens"),
            "lineage_note": (
                "Nohuto references only show upstream dump or naming links. "
                "Value semantics are validated separately in the record evidence and validation proof."
            ),
            "nohuto_references": nohuto_references,
            "windows_internals_references": internals_references,
            "other_references": other_references,
        }
    )


def compact_validation_proof(record: dict[str, Any]) -> dict[str, Any] | None:
    proof = record.get("validation_proof")
    if not isinstance(proof, dict):
        return None

    normalized = sanitize_value(proof)
    if isinstance(normalized.get("source_url"), str):
        normalized["source_url"] = normalize_reference(str(normalized["source_url"]), title="Validation proof")
    if isinstance(normalized.get("exact_quote_or_path"), str):
        normalized["exact_quote_or_path"] = normalize_reference_text(str(normalized["exact_quote_or_path"]), title="Validation proof")
    if isinstance(normalized.get("notes"), str):
        normalized["notes"] = normalize_reference_text(str(normalized["notes"]), title="Validation proof")
    return normalized


def compact_optional_block(record: dict[str, Any], key: str) -> dict[str, Any] | None:
    payload = record.get(key)
    if isinstance(payload, dict) and payload:
        return sanitize_value(payload)
    return None


def load_v31_companion(record_id: str) -> dict[str, Any] | None:
    path = V31_EVIDENCE_ROOT / record_id / "full-evidence.json"
    if not path.exists():
        return None
    with path.open("r", encoding="utf-8-sig") as handle:
        return json.load(handle)


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
    provenance_map = load_provenance_entries(PROVENANCE_PATH)
    overrides = load_overrides(OVERRIDES_PATH)
    class_counts: Counter[str] = Counter()

    for path in sorted(RECORDS_DIR.glob("*.json")):
        with path.open("r", encoding="utf-8-sig") as handle:
            record = json.load(handle)

        record_key = str(record.get("record_id") or record.get("tweak_id") or "")
        provenance_entry = provenance_map.get(record_key)
        status = record.get("record_status", "unknown")
        status_counts[status] += 1

        evidence = compact_evidence(record, provenance_entry)
        if not evidence:
            evidence_counts["missing"] += 1
        else:
            evidence_counts["present"] += 1

        proof = compact_validation_proof(record)
        if proof is None:
            validation_proof_missing += 1
            if status == "deprecated":
                deprecated_without_validation_proof += 1

        class_entry = build_class_entry(
            record,
            provenance_entry=provenance_entry,
            override=overrides.get(record_key),
        )
        class_counts[class_entry["evidence_class"]] += 1
        v31 = load_v31_companion(record_key)

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
                "evidence_class": class_entry,
                "decision": compact_decision(record),
                "app_current_implementation": compact_app_impl(record),
                "targets": compact_targets(record),
                "windows_defaults": compact_windows_defaults(record),
                "recommended_profiles": compact_profiles(record),
                "evidence": evidence,
                "validation_proof": proof,
                "doc_source": compact_optional_block(record, "doc_source"),
                "static_analysis": compact_optional_block(record, "static_analysis"),
                "cross_verification": compact_optional_block(record, "cross_verification"),
                "provenance": compact_provenance(record, provenance_map),
                "v31_evidence_root": (
                    str((V31_EVIDENCE_ROOT / record_key).relative_to(REPO_ROOT)).replace("\\", "/")
                    if (V31_EVIDENCE_ROOT / record_key).exists()
                    else None
                ),
                "v31_pipeline_version": (v31 or {}).get("classification", {}).get("pipeline_version"),
                "artifact_refs": (v31 or {}).get("artifact_refs", []),
                "re_audit": (v31 or {}).get("re_audit"),
            }
        )

    output = {
        "schema_version": "1.0",
        "generated_utc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "summary": {
            "total_records": len(records),
            "validated": status_counts.get("validated", 0),
            "deprecated": status_counts.get("deprecated", 0),
            "review_required": status_counts.get("review_required", 0),
            "records_with_evidence": evidence_counts.get("present", 0),
            "records_without_evidence": evidence_counts.get("missing", 0),
            "records_missing_validation_proof": validation_proof_missing,
            "deprecated_missing_validation_proof": deprecated_without_validation_proof,
            "class_counts": dict(class_counts),
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
