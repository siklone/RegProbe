from __future__ import annotations

import hashlib
import importlib.util
import json
import sys
from collections import Counter, defaultdict
from pathlib import Path
from typing import Any

from research_v36_lib import CURRENT_SCHEMA_VERSION, append_discovery_candidates, default_execution_context, now_utc


REPO_ROOT = Path(__file__).resolve().parents[1]
TOOLS_ROOT = REPO_ROOT / "registry-research-framework" / "tools"


def _load_registry_sideeffect_diff():
    path = TOOLS_ROOT / "registry_sideeffect_diff.py"
    spec = importlib.util.spec_from_file_location("registry_sideeffect_diff_v36", path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules["registry_sideeffect_diff_v36"] = module
    spec.loader.exec_module(module)
    return module


REGISTRY_SIDEFFECT_DIFF = _load_registry_sideeffect_diff()


def parse_timestamp_ms(value: Any) -> int | None:
    if value is None:
        return None
    text = str(value).strip()
    if not text:
        return None
    if text.isdigit():
        return int(text)
    try:
        from datetime import datetime

        normalized = text.replace("Z", "+00:00")
        parsed = datetime.fromisoformat(normalized)
        return int(parsed.timestamp() * 1000)
    except ValueError:
        return None


def normalize_registry_path(text: str | None) -> str | None:
    if not text:
        return None
    normalized = str(text).strip().replace("/", "\\")
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


def normalize_operation_family(operation: str | None) -> str:
    lowered = str(operation or "").strip().lower()
    if any(token in lowered for token in ("setvalue", "write", "set key value", "regsetvalue")):
        return "write"
    if any(token in lowered for token in ("create", "regcreatekey")):
        return "create"
    if any(token in lowered for token in ("delete", "regdelete")):
        return "delete"
    if any(token in lowered for token in ("enum", "querykey", "enumerate")):
        return "enumerate"
    if any(token in lowered for token in ("query", "open", "read", "close", "regopenkey")):
        return "read"
    return "other"


def value_extraction_confidence(event: dict[str, Any]) -> str:
    if any(event.get(key) not in (None, "", []) for key in ("value_data", "data", "after_value", "before_value")):
        return "high"
    if event.get("value_name"):
        return "medium"
    if event.get("key_path"):
        return "low"
    return "none"


def normalize_runtime_registry_event(event: dict[str, Any]) -> dict[str, Any] | None:
    key_path = normalize_registry_path(
        event.get("key_path")
        or event.get("path")
        or event.get("registry_path")
        or event.get("KeyPath")
    )
    raw_operation = event.get("operation") or event.get("Operation") or event.get("event_name")
    normalized = {
        "timestamp_ms": parse_timestamp_ms(event.get("timestamp_ms") or event.get("timestamp_utc") or event.get("timestamp")),
        "timestamp_utc": event.get("timestamp_utc") or event.get("timestamp"),
        "process_name": event.get("process_name") or event.get("ProcessName"),
        "process_id": str(event.get("process_id") or event.get("ProcessId") or event.get("pid") or "") or None,
        "parent_process_id": str(event.get("parent_process_id") or event.get("ParentProcessId") or event.get("ppid") or "") or None,
        "key_path": key_path,
        "value_name": event.get("value_name") or event.get("ValueName"),
        "value_data": event.get("value_data") or event.get("data"),
        "result": event.get("result") or event.get("Result"),
        "raw_operation": raw_operation,
        "operation_family": normalize_operation_family(str(raw_operation or "")),
        "source": event.get("source") or event.get("tool") or "runtime",
    }
    normalized["value_extraction_confidence"] = value_extraction_confidence(normalized)
    if not normalized["key_path"]:
        return None
    return normalized


def filter_trigger_window(
    events: list[dict[str, Any]],
    trigger_start_ms: int | None = None,
    trigger_end_ms: int | None = None,
    slack_ms: int = 1500,
) -> list[dict[str, Any]]:
    if trigger_start_ms is None and trigger_end_ms is None:
        return list(events)
    filtered: list[dict[str, Any]] = []
    for event in events:
        ts = event.get("timestamp_ms")
        if ts is None:
            continue
        if trigger_start_ms is not None and ts < trigger_start_ms - slack_ms:
            continue
        if trigger_end_ms is not None and ts > trigger_end_ms + slack_ms:
            continue
        filtered.append(event)
    return filtered


def collapse_noisy_bursts(events: list[dict[str, Any]], burst_window_ms: int = 250) -> list[dict[str, Any]]:
    sorted_events = sorted(events, key=lambda item: (item.get("timestamp_ms") is None, item.get("timestamp_ms") or 0))
    collapsed: list[dict[str, Any]] = []
    for event in sorted_events:
        if not collapsed:
            item = dict(event)
            item["burst_count"] = 1
            collapsed.append(item)
            continue
        previous = collapsed[-1]
        same_key = (
            previous.get("key_path") == event.get("key_path")
            and previous.get("value_name") == event.get("value_name")
            and previous.get("operation_family") == event.get("operation_family")
            and previous.get("process_name") == event.get("process_name")
            and previous.get("value_data") == event.get("value_data")
        )
        previous_ts = previous.get("timestamp_ms")
        current_ts = event.get("timestamp_ms")
        within_window = (
            previous_ts is not None
            and current_ts is not None
            and (current_ts - previous_ts) <= burst_window_ms
        )
        if same_key and within_window:
            previous["burst_count"] = int(previous.get("burst_count") or 1) + 1
            previous["last_timestamp_ms"] = current_ts
            continue
        item = dict(event)
        item["burst_count"] = 1
        collapsed.append(item)
    return collapsed


def aggregate_same_path(events: list[dict[str, Any]]) -> list[dict[str, Any]]:
    by_path: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for event in events:
        by_path[str(event.get("key_path"))].append(event)

    aggregates: list[dict[str, Any]] = []
    for key_path, group in sorted(by_path.items()):
        families = Counter(str(item.get("operation_family") or "other") for item in group)
        processes = sorted({str(item.get("process_name") or "unknown") for item in group})
        process_tree = []
        for item in group:
            parent = item.get("parent_process_id")
            pid = item.get("process_id")
            pname = item.get("process_name")
            if pid or parent or pname:
                process_tree.append(
                    {
                        "process_name": pname,
                        "process_id": pid,
                        "parent_process_id": parent,
                    }
                )
        aggregates.append(
            {
                "key_path": key_path,
                "event_count": len(group),
                "operation_families": dict(families),
                "processes": processes,
                "process_tree": process_tree,
                "value_names": sorted({str(item.get("value_name")) for item in group if item.get("value_name")}),
            }
        )
    return aggregates


def _feature_area_from_key_path(key_path: str) -> str:
    lowered = key_path.lower()
    if "\\control\\power" in lowered:
        return "Power"
    if "\\policies\\system" in lowered or "\\currentversion\\policies\\system" in lowered:
        return "System"
    if "\\explorer" in lowered:
        return "Explorer"
    if "\\multimedia\\audio" in lowered or "\\audio" in lowered:
        return "Audio"
    if "\\windows defender" in lowered or "\\defender" in lowered:
        return "Security"
    return "Runtime"


def build_discovery_candidates_from_runtime_events(
    events: list[dict[str, Any]],
    *,
    trace_source: str,
    seed_reference: str,
    backend_id: str = "rai-linux-vm",
) -> list[dict[str, Any]]:
    candidates: list[dict[str, Any]] = []
    for event in events:
        digest = hashlib.sha1(
            f"{trace_source}|{event.get('key_path')}|{event.get('value_name')}|{event.get('operation_family')}".encode("utf-8")
        ).hexdigest()[:16]
        candidates.append(
            {
                "schema_version": CURRENT_SCHEMA_VERSION,
                "candidate_id": f"{trace_source}::{digest}",
                "discovery_source": f"{trace_source}-registry-touch",
                "discovery_reason": "runtime_registry_touch",
                "feature_area": _feature_area_from_key_path(str(event.get("key_path") or "")),
                "key_path": event.get("key_path"),
                "value_name": event.get("value_name"),
                "value_type": None,
                "registry_clue": f"{event.get('operation_family')} via {event.get('process_name') or 'unknown-process'}",
                "initial_confidence": event.get("value_extraction_confidence") or "low",
                "seed_reference": seed_reference,
                "required_followup": "triage",
                "execution_context": default_execution_context(backend_id),
            }
        )
    return candidates


def normalize_runtime_registry_events(
    events: list[dict[str, Any]],
    *,
    mode: str,
    trace_source: str,
    seed_reference: str,
    trigger_start_ms: int | None = None,
    trigger_end_ms: int | None = None,
    burst_window_ms: int = 250,
    backend_id: str = "rai-linux-vm",
    append_discovery: bool = False,
) -> dict[str, Any]:
    normalized = [item for item in (normalize_runtime_registry_event(event) for event in events) if item]
    windowed = filter_trigger_window(normalized, trigger_start_ms=trigger_start_ms, trigger_end_ms=trigger_end_ms)
    collapsed = collapse_noisy_bursts(windowed, burst_window_ms=burst_window_ms)
    aggregates = aggregate_same_path(collapsed)
    family_counts = Counter(str(item.get("operation_family") or "other") for item in collapsed)
    confidence_counts = Counter(str(item.get("value_extraction_confidence") or "none") for item in collapsed)

    payload = {
        "schema_version": CURRENT_SCHEMA_VERSION,
        "generated_utc": now_utc(),
        "mode": mode,
        "trace_source": trace_source,
        "seed_reference": seed_reference,
        "summary": {
            "raw_event_count": len(events),
            "normalized_event_count": len(normalized),
            "windowed_event_count": len(windowed),
            "collapsed_event_count": len(collapsed),
            "aggregated_path_count": len(aggregates),
            "operation_family_counts": dict(family_counts),
            "value_extraction_confidence_counts": dict(confidence_counts),
        },
        "normalized_events": collapsed,
        "aggregated_paths": aggregates,
    }

    if mode == "discovery":
        candidates = build_discovery_candidates_from_runtime_events(
            collapsed,
            trace_source=trace_source,
            seed_reference=seed_reference,
            backend_id=backend_id,
        )
        payload["discovery_candidates"] = candidates
        payload["appended_event_count"] = append_discovery_candidates(candidates) if append_discovery else 0
    else:
        payload["runtime_evidence"] = {
            "executed": True,
            "format": "normalized-runtime-registry",
            "trace_source": trace_source,
            "event_count": len(collapsed),
            "operation_family_counts": dict(family_counts),
            "value_extraction_confidence_counts": dict(confidence_counts),
            "aggregated_paths": aggregates,
        }
    return payload


def _normalize_snapshot(snapshot: dict[str, Any], root_key: str = "__root__") -> tuple[dict[str, dict[str, Any]], set[str]]:
    keys: dict[str, dict[str, Any]] = {}
    for key, value in snapshot.items():
        if isinstance(value, dict):
            key_path = str(key)
            keys[key_path] = {str(name): data for name, data in value.items()}
        else:
            keys.setdefault(root_key, {})[str(key)] = value
    return keys, set(keys)


def build_structured_state_diff_from_snapshots(
    baseline_state: dict[str, Any],
    candidate_state: dict[str, Any],
) -> dict[str, Any]:
    baseline_keys_map, baseline_keys = _normalize_snapshot(baseline_state)
    candidate_keys_map, candidate_keys = _normalize_snapshot(candidate_state)

    key_added = [{"key_path": key_path} for key_path in sorted(candidate_keys - baseline_keys)]
    key_deleted = [{"key_path": key_path} for key_path in sorted(baseline_keys - candidate_keys)]
    value_added: list[dict[str, Any]] = []
    value_deleted: list[dict[str, Any]] = []
    value_changed: list[dict[str, Any]] = []

    for key_path in sorted(baseline_keys | candidate_keys):
        before_values = baseline_keys_map.get(key_path, {})
        after_values = candidate_keys_map.get(key_path, {})
        for value_name in sorted(set(before_values) | set(after_values)):
            before_value = before_values.get(value_name)
            after_value = after_values.get(value_name)
            if value_name not in before_values:
                value_added.append({"key_path": key_path, "value_name": value_name, "after_value": after_value})
            elif value_name not in after_values:
                value_deleted.append({"key_path": key_path, "value_name": value_name, "before_value": before_value})
            elif before_value != after_value:
                value_changed.append(
                    {
                        "key_path": key_path,
                        "value_name": value_name,
                        "before_value": before_value,
                        "after_value": after_value,
                    }
                )

    return {
        "key_added": key_added,
        "key_deleted": key_deleted,
        "value_added": value_added,
        "value_deleted": value_deleted,
        "value_changed": value_changed,
        "summary_counts": {
            "key_added": len(key_added),
            "key_deleted": len(key_deleted),
            "value_added": len(value_added),
            "value_deleted": len(value_deleted),
            "value_changed": len(value_changed),
        },
    }


def build_structured_state_diff_from_state_payload(payload: dict[str, Any]) -> dict[str, Any]:
    baseline_values = payload.get("baseline_values")
    candidate_values = payload.get("candidate_values")
    if not isinstance(baseline_values, dict) or not isinstance(candidate_values, dict):
        raise ValueError("State payload must contain baseline_values and candidate_values dictionaries.")
    return build_structured_state_diff_from_snapshots(baseline_values, candidate_values)


def build_structured_state_diff_from_registry_files(before_path: Path, after_path: Path) -> dict[str, Any]:
    payload = REGISTRY_SIDEFFECT_DIFF.build_diff_payload(before_path, after_path)
    sections = payload.get("sections") or {}
    return {
        "key_added": [
            {"key_path": item.get("KeyPath")}
            for item in sections.get("added_keys") or []
        ],
        "key_deleted": [
            {"key_path": item.get("KeyPath")}
            for item in sections.get("removed_keys") or []
        ],
        "value_added": [
            {"key_path": item.get("KeyPath"), "value_name": item.get("ValueName"), "after_value": item.get("DataText")}
            for item in sections.get("added_values") or []
        ],
        "value_deleted": [
            {"key_path": item.get("KeyPath"), "value_name": item.get("ValueName"), "before_value": item.get("DataText")}
            for item in sections.get("removed_values") or []
        ],
        "value_changed": [
            {
                "key_path": item.get("KeyPath"),
                "value_name": item.get("ValueName"),
                "before_value": item.get("BeforeData"),
                "after_value": item.get("AfterData"),
            }
            for item in sections.get("modified_values") or []
        ],
        "summary_counts": {
            "key_added": int((payload.get("summary_counts") or {}).get("added_keys", 0)),
            "key_deleted": int((payload.get("summary_counts") or {}).get("removed_keys", 0)),
            "value_added": int((payload.get("summary_counts") or {}).get("added_values", 0)),
            "value_deleted": int((payload.get("summary_counts") or {}).get("removed_values", 0)),
            "value_changed": int((payload.get("summary_counts") or {}).get("modified_values", 0)),
        },
    }


def evaluate_rollback_verification(
    *,
    baseline_state: dict[str, Any],
    candidate_state: dict[str, Any],
    restored_state: dict[str, Any] | None,
    rollback_declared: bool,
    rollback_executed: bool,
    verification_method: str = "state_diff",
) -> dict[str, Any]:
    apply_diff = build_structured_state_diff_from_snapshots(baseline_state, candidate_state)
    state_changed = any(apply_diff["summary_counts"].values())
    if not rollback_executed:
        return {
            "rollback_declared": rollback_declared,
            "rollback_executed": False,
            "rollback_verified": False,
            "rollback_verification_method": verification_method,
            "rollback_failure_reason": "rollback-not-executed",
            "state_changed": state_changed,
            "apply_diff": apply_diff,
            "restore_diff": None,
        }

    if restored_state is None:
        return {
            "rollback_declared": rollback_declared,
            "rollback_executed": True,
            "rollback_verified": False,
            "rollback_verification_method": verification_method,
            "rollback_failure_reason": "missing-restored-state",
            "state_changed": state_changed,
            "apply_diff": apply_diff,
            "restore_diff": None,
        }

    restore_diff = build_structured_state_diff_from_snapshots(baseline_state, restored_state)
    verified = not any(restore_diff["summary_counts"].values())
    return {
        "rollback_declared": rollback_declared,
        "rollback_executed": True,
        "rollback_verified": verified,
        "rollback_verification_method": verification_method,
        "rollback_failure_reason": None if verified else "rollback-state-mismatch",
        "state_changed": state_changed,
        "apply_diff": apply_diff,
        "restore_diff": restore_diff,
    }
