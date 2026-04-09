from __future__ import annotations

import hashlib
import json
import re
import subprocess
import xml.etree.ElementTree as ET
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from evidence_class_lib import (
    bool_value,
    extract_app_status,
    has_benchmark_evidence,
    has_ghidra_evidence,
    has_official_evidence,
    has_procmon_evidence,
    has_reboot_evidence,
    has_wpr_evidence,
    next_missing_layer,
    restore_story_known,
)
from research_path_lib import FRAMEWORK_ROOT, REPO_ROOT, RESEARCH_ROOT, normalize_repo_relative_path
from wave2_research_lib import evidence_freshness, reproducibility_manifest

CONTRACTS_ROOT = FRAMEWORK_ROOT / "contracts"
BACKENDS_ROOT = FRAMEWORK_ROOT / "config" / "backends"
DISCOVERY_ROOT = FRAMEWORK_ROOT / "discovery"
QUEUE_ROOT = FRAMEWORK_ROOT / "queue"
AUDIT_ROOT = FRAMEWORK_ROOT / "audit"
PROMOTION_GATES_PATH = RESEARCH_ROOT / "promotion-gates.json"
PROMOTION_AUDIT_LOG_PATH = AUDIT_ROOT / "promotion-audit-log.jsonl"
DISCOVERY_EVENTS_PATH = DISCOVERY_ROOT / "discovery-events.jsonl"
ETL_CORPUS_INVENTORY_PATH = DISCOVERY_ROOT / "etl-corpus-inventory.json"
ETL_REGISTRY_DISCOVERY_PATH = DISCOVERY_ROOT / "etl-registry-discovery.json"
GAP_ANALYSIS_SUMMARY_PATH = AUDIT_ROOT / "gap-analysis-summary.json"
ETL_PARSER_CONFIG_PATH = FRAMEWORK_ROOT / "config" / "etl-parser-config.json"
LEGACY_ETL_PARSER_CONFIG_PATH = FRAMEWORK_ROOT / "config" / "etl-parser.json"

QUEUE_STATES = {
    "discovered",
    "triaged",
    "scored",
    "blocked",
    "promotion-eligible",
    "promoted",
    "revalidation-pending",
    "rejected",
    "discarded",
}
PROMOTION_STATES = {
    "blocked",
    "promotion-eligible",
    "promoted",
    "revalidation-pending",
    "rejected",
}
EXECUTION_CONTEXT_TYPES = {"local", "vm", "remote"}
EXECUTION_CONTEXT_OS = {"windows", "linux-with-vm", "windows-runner"}
EXECUTION_CONTEXT_PRIVILEGE = {"user", "elevated", "system"}
EXECUTION_CONTEXT_ISOLATION = {"none", "vm", "container"}
SUPPORTED_SCHEMA_VERSIONS = {"1.0"}
EVALUATOR_VERSION = "3.6.0"
DEFAULT_BACKEND_ID = "rai-linux-vm"
CURRENT_SCHEMA_VERSION = "1.0"

DEFAULT_CAPABILITIES = {
    "registry_read": False,
    "registry_write": False,
    "reboot": False,
    "snapshot_restore": False,
    "etw_capture": False,
    "procmon_capture": False,
    "debugger_attach": False,
    "bench_run": False,
    "bench_bare_metal": False,
}

DEFAULT_REQUIRED_CAPABILITIES_BY_LANE = {
    "runtime": ["registry_read", "registry_write"],
    "procmon": ["procmon_capture", "registry_read"],
    "behavior": ["bench_run"],
}


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def load_json(path: Path) -> Any:
    with path.open("r", encoding="utf-8-sig") as handle:
        return json.load(handle)


def load_json_if_exists(path: Path) -> Any | None:
    if not path.exists():
        return None
    return load_json(path)


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(payload, handle, ensure_ascii=False, indent=2)
        handle.write("\n")


def append_jsonl(path: Path, entry: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("a", encoding="utf-8", newline="\n") as handle:
        handle.write(json.dumps(entry, ensure_ascii=False))
        handle.write("\n")


def list_records() -> list[Path]:
    return sorted((RESEARCH_ROOT / "records").glob("*.json"))


def load_records() -> list[dict[str, Any]]:
    return [load_json(path) for path in list_records()]


def load_etl_parser_config() -> dict[str, Any]:
    payload = load_json_if_exists(ETL_PARSER_CONFIG_PATH)
    if isinstance(payload, dict):
        return payload
    payload = load_json_if_exists(LEGACY_ETL_PARSER_CONFIG_PATH)
    if isinstance(payload, dict):
        return payload
    return {
        "schema_version": CURRENT_SCHEMA_VERSION,
        "default_parser": "tracerpt",
        "provider_guid": "{AE53722E-C863-11D2-8659-00C04FA321A1}",
        "parser_commands": {
            "tracerpt": "tracerpt",
        },
        "inventory_patterns": [
            "evidence/**/*.etl",
            "evidence/**/*.etl.md",
        ],
        "include_placeholder_markdown": True,
    }


def load_backend_capabilities(backend_id: str = DEFAULT_BACKEND_ID) -> dict[str, Any]:
    path = BACKENDS_ROOT / f"{backend_id}.capabilities.json"
    payload = load_json_if_exists(path)
    if not isinstance(payload, dict):
        raise FileNotFoundError(f"Missing backend manifest: {path}")
    return payload


def default_execution_context(backend_id: str = DEFAULT_BACKEND_ID) -> dict[str, Any]:
    payload = load_backend_capabilities(backend_id)
    return {
        "backend_id": backend_id,
        "type": payload.get("type"),
        "os": payload.get("os"),
        "privilege": payload.get("execution_context", {}).get("privilege", "user"),
        "isolation": payload.get("execution_context", {}).get("isolation", "vm" if payload.get("type") == "vm" else "none"),
    }


def validate_execution_context(payload: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    if str(payload.get("type") or "") not in EXECUTION_CONTEXT_TYPES:
        errors.append("type")
    if str(payload.get("os") or "") not in EXECUTION_CONTEXT_OS:
        errors.append("os")
    if str(payload.get("privilege") or "") not in EXECUTION_CONTEXT_PRIVILEGE:
        errors.append("privilege")
    if str(payload.get("isolation") or "") not in EXECUTION_CONTEXT_ISOLATION:
        errors.append("isolation")
    return errors


def validate_backend_capabilities(payload: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    if not str(payload.get("backend_id") or "").strip():
        errors.append("backend_id")
    if str(payload.get("type") or "") not in EXECUTION_CONTEXT_TYPES:
        errors.append("type")
    if str(payload.get("os") or "") not in EXECUTION_CONTEXT_OS:
        errors.append("os")
    capabilities = payload.get("capabilities")
    if not isinstance(capabilities, dict):
        errors.append("capabilities")
    else:
        for capability_name in DEFAULT_CAPABILITIES:
            if capability_name not in capabilities or not isinstance(capabilities.get(capability_name), bool):
                errors.append(f"capabilities.{capability_name}")
    execution_context = payload.get("execution_context")
    if not isinstance(execution_context, dict):
        errors.append("execution_context")
    else:
        errors.extend(f"execution_context.{name}" for name in validate_execution_context({
            "type": payload.get("type"),
            "os": payload.get("os"),
            "privilege": execution_context.get("privilege"),
            "isolation": execution_context.get("isolation"),
        }))
    return errors


def validate_discovery_candidate(payload: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    required = ("schema_version", "candidate_id", "discovery_source", "discovery_reason", "feature_area", "initial_confidence", "seed_reference", "required_followup")
    for field in required:
        if not payload.get(field):
            errors.append(field)
    if payload.get("schema_version") not in SUPPORTED_SCHEMA_VERSIONS:
        errors.append("schema_version")
    if not any(payload.get(field) for field in ("key_path", "value_name", "registry_clue")):
        errors.append("key_path|value_name|registry_clue")
    return errors


def serialize_discovery_event(candidate: dict[str, Any]) -> dict[str, Any]:
    payload = dict(candidate)
    payload.setdefault("recorded_utc", now_utc())
    return payload


def existing_discovery_candidate_ids(path: Path = DISCOVERY_EVENTS_PATH) -> set[str]:
    if not path.exists():
        return set()
    ids: set[str] = set()
    for line in path.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        try:
            payload = json.loads(line)
        except json.JSONDecodeError:
            continue
        candidate_id = str(payload.get("candidate_id") or "").strip()
        if candidate_id:
            ids.add(candidate_id)
    return ids


def append_discovery_candidates(candidates: list[dict[str, Any]], path: Path = DISCOVERY_EVENTS_PATH) -> int:
    seen = existing_discovery_candidate_ids(path)
    appended = 0
    for candidate in candidates:
        candidate_id = str(candidate.get("candidate_id") or "").strip()
        if not candidate_id or candidate_id in seen:
            continue
        append_jsonl(path, serialize_discovery_event(candidate))
        seen.add(candidate_id)
        appended += 1
    return appended


def validate_queue_entry(payload: dict[str, Any]) -> list[str]:
    errors = validate_discovery_candidate(payload)
    state = str(payload.get("state") or "")
    if state not in QUEUE_STATES:
        errors.append("state")
    if not isinstance(payload.get("required_capabilities") or [], list):
        errors.append("required_capabilities")
    if not isinstance(payload.get("blockers") or [], list):
        errors.append("blockers")
    return sorted(set(errors))


def validate_gate_result(payload: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    for field in ("schema_version", "evaluator_version", "candidate_id", "promotion_state"):
        if not payload.get(field):
            errors.append(field)
    if payload.get("schema_version") not in SUPPORTED_SCHEMA_VERSIONS:
        errors.append("schema_version")
    if str(payload.get("promotion_state") or "") not in PROMOTION_STATES:
        errors.append("promotion_state")
    if not isinstance(payload.get("promotion_blockers") or [], list):
        errors.append("promotion_blockers")
    return errors


def validate_canonical_bundle(payload: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    required = (
        "schema_version",
        "candidate_id",
        "feature_area",
        "discovery_source",
        "discovery_reason",
        "execution_context",
        "promotion_state",
        "promotion_blockers",
        "record_promotion_allowed",
        "tweak_ingest_allowed",
        "evidence_freshness",
        "last_verified_at",
        "verification_environment",
        "negative_evidence",
        "before_after",
        "bench_results",
    )
    for field in required:
        if field not in payload:
            errors.append(field)
    execution_context = payload.get("execution_context")
    if not isinstance(execution_context, dict):
        errors.append("execution_context")
    else:
        errors.extend(f"execution_context.{name}" for name in validate_execution_context(execution_context))
    return errors


def primary_target(record: dict[str, Any]) -> dict[str, Any]:
    setting = record.get("setting") or {}
    targets = setting.get("targets") or []
    if isinstance(targets, list) and targets:
        target = targets[0]
        if isinstance(target, dict):
            return target

    for collection_name in ("windows_defaults", "recommended_profiles"):
        collection = record.get(collection_name) or []
        if not isinstance(collection, list):
            continue
        for item in collection:
            if not isinstance(item, dict):
                continue
            for state in item.get("states") or []:
                if isinstance(state, dict) and any(state.get(key) for key in ("value_name", "path", "target_id")):
                    return state
    return {}


def record_feature_area(record: dict[str, Any]) -> str:
    setting = record.get("setting") or {}
    return str(setting.get("area") or setting.get("category") or record.get("record_id") or record.get("tweak_id") or "unknown")


def derive_tweak_origin(record: dict[str, Any]) -> str:
    app_status = extract_app_status(record)
    return "legacy-curated" if app_status == "matches-research" else "research-derived"


def discovery_seed_from_record(record: dict[str, Any]) -> dict[str, Any]:
    target = primary_target(record)
    validation_proof = record.get("validation_proof") or {}
    feature_area = record_feature_area(record)
    record_id = str(record.get("record_id") or record.get("tweak_id") or "")
    return {
        "schema_version": CURRENT_SCHEMA_VERSION,
        "candidate_id": record_id,
        "record_id": record_id,
        "discovery_source": "existing-record",
        "discovery_reason": "Compat seed imported from the current research record corpus.",
        "feature_area": feature_area,
        "key_path": target.get("path"),
        "value_name": target.get("value_name"),
        "value_type": target.get("value_type"),
        "registry_clue": validation_proof.get("exact_quote_or_path"),
        "initial_confidence": str((record.get("decision") or {}).get("confidence") or "unknown"),
        "seed_reference": f"research/records/{Path(record_id).name}.json",
        "required_followup": next_missing_layer(record),
        "execution_context": default_execution_context(),
    }


def _heuristic_gap(candidate_id: str, gap_type: str, key_path: str, seed_reference: str, feature_area: str) -> dict[str, Any]:
    return {
        "schema_version": CURRENT_SCHEMA_VERSION,
        "candidate_id": candidate_id,
        "discovery_source": "ai-gap-analysis",
        "discovery_reason": gap_type,
        "feature_area": feature_area,
        "key_path": key_path,
        "value_name": None,
        "value_type": None,
        "registry_clue": gap_type,
        "initial_confidence": "low",
        "seed_reference": seed_reference,
        "required_followup": "triage",
        "execution_context": default_execution_context(),
    }


def gap_analysis_candidates(records: list[dict[str, Any]]) -> list[dict[str, Any]]:
    discovered_paths: set[str] = set()
    results: list[dict[str, Any]] = []
    emitted_ids: set[str] = set()

    for record in records:
        seed = discovery_seed_from_record(record)
        if seed.get("key_path"):
            discovered_paths.add(str(seed["key_path"]))

    for record in records:
        seed = discovery_seed_from_record(record)
        key_path = str(seed.get("key_path") or "")
        if not key_path:
            continue
        feature_area = seed["feature_area"]
        seed_reference = seed["seed_reference"]

        if key_path.startswith("HKLM\\"):
            hkcu_key = key_path.replace("HKLM\\", "HKCU\\", 1)
            gap_id = f"gap.hkcu-analog::{hkcu_key}"
            if hkcu_key not in discovered_paths and gap_id not in emitted_ids:
                emitted_ids.add(gap_id)
                results.append(_heuristic_gap(gap_id, "missing_hkcu_analog", hkcu_key, seed_reference, feature_area))

        if "CurrentVersion" in key_path:
            policy_key = key_path.replace("CurrentVersion", "Policies")
            gap_id = f"gap.policy-analog::{policy_key}"
            if policy_key not in discovered_paths and gap_id not in emitted_ids:
                emitted_ids.add(gap_id)
                results.append(_heuristic_gap(gap_id, "missing_policy_analog", policy_key, seed_reference, feature_area))

        parent = "\\".join(part for part in key_path.split("\\")[:-1] if part)
        if parent:
            sibling_key = f"{parent}\\Policies"
            gap_id = f"gap.sibling::{sibling_key}"
            if sibling_key not in discovered_paths and gap_id not in emitted_ids and not sibling_key.endswith("\\Policies\\Policies"):
                emitted_ids.add(gap_id)
                results.append(_heuristic_gap(gap_id, "missing_sibling_branch", sibling_key, seed_reference, feature_area))

    return results


def triage_candidate(candidate: dict[str, Any]) -> tuple[bool, list[str]]:
    reasons: list[str] = []
    for field in ("discovery_source", "feature_area", "required_followup"):
        if not str(candidate.get(field) or "").strip():
            reasons.append(f"missing:{field}")
    if not any(str(candidate.get(field) or "").strip() for field in ("key_path", "value_name", "registry_clue")):
        reasons.append("missing:registry-clue")
    key_path = str(candidate.get("key_path") or "").strip()
    if key_path and not (
        key_path.startswith("HK")
        or key_path.startswith("\\REGISTRY\\")
        or key_path.startswith("HKEY_")
    ):
        reasons.append("invalid:key_path")
    return (len(reasons) == 0), reasons


def summarize_gap_analysis(entries: list[dict[str, Any]]) -> dict[str, Any]:
    gap_entries = [entry for entry in entries if str(entry.get("discovery_source") or "") == "ai-gap-analysis"]
    discard_reasons: Counter[str] = Counter()
    gap_types: Counter[str] = Counter()
    triaged = 0
    discarded = 0
    for entry in gap_entries:
        gap_types[str(entry.get("discovery_reason") or "unknown")] += 1
        state = str(entry.get("state") or "")
        if state == "discarded":
            discarded += 1
            for reason in entry.get("discard_reason") or []:
                discard_reasons[str(reason)] += 1
        elif state == "triaged":
            triaged += 1
    return {
        "generated_utc": now_utc(),
        "total_generated": len(gap_entries),
        "triaged": triaged,
        "discarded": discarded,
        "top_discard_reasons": [
            {"reason": reason, "count": count}
            for reason, count in discard_reasons.most_common(10)
        ],
        "top_gap_types": [
            {"gap_type": gap_type, "count": count}
            for gap_type, count in gap_types.most_common(10)
        ],
    }


def capability_status(required_capabilities: list[str], backend_manifest: dict[str, Any]) -> tuple[list[str], list[str]]:
    capabilities = backend_manifest.get("capabilities") or {}
    available: list[str] = []
    missing: list[str] = []
    for capability in required_capabilities:
        if bool(capabilities.get(capability)):
            available.append(capability)
        else:
            missing.append(capability)
    return available, missing


def required_capabilities_for_runner_entry(lane_name: str, tweak_id: str, runner_config: dict[str, Any]) -> list[str]:
    lane = runner_config.get(lane_name) or {}
    entry = lane.get(tweak_id) or {}
    explicit = entry.get("required_capabilities")
    if isinstance(explicit, list) and explicit:
        return [str(item) for item in explicit if item]
    return list(DEFAULT_REQUIRED_CAPABILITIES_BY_LANE.get(lane_name, []))


def score_candidate(record: dict[str, Any], audit: dict[str, Any] | None = None) -> dict[str, Any]:
    audit = audit or {}
    decision = record.get("decision") or {}
    tweak_id = str(record.get("tweak_id") or record.get("record_id") or "")
    target = primary_target(record)
    key_path = str(target.get("path") or "")
    prefix = tweak_id.split(".", 1)[0].lower() if tweak_id else "unknown"
    app_status = extract_app_status(record)
    apply_allowed = bool_value(decision.get("apply_allowed"))
    freshness = evidence_freshness(record, reproducibility_manifest())
    tested_build = str(freshness.get("os_build") or "").strip()
    current_build = str((reproducibility_manifest() or {}).get("os_build") or "").strip()
    stale_build = bool(tested_build and current_build and tested_build != current_build)

    static_evidence_strength = 1
    if has_official_evidence(record) and has_ghidra_evidence(record):
        static_evidence_strength = 5
    elif has_official_evidence(record) or has_ghidra_evidence(record):
        static_evidence_strength = 4
    elif record.get("evidence"):
        static_evidence_strength = 2

    runtime_hits = sum(
        1
        for present in (
            has_procmon_evidence(record),
            has_reboot_evidence(record),
            has_wpr_evidence(record),
            has_benchmark_evidence(record),
        )
        if present
    )
    runtime_evidence_strength = min(5, max(1, runtime_hits + 1))

    rollback_clarity = 5 if restore_story_known(record) else 2

    blast_radius = {
        "cleanup": 5,
        "visibility": 4,
        "audio": 4,
        "explorer": 4,
        "misc": 4,
        "privacy": 3,
        "network": 3,
        "notifications": 3,
        "security": 2,
        "system": 2,
        "power": 2,
        "performance": 2,
    }.get(prefix, 3)

    if apply_allowed and app_status == "matches-research":
        tweak_suitability = 5
    elif apply_allowed:
        tweak_suitability = 4
    elif app_status == "matches-research":
        tweak_suitability = 3
    else:
        tweak_suitability = 2

    privilege_complexity = 4 if key_path.startswith("HKCU\\") else 3 if key_path.startswith("HKLM\\") else 2
    build_specificity = 2 if stale_build else 5 if tested_build else 3
    sibling_expansion_value = 4 if ("CurrentVersion" in key_path or key_path.startswith("HKLM\\")) else 2
    bench_priority = 5 if prefix in {"power", "performance", "system", "network"} else 3

    components = {
        "static_evidence_strength": static_evidence_strength,
        "runtime_evidence_strength": runtime_evidence_strength,
        "rollback_clarity": rollback_clarity,
        "blast_radius": blast_radius,
        "tweak_suitability": tweak_suitability,
        "privilege_complexity": privilege_complexity,
        "build_specificity": build_specificity,
        "sibling_expansion_value": sibling_expansion_value,
        "bench_priority": bench_priority,
    }
    overall_score = round(sum(components.values()) / len(components), 2)

    return {
        **components,
        "overall_score": overall_score,
        "next_missing_layer": str(audit.get("next_missing_layer") or next_missing_layer(record) or "none"),
    }


def derive_promotion_state(record: dict[str, Any], audit: dict[str, Any] | None = None) -> dict[str, Any]:
    audit = audit or {}
    decision = record.get("decision") or {}
    app_status = extract_app_status(record)
    tweak_origin = derive_tweak_origin(record)
    missing_layer = str(audit.get("next_missing_layer") or next_missing_layer(record))
    blockers = [str(item) for item in (decision.get("blocking_issues") or []) if item]
    compatibility_mode = "native" if CURRENT_SCHEMA_VERSION in SUPPORTED_SCHEMA_VERSIONS else "unsupported"
    record_status = str(record.get("record_status") or "").strip().lower()
    freshness = evidence_freshness(record, reproducibility_manifest())
    tested_build = str(freshness.get("os_build") or "").strip()
    current_build = str((reproducibility_manifest() or {}).get("os_build") or "").strip()
    stale_build = bool(tested_build and current_build and tested_build != current_build)

    if record_status == "deprecated":
        state = "rejected"
        blockers = blockers or ["deprecated-record"]
    elif CURRENT_SCHEMA_VERSION not in SUPPORTED_SCHEMA_VERSIONS:
        state = "blocked"
        blockers = blockers or ["schema-version-unsupported"]
    elif blockers:
        state = "blocked"
    elif missing_layer in {"archived"}:
        state = "rejected"
        blockers = ["archived"]
    elif missing_layer not in {"none", ""}:
        state = "blocked"
        blockers = blockers or [missing_layer]
    elif stale_build:
        state = "revalidation-pending"
        blockers = ["stale-evidence"]
    elif bool_value(decision.get("apply_allowed")) and app_status == "matches-research" and tweak_origin == "legacy-curated":
        state = "promoted"
    else:
        state = "promotion-eligible"

    return {
        "schema_version": CURRENT_SCHEMA_VERSION,
        "evaluator_version": EVALUATOR_VERSION,
        "supported_schema_versions": sorted(SUPPORTED_SCHEMA_VERSIONS),
        "schema_compatibility_mode": compatibility_mode,
        "candidate_id": str(record.get("record_id") or record.get("tweak_id") or ""),
        "record_id": str(record.get("record_id") or record.get("tweak_id") or ""),
        "tweak_id": str(record.get("tweak_id") or record.get("record_id") or ""),
        "tweak_origin": tweak_origin,
        "promotion_state": state,
        "promotion_blockers": blockers,
        "record_promotion_allowed": state in {"promotion-eligible", "promoted", "revalidation-pending"},
        "tweak_ingest_allowed": state == "promoted",
        "apply_allowed": bool_value(decision.get("apply_allowed")),
        "app_mapping_status": app_status,
        "next_missing_layer": missing_layer or "none",
        "debug_override_allowed": tweak_origin == "research-derived",
        "last_evaluated_at": now_utc(),
    }


def before_after_projection(full_evidence: dict[str, Any]) -> dict[str, Any]:
    sideeffects = ((full_evidence.get("behavior") or {}).get("registry_sideeffects") or {}) if isinstance(full_evidence, dict) else {}
    counts = sideeffects.get("summary_counts") or {}
    return {
        "format": sideeffects.get("format"),
        "diff_file": sideeffects.get("diff_file"),
        "key_added": counts.get("added_keys", 0),
        "key_deleted": counts.get("removed_keys", 0),
        "value_added": counts.get("added_values", 0),
        "value_deleted": counts.get("removed_values", 0),
        "value_changed": counts.get("modified_values", 0),
        "unchanged_values": counts.get("unchanged_values", 0),
    }


def bench_results_projection(full_evidence: dict[str, Any]) -> dict[str, Any]:
    benchmark = ((full_evidence.get("behavior") or {}).get("benchmark") or {}) if isinstance(full_evidence, dict) else {}
    reproducibility = full_evidence.get("reproducibility") or {}
    return {
        "bench_tier": "vm" if reproducibility.get("vm_name") else "unknown",
        "bench_required": bool(benchmark.get("executed")),
        "bench_vm_capable": True,
        "bench_bare_metal_required": False,
        "executed": bool(benchmark.get("executed")),
        "summary": benchmark.get("summary"),
        "statistics": benchmark.get("statistics"),
        "significance_verdict": benchmark.get("significance_verdict"),
    }


def verification_environment_projection(record: dict[str, Any], full_evidence: dict[str, Any], execution_context: dict[str, Any]) -> dict[str, Any]:
    freshness = evidence_freshness(record, reproducibility_manifest())
    reproducibility = full_evidence.get("reproducibility") or {}
    return {
        "os_build": freshness.get("os_build"),
        "vm_name": reproducibility.get("vm_name"),
        "baseline_snapshot": reproducibility.get("baseline_snapshot"),
        "backend_id": execution_context.get("backend_id"),
        "execution_context": execution_context,
    }


def discovery_projection(record: dict[str, Any]) -> tuple[str, str]:
    proof = record.get("validation_proof") or {}
    if proof.get("source_url"):
        return "existing-record", "Compat import from validated research records with preserved validation proof."
    if record.get("evidence"):
        return "existing-record", "Compat import from existing record evidence without a dedicated validation proof URL."
    return "existing-record", "Compat import from an authoring record with incomplete provenance."


def candidate_identity(record: dict[str, Any]) -> tuple[str, str]:
    record_id = str(record.get("record_id") or record.get("tweak_id") or "")
    tweak_id = str(record.get("tweak_id") or record_id)
    return record_id, tweak_id


def canonical_bundle_projection(record: dict[str, Any], audit: dict[str, Any], full_evidence: dict[str, Any], backend_id: str = DEFAULT_BACKEND_ID) -> dict[str, Any]:
    record_id, tweak_id = candidate_identity(record)
    target = primary_target(record)
    gate = derive_promotion_state(record, audit)
    execution_context = default_execution_context(backend_id)
    discovery_source, discovery_reason = discovery_projection(record)
    freshness = evidence_freshness(record, reproducibility_manifest())

    return {
        "schema_version": CURRENT_SCHEMA_VERSION,
        "candidate_id": record_id,
        "feature_area": record_feature_area(record),
        "discovery_source": discovery_source,
        "discovery_reason": discovery_reason,
        "key_path": target.get("path"),
        "value_name": target.get("value_name"),
        "value_type": target.get("value_type"),
        "execution_context": execution_context,
        "promotion_state": gate["promotion_state"],
        "promotion_blockers": gate["promotion_blockers"],
        "record_promotion_allowed": gate["record_promotion_allowed"],
        "tweak_ingest_allowed": gate["tweak_ingest_allowed"],
        "tweak_origin": gate["tweak_origin"],
        "gate_result": gate,
        "evidence_freshness": freshness,
        "last_verified_at": record.get("last_reviewed_utc"),
        "verification_environment": verification_environment_projection(record, full_evidence, execution_context),
        "negative_evidence": full_evidence.get("negative_evidence") or {},
        "before_after": before_after_projection(full_evidence),
        "bench_results": bench_results_projection(full_evidence),
        "score_breakdown": score_candidate(record, audit),
        "verification_context": {
            "record_id": record_id,
            "tweak_id": tweak_id,
            "supported_schema_versions": gate["supported_schema_versions"],
            "schema_compatibility_mode": gate["schema_compatibility_mode"],
            "evaluator_version": gate["evaluator_version"],
        },
    }


def build_queue_entry(candidate: dict[str, Any], state: str, blockers: list[str] | None = None, required_capabilities: list[str] | None = None, next_lane: str | None = None, linked_record_id: str | None = None, gate_result: dict[str, Any] | None = None) -> dict[str, Any]:
    return {
        **candidate,
        "state": state,
        "blockers": blockers or [],
        "required_capabilities": required_capabilities or [],
        "next_lane": next_lane or candidate.get("required_followup") or "triage",
        "linked_record_id": linked_record_id,
        "last_evaluator_result": gate_result,
        "updated_utc": now_utc(),
    }


def summarize_queue(entries: list[dict[str, Any]]) -> dict[str, Any]:
    state_counts = Counter(str(entry.get("state") or "unknown") for entry in entries)
    discovery_counts = Counter(str(entry.get("discovery_source") or "unknown") for entry in entries)
    return {
        "total_entries": len(entries),
        "state_counts": dict(state_counts),
        "discovery_source_counts": dict(discovery_counts),
    }


def discover_etl_files() -> list[str]:
    results: list[str] = []
    for path in (REPO_ROOT / "evidence").rglob("*.etl"):
        if path.is_file():
            results.append(normalize_repo_relative_path(str(path.relative_to(REPO_ROOT))))
    return sorted(set(results))


def discover_etl_artifacts() -> list[dict[str, Any]]:
    artifacts: list[dict[str, Any]] = []
    for path in (REPO_ROOT / "evidence").rglob("*"):
        if not path.is_file():
            continue
        lower_name = path.name.lower()
        if ".etl" not in lower_name:
            continue
        repo_ref = normalize_repo_relative_path(str(path.relative_to(REPO_ROOT)))
        is_placeholder = lower_name.endswith(".etl.md")
        artifacts.append(
            {
                "path": repo_ref,
                "size": path.stat().st_size,
                "is_placeholder": is_placeholder,
                "estimated_source": infer_etl_source(repo_ref),
                "actual_etl_path": None if is_placeholder else repo_ref,
            }
        )
    artifacts.sort(key=lambda item: str(item["path"]))
    return artifacts


def infer_etl_source(repo_ref: str) -> str:
    lowered = repo_ref.lower()
    if "mega-trigger" in lowered:
        return "mega-trigger"
    if "lightweight-runtime" in lowered:
        return "lightweight-runtime"
    if "boottrace" in lowered or "watchdog-timeouts-boot" in lowered:
        return "boot-trace"
    if "trigger-etw" in lowered:
        return "trigger-etw"
    if "runtime" in lowered:
        return "runtime-probe"
    if "host-temp" in lowered or "terminal-launch" in lowered:
        return "manual-trace"
    return "unknown"


def build_etl_corpus_inventory(
    artifacts: list[dict[str, Any]],
    parse_results: list[dict[str, Any]] | None = None,
    parser_name: str | None = None,
    provider_guid: str | None = None,
) -> dict[str, Any]:
    parse_results = parse_results or []
    parse_map = {
        str(item.get("etl_path") or ""): item
        for item in parse_results
        if isinstance(item, dict) and item.get("etl_path")
    }
    entries: list[dict[str, Any]] = []
    source_counts: Counter[str] = Counter()
    parsed_count = 0
    physical_count = 0
    placeholder_count = 0

    for artifact in artifacts:
        repo_ref = str(artifact.get("path") or "")
        actual_etl_path = artifact.get("actual_etl_path")
        estimated_source = str(artifact.get("estimated_source") or "unknown")
        source_counts[estimated_source] += 1
        parse_result = parse_map.get(str(actual_etl_path or ""))
        is_placeholder = bool(artifact.get("is_placeholder"))
        if is_placeholder:
            placeholder_count += 1
        if actual_etl_path:
            physical_count += 1

        if parse_result:
            parse_status = str(parse_result.get("status") or "not-parsed")
            parse_reason = next(
                (str(note).strip() for note in (parse_result.get("notes") or []) if str(note).strip()),
                None,
            )
            parsed = parse_status == "parsed"
            xml_output = parse_result.get("xml_output")
            normalized_touch_count = int(parse_result.get("normalized_touch_count") or 0)
        elif is_placeholder:
            parse_status = "not-parsed"
            parse_reason = "placeholder-markdown-only"
            parsed = False
            xml_output = None
            normalized_touch_count = 0
        elif actual_etl_path:
            parse_status = "not-parsed"
            parse_reason = "not-attempted"
            parsed = False
            xml_output = None
            normalized_touch_count = 0
        else:
            parse_status = "not-parsed"
            parse_reason = "missing-raw-etl"
            parsed = False
            xml_output = None
            normalized_touch_count = 0

        if parsed:
            parsed_count += 1

        entries.append(
            {
                "path": repo_ref,
                "size_bytes": int(artifact.get("size") or 0),
                "estimated_source": estimated_source,
                "is_placeholder": is_placeholder,
                "actual_etl_path": actual_etl_path,
                "parsed": parsed,
                "parse_status": parse_status,
                "parse_reason": parse_reason,
                "xml_output": xml_output,
                "normalized_touch_count": normalized_touch_count,
            }
        )

    return {
        "schema_version": CURRENT_SCHEMA_VERSION,
        "generated_utc": now_utc(),
        "parser": parser_name,
        "provider_guid": provider_guid,
        "summary": {
            "total_artifacts": len(entries),
            "physical_etl_count": physical_count,
            "placeholder_only_count": placeholder_count,
            "parsed_count": parsed_count,
            "source_counts": dict(source_counts),
        },
        "entries": entries,
    }


def _normalize_registry_path(text: str | None) -> str | None:
    if not text:
        return None
    normalized = str(text).strip().replace("/", "\\")
    normalized = re.sub(r"\s+", " ", normalized)
    if not normalized:
        return None
    replacements = {
        "HKEY_LOCAL_MACHINE\\": "HKLM\\",
        "HKEY_CURRENT_USER\\": "HKCU\\",
        "HKEY_CLASSES_ROOT\\": "HKCR\\",
        "HKEY_USERS\\": "HKU\\",
        "\\REGISTRY\\MACHINE\\": "HKLM\\",
        "\\REGISTRY\\USER\\": "HKU\\",
    }
    upper = normalized.upper()
    for source, target in replacements.items():
        if upper.startswith(source.upper()):
            normalized = target + normalized[len(source):]
            break
    return normalized if normalized.startswith(("HKLM\\", "HKCU\\", "HKCR\\", "HKU\\")) else None


def _guess_registry_operation(text_blob: str) -> str | None:
    operation_map = {
        "setvalue": "RegSetValue",
        "queryvalue": "RegQueryValue",
        "openkey": "RegOpenKey",
        "createkey": "RegCreateKey",
        "deletevalue": "RegDeleteValue",
        "deletekey": "RegDeleteKey",
        "closekey": "RegCloseKey",
    }
    lowered = text_blob.lower()
    for needle, operation in operation_map.items():
        if needle in lowered:
            return operation
    return None


def _feature_area_from_key_path(key_path: str | None, fallback_source: str) -> str:
    path = str(key_path or "").lower()
    if "\\control\\power" in path:
        return "Power"
    if "\\policies\\system" in path or "\\currentversion\\policies\\system" in path:
        return "System"
    if "\\explorer" in path:
        return "Explorer"
    if "\\windows defender" in path or "\\defender" in path:
        return "Security"
    if "\\audio" in path or "\\multimedia" in path:
        return "Audio"
    return fallback_source.replace("-", " ").title()


def extract_registry_touches_from_tracerpt_xml(xml_path: Path, provider_guid: str | None = None) -> list[dict[str, Any]]:
    if not xml_path.exists():
        return []

    provider_guid = str(provider_guid or "").strip("{}").lower()
    try:
        tree = ET.parse(xml_path)
    except ET.ParseError:
        return []

    touches: list[dict[str, Any]] = []
    seen: set[tuple[str, str, str, str]] = set()

    for event in tree.iter():
        if not str(event.tag).lower().endswith("event"):
            continue

        provider = ""
        event_id = ""
        process_name = ""
        process_id = ""
        key_path = None
        value_name = None
        text_parts: list[str] = []

        for child in event.iter():
            tag = str(child.tag).lower()
            text_value = (child.text or "").strip()
            if text_value:
                text_parts.append(text_value)
            if tag.endswith("provider"):
                provider = (
                    child.attrib.get("Guid")
                    or child.attrib.get("GUID")
                    or child.attrib.get("Name")
                    or provider
                )
            elif tag.endswith("eventid"):
                event_id = text_value or event_id
            elif tag.endswith("execution"):
                process_id = child.attrib.get("ProcessID") or process_id
            elif tag.endswith("data"):
                name = str(child.attrib.get("Name") or "").lower()
                if name in {"keyname", "pathname", "path", "keypath"}:
                    key_path = _normalize_registry_path(text_value) or key_path
                elif name in {"valuename", "value", "name"} and text_value and "\\" not in text_value:
                    value_name = text_value
                elif name in {"processname", "image"}:
                    process_name = text_value or process_name

        text_blob = " ".join(part for part in text_parts if part)
        if not key_path:
            match = re.search(
                r"(HKLM|HKCU|HKCR|HKU|HKEY_LOCAL_MACHINE|HKEY_CURRENT_USER|HKEY_CLASSES_ROOT|HKEY_USERS|\\\\REGISTRY\\\\MACHINE|\\\\REGISTRY\\\\USER)\\\\[^\r\n\"<>]+",
                text_blob,
                flags=re.IGNORECASE,
            )
            if match:
                key_path = _normalize_registry_path(match.group(0))

        provider_match = False
        if provider_guid:
            provider_match = provider_guid in provider.lower().strip("{}")
        operation = _guess_registry_operation(text_blob)
        if not key_path and not provider_match:
            continue

        touch = {
            "event_id": event_id or None,
            "provider": provider or None,
            "provider_guid_matched": provider_match,
            "process_name": process_name or None,
            "process_id": process_id or None,
            "operation": operation or "registry-touch",
            "key_path": key_path,
            "value_name": value_name,
            "raw_excerpt": text_blob[:400],
        }
        dedupe_key = (
            str(touch.get("operation") or ""),
            str(touch.get("key_path") or ""),
            str(touch.get("value_name") or ""),
            str(touch.get("process_name") or ""),
        )
        if dedupe_key in seen:
            continue
        seen.add(dedupe_key)
        touches.append(touch)

    return touches


def etl_touch_candidates(parse_result: dict[str, Any], backend_id: str = DEFAULT_BACKEND_ID) -> list[dict[str, Any]]:
    etl_path = str(parse_result.get("etl_path") or "")
    feature_source = infer_etl_source(etl_path)
    candidates: list[dict[str, Any]] = []
    for touch in parse_result.get("registry_touches") or []:
        key_path = touch.get("key_path")
        value_name = touch.get("value_name")
        operation = touch.get("operation") or "registry-touch"
        digest = hashlib.sha1(f"{etl_path}|{operation}|{key_path}|{value_name}".encode("utf-8")).hexdigest()[:16]
        candidates.append(
            {
                "schema_version": CURRENT_SCHEMA_VERSION,
                "candidate_id": f"etl::{digest}",
                "discovery_source": "etl-registry-touch",
                "discovery_reason": "registry_touch_extracted",
                "feature_area": _feature_area_from_key_path(key_path, feature_source),
                "key_path": key_path,
                "value_name": value_name,
                "value_type": None,
                "registry_clue": f"{operation} via {touch.get('process_name') or 'unknown-process'}",
                "initial_confidence": "medium" if touch.get("provider_guid_matched") else "low",
                "seed_reference": etl_path,
                "required_followup": "triage",
                "execution_context": default_execution_context(backend_id),
            }
        )
    return candidates


def parse_etl_registry_touches(etl_path: Path, parser: str = "tracerpt", provider_guid: str = "{AE53722E-C863-11D2-8659-00C04FA321A1}") -> dict[str, Any]:
    output = {
        "schema_version": CURRENT_SCHEMA_VERSION,
        "etl_path": normalize_repo_relative_path(str(etl_path.relative_to(REPO_ROOT))),
        "parser": parser,
        "provider_guid": provider_guid,
        "status": "not-run",
        "registry_touches": [],
        "notes": [],
    }
    if parser != "tracerpt":
        output["status"] = "unsupported-parser"
        output["notes"].append(f"Unsupported parser: {parser}")
        return output

    if not etl_path.exists():
        output["status"] = "missing-input"
        output["notes"].append("ETL file not found.")
        return output

    if etl_path.suffix.lower() != ".etl":
        output["status"] = "skipped-non-etl"
        output["notes"].append("Only physical .etl files can be parsed.")
        return output

    config = load_etl_parser_config()
    parser_commands = config.get("parser_commands") or {}
    tracerpt = str(parser_commands.get("tracerpt") or "tracerpt")
    xml_output = etl_path.with_suffix(".etl.xml")
    command = [tracerpt, str(etl_path), "-o", str(xml_output), "-of", "XML"]
    try:
        completed = subprocess.run(command, capture_output=True, text=True, check=False)
    except FileNotFoundError:
        output["status"] = "parser-unavailable"
        output["notes"].append("tracerpt.exe is not available in this environment.")
        return output

    output["notes"].append((completed.stdout or completed.stderr or "").strip())
    if completed.returncode != 0:
        output["status"] = "parser-failed"
        return output

    output["status"] = "parsed"
    output["xml_output"] = normalize_repo_relative_path(str(xml_output.relative_to(REPO_ROOT)))
    output["registry_touches"] = extract_registry_touches_from_tracerpt_xml(xml_output, provider_guid=provider_guid)
    output["normalized_touch_count"] = len(output["registry_touches"])
    return output
