from __future__ import annotations

import hashlib
import json
import re
import subprocess
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
from collections import Counter
from datetime import datetime, timedelta, timezone
from functools import lru_cache
from pathlib import Path
from typing import Any

from evidence_class_lib import (
    bool_value,
    evidence_items,
    evidence_kind,
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
ENRICHMENT_ROOT = FRAMEWORK_ROOT / "enrichment"
REGRESSION_PACKS_ROOT = FRAMEWORK_ROOT / "regression-packs"
PROMOTION_GATES_PATH = RESEARCH_ROOT / "promotion-gates.json"
PROMOTION_AUDIT_LOG_PATH = AUDIT_ROOT / "promotion-audit-log.jsonl"
DISCOVERY_EVENTS_PATH = DISCOVERY_ROOT / "discovery-events.jsonl"
ETL_CORPUS_INVENTORY_PATH = DISCOVERY_ROOT / "etl-corpus-inventory.json"
ETL_REGISTRY_DISCOVERY_PATH = DISCOVERY_ROOT / "etl-registry-discovery.json"
GAP_ANALYSIS_SUMMARY_PATH = AUDIT_ROOT / "gap-analysis-summary.json"
ETL_PARSER_CONFIG_PATH = FRAMEWORK_ROOT / "config" / "etl-parser-config.json"
LEGACY_ETL_PARSER_CONFIG_PATH = FRAMEWORK_ROOT / "config" / "etl-parser.json"
ETL_FEATURE_AREA_MAP_PATH = FRAMEWORK_ROOT / "config" / "etl-feature-area-map.json"
ETL_TRIAGE_RULES_PATH = FRAMEWORK_ROOT / "config" / "etl-triage-rules.json"
ENRICHMENT_CACHE_PATH = ENRICHMENT_ROOT / "enrichment-cache.jsonl"
BENCH_PROFILE_MAP_PATH = FRAMEWORK_ROOT / "config" / "bench-profile-map.json"
URL_VALIDATION_REPORT_PATH = AUDIT_ROOT / "url-validation-report.json"
MUTATION_AUDIT_LOG_PATH = AUDIT_ROOT / "mutation-override-audit-log.jsonl"

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
STALE_REVALIDATION_DAYS = 28
STALE_BUILD_THRESHOLD = 2
STRONG_STATIC_SUPPORTS = {
    "allowed-values",
    "behavior",
    "runtime-read",
    "value semantics",
    "kernel-derivation",
}
WEAK_STATIC_SUPPORTS = {
    "string-reference",
    "binary-scan",
    "open-question",
}
STRONG_STATIC_KINDS = {
    "official-doc",
    "policy-csp",
    "troubleshoot-doc",
    "decompilation",
    "decompiled-pseudocode",
    "pdb-symbolized",
}
WEAK_STATIC_KINDS = {
    "repo-code",
    "json-definition",
    "source-code",
}
SOURCE_ENRICHMENT_KINDS = {
    "official-doc",
    "policy-csp",
    "troubleshoot-doc",
    "repo-doc",
    "decompilation",
    "decompiled-pseudocode",
    "ghidra-headless",
    "ghidra-trace",
    "open-source-reference",
    "inference",
}
ENRICHMENT_CLUE_TYPES = {
    "caller_chain",
    "api_semantics",
    "default_behavior_hint",
    "privilege_hint",
    "sibling_path_hint",
    "legacy_behavior_hint",
}
ROLLBACK_BLOCKERS = {"rollback-unverified", "rollback-failed"}
MCP_METHOD_NAMES = (
    "get_candidate_by_key_path",
    "list_blocked_candidates",
    "score_candidate_by_id",
    "evaluate_candidate_by_id",
    "get_evidence_bundle",
    "list_revalidation_pending_candidates",
    "apply_candidate",
    "rollback_candidate",
)

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

ETL_SCORE_AREA_WEIGHTS = {
    "Security": 0.9,
    "Networking": 0.8,
    "Policy": 0.8,
    "System": 0.7,
    "Power": 0.7,
    "Services": 0.6,
    "Explorer": 0.5,
    "UserProfile": 0.4,
    "Platform": 0.3,
    "Unknown": 0.1,
}

ETL_SCORE_COMPONENT_WEIGHTS = {
    "static_evidence_strength": 0.15,
    "runtime_evidence_strength": 0.35,
    "rollback_clarity": 0.20,
    "tweak_suitability": 0.20,
    "bench_priority": 0.10,
}


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def parse_utc(value: str | None) -> datetime | None:
    text = str(value or "").strip()
    if not text:
        return None
    normalized = text.replace("Z", "+00:00")
    try:
        parsed = datetime.fromisoformat(normalized)
    except ValueError:
        return None
    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=timezone.utc)
    return parsed.astimezone(timezone.utc)


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


def load_jsonl(path: Path) -> list[dict[str, Any]]:
    if not path.exists():
        return []
    entries: list[dict[str, Any]] = []
    for line in path.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        payload = json.loads(line)
        if isinstance(payload, dict):
            entries.append(payload)
    return entries


def write_jsonl(path: Path, entries: list[dict[str, Any]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        for entry in entries:
            handle.write(json.dumps(entry, ensure_ascii=False))
            handle.write("\n")


def list_records() -> list[Path]:
    return sorted((RESEARCH_ROOT / "records").glob("*.json"))


def load_records() -> list[dict[str, Any]]:
    return [load_json(path) for path in list_records()]


def load_audit_entries(path: Path | None = None) -> dict[str, dict[str, Any]]:
    payload = load_json_if_exists(path or (RESEARCH_ROOT / "evidence-audit.json"))
    if not isinstance(payload, dict):
        return {}
    return {
        str(entry.get("record_id") or entry.get("tweak_id") or ""): entry
        for entry in payload.get("entries") or []
        if isinstance(entry, dict) and str(entry.get("record_id") or entry.get("tweak_id") or "").strip()
    }


def load_promotion_gate_catalog(path: Path | None = None) -> dict[str, Any]:
    payload = load_json_if_exists(path or PROMOTION_GATES_PATH)
    return payload if isinstance(payload, dict) else {"entries": [], "summary": {}}


def load_promotion_gate_map(path: Path | None = None) -> dict[str, dict[str, Any]]:
    payload = load_promotion_gate_catalog(path)
    result: dict[str, dict[str, Any]] = {}
    for entry in payload.get("entries") or []:
        if not isinstance(entry, dict):
            continue
        for key_name in ("candidate_id", "record_id", "tweak_id"):
            key = str(entry.get(key_name) or "").strip()
            if key and key not in result:
                result[key] = entry
    return result


def record_map(records: list[dict[str, Any]] | None = None) -> dict[str, dict[str, Any]]:
    records = records or load_records()
    result: dict[str, dict[str, Any]] = {}
    for record in records:
        for key_name in ("record_id", "tweak_id"):
            key = str(record.get(key_name) or "").strip()
            if key and key not in result:
                result[key] = record
    return result


def load_full_evidence_bundle(candidate_id: str, path_root: Path | None = None) -> dict[str, Any]:
    base = path_root or (REPO_ROOT / "evidence" / "records")
    path = base / candidate_id / "full-evidence.json"
    payload = load_json_if_exists(path)
    return payload if isinstance(payload, dict) else {}


def load_bench_profile_map(path: Path | None = None) -> dict[str, Any]:
    payload = load_json_if_exists(path or BENCH_PROFILE_MAP_PATH)
    return payload if isinstance(payload, dict) else {"schema_version": CURRENT_SCHEMA_VERSION, "entries": {}}


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


@lru_cache(maxsize=1)
def load_etl_feature_area_map() -> dict[str, Any]:
    payload = load_json_if_exists(ETL_FEATURE_AREA_MAP_PATH)
    if not isinstance(payload, dict):
        return {"version": CURRENT_SCHEMA_VERSION, "normalize_case": True, "prefix_map": []}

    normalize_case = bool(payload.get("normalize_case", True))
    raw_prefix_map = payload.get("prefix_map")
    entries: list[dict[str, str]] = []
    if isinstance(raw_prefix_map, list):
        for raw_entry in raw_prefix_map:
            if not isinstance(raw_entry, dict):
                continue
            prefix = str(raw_entry.get("prefix") or "").strip().replace("/", "\\")
            feature_area = str(raw_entry.get("feature_area") or "").strip()
            if not prefix or not feature_area:
                continue
            entries.append(
                {
                    "prefix": prefix.lower() if normalize_case else prefix,
                    "feature_area": feature_area,
                }
            )

    entries.sort(key=lambda item: len(item["prefix"]), reverse=True)
    return {
        "version": str(payload.get("version") or CURRENT_SCHEMA_VERSION),
        "normalize_case": normalize_case,
        "prefix_map": entries,
    }


@lru_cache(maxsize=1)
def load_etl_triage_rules() -> dict[str, Any]:
    payload = load_json_if_exists(ETL_TRIAGE_RULES_PATH)
    if not isinstance(payload, dict):
        return {"version": CURRENT_SCHEMA_VERSION, "discard_rules": []}

    rules: list[dict[str, str]] = []
    for raw_rule in payload.get("discard_rules") or []:
        if not isinstance(raw_rule, dict):
            continue
        rule = str(raw_rule.get("rule") or "").strip()
        description = str(raw_rule.get("description") or "").strip()
        condition = str(raw_rule.get("condition") or "").strip()
        if not rule:
            continue
        rules.append(
            {
                "rule": rule,
                "description": description,
                "condition": condition,
            }
        )

    return {
        "version": str(payload.get("version") or CURRENT_SCHEMA_VERSION),
        "discard_rules": rules,
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


def parse_build_number(value: Any) -> int | None:
    text = str(value or "").strip()
    match = re.search(r"\d+", text)
    if not match:
        return None
    return int(match.group(0))


def builds_since(current_build: Any, tested_build: Any) -> int | None:
    current = parse_build_number(current_build)
    tested = parse_build_number(tested_build)
    if current is None or tested is None:
        return None
    return max(0, current - tested)


def machine_user_scope_for_key_path(key_path: str | None) -> str:
    path = str(key_path or "").upper()
    if path.startswith("HKCU\\") or path.startswith("HKU\\"):
        return "user"
    return "machine"


def build_sku_awareness_for_key_path(
    key_path: str | None,
    execution_context: dict[str, Any] | None = None,
    backend_id: str = DEFAULT_BACKEND_ID,
) -> dict[str, Any]:
    execution_context = execution_context or default_execution_context(backend_id)
    manifest = reproducibility_manifest()
    backend = load_backend_capabilities(backend_id)
    return {
        "os_build": str(manifest.get("os_build") or "") or None,
        "os_edition": str(manifest.get("os_edition") or backend.get("os_edition") or "unknown"),
        "architecture": str(manifest.get("architecture") or backend.get("architecture") or "unknown"),
        "elevation_context": str(execution_context.get("privilege") or "user"),
        "machine_user_scope": machine_user_scope_for_key_path(key_path),
    }


def build_sku_awareness(record: dict[str, Any], execution_context: dict[str, Any], backend_id: str = DEFAULT_BACKEND_ID) -> dict[str, Any]:
    return build_sku_awareness_for_key_path(primary_target(record).get("path"), execution_context, backend_id)


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
        "observed_default",
        "recommended_value",
        "rollback_value",
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
        "source_enrichment",
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
    execution_context = default_execution_context()
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
        "execution_context": execution_context,
        **build_sku_awareness(record, execution_context),
    }


def _first_state_block(collection: Any) -> dict[str, Any]:
    if not isinstance(collection, list):
        return {}
    for item in collection:
        if not isinstance(item, dict):
            continue
        for state in item.get("states") or []:
            if isinstance(state, dict):
                return {
                    "profile_id": item.get("profile_id") or item.get("label"),
                    "label": item.get("label"),
                    "applies_to": item.get("applies_to"),
                    "good_when": item.get("good_when"),
                    "bad_when": item.get("bad_when"),
                    "state": state,
                    "evidence_ids": item.get("evidence_ids") or [],
                }
    return {}


def _state_projection(block: dict[str, Any], source: str) -> dict[str, Any] | None:
    if not block:
        return None
    state = block.get("state") or {}
    return {
        "source": source,
        "profile_id": block.get("profile_id"),
        "label": block.get("label"),
        "applies_to": block.get("applies_to"),
        "state_kind": state.get("state_kind"),
        "value": state.get("value"),
        "rationale": state.get("rationale"),
        "evidence_ids": block.get("evidence_ids") or [],
    }


def observed_default_projection(record: dict[str, Any]) -> dict[str, Any] | None:
    return _state_projection(_first_state_block(record.get("windows_defaults")), "windows_defaults")


def recommended_value_projection(record: dict[str, Any]) -> dict[str, Any] | None:
    profiles = record.get("recommended_profiles") or []
    if isinstance(profiles, list):
        preferred = [item for item in profiles if isinstance(item, dict) and bool(item.get("apply_allowed"))]
        if preferred:
            return _state_projection(_first_state_block(preferred), "recommended_profiles")
    return _state_projection(_first_state_block(record.get("recommended_profiles")), "recommended_profiles")


def rollback_value_projection(record: dict[str, Any]) -> dict[str, Any] | None:
    default_state = observed_default_projection(record)
    if default_state:
        return {
            **default_state,
            "rollback_strategy": "restore_default",
        }
    decision = record.get("decision") or {}
    if bool_value(decision.get("restore_previous_supported")):
        return {
            "source": "decision",
            "rollback_strategy": "restore_previous",
            "supported": True,
        }
    if bool_value(decision.get("restore_default_supported")):
        return {
            "source": "decision",
            "rollback_strategy": "restore_default",
            "supported": True,
        }
    return {
        "source": "decision",
        "rollback_strategy": "unknown",
        "supported": False,
    }


def infer_enrichment_clue_type(*, supports: list[Any] | None = None, summary: str | None = None, kind: str | None = None) -> str:
    support_set = {str(item).strip().lower() for item in (supports or []) if item}
    lowered = str(summary or "").lower()
    lowered_kind = str(kind or "").lower()
    if "caller_chain" in support_set or "caller chain" in lowered:
        return "caller_chain"
    if "api_semantics" in support_set or "api semantics" in lowered or "decompiled" in lowered or lowered_kind in {"pdb-symbolized", "decompilation", "decompiled-pseudocode"}:
        return "api_semantics"
    if "default" in lowered or "default_behavior" in support_set:
        return "default_behavior_hint"
    if "privilege" in lowered or "privilege" in support_set:
        return "privilege_hint"
    if "sibling" in lowered or "analog" in lowered or "sibling_path" in support_set:
        return "sibling_path_hint"
    return "legacy_behavior_hint"


def _range_from_exact_path(text: str | None) -> list[int] | None:
    value = str(text or "").strip()
    match = re.search(r":(\d+)(?:-(\d+))?$", value)
    if not match:
        return None
    start = int(match.group(1))
    end = int(match.group(2) or start)
    return [start, end]


def _confidence_from_static_block(block: dict[str, Any]) -> str:
    confidence = str(block.get("function_confidence") or "").strip()
    if confidence in {"symbolized_branch", "high"}:
        return "high"
    if confidence in {"string_only_review", "medium"}:
        return "medium"
    return "low"


def is_http_url(value: Any) -> bool:
    return bool(re.match(r"^https?://", str(value or "").strip(), re.IGNORECASE))


def enrichment_reference_projection(location: Any) -> tuple[str, str | None]:
    normalized = str(location or "").strip()
    if is_http_url(normalized):
        return "url", normalized
    return "path", None


def static_enrichment_projection(record: dict[str, Any]) -> list[dict[str, Any]]:
    items: list[dict[str, Any]] = []
    static_analysis = record.get("static_analysis") or {}
    if not isinstance(static_analysis, dict):
        return items
    for tool_name in ("ghidra", "ida"):
        block = static_analysis.get(tool_name) or {}
        if not isinstance(block, dict) or not block:
            continue
        branch_analysis = block.get("branch_analysis") or []
        caller_chain = " | ".join(
            str(item.get("condition") or item.get("effect_summary") or "")
            for item in branch_analysis
            if isinstance(item, dict) and (item.get("condition") or item.get("effect_summary"))
        ) or None
        xref = " | ".join(
            str(item.get("compare_condition") or item.get("jump_condition") or "")
            for item in branch_analysis
            if isinstance(item, dict) and (item.get("compare_condition") or item.get("jump_condition"))
        ) or None
        items.append(
            {
                "evidence_id": f"static-{tool_name}",
                "kind": f"{tool_name}-enrichment",
                "title": f"{tool_name.upper()} static enrichment",
                "location": block.get("artifact_path"),
                "supports": ["api_semantics"] if caller_chain or xref else ["legacy_behavior_hint"],
                "summary": block.get("effect_summary"),
                "strength": _confidence_from_static_block(block),
                "clue_type": infer_enrichment_clue_type(
                    supports=["caller_chain"] if caller_chain else ["api_semantics"] if xref else ["legacy_behavior_hint"],
                    summary=block.get("effect_summary"),
                    kind=tool_name,
                ),
                "symbol_clue": block.get("function_name"),
                "xref": xref,
                "caller_chain": caller_chain,
                "wrapper_name": block.get("function_name"),
                "decompiled_interpretation": block.get("effect_summary"),
                "confidence": _confidence_from_static_block(block),
            }
        )
    return items


def build_enrichment_cache_entries(record: dict[str, Any]) -> list[dict[str, Any]]:
    record_id = str(record.get("record_id") or record.get("tweak_id") or "")
    entries: list[dict[str, Any]] = []
    validation_proof = record.get("validation_proof") or {}
    if isinstance(validation_proof, dict) and any(validation_proof.get(field) for field in ("source_url", "exact_quote_or_path", "notes")):
        entries.append(
            {
                "record_id": record_id,
                "source": "validation-proof",
                "query_term": record_id,
                "hit_file": validation_proof.get("source_url") or validation_proof.get("exact_quote_or_path"),
                "line_range": _range_from_exact_path(validation_proof.get("exact_quote_or_path")),
                "clue_type": infer_enrichment_clue_type(supports=["api_semantics"], summary=validation_proof.get("notes"), kind="validation-proof"),
                "cached_at": now_utc(),
            }
        )
    for item in evidence_items(record):
        kind = evidence_kind(item)
        if kind not in SOURCE_ENRICHMENT_KINDS:
            continue
        entries.append(
            {
                "record_id": record_id,
                "source": kind,
                "query_term": item.get("title") or record_id,
                "hit_file": item.get("location"),
                "line_range": _range_from_exact_path(item.get("location") or item.get("summary")),
                "clue_type": infer_enrichment_clue_type(supports=item.get("supports") or [], summary=item.get("summary"), kind=kind),
                "cached_at": now_utc(),
            }
        )
    for item in static_enrichment_projection(record):
        entries.append(
            {
                "record_id": record_id,
                "source": item.get("kind"),
                "query_term": item.get("wrapper_name") or record_id,
                "hit_file": item.get("location") or item.get("symbol_clue"),
                "line_range": None,
                "clue_type": item.get("clue_type"),
                "cached_at": now_utc(),
            }
        )
    return entries


def write_enrichment_cache(entries: list[dict[str, Any]], path: Path = ENRICHMENT_CACHE_PATH) -> None:
    deduped: list[dict[str, Any]] = []
    seen: set[tuple[str, str, str, str]] = set()
    for entry in entries:
        clue_type = str(entry.get("clue_type") or "legacy_behavior_hint")
        if clue_type not in ENRICHMENT_CLUE_TYPES:
            continue
        key = (
            str(entry.get("record_id") or ""),
            str(entry.get("source") or ""),
            str(entry.get("query_term") or ""),
            str(entry.get("hit_file") or ""),
        )
        if key in seen:
            continue
        seen.add(key)
        deduped.append(entry)
    write_jsonl(path, deduped)


def load_enrichment_cache(path: Path = ENRICHMENT_CACHE_PATH) -> list[dict[str, Any]]:
    return [entry for entry in load_jsonl(path) if str(entry.get("clue_type") or "") in ENRICHMENT_CLUE_TYPES]


def source_enrichment_projection(record: dict[str, Any]) -> list[dict[str, Any]]:
    items: list[dict[str, Any]] = []
    for item in evidence_items(record):
        kind = evidence_kind(item)
        if kind not in SOURCE_ENRICHMENT_KINDS:
            continue
        location = item.get("location")
        reference_type, url = enrichment_reference_projection(location)
        items.append(
            {
                "evidence_id": item.get("evidence_id"),
                "kind": kind,
                "title": item.get("title"),
                "location": location,
                "reference_type": reference_type,
                "url": url,
                "supports": item.get("supports") or [],
                "summary": item.get("summary"),
                "strength": item.get("strength"),
                "clue_type": infer_enrichment_clue_type(supports=item.get("supports") or [], summary=item.get("summary"), kind=kind),
            }
        )
    proof = record.get("validation_proof") or {}
    if isinstance(proof, dict) and any(proof.get(field) for field in ("source_url", "exact_quote_or_path", "notes")):
        reference_type, url = enrichment_reference_projection(proof.get("source_url"))
        items.append(
            {
                "evidence_id": "validation-proof",
                "kind": "validation-proof",
                "title": "Validation proof",
                "location": proof.get("source_url"),
                "reference_type": reference_type,
                "url": url,
                "supports": ["path", "behavior"],
                "summary": proof.get("notes") or proof.get("exact_quote_or_path"),
                "strength": "high" if proof.get("key_found_on_page") else "medium",
                "clue_type": infer_enrichment_clue_type(supports=["api_semantics"], summary=proof.get("notes"), kind="validation-proof"),
            }
        )
    items.extend(static_enrichment_projection(record))
    return items


def documentation_quality_projection(
    record: dict[str, Any],
    full_evidence: dict[str, Any] | None = None,
    gate: dict[str, Any] | None = None,
) -> dict[str, Any]:
    full_evidence = full_evidence or {}
    gate = gate or {}
    target = primary_target(record)
    source_enrichment = full_evidence.get("source_enrichment")
    if not isinstance(source_enrichment, list):
        source_enrichment = source_enrichment_projection(record)
    rollback_status = full_evidence.get("rollback_status")
    if not isinstance(rollback_status, dict):
        rollback_status = rollback_status_projection(record)

    issues: list[str] = []
    value_name = target.get("value_name")
    if not target.get("path"):
        issues.append("missing-key-path")
    if value_name is None:
        issues.append("missing-value-name")
    if not target.get("value_type"):
        issues.append("missing-value-type")
    if not observed_default_projection(record):
        issues.append("missing-observed-default")
    if not recommended_value_projection(record):
        issues.append("missing-recommended-value")
    if not record.get("validation_proof"):
        issues.append("missing-validation-proof")
    if not source_enrichment:
        issues.append("missing-source-enrichment")

    blockers = {str(item) for item in (gate.get("promotion_blockers") or []) if item}
    if not rollback_status.get("rollback_value") and not (blockers & ROLLBACK_BLOCKERS):
        issues.append("missing-rollback-context")

    return {
        "documentation_quality_pass": not issues,
        "documentation_issues": issues,
        "documented_behavior": bool(record.get("validation_proof")) and bool(source_enrichment),
        "has_value_context": bool(target.get("path") and value_name is not None and target.get("value_type")),
        "has_default_context": bool(observed_default_projection(record)),
        "has_recommended_context": bool(recommended_value_projection(record)),
        "has_rollback_context": bool(rollback_status.get("rollback_value")) or bool(blockers & ROLLBACK_BLOCKERS),
    }


def documentation_status_projection(
    record: dict[str, Any],
    full_evidence: dict[str, Any] | None = None,
    gate: dict[str, Any] | None = None,
) -> dict[str, Any]:
    decision = record.get("decision") or {}
    quality = documentation_quality_projection(record, full_evidence, gate)
    return {
        "record_status": record.get("record_status"),
        "has_validation_proof": bool(record.get("validation_proof")),
        "has_official_evidence": has_official_evidence(record),
        "confidence": decision.get("confidence"),
        **quality,
    }


def evidence_status_projection(record: dict[str, Any], audit: dict[str, Any] | None = None) -> dict[str, Any]:
    audit = audit or {}
    return {
        "evidence_count": len(evidence_items(record)),
        "next_missing_layer": str(audit.get("next_missing_layer") or next_missing_layer(record) or "none"),
        "has_procmon_evidence": has_procmon_evidence(record),
        "has_ghidra_evidence": has_ghidra_evidence(record),
        "has_benchmark_evidence": has_benchmark_evidence(record),
        "has_reboot_evidence": has_reboot_evidence(record),
    }


def freshness_status_projection(
    record: dict[str, Any],
    execution_context: dict[str, Any],
    *,
    evaluated_at: str | None = None,
) -> dict[str, Any]:
    freshness = evidence_freshness(record, reproducibility_manifest())
    tested_build = str(freshness.get("os_build") or "").strip()
    current_build = str((reproducibility_manifest() or {}).get("os_build") or "").strip()
    build_delta = builds_since(current_build, tested_build)
    evaluation_time = parse_utc(evaluated_at) or datetime.now(timezone.utc)
    last_verified_at = parse_utc(str(record.get("last_reviewed_utc") or ""))
    stale_age = bool(last_verified_at and (evaluation_time - last_verified_at) >= timedelta(days=STALE_REVALIDATION_DAYS))
    stale_build = build_delta is not None and build_delta >= STALE_BUILD_THRESHOLD
    stale_reason = None
    if stale_build and stale_age:
        stale_reason = "build-drift-and-age-threshold"
    elif stale_build:
        stale_reason = "build-drift-threshold"
    elif stale_age:
        stale_reason = "verification-age-threshold"
    return {
        "status": "stale" if stale_reason else "fresh",
        "revalidation_needed": bool(stale_reason),
        "stale_reason": stale_reason,
        "last_known_good_build": tested_build or None,
        "current_build": current_build or None,
        "builds_since": build_delta,
        "last_verified_at": record.get("last_reviewed_utc"),
        "last_known_good_verification_context": {
            "backend_id": execution_context.get("backend_id"),
            "tested_build": tested_build or None,
            "current_build": current_build or None,
            "last_verified_at": record.get("last_reviewed_utc"),
        },
    }


def rollback_status_projection(record: dict[str, Any]) -> dict[str, Any]:
    decision = record.get("decision") or {}
    rollback_value = rollback_value_projection(record)
    declared = bool_value(decision.get("restore_default_supported")) or bool_value(decision.get("restore_previous_supported"))
    verified = restore_story_known(record)
    failure_reason = None if verified else "rollback-unverified"
    return {
        "rollback_declared": declared,
        "rollback_executed": False,
        "rollback_verified": verified,
        "rollback_verification_method": "record-restore-story" if verified else "missing",
        "rollback_failure_reason": failure_reason,
        "rollback_value": rollback_value,
    }


def rollback_verification_projection(full_evidence: dict[str, Any], record: dict[str, Any]) -> dict[str, Any]:
    fallback = rollback_status_projection(record)
    candidates = [
        full_evidence.get("rollback_verification"),
        (full_evidence.get("behavior") or {}).get("rollback_verification"),
        (full_evidence.get("runtime") or {}).get("rollback_verification"),
    ]
    payload = next((item for item in candidates if isinstance(item, dict) and item), None)
    if not payload:
        return fallback

    rollback_value = fallback.get("rollback_value")
    return {
        "rollback_declared": bool_value(payload.get("rollback_declared")) if payload.get("rollback_declared") is not None else fallback.get("rollback_declared"),
        "rollback_executed": bool_value(payload.get("rollback_executed")) if payload.get("rollback_executed") is not None else fallback.get("rollback_executed"),
        "rollback_verified": bool_value(payload.get("rollback_verified")) if payload.get("rollback_verified") is not None else fallback.get("rollback_verified"),
        "rollback_verification_method": payload.get("rollback_verification_method") or fallback.get("rollback_verification_method"),
        "rollback_failure_reason": payload.get("rollback_failure_reason"),
        "rollback_value": rollback_value,
        "state_changed": payload.get("state_changed"),
        "apply_diff": payload.get("apply_diff"),
        "restore_diff": payload.get("restore_diff"),
    }


def negative_evidence_projection(
    full_evidence: dict[str, Any],
    record: dict[str, Any],
    audit: dict[str, Any] | None = None,
    rollback_status: dict[str, Any] | None = None,
    bench_results: dict[str, Any] | None = None,
) -> dict[str, Any]:
    audit = audit or {}
    base = dict(full_evidence.get("negative_evidence") or {})
    attempted_tools = [str(item) for item in (base.get("attempted_tools") or audit.get("tools_used") or []) if item]
    attempted_layers = [str(item) for item in (base.get("attempted_layers") or audit.get("layers_used") or []) if item]
    signals: list[str] = [str(item) for item in (base.get("signals") or []) if item]
    runtime_negative = bool(base.get("eligible")) or bool(base.get("reason") not in {None, "", "not-applicable"})

    if runtime_negative and "procmon" in attempted_tools and not has_procmon_evidence(record) and "procmon-no-hit" not in signals:
        signals.append("procmon-no-hit")
    debugger_attempted = any(item in {"debugger", "windbg"} for item in attempted_tools)
    if runtime_negative and "etw" in attempted_tools and debugger_attempted and not has_wpr_evidence(record) and "etw-no-hit-debugger-no-call" not in signals:
        signals.append("etw-no-hit-debugger-no-call")

    sideeffects = ((full_evidence.get("behavior") or {}).get("registry_sideeffects") or {})
    sideeffect_count = sideeffects.get("sideeffect_count")
    if bool_value((record.get("decision") or {}).get("apply_allowed")) and sideeffects.get("executed") and isinstance(sideeffect_count, int) and sideeffect_count == 0 and "functional-no-effect" not in signals:
        signals.append("functional-no-effect")

    rollback_status = rollback_status or rollback_verification_projection(full_evidence, record)
    if rollback_status.get("rollback_failure_reason") == "rollback-state-mismatch" and "rollback-restore-mismatch" not in signals:
        signals.append("rollback-restore-mismatch")

    bench_results = bench_results or {}
    bench_summary = str(bench_results.get("summary") or "").lower()
    if (
        bench_results.get("executed")
        and (
            bench_results.get("safety_status") == "failed"
            or bench_results.get("safety_verdict") == "failed-safety"
            or bench_results.get("safety_passed") is False
            or "failed safety" in bench_summary
            or "safety fail" in bench_summary
        )
        and "bench-failed-safety" not in signals
    ):
        signals.append("bench-failed-safety")

    strong_signals = {"etw-no-hit-debugger-no-call", "functional-no-effect", "rollback-restore-mismatch", "bench-failed-safety"}
    signal_strength = "strong" if any(item in strong_signals for item in signals) else "medium" if signals else "none"
    conflict_reason = None
    if "functional-no-effect" in signals:
        conflict_reason = "state-change-expected-but-diff-empty"
    elif "rollback-restore-mismatch" in signals:
        conflict_reason = "rollback-restore-mismatch"
    elif "bench-failed-safety" in signals:
        conflict_reason = "safety-bench-failed"

    return {
        **base,
        "attempted_tools": attempted_tools,
        "attempted_layers": attempted_layers,
        "signals": signals,
        "signal_strength": signal_strength,
        "runtime_negative": runtime_negative or bool(signals),
        "conflict_reason": conflict_reason,
    }


def url_references_from_source_enrichment(items: list[dict[str, Any]] | None) -> list[dict[str, Any]]:
    references: list[dict[str, Any]] = []
    for item in items or []:
        if not isinstance(item, dict):
            continue
        url = str(item.get("url") or "").strip()
        location = str(item.get("location") or "").strip()
        if not url and is_http_url(location):
            url = location
        if not url:
            continue
        references.append(
            {
                "evidence_id": item.get("evidence_id"),
                "kind": item.get("kind"),
                "title": item.get("title"),
                "url": url,
            }
        )
    return references


def is_url_reachable(url: str, timeout: float = 5.0) -> tuple[bool, int | None, str | None]:
    request = urllib.request.Request(url, method="HEAD")
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            status = getattr(response, "status", None)
            return bool(status is None or status < 400), status, None
    except urllib.error.HTTPError as exc:
        if exc.code == 405:
            fallback = urllib.request.Request(url, method="GET")
            try:
                with urllib.request.urlopen(fallback, timeout=timeout) as response:
                    status = getattr(response, "status", None)
                    return bool(status is None or status < 400), status, None
            except Exception as inner_exc:  # pragma: no cover - defensive fallback
                return False, getattr(inner_exc, "code", None), str(inner_exc)
        return False, exc.code, str(exc)
    except Exception as exc:  # pragma: no cover - network variability
        return False, None, str(exc)


def validate_candidate_urls(
    record: dict[str, Any],
    full_evidence: dict[str, Any] | None = None,
    *,
    checker: Any = None,
    timeout: float = 5.0,
) -> dict[str, Any]:
    full_evidence = full_evidence or {}
    checker = checker or is_url_reachable
    source_enrichment = full_evidence.get("source_enrichment")
    if not isinstance(source_enrichment, list):
        source_enrichment = source_enrichment_projection(record)

    checked: list[dict[str, Any]] = []
    dead_links: list[dict[str, Any]] = []
    for reference in url_references_from_source_enrichment(source_enrichment):
        reachable, status_code, error = checker(reference["url"], timeout=timeout)
        item = {
            **reference,
            "reachable": reachable,
            "status_code": status_code,
            "error": error,
        }
        checked.append(item)
        if not reachable:
            dead_links.append(item)

    return {
        "checked_url_count": len(checked),
        "reachable_url_count": sum(1 for item in checked if item["reachable"]),
        "dead_link_count": len(dead_links),
        "dead_links": dead_links,
        "checked_urls": checked,
        "status": "dead-link" if dead_links else "ok" if checked else "not-run",
    }


def load_url_validation_report(path: Path | None = None) -> dict[str, dict[str, Any]]:
    payload = load_json_if_exists(path or URL_VALIDATION_REPORT_PATH)
    if not isinstance(payload, dict):
        return {}
    result: dict[str, dict[str, Any]] = {}
    for entry in payload.get("entries") or []:
        if not isinstance(entry, dict):
            continue
        for key_name in ("candidate_id", "record_id", "tweak_id"):
            key = str(entry.get(key_name) or "").strip()
            if key and key not in result:
                result[key] = entry
    return result


def url_validation_status_projection(
    record: dict[str, Any],
    full_evidence: dict[str, Any] | None = None,
    report_map: dict[str, dict[str, Any]] | None = None,
) -> dict[str, Any]:
    full_evidence = full_evidence or {}
    payload = full_evidence.get("url_validation")
    if isinstance(payload, dict) and payload:
        return payload
    report_map = report_map or load_url_validation_report()
    for key_name in ("record_id", "tweak_id"):
        key = str(record.get(key_name) or "").strip()
        if key and key in report_map:
            return report_map[key]
    return {
        "checked_url_count": 0,
        "reachable_url_count": 0,
        "dead_link_count": 0,
        "dead_links": [],
        "checked_urls": [],
        "status": "not-run",
    }


def infer_bench_profile_group(feature_area: str) -> str:
    area = str(feature_area or "").strip().lower()
    if any(token in area for token in ("scheduler", "priority")):
        return "scheduler"
    if any(token in area for token in ("service", "maintenance", "watchdog")):
        return "service"
    if any(token in area for token in ("display", "graphics")):
        return "display"
    if any(token in area for token in ("network", "dns")):
        return "network"
    if "power" in area:
        return "power"
    return "default"


def bench_profile_projection(record: dict[str, Any]) -> dict[str, Any]:
    payload = load_bench_profile_map()
    entries = payload.get("entries") or {}
    feature_area_group = infer_bench_profile_group(record_feature_area(record))
    entry = entries.get(feature_area_group) or entries.get("default") or {}
    return {
        "feature_area_group": feature_area_group,
        "profiles": list(entry.get("profiles") or []),
        "bench_vm_capable": entry.get("bench_vm_capable", True),
        "bare_metal_required_for_perf_claim": bool(entry.get("bare_metal_required_for_perf_claim")),
    }


def _heuristic_gap(candidate_id: str, gap_type: str, key_path: str, seed_reference: str, feature_area: str) -> dict[str, Any]:
    execution_context = default_execution_context()
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
        "execution_context": execution_context,
        **build_sku_awareness_for_key_path(key_path, execution_context),
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
    if str(candidate.get("discovery_source") or "") == "etl-registry-touch":
        reasons.extend(f"etl:{reason}" for reason in _etl_triage_reasons(candidate))
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


def score_candidate(record: dict[str, Any], audit: dict[str, Any] | None = None, full_evidence: dict[str, Any] | None = None) -> dict[str, Any]:
    audit = audit or {}
    full_evidence = full_evidence or {}
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
    stale_build = (builds_since(current_build, tested_build) or 0) >= STALE_BUILD_THRESHOLD

    static_items = [
        item
        for item in evidence_items(record)
        if evidence_kind(item) not in {
            "procmon-trace",
            "runtime-diff",
            "runtime-trace",
            "vm-test",
            "registry-observation",
            "wpr-trace",
            "etw-trace",
            "runtime-benchmark",
        }
    ]
    static_strengths: list[int] = []
    for item in static_items:
        kind = evidence_kind(item)
        supports = {str(value) for value in (item.get("supports") or []) if value}
        summary = str(item.get("summary") or "").lower()
        if kind in WEAK_STATIC_KINDS:
            static_strengths.append(1)
            continue
        if supports & WEAK_STATIC_SUPPORTS or "string/xref" in summary or "string reference" in summary or "xref" in summary or "raw-memory scan" in summary:
            static_strengths.append(2)
            continue
        strong_semantics = bool(supports & STRONG_STATIC_SUPPORTS) or any(
            needle in summary
            for needle in (
                "caller chain",
                "api semantic",
                "reads and writes",
                "reads ",
                " writes ",
                "query",
                "decompiled the functions",
                "maps ",
                "derived from",
                "shgetvaluew",
                "shsetvaluew",
            )
        )
        if kind in STRONG_STATIC_KINDS and strong_semantics:
            static_strengths.append(5 if kind in {"official-doc", "policy-csp", "troubleshoot-doc"} else 4)
            continue
        if kind in {"ghidra-headless", "ghidra-trace", "pdb-symbolized"}:
            static_strengths.append(4 if strong_semantics else 2)
            continue
        if kind in {"repo-doc", "open-source-reference", "inference"}:
            static_strengths.append(3 if strong_semantics else 2)
            continue
        static_strengths.append(2)

    static_evidence_strength = max(static_strengths) if static_strengths else 1

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
    negative_evidence = negative_evidence_projection(full_evidence, record, audit)
    negative_penalty = 2 if negative_evidence.get("signal_strength") == "strong" else 1 if negative_evidence.get("signal_strength") == "medium" else 0
    runtime_evidence_strength = min(5, max(1, runtime_hits + 1 - negative_penalty))

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


def _etl_candidate_has_value_context(candidate: dict[str, Any]) -> bool:
    for field in ("value_name", "value_data"):
        value = candidate.get(field)
        if value is None:
            continue
        if isinstance(value, str):
            if value.strip():
                return True
            continue
        return True
    return False


def score_etl_candidate(candidate: dict[str, Any]) -> dict[str, Any]:
    operation = _etl_candidate_operation(candidate)
    has_value = _etl_candidate_has_value_context(candidate)
    feature_area = str(candidate.get("feature_area") or "Unknown")

    if operation in {"RegSetValue", "SetValueKey", "RegCreateKey", "CreateKey"} and has_value:
        runtime_evidence_strength = 0.8
    elif operation in {"RegSetValue", "SetValueKey", "RegCreateKey", "CreateKey"}:
        runtime_evidence_strength = 0.5
    elif operation == "RegQueryValue" and has_value:
        runtime_evidence_strength = 0.4
    else:
        runtime_evidence_strength = 0.2

    components = {
        "static_evidence_strength": 0.1,
        "runtime_evidence_strength": runtime_evidence_strength,
        "rollback_clarity": 0.3,
        "tweak_suitability": ETL_SCORE_AREA_WEIGHTS.get(feature_area, 0.1),
        "bench_priority": 0.4,
    }
    total = round(
        sum(components[name] * ETL_SCORE_COMPONENT_WEIGHTS[name] for name in ETL_SCORE_COMPONENT_WEIGHTS),
        3,
    )

    return {
        "profile": "etl-runtime-v1",
        "feature_area": feature_area,
        "operation": operation or "unknown",
        "has_value_context": has_value,
        **components,
        "total": total,
    }


def bench_results_projection(full_evidence: dict[str, Any], record: dict[str, Any] | None = None, audit: dict[str, Any] | None = None) -> dict[str, Any]:
    behavior = (full_evidence.get("behavior") or {}) if isinstance(full_evidence, dict) else {}
    benchmark = (behavior.get("benchmark") or {}) if isinstance(behavior, dict) else {}
    record_bench_results = (record.get("bench_results") or {}) if isinstance(record, dict) else {}
    # Record-level bench results are the curated canonical verdict. Older
    # full-evidence bundles may still retain failed exploratory runs.
    explicit_results = (record_bench_results or full_evidence.get("bench_results") or {}) if isinstance(full_evidence, dict) else record_bench_results
    reproducibility = full_evidence.get("reproducibility") or {}
    audit = audit or {}
    record = record or {}
    next_layer = str(audit.get("next_missing_layer") or next_missing_layer(record) or "none")
    bench_required = next_layer in {"wpr-or-benchmark", "runtime-benchmark"}
    bench_profile = bench_profile_projection(record)

    def first_present(name: str) -> Any:
        if isinstance(explicit_results, dict) and name in explicit_results:
            return explicit_results.get(name)
        if isinstance(benchmark, dict) and name in benchmark:
            return benchmark.get(name)
        return None

    safety_passed_raw = first_present("safety_passed")
    safety_passed = None if safety_passed_raw is None else bool_value(safety_passed_raw)
    safety_status = first_present("safety_status")
    if safety_status is None and safety_passed is not None:
        safety_status = "passed" if safety_passed else "failed"
    executed = bool_value(first_present("executed")) or safety_passed is not None
    bench_tier = first_present("bench_tier") or ("vm" if reproducibility.get("vm_name") else "unknown")

    result = {
        "bench_tier": bench_tier,
        "bench_required": bench_required,
        "bench_vm_capable": bench_profile.get("bench_vm_capable"),
        "bench_bare_metal_required": bench_profile.get("bare_metal_required_for_perf_claim"),
        "feature_area_group": bench_profile.get("feature_area_group"),
        "profiles": bench_profile.get("profiles"),
        "executed": executed,
        "summary": first_present("summary"),
        "statistics": first_present("statistics"),
        "significance_verdict": first_present("significance_verdict"),
    }

    optional_fields = {
        "safety_passed": safety_passed,
        "safety_status": safety_status,
        "boot_success": first_present("boot_success"),
        "shell_usable": first_present("shell_usable"),
        "services_healthy": first_present("services_healthy"),
        "event_log_clean": first_present("event_log_clean"),
        "apply_verified": first_present("apply_verified"),
        "rollback_executed": first_present("rollback_executed"),
        "rollback_verified": first_present("rollback_verified"),
        "rollback_failure_reason": first_present("rollback_failure_reason"),
        "bench_environment": first_present("bench_environment"),
        "bench_measurement_reliability": first_present("bench_measurement_reliability"),
        "output_file": first_present("output_file"),
        "executed_at": first_present("executed_at"),
    }
    for key, value in optional_fields.items():
        if value is not None:
            result[key] = value

    return result


def verification_environment_projection(record: dict[str, Any], full_evidence: dict[str, Any], execution_context: dict[str, Any]) -> dict[str, Any]:
    freshness = evidence_freshness(record, reproducibility_manifest())
    reproducibility = full_evidence.get("reproducibility") or {}
    build_sku = build_sku_awareness(record, execution_context)
    return {
        "os_build": freshness.get("os_build"),
        "os_edition": build_sku.get("os_edition"),
        "architecture": build_sku.get("architecture"),
        "vm_name": reproducibility.get("vm_name"),
        "baseline_snapshot": reproducibility.get("baseline_snapshot"),
        "backend_id": execution_context.get("backend_id"),
        "execution_context": execution_context,
    }


def discovery_projection(record: dict[str, Any]) -> tuple[str, str]:
    return "imported_record", "existing_research"


def candidate_identity(record: dict[str, Any]) -> tuple[str, str]:
    record_id = str(record.get("record_id") or record.get("tweak_id") or "")
    tweak_id = str(record.get("tweak_id") or record_id)
    return record_id, tweak_id


def _gate_layer_blockers(next_layer: str) -> list[str]:
    mapping = {
        "decision-gate": ["documentation-first-review"],
        "runtime-trace": ["no-runtime-proof"],
        "procmon": ["no-runtime-proof"],
        "wpr-or-benchmark": ["bench-not-run"],
        "runtime-benchmark": ["bench-not-run"],
        "ghidra": ["no-runtime-proof"],
        "early-boot": ["no-runtime-proof"],
    }
    return list(mapping.get(next_layer, [next_layer])) if next_layer not in {"", "none"} else []


def _gate_layer_blockers_for_record(next_layer: str, existing_blockers: set[str]) -> list[str]:
    # Surface the specific blocker family when a record already carries one.
    # Generic layer labels are only fallbacks for otherwise clean records.
    if next_layer in {
        "decision-gate",
        "official-doc",
        "runtime-trace",
        "ghidra",
        "restore-story",
        "intentional-hold",
    } and existing_blockers:
        return []
    return _gate_layer_blockers(next_layer)


def _infer_blocker_driven_missing_layer(base_layer: str, blockers: set[str]) -> str:
    if base_layer != "decision-gate":
        return base_layer

    blocker_texts = [str(item).strip().lower() for item in blockers if str(item).strip()]
    if not blocker_texts:
        return base_layer

    def has_phrase(*phrases: str) -> bool:
        return any(phrase in blocker for blocker in blocker_texts for phrase in phrases)

    if has_phrase(
        "intentional-hold",
        "boot-unsafe",
        "not-mapped-to-supported-app-surface",
        "research-only-raw-",
        "do not probe without dedicated boot lane",
        "virtualized baselines do not support",
        "a real hibernation trigger is not available",
        "a real drips-exit trigger cannot be exercised here",
    ):
        return "intentional-hold"

    if has_phrase(
        "restore story",
        "rollback",
        "restore/default story",
    ):
        return "restore-story"

    if has_phrase(
        "no-current-build-registry-seeding-path",
        "no-current-build-string-or-symbol-hit",
        "caller into that helper path is still unresolved",
        "exact watchdog-specific caller",
        "adjacent rather than leaf-specific",
        "leaf-specific",
        "direct-reading function",
        "conditional-initialization-unproven",
    ):
        return "ghidra"

    if has_phrase(
        "runtime_no_read",
        "no-runtime-proof",
        "wpr-boot-registry-no-hit",
        "exact runtime query/read",
        "exact live read",
        "runtime package",
        "procmon-saveas-timeout",
    ):
        return "runtime-trace"

    if has_phrase(
        "no-primary-current-build-doc",
        "no-doc-source-outside-repo",
        "official documentation",
    ):
        return "official-doc"

    return base_layer


def evaluate_candidate_gate(
    record: dict[str, Any],
    audit: dict[str, Any] | None = None,
    full_evidence: dict[str, Any] | None = None,
    *,
    backend_id: str = DEFAULT_BACKEND_ID,
    evaluated_at: str | None = None,
    url_validation_status: dict[str, Any] | None = None,
) -> dict[str, Any]:
    audit = audit or {}
    full_evidence = full_evidence or {}
    decision = record.get("decision") or {}
    app_status = extract_app_status(record)
    tweak_origin = derive_tweak_origin(record)
    missing_layer = str(audit.get("next_missing_layer") or next_missing_layer(record))
    compatibility_mode = "native" if CURRENT_SCHEMA_VERSION in SUPPORTED_SCHEMA_VERSIONS else "unsupported"
    record_status = str(record.get("record_status") or "").strip().lower()
    execution_context = default_execution_context(backend_id)
    freshness = evidence_freshness(record, reproducibility_manifest())
    freshness_status = freshness_status_projection(record, execution_context, evaluated_at=evaluated_at)
    tested_build = str(freshness.get("os_build") or "").strip()
    current_build = str((reproducibility_manifest() or {}).get("os_build") or "").strip()
    rollback_status = rollback_verification_projection(full_evidence, record)
    bench_results = bench_results_projection(full_evidence, record, audit)
    negative_evidence = negative_evidence_projection(full_evidence, record, audit, rollback_status, bench_results)
    score_breakdown = score_candidate(record, audit, full_evidence)
    url_validation = (
        dict(url_validation_status)
        if isinstance(url_validation_status, dict)
        else url_validation_status_projection(record, full_evidence)
    )

    blockers = [str(item) for item in (decision.get("blocking_issues") or []) if item]
    blocker_set = set(blockers)

    target = primary_target(record)
    if CURRENT_SCHEMA_VERSION not in SUPPORTED_SCHEMA_VERSIONS:
        blocker_set.add("schema-version-unsupported")
    if record_status == "deprecated":
        blocker_set.add("deprecated-record")
    if record_status == "archived" or missing_layer == "archived":
        blocker_set.add("archived")
    value_name_present = "value_name" in target and target.get("value_name") is not None
    if not target.get("path") or not value_name_present or not target.get("value_type"):
        blocker_set.add("schema-incomplete")
    rollback_failure_reason = str(rollback_status.get("rollback_failure_reason") or "")
    if rollback_failure_reason == "rollback-state-mismatch":
        blocker_set.add("rollback-failed")
    elif bool_value(decision.get("apply_allowed")) and not rollback_status.get("rollback_verified"):
        blocker_set.add("rollback-unverified")
    if "functional-no-effect" in (negative_evidence.get("signals") or []):
        blocker_set.add("functional-no-effect")
    if "bench-failed-safety" in (negative_evidence.get("signals") or []):
        blocker_set.add("bench-failed-safety")
    if any(signal in {"procmon-no-hit", "etw-no-hit-debugger-no-call"} for signal in (negative_evidence.get("signals") or [])):
        blocker_set.add("no-runtime-proof")
    if int(url_validation.get("dead_link_count") or 0) > 0:
        blocker_set.add("dead-link")
    if bench_results.get("bench_required") and not bench_results.get("executed"):
        blocker_set.add("bench-not-run")
    missing_layer = _infer_blocker_driven_missing_layer(missing_layer, blocker_set)
    blocker_set.update(_gate_layer_blockers_for_record(missing_layer, blocker_set))

    evidence_status = evidence_status_projection(
        record,
        {**audit, "next_missing_layer": missing_layer},
    )

    documentation_status = documentation_status_projection(
        record,
        full_evidence,
        {"promotion_blockers": sorted(blocker_set)},
    )

    hard_blockers = sorted(
        blocker
        for blocker in blocker_set
        if blocker not in {"stale-evidence"}
    )
    stale_revalidation = bool(freshness_status.get("revalidation_needed"))

    legacy_ingest_promotable = (
        bool_value(decision.get("apply_allowed"))
        and app_status == "matches-research"
        and tweak_origin == "legacy-curated"
    )
    research_record_promotable = (
        bool_value(bench_results.get("executed"))
        and bench_results.get("safety_passed") is True
        and rollback_status.get("rollback_verified") is True
    )

    if record_status == "deprecated" or "archived" in blocker_set:
        state = "rejected"
    elif "schema-version-unsupported" in blocker_set:
        state = "blocked"
    elif hard_blockers:
        state = "blocked"
    elif stale_revalidation:
        state = "revalidation-pending"
    elif legacy_ingest_promotable or research_record_promotable:
        state = "promoted"
    else:
        state = "promotion-eligible"

    promotion_blockers = hard_blockers
    if state == "revalidation-pending":
        promotion_blockers = ["stale-evidence"]
    elif state == "rejected" and "deprecated-record" in blocker_set:
        promotion_blockers = ["deprecated-record"]

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
        "promotion_blockers": promotion_blockers,
        "record_promotion_allowed": state in {"promotion-eligible", "promoted", "revalidation-pending"},
        "tweak_ingest_allowed": state == "promoted" and legacy_ingest_promotable,
        "apply_allowed": bool_value(decision.get("apply_allowed")),
        "app_mapping_status": app_status,
        "next_missing_layer": missing_layer or "none",
        "debug_override_allowed": tweak_origin == "research-derived",
        "last_evaluated_at": now_utc(),
        "score_breakdown": score_breakdown,
        "documentation_status": documentation_status,
        "evidence_status": evidence_status,
        "rollback_status": rollback_status,
        "bench_status": bench_results,
        "negative_evidence_status": negative_evidence,
        "url_validation_status": url_validation,
        "freshness_status": freshness_status,
        "verification_context": {
            "backend_id": backend_id,
            "current_build": current_build,
            "tested_build": tested_build,
            "last_verified_at": record.get("last_reviewed_utc"),
            "stale_reason": freshness_status.get("stale_reason"),
            "revalidation_needed": freshness_status.get("revalidation_needed"),
            "last_known_good_build": freshness_status.get("last_known_good_build"),
        },
    }


def derive_promotion_state(record: dict[str, Any], audit: dict[str, Any] | None = None) -> dict[str, Any]:
    return evaluate_candidate_gate(
        record,
        audit,
        {
            "behavior": {},
            "negative_evidence": {},
            "reproducibility": reproducibility_manifest(),
        },
    )


def before_after_projection(full_evidence: dict[str, Any]) -> dict[str, Any]:
    sideeffects = ((full_evidence.get("behavior") or {}).get("registry_sideeffects") or {}) if isinstance(full_evidence, dict) else {}
    counts = sideeffects.get("summary_counts") or {}
    structured_diff = (
        full_evidence.get("structured_diff")
        or sideeffects.get("structured_diff")
        or ((full_evidence.get("behavior") or {}).get("structured_diff"))
        or {}
    )
    return {
        "format": sideeffects.get("format"),
        "diff_file": sideeffects.get("diff_file"),
        "key_added": counts.get("added_keys", 0),
        "key_deleted": counts.get("removed_keys", 0),
        "value_added": counts.get("added_values", 0),
        "value_deleted": counts.get("removed_values", 0),
        "value_changed": counts.get("modified_values", 0),
        "unchanged_values": counts.get("unchanged_values", 0),
        "key_added_entries": structured_diff.get("key_added") or [],
        "key_deleted_entries": structured_diff.get("key_deleted") or [],
        "value_added_entries": structured_diff.get("value_added") or [],
        "value_deleted_entries": structured_diff.get("value_deleted") or [],
        "value_changed_entries": structured_diff.get("value_changed") or [],
    }


def canonical_bundle_projection(
    record: dict[str, Any],
    audit: dict[str, Any],
    full_evidence: dict[str, Any],
    backend_id: str = DEFAULT_BACKEND_ID,
    *,
    url_validation_status: dict[str, Any] | None = None,
) -> dict[str, Any]:
    record_id, tweak_id = candidate_identity(record)
    target = primary_target(record)
    gate = evaluate_candidate_gate(
        record,
        audit,
        full_evidence,
        backend_id=backend_id,
        url_validation_status=url_validation_status,
    )
    execution_context = default_execution_context(backend_id)
    discovery_source, discovery_reason = discovery_projection(record)
    freshness = evidence_freshness(record, reproducibility_manifest())
    rollback_status = rollback_verification_projection(full_evidence, record)
    before_after = before_after_projection(full_evidence)
    bench_results = bench_results_projection(full_evidence, record, audit)
    build_sku = build_sku_awareness(record, execution_context, backend_id)
    negative_evidence = negative_evidence_projection(full_evidence, record, audit, rollback_status, bench_results)
    documentation_status = documentation_status_projection(record, full_evidence, gate)
    url_validation = gate.get("url_validation_status") or url_validation_status_projection(record, full_evidence)

    return {
        "schema_version": CURRENT_SCHEMA_VERSION,
        "candidate_id": record_id,
        "feature_area": record_feature_area(record),
        "discovery_source": discovery_source,
        "discovery_reason": discovery_reason,
        "key_path": target.get("path"),
        "value_name": target.get("value_name"),
        "value_type": target.get("value_type"),
        "observed_default": observed_default_projection(record),
        "recommended_value": recommended_value_projection(record),
        "rollback_value": rollback_status.get("rollback_value"),
        "execution_context": execution_context,
        "promotion_state": gate["promotion_state"],
        "promotion_blockers": gate["promotion_blockers"],
        "record_promotion_allowed": gate["record_promotion_allowed"],
        "tweak_ingest_allowed": gate["tweak_ingest_allowed"],
        "tweak_origin": gate["tweak_origin"],
        "os_build": build_sku.get("os_build"),
        "os_edition": build_sku.get("os_edition"),
        "architecture": build_sku.get("architecture"),
        "elevation_context": build_sku.get("elevation_context"),
        "machine_user_scope": build_sku.get("machine_user_scope"),
        "build_sku_awareness": build_sku,
        "gate_result": gate,
        "evidence_freshness": freshness,
        "last_verified_at": record.get("last_reviewed_utc"),
        "verification_environment": verification_environment_projection(record, full_evidence, execution_context),
        "negative_evidence": negative_evidence,
        "url_validation": url_validation,
        "before_after": before_after,
        "source_enrichment": source_enrichment_projection(record),
        "bench_results": bench_results,
        "score_breakdown": gate["score_breakdown"],
        "documentation_status": documentation_status,
        "evidence_status": gate["evidence_status"],
        "rollback_status": rollback_status,
        "rollback_verification": rollback_status,
        "freshness_status": gate.get("freshness_status"),
        "verification_context": {
            "record_id": record_id,
            "tweak_id": tweak_id,
            "supported_schema_versions": gate["supported_schema_versions"],
            "schema_compatibility_mode": gate["schema_compatibility_mode"],
            "evaluator_version": gate["evaluator_version"],
        },
    }


def normalized_gate_snapshot(gate: dict[str, Any]) -> dict[str, Any]:
    payload = json.loads(json.dumps(gate))
    payload.pop("last_evaluated_at", None)
    return payload


def before_after_parse_test_projection(full_evidence: dict[str, Any]) -> dict[str, Any]:
    payload = before_after_projection(full_evidence)
    required_lists = (
        "key_added_entries",
        "key_deleted_entries",
        "value_added_entries",
        "value_deleted_entries",
        "value_changed_entries",
    )
    required_counts = (
        "key_added",
        "key_deleted",
        "value_added",
        "value_deleted",
        "value_changed",
        "unchanged_values",
    )
    errors: list[str] = []
    for key in required_counts:
        if not isinstance(payload.get(key), int):
            errors.append(f"invalid-count:{key}")
    for key in required_lists:
        if not isinstance(payload.get(key), list):
            errors.append(f"invalid-list:{key}")
    return {
        "pass": not errors,
        "errors": errors,
        "before_after": payload,
    }


def rollback_presence_test_projection(gate: dict[str, Any], full_evidence: dict[str, Any], record: dict[str, Any]) -> dict[str, Any]:
    rollback_status = (full_evidence.get("rollback_status") or gate.get("rollback_status") or rollback_verification_projection(full_evidence, record) or {})
    blockers = {str(item) for item in (gate.get("promotion_blockers") or []) if item}
    rollback_value_present = rollback_status.get("rollback_value") is not None
    rollback_blocker_present = bool(blockers & ROLLBACK_BLOCKERS)
    return {
        "pass": rollback_value_present or rollback_blocker_present,
        "rollback_value_present": rollback_value_present,
        "rollback_blocker_present": rollback_blocker_present,
        "rollback_status": rollback_status,
    }


def rollback_verification_test_projection(gate: dict[str, Any], full_evidence: dict[str, Any], record: dict[str, Any]) -> dict[str, Any]:
    rollback_status = (full_evidence.get("rollback_status") or gate.get("rollback_status") or rollback_verification_projection(full_evidence, record) or {})
    rollback_verified = rollback_status.get("rollback_verified")
    defined = isinstance(rollback_verified, bool)
    return {
        "pass": defined,
        "rollback_declared": rollback_status.get("rollback_declared"),
        "rollback_executed": rollback_status.get("rollback_executed"),
        "rollback_verified": rollback_verified,
        "rollback_failure_reason": rollback_status.get("rollback_failure_reason"),
    }


def bench_profile_consistency_test_projection(gate: dict[str, Any], full_evidence: dict[str, Any], record: dict[str, Any]) -> dict[str, Any]:
    expected = bench_profile_projection(record)
    actual = (full_evidence.get("bench_results") or gate.get("bench_status") or bench_results_projection(full_evidence, record, {})) or {}
    profiles_match = list(actual.get("profiles") or []) == list(expected.get("profiles") or [])
    vm_match = actual.get("bench_vm_capable") == expected.get("bench_vm_capable")
    bare_match = bool(actual.get("bench_bare_metal_required")) == bool(expected.get("bare_metal_required_for_perf_claim"))
    group_match = str(actual.get("feature_area_group") or "") == str(expected.get("feature_area_group") or "")
    return {
        "pass": profiles_match and vm_match and bare_match and group_match,
        "expected": expected,
        "actual": {
            "feature_area_group": actual.get("feature_area_group"),
            "profiles": actual.get("profiles"),
            "bench_vm_capable": actual.get("bench_vm_capable"),
            "bench_bare_metal_required": actual.get("bench_bare_metal_required"),
        },
    }


def build_regression_pack(record: dict[str, Any], audit: dict[str, Any], full_evidence: dict[str, Any], gate: dict[str, Any] | None = None) -> dict[str, dict[str, Any]]:
    url_validation = url_validation_status_projection(record, full_evidence)
    gate = gate or evaluate_candidate_gate(record, audit, full_evidence, url_validation_status=url_validation)
    bundle = canonical_bundle_projection(record, audit, full_evidence, url_validation_status=url_validation)
    schema_errors = validate_canonical_bundle(bundle)
    first_gate = normalized_gate_snapshot(gate)
    second_gate = normalized_gate_snapshot(
        evaluate_candidate_gate(record, audit, full_evidence, url_validation_status=url_validation)
    )
    docs_status = documentation_status_projection(record, full_evidence, gate)
    before_after_test = before_after_parse_test_projection(full_evidence)
    rollback_presence = rollback_presence_test_projection(gate, full_evidence, record)
    rollback_verification = rollback_verification_test_projection(gate, full_evidence, record)
    bench_consistency = bench_profile_consistency_test_projection(gate, full_evidence, record)
    return {
        "schema_test.json": {
            "pass": not schema_errors,
            "errors": schema_errors,
            "schema_version": bundle.get("schema_version"),
            "candidate_id": bundle.get("candidate_id"),
        },
        "gate_test.json": {
            "pass": first_gate == second_gate,
            "first": first_gate,
            "second": second_gate,
        },
        "docs_test.json": docs_status,
        "before_after_parse_test.json": before_after_test,
        "rollback_presence_test.json": rollback_presence,
        "rollback_verification_test.json": rollback_verification,
        "bench_profile_consistency_test.json": bench_consistency,
    }


def candidate_regression_pack_dir(candidate_id: str, root: Path | None = None) -> Path:
    sanitized = str(candidate_id).replace("\\", "__").replace("/", "__")
    return (root or REGRESSION_PACKS_ROOT) / sanitized


def write_regression_pack(candidate_id: str, pack: dict[str, dict[str, Any]], root: Path | None = None) -> Path:
    output_dir = candidate_regression_pack_dir(candidate_id, root)
    output_dir.mkdir(parents=True, exist_ok=True)
    for name, payload in pack.items():
        write_json(output_dir / name, payload)
    return output_dir


def resolve_record(candidate_id: str, records: list[dict[str, Any]] | None = None) -> dict[str, Any] | None:
    return record_map(records).get(candidate_id)


def get_evidence_bundle(candidate_id: str, records: list[dict[str, Any]] | None = None, audit_map: dict[str, dict[str, Any]] | None = None) -> dict[str, Any] | None:
    record = resolve_record(candidate_id, records)
    if not record:
        return None
    audit_map = audit_map or load_audit_entries()
    full_evidence = load_full_evidence_bundle(str(record.get("record_id") or record.get("tweak_id") or ""))
    return canonical_bundle_projection(record, audit_map.get(candidate_id, {}), full_evidence)


def get_candidate_by_key_path(key_path: str, records: list[dict[str, Any]] | None = None) -> dict[str, Any] | None:
    key_path = str(key_path or "").strip().lower()
    if not key_path:
        return None
    records = records or load_records()
    audit_map = load_audit_entries()
    for record in records:
        target_path = str(primary_target(record).get("path") or "").strip().lower()
        if target_path != key_path:
            continue
        record_id = str(record.get("record_id") or record.get("tweak_id") or "")
        full_evidence = load_full_evidence_bundle(record_id)
        return canonical_bundle_projection(record, audit_map.get(record_id, {}), full_evidence)
    return None


def score_candidate_by_id(candidate_id: str, records: list[dict[str, Any]] | None = None, audit_map: dict[str, dict[str, Any]] | None = None) -> dict[str, Any] | None:
    record = resolve_record(candidate_id, records)
    if not record:
        return None
    audit_map = audit_map or load_audit_entries()
    full_evidence = load_full_evidence_bundle(str(record.get("record_id") or record.get("tweak_id") or ""))
    return score_candidate(record, audit_map.get(candidate_id, {}), full_evidence)


def evaluate_candidate_by_id(candidate_id: str, records: list[dict[str, Any]] | None = None, audit_map: dict[str, dict[str, Any]] | None = None) -> dict[str, Any] | None:
    record = resolve_record(candidate_id, records)
    if not record:
        return None
    audit_map = audit_map or load_audit_entries()
    full_evidence = load_full_evidence_bundle(str(record.get("record_id") or record.get("tweak_id") or ""))
    return evaluate_candidate_gate(record, audit_map.get(candidate_id, {}), full_evidence)


def list_blocked_candidates(reason_type: str | None = None, gate_map: dict[str, dict[str, Any]] | None = None) -> list[dict[str, Any]]:
    gate_map = gate_map or load_promotion_gate_map()
    entries = [entry for entry in gate_map.values() if str(entry.get("promotion_state") or "") == "blocked"]
    if reason_type:
        lowered = reason_type.lower()
        entries = [
            entry
            for entry in entries
            if any(lowered in str(blocker).lower() for blocker in (entry.get("promotion_blockers") or []))
        ]
    return sorted(entries, key=lambda item: str(item.get("tweak_id") or item.get("candidate_id") or ""))


def list_revalidation_pending_candidates(gate_map: dict[str, dict[str, Any]] | None = None) -> list[dict[str, Any]]:
    gate_map = gate_map or load_promotion_gate_map()
    entries = [
        entry for entry in gate_map.values()
        if str(entry.get("promotion_state") or "") == "revalidation-pending"
    ]
    return sorted(entries, key=lambda item: str(item.get("tweak_id") or item.get("candidate_id") or ""))


def append_mutation_audit_log(entry: dict[str, Any], path: Path | None = None) -> None:
    append_jsonl(path or MUTATION_AUDIT_LOG_PATH, entry)


def evaluate_apply_request(
    candidate_id: str,
    *,
    override: bool = False,
    reason: str | None = None,
    contributor_mode: bool = False,
    gate_map: dict[str, dict[str, Any]] | None = None,
    audit_path: Path | None = None,
) -> dict[str, Any]:
    gate_map = gate_map or load_promotion_gate_map()
    entry = gate_map.get(candidate_id)
    if not entry:
        return {
            "allowed": False,
            "candidate_id": candidate_id,
            "message": "candidate-not-found",
            "override_used": False,
        }

    promoted = str(entry.get("promotion_state") or "") == "promoted"
    legacy = str(entry.get("tweak_origin") or "") == "legacy-curated"
    override_allowed = override and contributor_mode and bool(entry.get("debug_override_allowed"))
    allowed = legacy or promoted or override_allowed
    override_used = not (legacy or promoted) and override_allowed
    message = (
        "apply-allowed"
        if allowed and not override_used
        else "apply-override-allowed"
        if allowed
        else f"promotion-state:{entry.get('promotion_state')}"
    )
    payload = {
        "allowed": allowed,
        "candidate_id": candidate_id,
        "promotion_state": entry.get("promotion_state"),
        "override_requested": override,
        "override_used": override_used,
        "override_reason": reason or "",
        "contributor_mode": contributor_mode,
        "message": message,
        "entry": entry,
    }
    if override:
        append_mutation_audit_log(
            {
                "timestamp_utc": now_utc(),
                "action": "apply",
                "candidate_id": candidate_id,
                "promotion_state": entry.get("promotion_state"),
                "override_requested": True,
                "override_used": override_used,
                "override_reason": reason or "unspecified",
                "contributor_mode": contributor_mode,
                "allowed": allowed,
                "message": message,
            },
            audit_path,
        )
    return payload


def apply_candidate(
    candidate_id: str,
    *,
    override: bool = False,
    reason: str | None = None,
    contributor_mode: bool = False,
    gate_map: dict[str, dict[str, Any]] | None = None,
    audit_path: Path | None = None,
) -> dict[str, Any]:
    return evaluate_apply_request(
        candidate_id,
        override=override,
        reason=reason,
        contributor_mode=contributor_mode,
        gate_map=gate_map,
        audit_path=audit_path,
    )


def rollback_candidate(
    candidate_id: str,
    *,
    override: bool = False,
    reason: str | None = None,
    contributor_mode: bool = False,
    gate_map: dict[str, dict[str, Any]] | None = None,
    audit_path: Path | None = None,
) -> dict[str, Any]:
    decision = evaluate_apply_request(
        candidate_id,
        override=override,
        reason=reason,
        contributor_mode=contributor_mode,
        gate_map=gate_map,
        audit_path=None,
    )
    if not decision.get("allowed"):
        return {**decision, "action": "rollback", "warnings": []}

    entry = dict(decision.get("entry") or {})
    rollback_status = dict(entry.get("rollback_status") or {})
    declared = bool(rollback_status.get("rollback_declared"))
    executed = bool(rollback_status.get("rollback_executed"))
    verified = bool(rollback_status.get("rollback_verified"))
    warnings: list[str] = []
    allowed = True
    message = "rollback-allowed"

    if not declared and not executed and str(entry.get("tweak_origin") or "") != "legacy-curated":
        allowed = False
        message = "rollback-not-declared"
    else:
        if declared and not executed:
            warnings.append("rollback-declared-but-not-executed")
        if not verified:
            warnings.append("rollback-unverified")

    if override or warnings:
        append_mutation_audit_log(
            {
                "timestamp_utc": now_utc(),
                "action": "rollback",
                "candidate_id": candidate_id,
                "promotion_state": entry.get("promotion_state"),
                "override_requested": override,
                "override_used": bool(decision.get("override_used")),
                "override_reason": reason or "unspecified",
                "contributor_mode": contributor_mode,
                "allowed": allowed,
                "message": message,
                "warnings": warnings,
            },
            audit_path,
        )

    return {
        **decision,
        "allowed": allowed,
        "action": "rollback",
        "message": message,
        "warnings": warnings,
        "rollback_status": rollback_status,
    }


def core_cli_surface_status(cli_source_text: str | None = None) -> dict[str, Any]:
    if cli_source_text is None:
        cli_path = REPO_ROOT / "cli" / "Program.cs"
        cli_source_text = cli_path.read_text(encoding="utf-8") if cli_path.exists() else ""
    required_tokens = [
        "list-blocked",
        "show-stale",
        "show-revalidation-pending",
        "generate-regression-pack",
        "validate-batch",
        "apply",
        "rollback",
    ]
    missing = [token for token in required_tokens if token not in cli_source_text]
    return {
        "pass": not missing,
        "missing_commands": missing,
    }


def check_mcp_readiness(promotion_catalog: dict[str, Any] | None = None, cli_source_text: str | None = None) -> dict[str, Any]:
    promotion_catalog = promotion_catalog or load_promotion_gate_catalog()
    summary = promotion_catalog.get("summary") or {}
    entries = promotion_catalog.get("entries") or []
    promotion_counts = summary.get("promotion_state_counts") or {}
    blocker_counts = summary.get("blocker_counts") or {}
    version_aware = bool(
        entries
        and all(
            isinstance(entry, dict)
            and entry.get("supported_schema_versions")
            and entry.get("schema_compatibility_mode") in {"native", "compatibility"}
            for entry in entries[: min(10, len(entries))]
        )
    )
    methods_ready = all(callable(globals().get(name)) for name in MCP_METHOD_NAMES)
    cli_status = core_cli_surface_status(cli_source_text)
    revalidation_pending_count = int(promotion_counts.get("revalidation-pending", 0) or 0)
    stale_blocker_count = int(blocker_counts.get("stale-evidence", 0) or 0)
    status_checks = {
        "schema_version_stable": str(CURRENT_SCHEMA_VERSION).startswith("1."),
        "gate_evaluator_version_aware": version_aware,
        "has_promoted": int(promotion_counts.get("promoted", 0) or 0) >= 1,
        "has_blocked": int(promotion_counts.get("blocked", 0) or 0) >= 1,
        "revalidation_queue_accounted_for": revalidation_pending_count >= 1 or stale_blocker_count == 0,
        "core_cli_green": bool(cli_status.get("pass")),
        "ci_gate_metrics_green": int(summary.get("invalid_gate_entries", 0)) == 0,
        "mcp_methods_present": methods_ready,
    }
    ready = all(status_checks.values())
    return {
        "status": "MCP_READY" if ready else "MCP_BLOCKED",
        "checks": status_checks,
        "missing_cli_commands": cli_status.get("missing_commands"),
        "supported_methods": list(MCP_METHOD_NAMES),
        "promotion_state_counts": summary.get("promotion_state_counts") or {},
    }


def should_trigger_sibling_discovery(candidate: dict[str, Any]) -> bool:
    state = str(candidate.get("promotion_state") or candidate.get("state") or "").strip()
    return state in {"promotion-eligible", "promoted"}


def sibling_expansion_candidates(
    records: list[dict[str, Any]],
    audit_map: dict[str, dict[str, Any]] | None = None,
    gate_map: dict[str, dict[str, Any]] | None = None,
) -> list[dict[str, Any]]:
    audit_map = audit_map or {}
    gate_map = gate_map or {}
    discovered_paths: set[str] = set()
    emitted_ids: set[str] = set()
    results: list[dict[str, Any]] = []

    for record in records:
        seed = discovery_seed_from_record(record)
        key_path = str(seed.get("key_path") or "").strip()
        if key_path:
            discovered_paths.add(key_path)

    def emit_candidate(record: dict[str, Any], key_path: str, reason: str) -> None:
        if not key_path or key_path in discovered_paths:
            return
        candidate_id = f"sibling::{reason}::{key_path}"
        if candidate_id in emitted_ids:
            return
        emitted_ids.add(candidate_id)
        execution_context = default_execution_context()
        results.append(
            {
                "schema_version": CURRENT_SCHEMA_VERSION,
                "candidate_id": candidate_id,
                "discovery_source": "sibling_expansion",
                "discovery_reason": reason,
                "feature_area": record_feature_area(record),
                "key_path": key_path,
                "value_name": None,
                "value_type": None,
                "registry_clue": f"sibling expansion from {record.get('record_id') or record.get('tweak_id')}",
                "initial_confidence": "low",
                "seed_reference": f"research/records/{record.get('record_id') or record.get('tweak_id')}.json",
                "required_followup": "triage",
                "execution_context": execution_context,
                **build_sku_awareness_for_key_path(key_path, execution_context),
            }
        )

    for record in records:
        record_id = str(record.get("record_id") or record.get("tweak_id") or "")
        gate = gate_map.get(record_id) or derive_promotion_state(record, audit_map.get(record_id, {}))
        if not should_trigger_sibling_discovery(gate):
            continue
        key_path = str(primary_target(record).get("path") or "").strip()
        if not key_path:
            continue

        if key_path.startswith("HKLM\\"):
            emit_candidate(record, key_path.replace("HKLM\\", "HKCU\\", 1), "missing_hkcu_analog")
        elif key_path.startswith("HKCU\\"):
            emit_candidate(record, key_path.replace("HKCU\\", "HKLM\\", 1), "missing_hklm_analog")

        if "CurrentVersion" in key_path:
            emit_candidate(record, key_path.replace("CurrentVersion", "Policies"), "missing_policy_analog")
        if "\\Policies\\" in key_path:
            emit_candidate(record, key_path.replace("\\Policies\\", "\\CurrentVersion\\"), "missing_currentversion_analog")

        parent = "\\".join(part for part in key_path.split("\\")[:-1] if part)
        if parent and not key_path.endswith("\\Policies"):
            emit_candidate(record, f"{parent}\\Policies", "missing_sibling_branch")

        services_match = re.search(r"(HKLM\\System\\CurrentControlSet\\Services\\[^\\]+)(?:\\.*)?$", key_path, flags=re.IGNORECASE)
        if services_match:
            service_root = services_match.group(1)
            if not key_path.lower().endswith("\\parameters"):
                emit_candidate(record, f"{service_root}\\Parameters", "service_backed_branch")

        relation_match = re.search(r"HKCR\\(CLSID|AppID|Interface)\\(\{[^\\]+\})", key_path, flags=re.IGNORECASE)
        if relation_match:
            relation_kind = relation_match.group(1).upper()
            guid = relation_match.group(2)
            for sibling_kind in ("CLSID", "AppID", "Interface"):
                if sibling_kind == relation_kind:
                    continue
                emit_candidate(record, f"HKCR\\{sibling_kind}\\{guid}", "com_relation")

    return results


def build_queue_entry(candidate: dict[str, Any], state: str, blockers: list[str] | None = None, required_capabilities: list[str] | None = None, next_lane: str | None = None, linked_record_id: str | None = None, gate_result: dict[str, Any] | None = None) -> dict[str, Any]:
    build_sku = {
        "os_build": candidate.get("os_build"),
        "os_edition": candidate.get("os_edition"),
        "architecture": candidate.get("architecture"),
        "elevation_context": candidate.get("elevation_context"),
        "machine_user_scope": candidate.get("machine_user_scope"),
    }
    return {
        **candidate,
        "state": state,
        "blockers": blockers or [],
        "required_capabilities": required_capabilities or [],
        "next_lane": next_lane or candidate.get("required_followup") or "triage",
        "linked_record_id": linked_record_id,
        "last_evaluator_result": gate_result,
        "build_sku_awareness": build_sku,
        **build_sku,
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
        if not (lower_name.endswith(".etl") or lower_name.endswith(".etl.md")):
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
            parsed = parse_status.startswith("parsed")
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


def _normalize_registry_context_path(text: str | None) -> str | None:
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
    return normalized.rstrip("\\") if normalized not in {"HKLM\\", "HKCU\\", "HKCR\\", "HKU\\"} else normalized.rstrip("\\")


def _normalize_registry_path(text: str | None) -> str | None:
    normalized = _normalize_registry_context_path(text)
    if not normalized:
        return None
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


REGISTRY_XML_EVENT_ID_MAP = {
    "1": "RegCreateKey",
    "2": "OpenKey",
    "3": "RegDeleteKey",
    "4": "QueryKey",
    "5": "RegSetValue",
    "6": "RegDeleteValue",
    "7": "RegQueryValue",
    "8": "EnumerateKey",
    "9": "EnumerateValueKey",
    "10": "QueryMultipleValueKey",
    "11": "SetInformationKey",
    "12": "FlushKey",
    "13": "CloseKey",
    "14": "QuerySecurityKey",
}


def _xml_local_name(tag: str) -> str:
    return str(tag).rsplit("}", 1)[-1]


def _xml_text_or_attribute(element: ET.Element) -> str:
    text_value = "".join(element.itertext()).strip()
    if text_value:
        return text_value
    for attribute_name in ("Value", "value", "FormattedValue", "formattedValue", "Text", "text"):
        attribute_value = str(element.attrib.get(attribute_name) or "").strip()
        if attribute_value:
            return attribute_value
    return ""


def _normalize_registry_relative_path(text: str | None) -> str | None:
    normalized = _normalize_registry_context_path(text)
    if not normalized:
        return None
    normalized = normalized.strip("\\")
    return normalized or None


def _is_registry_path_rooted(text: str | None) -> bool:
    candidate = str(text or "").strip()
    if not candidate:
        return False
    upper = candidate.upper()
    return upper.startswith(
        (
            "HKLM\\",
            "HKCU\\",
            "HKCR\\",
            "HKU\\",
            "HKEY_",
            "\\REGISTRY\\",
        )
    )


def _combine_registry_path(base_path: str | None, relative_path: str | None) -> str | None:
    if not relative_path:
        return _normalize_registry_context_path(base_path)
    if _is_registry_path_rooted(relative_path):
        return _normalize_registry_context_path(relative_path)
    normalized_base = _normalize_registry_context_path(base_path)
    normalized_relative = _normalize_registry_relative_path(relative_path)
    if not normalized_base or not normalized_relative:
        return None
    return _normalize_registry_context_path(f"{normalized_base.rstrip(chr(92))}\\{normalized_relative}")


def _registry_parent_path(path: str | None) -> str | None:
    normalized = _normalize_registry_context_path(path)
    if not normalized or "\\" not in normalized:
        return None
    return normalized.rsplit("\\", 1)[0]


def _registry_operation_for_event(event_id: str | None, text_blob: str) -> str:
    mapped = REGISTRY_XML_EVENT_ID_MAP.get(str(event_id or ""))
    guessed = _guess_registry_operation(text_blob or "")
    return guessed or mapped or "registry-touch"


def _resolve_etl_feature_area_from_prefix_map(key_path: str | None) -> str | None:
    path = str(key_path or "").strip().replace("/", "\\")
    if not path:
        return None

    config = load_etl_feature_area_map()
    normalized = path.lower() if config.get("normalize_case") else path
    for entry in config.get("prefix_map") or []:
        prefix = str(entry.get("prefix") or "")
        if prefix and normalized.startswith(prefix):
            return str(entry.get("feature_area") or "") or None
    return None


def _etl_candidate_operation(candidate: dict[str, Any]) -> str:
    operation = str(candidate.get("operation") or "").strip()
    if operation:
        return operation
    registry_clue = str(candidate.get("registry_clue") or "").strip()
    if not registry_clue:
        return ""
    return registry_clue.split(" via ")[0].strip()


def _registry_key_path_depth(key_path: str | None) -> int:
    return len([segment for segment in str(key_path or "").split("\\") if segment])


def _etl_triage_reasons(candidate: dict[str, Any]) -> list[str]:
    config = load_etl_triage_rules()
    operation = _etl_candidate_operation(candidate)
    key_path = str(candidate.get("key_path") or "").strip()
    key_path_lower = key_path.lower()
    value_name = str(candidate.get("value_name") or "").strip()
    value_name_lower = value_name.lower()
    value_data = str(candidate.get("value_data") or "").strip()
    depth = _registry_key_path_depth(key_path)
    reasons: list[str] = []

    for entry in config.get("discard_rules") or []:
        rule = str(entry.get("rule") or "").strip()
        matched = False
        if rule == "open-close-only":
            matched = operation in {"OpenKey", "CloseKey", "RegOpenKey", "RegCloseKey"} and not value_name
        elif rule == "shallow-path":
            matched = depth <= 3
        elif rule == "package-registration-churn":
            matched = "packagedcom" in key_path_lower or "activatableclassid" in key_path_lower
        elif rule == "no-signal":
            matched = operation == "RegQueryValue" and not value_name and not value_data
        elif rule == "cryptography-oid-noise":
            matched = "cryptography\\oid" in key_path_lower
        elif rule == "windowsselfhost-fid-noise":
            matched = "windowsselfhost\\fids" in key_path_lower
        elif rule == "wbem-tracing-noise":
            matched = "wbem\\tracing\\providers" in key_path_lower
        elif rule == "taskcache-dynamic-noise":
            matched = "taskcache\\tasks" in key_path_lower and value_name_lower == "dynamicinfo"
        elif rule == "timestamp-value-noise":
            matched = value_name_lower in {"starttime", "lastdownloadtime", "lastwritetime", "lastaccesstime"}
        elif rule == "diagtrack-noise":
            matched = "diagtrack" in key_path_lower
        if matched:
            reasons.append(rule)

    return reasons


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
    configured = _resolve_etl_feature_area_from_prefix_map(key_path)
    if configured:
        return configured
    return fallback_source.replace("-", " ").title()


def extract_registry_touches_from_tracerpt_xml(xml_path: Path, provider_guid: str | None = None) -> list[dict[str, Any]]:
    if not xml_path.exists():
        return []

    provider_guid = str(provider_guid or "").strip("{}").lower()
    try:
        iterator = ET.iterparse(xml_path, events=("end",))
    except ET.ParseError:
        return []

    touches: list[dict[str, Any]] = []
    seen: set[tuple[str, str, str]] = set()
    open_key_paths: dict[str, str] = {}

    for _, event in iterator:
        if _xml_local_name(event.tag).lower() != "event":
            continue

        provider = ""
        event_id = ""
        process_id = ""
        key_path = None
        value_name = None
        event_data: dict[str, str] = {}

        for child in event:
            tag = _xml_local_name(child.tag).lower()
            if tag == "system":
                for system_child in child:
                    system_tag = _xml_local_name(system_child.tag).lower()
                    if system_tag == "provider":
                        provider = (
                            system_child.attrib.get("Guid")
                            or system_child.attrib.get("GUID")
                            or system_child.attrib.get("Name")
                            or provider
                        )
                    elif system_tag == "eventid":
                        event_id = _xml_text_or_attribute(system_child) or event_id
                    elif system_tag == "execution":
                        process_id = system_child.attrib.get("ProcessID") or process_id
            elif tag == "eventdata":
                for data_child in child:
                    if _xml_local_name(data_child.tag).lower() != "data":
                        continue
                    name = str(data_child.attrib.get("Name") or "").strip()
                    if not name:
                        continue
                    event_data[name] = _xml_text_or_attribute(data_child)

        key_object = (
            event_data.get("KeyObject")
            or event_data.get("KeyHandle")
            or event_data.get("Key")
        )
        base_object = event_data.get("BaseObject") or event_data.get("BaseHandle")
        key_name = event_data.get("KeyName") or event_data.get("PathName") or event_data.get("Path")
        base_name = event_data.get("BaseName")
        relative_name = event_data.get("RelativeName")
        value_name = event_data.get("ValueName") or None
        process_name = event_data.get("ProcessName") or event_data.get("Image") or (f"pid:{process_id}" if process_id else None)

        raw_excerpt = "; ".join(
            f"{name}={value}"
            for name, value in event_data.items()
            if value
        )[:400]
        text_blob = " ".join(
            part
            for part in [
                provider,
                event_id,
                REGISTRY_XML_EVENT_ID_MAP.get(event_id, ""),
                raw_excerpt,
            ]
            if part
        )

        context_key_path = None
        base_path = _normalize_registry_context_path(base_name) or _normalize_registry_context_path(key_name)
        inherited_base_path = base_path or (open_key_paths.get(base_object) if base_object else None)
        if event_id in {"1", "2"}:
            context_key_path = (
                _combine_registry_path(inherited_base_path, relative_name)
                or base_path
                or (open_key_paths.get(key_object) if key_object else None)
            )
            status_text = str(event_data.get("Status") or "").strip().lower()
            if context_key_path and key_object and status_text in {"", "0", "0x0"}:
                open_key_paths[key_object] = context_key_path
                if base_object and base_object not in open_key_paths:
                    parent_path = _registry_parent_path(context_key_path)
                    if parent_path:
                        open_key_paths[base_object] = parent_path
        else:
            context_key_path = (
                (open_key_paths.get(key_object) if key_object else None)
                or _normalize_registry_context_path(key_name)
                or _combine_registry_path(inherited_base_path, relative_name)
            )
            if not context_key_path:
                match = re.search(
                    r"(HKLM|HKCU|HKCR|HKU|HKEY_LOCAL_MACHINE|HKEY_CURRENT_USER|HKEY_CLASSES_ROOT|HKEY_USERS|\\\\REGISTRY\\\\MACHINE|\\\\REGISTRY\\\\USER)\\\\[^\r\n\"<>]+",
                    text_blob,
                    flags=re.IGNORECASE,
                )
                if match:
                    context_key_path = _normalize_registry_context_path(match.group(0))
        key_path = _normalize_registry_path(context_key_path)

        provider_match = False
        if provider_guid:
            provider_match = provider_guid in provider.lower().strip("{}")
        operation = _registry_operation_for_event(event_id, text_blob)
        if not key_path and not provider_match:
            if event_id == "13" and key_object:
                open_key_paths.pop(key_object, None)
            event.clear()
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
            "raw_excerpt": raw_excerpt or text_blob[:400],
        }
        dedupe_key = (
            str(touch.get("operation") or ""),
            str(touch.get("key_path") or ""),
            str(touch.get("value_name") or ""),
        )
        if dedupe_key in seen:
            if event_id == "13" and key_object:
                open_key_paths.pop(key_object, None)
            event.clear()
            continue
        seen.add(dedupe_key)
        touches.append(touch)
        if event_id == "13" and key_object:
            open_key_paths.pop(key_object, None)
        event.clear()

    return touches


def extract_registry_touches_from_sidecar_json(json_path: Path, provider_guid: str | None = None) -> list[dict[str, Any]]:
    if not json_path.exists():
        return []

    try:
        payload = json.loads(json_path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError):
        return []

    touches = payload.get("registry_touches") if isinstance(payload, dict) else payload
    if not isinstance(touches, list):
        return []

    provider_guid = str(provider_guid or "").strip("{}").lower()
    normalized: list[dict[str, Any]] = []
    seen: set[tuple[str, str, str, str]] = set()

    for raw in touches:
        if not isinstance(raw, dict):
            continue
        provider = str(raw.get("provider") or "")
        operation = _guess_registry_operation(str(raw.get("operation") or "")) or str(raw.get("operation") or "registry-touch")
        key_path = _normalize_registry_path(str(raw.get("key_path") or "")) or None
        value_name = str(raw.get("value_name") or "") or None
        process_name = str(raw.get("process_name") or "") or None
        process_id = str(raw.get("process_id") or "") or None
        event_id = str(raw.get("event_id") or "") or None
        provider_match = bool(provider_guid and provider_guid in provider.lower().strip("{}"))
        if not key_path and not provider_match:
            continue

        touch = {
            "event_id": event_id,
            "provider": provider or None,
            "provider_guid_matched": provider_match,
            "process_name": process_name,
            "process_id": process_id,
            "operation": operation or "registry-touch",
            "key_path": key_path,
            "value_name": value_name,
            "raw_excerpt": str(raw.get("raw_excerpt") or "")[:400],
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
        normalized.append(touch)

    return normalized


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
                "value_data": touch.get("value_data"),
                "operation": operation,
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
    legacy_xml_output = etl_path.with_suffix(".xml")
    touch_sidecar = etl_path.with_suffix(".etl.registry-touches.json")
    command = [tracerpt, str(etl_path), "-o", str(xml_output), "-of", "XML"]

    def resolve_existing_xml_sidecar() -> Path | None:
        if xml_output.exists():
            return xml_output
        if legacy_xml_output.exists():
            return legacy_xml_output
        return None

    def use_existing_xml_sidecar(note: str, xml_sidecar_path: Path) -> dict[str, Any]:
        output["status"] = "parsed-sidecar-xml"
        output["notes"].append(note)
        output["xml_output"] = normalize_repo_relative_path(str(xml_sidecar_path.relative_to(REPO_ROOT)))
        output["registry_touches"] = extract_registry_touches_from_tracerpt_xml(xml_sidecar_path, provider_guid=provider_guid)
        output["normalized_touch_count"] = len(output["registry_touches"])
        return output

    def use_existing_touch_sidecar(note: str) -> dict[str, Any]:
        output["status"] = "parsed-sidecar-json"
        output["notes"].append(note)
        output["touch_sidecar_output"] = normalize_repo_relative_path(str(touch_sidecar.relative_to(REPO_ROOT)))
        output["registry_touches"] = extract_registry_touches_from_sidecar_json(touch_sidecar, provider_guid=provider_guid)
        output["normalized_touch_count"] = len(output["registry_touches"])
        return output

    try:
        completed = subprocess.run(command, capture_output=True, text=True, check=False)
    except FileNotFoundError:
        existing_xml_sidecar = resolve_existing_xml_sidecar()
        if existing_xml_sidecar:
            return use_existing_xml_sidecar("tracerpt.exe is not available in this environment; parsed existing XML sidecar.", existing_xml_sidecar)
        if touch_sidecar.exists():
            return use_existing_touch_sidecar("tracerpt.exe is not available in this environment; parsed existing touch sidecar.")
        output["status"] = "parser-unavailable"
        output["notes"].append("tracerpt.exe is not available in this environment.")
        return output

    output["notes"].append((completed.stdout or completed.stderr or "").strip())
    if completed.returncode != 0:
        existing_xml_sidecar = resolve_existing_xml_sidecar()
        if existing_xml_sidecar:
            return use_existing_xml_sidecar("tracerpt returned a non-zero exit code; parsed existing XML sidecar.", existing_xml_sidecar)
        if touch_sidecar.exists():
            return use_existing_touch_sidecar("tracerpt returned a non-zero exit code; parsed existing touch sidecar.")
        output["status"] = "parser-failed"
        return output

    output["status"] = "parsed"
    output["xml_output"] = normalize_repo_relative_path(str(xml_output.relative_to(REPO_ROOT)))
    output["registry_touches"] = extract_registry_touches_from_tracerpt_xml(xml_output, provider_guid=provider_guid)
    output["normalized_touch_count"] = len(output["registry_touches"])
    return output
