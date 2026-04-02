#!/usr/bin/env python3
from __future__ import annotations

import argparse
import csv
import json
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"
if str(SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_ROOT))

from research_path_lib import V31_EVIDENCE_ROOT, normalize_reference_text  # noqa: E402
from artifact_metadata_lib import build_artifact_metadata  # noqa: E402
from behavior_stats_lib import summarize_before_after  # noqa: E402
from evidence_class_lib import (  # noqa: E402
    has_benchmark_evidence,
    has_ghidra_evidence,
    static_tool_block,
    static_tool_counts_as_evidence,
    has_official_evidence,
    has_procmon_evidence,
    has_reboot_evidence,
    has_wpr_evidence,
    evidence_items,
    evidence_kind,
)
from wave2_research_lib import (  # noqa: E402
    anticheat_risk_for_tweak,
    dependency_entry_for_tweak,
    evidence_freshness,
    interaction_groups_for_tweak,
    reproducibility_manifest,
)

RESEARCH_ROOT = REPO_ROOT / "research"
RECORDS_DIR = RESEARCH_ROOT / "records"
AUDIT_PATH = RESEARCH_ROOT / "evidence-audit.json"
DEFAULT_QUEUE_PATH = REPO_ROOT / "registry-research-framework" / "audit" / "re-audit-queue.csv"
RUNNER_COVERAGE_POLICY_PATH = REPO_ROOT / "registry-research-framework" / "config" / "runner-coverage-policy.json"
PIPELINE_VERSION = "v3.2"
PHYSICAL_CAPTURE_SUFFIXES = {".etl", ".pml", ".json", ".csv"}


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def load_json(path: Path) -> Any:
    with path.open("r", encoding="utf-8-sig") as handle:
        return json.load(handle)


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(payload, handle, ensure_ascii=False, indent=2)
        handle.write("\n")


def write_text(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content.rstrip() + "\n", encoding="utf-8", newline="\n")


def load_text_if_exists(path: Path) -> str | None:
    if not path.exists():
        return None
    return path.read_text(encoding="utf-8-sig")


def load_record(tweak_id: str) -> dict[str, Any]:
    exact = RECORDS_DIR / f"{tweak_id}.json"
    review = RECORDS_DIR / f"{tweak_id}.review.json"
    for candidate in (exact, review):
        if candidate.exists():
            return load_json(candidate)
    raise FileNotFoundError(f"Missing record for {tweak_id}")


def load_audit_entry(tweak_id: str) -> dict[str, Any]:
    payload = load_json(AUDIT_PATH)
    for entry in payload.get("entries", []):
        if str(entry.get("tweak_id") or "") == tweak_id:
            return entry
    raise KeyError(f"Missing audit entry for {tweak_id}")


def evidence_dir(tweak_id: str) -> Path:
    return V31_EVIDENCE_ROOT / tweak_id


def load_json_if_exists(path: Path) -> Any | None:
    if not path.exists():
        return None
    return load_json(path)


def repo_relative_text(value: Any) -> str | None:
    if not isinstance(value, str):
        return None
    text = value.strip()
    if not text:
        return None
    normalized = text.replace("\\", "/").lstrip("/")
    if normalized.startswith(("evidence/", "research/", "registry-research-framework/")):
        return normalized
    return None


def extract_repo_ref_from_text(value: Any) -> str | None:
    if not isinstance(value, str):
        return None
    for line in reversed(value.splitlines()):
        candidate = repo_relative_text(line.strip())
        if candidate:
            return candidate
    return None


def repo_path_from_ref(value: Any) -> Path | None:
    repo_ref = repo_relative_text(value)
    if not repo_ref:
        return None
    return REPO_ROOT / Path(repo_ref)


def load_lane_manifest(tweak_id: str, filename: str) -> dict[str, Any] | None:
    payload = load_json_if_exists(evidence_dir(tweak_id) / filename)
    return payload if isinstance(payload, dict) else None


def load_lane_result(manifest: dict[str, Any] | None) -> dict[str, Any] | None:
    if not manifest:
        return None
    result_ref = repo_relative_text(manifest.get("result_ref"))
    if result_ref is None:
        log_path = repo_path_from_ref(manifest.get("log_file"))
        log_text = load_text_if_exists(log_path) if log_path else None
        result_ref = extract_repo_ref_from_text(log_text)
    result_path = repo_path_from_ref(result_ref)
    if result_path is None:
        return None
    payload = load_json_if_exists(result_path)
    return payload if isinstance(payload, dict) else None


def load_runner_coverage_policy() -> dict[str, Any]:
    payload = load_json_if_exists(RUNNER_COVERAGE_POLICY_PATH)
    return payload if isinstance(payload, dict) else {}


def runner_required(audit: dict[str, Any]) -> bool:
    policy = load_runner_coverage_policy()
    required_layers = set(policy.get("required_layers") or [])
    if str(audit.get("suspected_layer") or "") in required_layers:
        return True
    if bool(policy.get("required_when_boot_phase_relevant")) and bool(audit.get("boot_phase_relevant")):
        return True
    return False


def normalize_capture_artifact(item: Any) -> dict[str, Any] | None:
    if isinstance(item, str):
        candidate = repo_relative_text(item)
        if not candidate:
            return None
        item = {"path": candidate}
    if not isinstance(item, dict):
        return None
    repo_ref = repo_relative_text(item.get("path") or item.get("filename"))
    if not repo_ref:
        return None
    repo_path = repo_path_from_ref(repo_ref)
    suffix = repo_path.suffix.lower() if repo_path else Path(repo_ref).suffix.lower()
    return {
        "path": repo_ref,
        "exists": bool(repo_path and repo_path.exists()),
        "placeholder": bool(str(item.get("placeholder")).lower() == "true") or suffix == ".md",
        "kind": item.get("kind"),
    }


def manifest_capture_artifacts(manifest: dict[str, Any] | None) -> list[dict[str, Any]]:
    if not manifest:
        return []
    normalized: list[dict[str, Any]] = []
    seen: set[str] = set()
    for raw in manifest.get("capture_artifacts") or []:
        item = normalize_capture_artifact(raw)
        if item and item["path"] not in seen:
            seen.add(item["path"])
            normalized.append(item)
    for key in ("result_ref", "log_file"):
        repo_ref = repo_relative_text(manifest.get(key))
        if repo_ref and repo_ref not in seen:
            seen.add(repo_ref)
            item = normalize_capture_artifact({"path": repo_ref, "kind": key})
            if item:
                normalized.append(item)
    return normalized


def lane_capture_status(manifest: dict[str, Any] | None) -> str:
    if not manifest:
        return "missing-required-runner"
    status = str(manifest.get("status") or "").strip().lower()
    capture_status = str(manifest.get("capture_status") or "").strip().lower()
    if status in {"", "staged"} or capture_status == "staged":
        return "staged-without-capture"
    physical_capture = any(
        artifact["exists"]
        and not artifact["placeholder"]
        and Path(artifact["path"]).suffix.lower() in PHYSICAL_CAPTURE_SUFFIXES
        for artifact in manifest_capture_artifacts(manifest)
    )
    if physical_capture:
        return "runner-ok" if status in {"runner-ok", "invoked-runner", "captured"} else status
    if status in {"runner-ok", "invoked-runner"} or capture_status in {"captured", "missing-capture"}:
        return "missing-capture"
    return status or capture_status or "missing-capture"


def lane_executed(manifest: dict[str, Any] | None) -> bool:
    return lane_capture_status(manifest) == "runner-ok"


def lane_repo_ref(manifest: dict[str, Any] | None) -> str | None:
    if not manifest:
        return None
    for key in ("result_ref", "log_file"):
        candidate = repo_relative_text(manifest.get(key))
        if candidate:
            return candidate
    return None


def normalized_location(item: dict[str, Any] | None, title: str) -> str | None:
    if not item:
        return None
    location = str(item.get("location") or "").strip()
    if not location:
        return None
    return normalize_reference_text(location, title=title)


def first_target(record: dict[str, Any]) -> dict[str, Any]:
    targets = record.get("setting", {}).get("targets", []) or []
    return targets[0] if targets else {}


def baseline_value(record: dict[str, Any]) -> Any:
    defaults = record.get("windows_defaults", []) or []
    if not defaults:
        return None
    states = defaults[0].get("states", []) or []
    return states[0].get("value") if states else None


def candidate_value(record: dict[str, Any]) -> Any:
    profiles = record.get("recommended_profiles", []) or []
    preferred = None
    for profile in profiles:
        if profile.get("apply_allowed") is True:
            preferred = profile
            break
    if preferred is None:
        for profile in profiles:
            if str(profile.get("profile_id") or "") == "current-app-profile":
                preferred = profile
                break
    if preferred is None and profiles:
        preferred = profiles[0]
    if preferred is None:
        return None
    states = preferred.get("states", []) or []
    return states[0].get("value") if states else None


def collect_evidence(record: dict[str, Any], kinds: set[str]) -> list[dict[str, Any]]:
    return [
        item
        for item in (record.get("evidence") or [])
        if isinstance(item, dict) and str(item.get("kind") or "") in kinds
    ]


def build_metadata(record: dict[str, Any], audit: dict[str, Any]) -> dict[str, Any]:
    target = first_target(record)
    provenance = record.get("provenance") or {}
    repro_manifest = reproducibility_manifest()
    return {
        "$schema": "registry-evidence-v3.2/metadata",
        "tweak_id": record.get("tweak_id"),
        "key_path": target.get("path"),
        "value_name": target.get("value_name"),
        "value_type": target.get("value_type"),
        "value_data_before": baseline_value(record),
        "value_data_after": candidate_value(record),
        "enrichment": {
            "hive": str(target.get("path") or "").split("\\", 2)[0] if target.get("path") else None,
            "suspected_layer": audit.get("suspected_layer"),
            "boot_phase_relevant": audit.get("boot_phase_relevant"),
            "acl_owner": None,
            "acl_sddl": None,
            "acl_user_readable": None,
            "source": "official_doc" if audit.get("official_doc_exists") else "repo_research",
            "official_doc_exists": audit.get("official_doc_exists"),
            "reactos_reference": False,
            "geoff_chappell_reference": False,
            "pdb_symbol_match": None,
            "source_repositories": provenance.get("source_repositories") or [],
        },
        "freshness": evidence_freshness(record, repro_manifest),
        "reproducibility_manifest": {
            "path": "registry-research-framework/config/reproducibility-manifest.json",
            "baseline_id": repro_manifest.get("baseline_id"),
            "vm_name": repro_manifest.get("vm_name"),
            "os_build": repro_manifest.get("os_build"),
            "baseline_snapshot": repro_manifest.get("baseline_snapshot"),
        },
        "doc_source": normalize_doc_source(record),
    }


def normalize_branch_analysis_entry(entry: dict[str, Any]) -> dict[str, Any]:
    if not isinstance(entry, dict):
        return {
            "condition": None,
            "register_focus": [],
            "flag_focus": [],
            "compare_condition": "unclear",
            "jump_condition": "unclear",
            "value_map": "unclear",
            "branch_effect": "unclear",
            "stack_summary": "unclear",
            "exception_review_required": False,
            "exception_reason": "none",
            "heuristic_score": 0,
            "heuristic_reasons": [],
            "context_before": [],
            "context_after": [],
            "branch_snippet": [],
            "effect_summary": None,
            "unclear": True,
        }

    branch_snippet = [str(line) for line in (entry.get("branch_snippet") or []) if isinstance(line, str)]
    compare_condition = str(entry.get("compare_condition") or entry.get("condition") or "unclear")
    jump_condition = str(entry.get("jump_condition") or "unclear")
    branch_effect = str(entry.get("branch_effect") or entry.get("effect_summary") or "unclear")
    stack_summary = str(entry.get("stack_summary") or "unclear")
    heuristic_score = entry.get("heuristic_score")
    if not isinstance(heuristic_score, int):
        heuristic_score = 0

    return {
        "condition": str(entry.get("condition") or compare_condition),
        "register_focus": [str(item) for item in (entry.get("register_focus") or []) if item is not None],
        "flag_focus": [str(item) for item in (entry.get("flag_focus") or []) if item is not None],
        "compare_condition": compare_condition,
        "jump_condition": jump_condition,
        "value_map": str(entry.get("value_map") or "unclear"),
        "branch_effect": branch_effect,
        "stack_summary": stack_summary,
        "exception_review_required": bool(entry.get("exception_review_required")),
        "exception_reason": str(entry.get("exception_reason") or "none"),
        "heuristic_score": heuristic_score,
        "heuristic_reasons": [str(item) for item in (entry.get("heuristic_reasons") or []) if item is not None],
        "context_before": [str(line) for line in (entry.get("context_before") or []) if isinstance(line, str)],
        "context_after": [str(line) for line in (entry.get("context_after") or []) if isinstance(line, str)],
        "branch_snippet": branch_snippet,
        "effect_summary": str(entry.get("effect_summary") or branch_effect or ""),
        "unclear": bool(entry.get("unclear", False)),
    }


def normalize_doc_source(record: dict[str, Any]) -> dict[str, Any]:
    payload = record.get("doc_source")
    block = payload if isinstance(payload, dict) else {}
    source_origin = str(block.get("source_origin") or "").strip()
    if not source_origin and has_official_evidence(record):
        source_origin = "microsoft-docs"

    binary_semantics_source = str(block.get("binary_semantics_source") or "").strip()
    if not binary_semantics_source:
        if static_tool_counts_as_evidence(static_tool_block(record, "ghidra")) or static_tool_counts_as_evidence(static_tool_block(record, "ida")):
            binary_semantics_source = "static-analysis"
        elif has_procmon_evidence(record):
            binary_semantics_source = "runtime-procmon"
        elif has_wpr_evidence(record):
            binary_semantics_source = "runtime-etw"
        else:
            binary_semantics_source = "none"

    policy_or_intent_only = block.get("policy_or_intent_only")
    if policy_or_intent_only is None:
        policy_or_intent_only = source_origin in {"microsoft-docs", "repo-doc"}

    docs_do_not_prove_binary_semantics = block.get("docs_do_not_prove_binary_semantics")
    if docs_do_not_prove_binary_semantics is None:
        docs_do_not_prove_binary_semantics = source_origin in {"microsoft-docs", "repo-doc", "mixed"}

    return {
        "source_origin": source_origin or None,
        "policy_or_intent_only": bool(policy_or_intent_only),
        "docs_do_not_prove_binary_semantics": bool(docs_do_not_prove_binary_semantics),
        "binary_semantics_source": binary_semantics_source,
        "primary_contract": block.get("primary_contract"),
        "repo_interpretation": block.get("repo_interpretation"),
        "notes": block.get("notes"),
    }


def normalize_static_tool(record: dict[str, Any], key: str, fallback_item: dict[str, Any] | None) -> dict[str, Any]:
    payload = ((record.get("static_analysis") or {}).get(key) or {}) if isinstance(record.get("static_analysis"), dict) else {}
    if isinstance(payload, dict) and payload:
        branch_analysis = [normalize_branch_analysis_entry(item) for item in (payload.get("branch_analysis") or []) if isinstance(item, dict)]
        heuristic_score = payload.get("heuristic_score")
        if heuristic_score is None and branch_analysis:
            heuristic_score = max((item.get("heuristic_score") or 0) for item in branch_analysis)
        heuristic_reasons = payload.get("heuristic_reasons") or []
        if not heuristic_reasons and branch_analysis:
            merged: list[str] = []
            for item in branch_analysis:
                for reason in item.get("heuristic_reasons") or []:
                    if reason not in merged:
                        merged.append(reason)
            heuristic_reasons = merged

        function_confidence = payload.get("function_confidence")
        if function_confidence is None:
            if branch_analysis and any(item.get("unclear") is not True for item in branch_analysis):
                function_confidence = "symbolized_branch" if bool(payload.get("pdb_loaded")) else "string_only_review"
            elif payload.get("executed") is True:
                function_confidence = "string_only_review"

        unclear = payload.get("unclear")
        if unclear is None:
            unclear = any(item.get("unclear") for item in branch_analysis) if branch_analysis else False

        return {
            "executed": payload.get("executed", False),
            "status": payload.get("status", "not-run"),
            "pdb_loaded": payload.get("pdb_loaded", False),
            "pdb_source": payload.get("pdb_source"),
            "binary": payload.get("binary"),
            "function_name": payload.get("function_name"),
            "function_source": payload.get("function_source"),
            "function_confidence": function_confidence,
            "branch_analysis": branch_analysis,
            "heuristic_score": heuristic_score,
            "heuristic_reasons": [str(item) for item in heuristic_reasons if item is not None],
            "effect_summary": payload.get("effect_summary"),
            "unclear": bool(unclear),
            "artifact_path": payload.get("artifact_path"),
        }

    if key == "ghidra":
        return {
            "executed": fallback_item is not None,
            "status": "legacy-evidence" if fallback_item else "not-run",
            "pdb_loaded": False,
            "pdb_source": None,
            "binary": None,
            "function_name": None,
            "function_source": None,
            "function_confidence": "string_only_review" if fallback_item else None,
            "branch_analysis": [],
            "heuristic_score": None,
            "heuristic_reasons": [],
            "effect_summary": fallback_item.get("summary") if fallback_item else None,
            "unclear": True if fallback_item else False,
            "artifact_path": normalized_location(fallback_item, "Ghidra output"),
        }

    return {
        "executed": False,
        "status": "not-run",
        "pdb_loaded": False,
        "pdb_source": None,
        "binary": None,
        "function_name": None,
        "function_source": None,
        "function_confidence": None,
        "branch_analysis": [],
        "heuristic_score": None,
        "heuristic_reasons": [],
        "effect_summary": None,
        "unclear": False,
        "artifact_path": None,
    }


def normalize_cross_verification(record: dict[str, Any]) -> dict[str, Any]:
    payload = record.get("cross_verification")
    if isinstance(payload, dict) and payload:
        return {
            "executed": payload.get("executed", False),
            "functions_match": payload.get("functions_match"),
            "branches_match": payload.get("branches_match"),
            "ghidra_function": payload.get("ghidra_function"),
            "ida_function": payload.get("ida_function"),
            "ghidra_function_confidence": payload.get("ghidra_function_confidence"),
            "ida_function_confidence": payload.get("ida_function_confidence"),
            "ghidra_value_map": payload.get("ghidra_value_map"),
            "ida_value_map": payload.get("ida_value_map"),
            "status": payload.get("status", "insufficient"),
            "verdict": payload.get("verdict", "review-only"),
            "parity_summary": payload.get("parity_summary"),
            "confidence": payload.get("confidence", "low"),
            "cross_conflict": payload.get("cross_conflict", False),
            "notes": payload.get("notes", "No dedicated Ghidra+IDA comparison is attached to this record yet. Ghidra remains the primary static lane; IDA is optional when a working automation-capable build is available."),
        }
    return {
        "executed": False,
        "functions_match": None,
        "branches_match": None,
        "ghidra_function": None,
        "ida_function": None,
        "ghidra_function_confidence": None,
        "ida_function_confidence": None,
        "ghidra_value_map": None,
        "ida_value_map": None,
        "status": "insufficient",
        "verdict": "review-only",
        "parity_summary": "Ghidra function=<none>; IDA function=<none>; branch=insufficient; value_map=<none> vs <none>; verdict=review-only.",
        "confidence": "low",
        "cross_conflict": False,
        "notes": "No dedicated Ghidra+IDA comparison is attached to this record yet. Ghidra remains the primary static lane; IDA is optional when a working automation-capable build is available.",
    }


def build_runtime(record: dict[str, Any], audit: dict[str, Any]) -> dict[str, Any]:
    tweak_id = str(record.get("tweak_id") or "")
    runtime_items = collect_evidence(record, {"procmon-trace", "runtime-diff", "runtime-trace", "vm-test", "registry-observation", "etw-trace"})
    kernelish = audit.get("suspected_layer") in {"kernel", "boot", "driver"}
    procmon_item = next((item for item in runtime_items if item.get("kind") == "procmon-trace"), None)
    etw_item = next((item for item in runtime_items if item.get("kind") == "etw-trace"), None)
    runtime_lane = load_lane_manifest(tweak_id, "runtime-lane.json")
    runtime_result = load_lane_result(runtime_lane)
    procmon_lane = load_lane_manifest(tweak_id, "procmon-lane.json")
    runtime_lane_status = lane_capture_status(runtime_lane)
    procmon_lane_status = lane_capture_status(procmon_lane)

    etw_trace_file = normalized_location(etw_item, "ETW trace")
    if isinstance(runtime_result, dict):
        runtime_summary = runtime_result.get("summary") or {}
        etw_trace_file = (
            repo_relative_text(((runtime_result.get("wpr") or {}).get("repo_etl_placeholder")))
            or repo_relative_text(runtime_summary.get("repo_etl_placeholder"))
            or repo_relative_text(runtime_lane.get("result_ref") if runtime_lane else None)
            or etw_trace_file
        )
    else:
        etw_trace_file = lane_repo_ref(runtime_lane) or etw_trace_file

    procmon_trace_file = normalized_location(procmon_item, "Procmon trace") or lane_repo_ref(procmon_lane)
    operation = None
    if isinstance(runtime_result, dict):
        if isinstance(runtime_result.get("summary"), dict) and runtime_result["summary"].get("control_panel"):
            operation = "runtime-control-panel-probe"
        elif isinstance(runtime_result.get("summary"), dict) and runtime_result["summary"].get("explorer_restart"):
            operation = "runtime-explorer-restart-probe"
        elif isinstance(runtime_result.get("summary"), dict) and runtime_result["summary"].get("wallpaper_apply"):
            operation = "runtime-wallpaper-apply-probe"
        elif runtime_result.get("post_boot") or runtime_result.get("wpr"):
            operation = "runtime-reboot-probe"

    return {
        "$schema": "registry-evidence-v3.2/runtime-evidence",
        "runtime": {
            "etw": {
                "executed": etw_item is not None or lane_executed(runtime_lane),
                "events_found": etw_item is not None or bool(runtime_result and (runtime_result.get("post_boot") or (runtime_result.get("wpr") or {}).get("stopped"))),
                "reading_process": None,
                "reading_pid": None,
                "operation": operation,
                "timestamp": (runtime_result or {}).get("generated_utc"),
                "boot_phase_included": bool((runtime_result or {}).get("post_boot")) or (audit.get("boot_phase_relevant") and bool(audit.get("wpr") or audit.get("reboot_tested"))),
                "trace_file": etw_trace_file,
                "capture_status": runtime_lane_status,
                "collection_mode": (runtime_lane or {}).get("collection_mode") or "evidence",
                "runner_required": runner_required(audit),
                "rollback_pending": bool((runtime_lane or {}).get("rollback_pending")),
                "capture_artifacts": manifest_capture_artifacts(runtime_lane),
            },
            "procmon": {
                "executed": procmon_item is not None or lane_executed(procmon_lane),
                "events_found": procmon_item is not None or bool(procmon_lane and procmon_lane.get("exit_code") == 0),
                "reading_process": None,
                "trace_file": procmon_trace_file,
                "capture_status": procmon_lane_status,
                "collection_mode": (procmon_lane or {}).get("collection_mode") or "evidence",
                "runner_required": runner_required(audit),
                "rollback_pending": bool((procmon_lane or {}).get("rollback_pending")),
                "capture_artifacts": manifest_capture_artifacts(procmon_lane),
            },
            "frida": {
                "executed": False,
                "skip_reason": (
                    f"KERNEL_GUARD: key is treated as {audit.get('suspected_layer')}, Frida is not valid for this lane."
                    if kernelish
                    else "Not yet executed under v3.1."
                ),
                "result_valid": None if not kernelish else False,
            },
            "dtrace": {
                "executed": False,
                "events_found": False,
                "script_file": "registry-research-framework/tools/dtrace-registry-probe.d",
                "output_file": None,
            },
        },
        "source_evidence_ids": [item.get("evidence_id") for item in runtime_items if item.get("evidence_id")],
    }


def build_static(record: dict[str, Any], audit: dict[str, Any]) -> dict[str, Any]:
    static_items = collect_evidence(record, {"decompilation", "ghidra-headless", "ghidra-trace"})
    ghidra_item = static_items[0] if static_items else None
    ghidra_block = normalize_static_tool(record, "ghidra", ghidra_item)
    ida_block = normalize_static_tool(record, "ida", None)
    cross_block = normalize_cross_verification(record)
    doc_block = normalize_doc_source(record)
    return {
        "$schema": "registry-evidence-v3.2/static-evidence",
        "static": {
            "bingrep": {
                "executed": False,
                "ascii_hits": 0,
                "wide_hits": 0,
                "hit_files": [],
                "output_file": None,
            },
            "floss": {
                "executed": False,
                "hits": 0,
                "output_file": None,
            },
            "capa": {
                "executed": False,
                "registry_read_capability": False,
                "matched_rules": [],
                "target_binary": None,
                "output_file": None,
            },
            "dynamic_resolution": {
                "executed": False,
                "signal_present": False,
                "heuristic_score": None,
                "categories": [],
                "matched_apis": [],
                "output_file": None,
            },
            "ghidra": ghidra_block,
            "ida": ida_block,
            "cross_verification": cross_block,
            "doc_source": doc_block,
        },
        "source_evidence_ids": [item.get("evidence_id") for item in static_items if item.get("evidence_id")],
    }


def normalize_behavior_statistics_payload(payload: Any) -> dict[str, Any]:
    if not isinstance(payload, dict):
        return {
            "before_mean": None,
            "before_stdev": None,
            "after_mean": None,
            "after_stdev": None,
            "sample_count": 0,
            "p_value": None,
            "significant": None,
            "effect_size": None,
            "confidence_interval_95": None,
        }

    if isinstance(payload.get("statistics"), dict):
        payload = payload.get("statistics") or {}

    if any(key in payload for key in ("before_samples", "after_samples")):
        return summarize_before_after(payload.get("before_samples"), payload.get("after_samples"))

    return {
        "before_mean": payload.get("before_mean"),
        "before_stdev": payload.get("before_stdev"),
        "after_mean": payload.get("after_mean"),
        "after_stdev": payload.get("after_stdev"),
        "sample_count": payload.get("sample_count", 0),
        "p_value": payload.get("p_value"),
        "significant": payload.get("significant"),
        "effect_size": payload.get("effect_size"),
        "confidence_interval_95": payload.get("confidence_interval_95"),
    }


def extract_behavior_statistics(bench_item: dict[str, Any] | None, behavior_result: dict[str, Any] | None) -> dict[str, Any]:
    candidates = [
        bench_item.get("statistics") if isinstance(bench_item, dict) else None,
        bench_item if isinstance(bench_item, dict) else None,
        behavior_result.get("statistics") if isinstance(behavior_result, dict) else None,
        behavior_result.get("summary") if isinstance(behavior_result, dict) else None,
        behavior_result if isinstance(behavior_result, dict) else None,
    ]
    for candidate in candidates:
        normalized = normalize_behavior_statistics_payload(candidate)
        if normalized.get("sample_count") or normalized.get("before_mean") is not None or normalized.get("after_mean") is not None:
            return normalized
    return normalize_behavior_statistics_payload(None)


def build_negative_evidence_profile(record: dict[str, Any], audit: dict[str, Any], classification: dict[str, Any]) -> dict[str, Any]:
    text = json.dumps(
        {
            "summary": record.get("summary"),
            "decision": record.get("decision"),
            "evidence": record.get("evidence"),
        },
        ensure_ascii=False,
    ).lower()
    class_id = str(classification.get("class") or "")
    eligible = class_id == "E" or any(
        phrase in text
        for phrase in (
            "did not capture",
            "no exact runtime read",
            "not found",
            "no supporting evidence",
            "did not find",
        )
    )
    reason = "archived-or-no-hit"
    if class_id == "E":
        reason = "class-e"
    elif eligible:
        reason = "runtime-or-source-no-hit"
    else:
        reason = "not-applicable"
    return {
        "eligible": eligible,
        "reason": reason,
        "attempted_layers": audit.get("layers_used") or [],
        "attempted_tools": audit.get("tools_used") or [],
        "tested_build": metadata_build_from_record(record),
    }


def metadata_build_from_record(record: dict[str, Any]) -> str | None:
    return evidence_freshness(record, reproducibility_manifest()).get("os_build")


def build_behavior(record: dict[str, Any], audit: dict[str, Any]) -> dict[str, Any]:
    tweak_id = str(record.get("tweak_id") or "")
    behavior_items = collect_evidence(record, {"wpr-trace", "etw-trace", "runtime-benchmark"})
    wpr_item = next((item for item in behavior_items if item.get("kind") in {"wpr-trace", "etw-trace"}), None)
    bench_item = next((item for item in behavior_items if item.get("kind") == "runtime-benchmark"), None)
    behavior_lane = load_lane_manifest(tweak_id, "behavior-lane.json")
    behavior_result = load_lane_result(behavior_lane)
    behavior_ref = lane_repo_ref(behavior_lane)
    behavior_lane_status = lane_capture_status(behavior_lane)
    behavior_summary = bench_item.get("summary") if bench_item else None
    if behavior_summary is None and behavior_lane and behavior_lane.get("exit_code") not in (None, 0):
        behavior_summary = f"Runner failed with exit code {behavior_lane.get('exit_code')}. See {behavior_lane.get('log_file') or behavior_lane.get('output_file')}."
    behavior_stats = extract_behavior_statistics(bench_item, behavior_result)
    significance_verdict = "insufficient"
    if behavior_stats.get("significant") is True:
        significance_verdict = "significant-change"
    elif behavior_stats.get("significant") is False:
        significance_verdict = "no-demonstrated-change"

    return {
        "$schema": "registry-evidence-v3.2/behavior-evidence",
        "behavior": {
            "typeperf": {
                "executed": False,
                "counter": None,
                "before_average": None,
                "after_average": None,
                "delta_percent": None,
                "sample_count": None,
                "before_file": None,
                "after_file": None,
            },
            "wpr": {
                "executed": wpr_item is not None or lane_executed(behavior_lane),
                "boot_time_before_ms": None,
                "boot_time_after_ms": None,
                "cpu_delta": None,
                "trace_file": normalized_location(wpr_item, "WPR trace") or behavior_ref,
                "capture_status": behavior_lane_status,
                "collection_mode": (behavior_lane or {}).get("collection_mode") or "evidence",
                "runner_required": runner_required(audit),
                "rollback_pending": bool((behavior_lane or {}).get("rollback_pending")),
                "capture_artifacts": manifest_capture_artifacts(behavior_lane),
            },
            "benchmark": {
                "executed": bench_item is not None or lane_executed(behavior_lane),
                "summary": behavior_summary,
                "output_file": normalized_location(bench_item, "Benchmark output") or behavior_ref,
                "capture_status": behavior_lane_status,
                "statistics": behavior_stats,
                "significance_verdict": significance_verdict,
            },
            "registry_sideeffects": {
                "executed": False,
                "sideeffect_count": None,
                "diff_file": None,
            },
        },
        "source_evidence_ids": [item.get("evidence_id") for item in behavior_items if item.get("evidence_id")],
    }


def live_dead_flag_checks(record: dict[str, Any], audit: dict[str, Any]) -> dict[str, bool]:
    tweak_id = str(record.get("tweak_id") or "")
    runtime_lane = load_lane_manifest(tweak_id, "runtime-lane.json")
    procmon_lane = load_lane_manifest(tweak_id, "procmon-lane.json")
    behavior_lane = load_lane_manifest(tweak_id, "behavior-lane.json")
    full_path = evidence_dir(tweak_id) / "full-evidence.json"
    payload = load_json_if_exists(full_path) if full_path.exists() else {}
    runtime = payload.get("runtime") or {}
    etw = runtime.get("etw") or {}
    boot_relevant = bool(audit.get("boot_phase_relevant"))
    etw_done = bool(etw.get("executed")) or any(evidence_kind(item) == "etw-trace" for item in evidence_items(record)) or lane_executed(runtime_lane)
    trigger_tested = (
        has_procmon_evidence(record)
        or has_reboot_evidence(record)
        or has_wpr_evidence(record)
        or has_benchmark_evidence(record)
        or lane_executed(procmon_lane)
        or lane_executed(behavior_lane)
        or lane_executed(runtime_lane)
    )
    return {
        "etw_executed": etw_done,
        "boot_phase_included": (not boot_relevant) or has_wpr_evidence(record) or has_reboot_evidence(record) or lane_executed(behavior_lane),
        "correct_tool_used": not (audit.get("suspected_layer") in {"kernel", "boot", "driver"} and "frida" in (audit.get("tools_used") or [])),
        "trigger_condition_tested": trigger_tested,
    }


def original_record_tools(record: dict[str, Any]) -> list[str]:
    tools: list[str] = []
    if has_official_evidence(record):
        tools.append("official-doc")
    if has_procmon_evidence(record):
        tools.append("procmon")
    if has_ghidra_evidence(record):
        tools.append("ghidra")
    if has_wpr_evidence(record):
        tools.append("wpr")
    if has_benchmark_evidence(record):
        tools.append("benchmark")
    if has_reboot_evidence(record):
        tools.append("reboot")
    return tools


def build_re_audit(record: dict[str, Any], audit: dict[str, Any]) -> dict[str, Any] | None:
    if not audit.get("re_audit_required"):
        return None
    tweak_id = str(record.get("tweak_id") or "")
    runtime_lane = load_lane_manifest(tweak_id, "runtime-lane.json")
    procmon_lane = load_lane_manifest(tweak_id, "procmon-lane.json")
    behavior_lane = load_lane_manifest(tweak_id, "behavior-lane.json")
    existing_re_audit_payload = load_json_if_exists(evidence_dir(tweak_id) / "re-audit.json")
    existing_re_audit = existing_re_audit_payload.get("re_audit") if isinstance(existing_re_audit_payload, dict) else {}
    inferred_original_tools = original_record_tools(record)
    original_tools = existing_re_audit.get("original_evidence_tools") or inferred_original_tools or audit.get("tools_used") or []
    if existing_re_audit.get("original_pipeline_version") == "pre-v3.1" and "etw" in original_tools and "etw" not in inferred_original_tools:
        original_tools = inferred_original_tools

    new_tools_applied: list[str] = []
    notes: list[str] = []
    if lane_executed(runtime_lane):
        new_tools_applied.extend(["etw", "vm-runtime-runner"])
        runtime_state = "succeeded" if runtime_lane and runtime_lane.get("exit_code") == 0 else "failed"
        runtime_ref = repo_relative_text(runtime_lane.get("result_ref") if runtime_lane else None) or repo_relative_text(runtime_lane.get("log_file") if runtime_lane else None)
        notes.append(f"Runtime lane {runtime_state}" + (f" ({runtime_ref})" if runtime_ref else ""))
    if lane_executed(procmon_lane):
        new_tools_applied.append("procmon")
        procmon_state = "succeeded" if procmon_lane and procmon_lane.get("exit_code") == 0 else "failed"
        procmon_ref = repo_relative_text(procmon_lane.get("result_ref") if procmon_lane else None) or repo_relative_text(procmon_lane.get("log_file") if procmon_lane else None)
        notes.append(f"Procmon lane {procmon_state}" + (f" ({procmon_ref})" if procmon_ref else ""))
    if lane_executed(behavior_lane):
        new_tools_applied.extend(["wpr", "vm-benchmark-runner"])
        behavior_state = "succeeded" if behavior_lane and behavior_lane.get("exit_code") == 0 else "failed"
        behavior_ref = repo_relative_text(behavior_lane.get("result_ref") if behavior_lane else None) or repo_relative_text(behavior_lane.get("log_file") if behavior_lane else None)
        notes.append(f"Behavior lane {behavior_state}" + (f" ({behavior_ref})" if behavior_ref else ""))

    checks = live_dead_flag_checks(record, audit)
    reason_parts = [part.strip() for part in str(audit.get("re_audit_reason") or "").split(";") if part.strip()]
    if checks["etw_executed"]:
        reason_parts = [part for part in reason_parts if part != "etw_not_recorded"]
    if all(checks.values()):
        reason_parts = [part for part in reason_parts if part != "dead_flag_checks_incomplete"]
    re_audit_reason = "; ".join(reason_parts)
    re_audit_note = "; ".join(notes) if notes else "Bootstrapped from the current research record and evidence audit."
    return {
        "$schema": "registry-evidence-v3.2/re-audit",
        "re_audit": {
            "is_re_audit": True,
            "original_class": existing_re_audit.get("original_class") or audit.get("original_class") or audit.get("evidence_class"),
            "original_evidence_tools": original_tools,
            "original_cross_layer": existing_re_audit.get("original_cross_layer") if "original_cross_layer" in existing_re_audit else audit.get("cross_layer_satisfied"),
            "original_pipeline_version": existing_re_audit.get("original_pipeline_version") or audit.get("original_pipeline_version") or "pre-v3.1",
            "re_audit_date": now_utc(),
            "re_audit_reason": re_audit_reason,
            "re_audit_priority": audit.get("re_audit_priority"),
            "new_tools_applied": new_tools_applied,
            "new_cross_layer": audit.get("cross_layer_satisfied"),
            "new_class": audit.get("evidence_class"),
            "class_changed": False,
            "frida_kernel_guard_applied": audit.get("frida_kernel_guard_applied"),
            "dead_flag_four_conditions_met": all(checks.values()),
            "notes": re_audit_note,
            "new_pipeline_version": PIPELINE_VERSION,
        },
    }


def build_classification(record: dict[str, Any], audit: dict[str, Any]) -> dict[str, Any]:
    checks = live_dead_flag_checks(record, audit)
    cross_block = normalize_cross_verification(record)
    tweak_id = str(record.get("tweak_id") or "")
    runtime_lane = load_lane_manifest(tweak_id, "runtime-lane.json")
    procmon_lane = load_lane_manifest(tweak_id, "procmon-lane.json")
    behavior_lane = load_lane_manifest(tweak_id, "behavior-lane.json")
    runtime_required = runner_required(audit)
    runtime_lane_raw_status = lane_capture_status(runtime_lane) if runtime_lane else "missing-required-runner"
    if runtime_required and (not runtime_lane or runtime_lane.get("runner") is None):
        runner_status = "missing-required-runner"
    elif runtime_required and runtime_lane_raw_status not in {"runner-ok", "staged-without-capture", "missing-capture"}:
        runner_status = "missing-capture"
    elif runtime_required:
        runner_status = runtime_lane_raw_status
    else:
        runner_status = runtime_lane_raw_status if runtime_lane else "not-required"
    behavior_block = build_behavior(record, audit)
    benchmark = (behavior_block.get("behavior") or {}).get("benchmark") or {}
    return {
        "$schema": "registry-evidence-v3.2/classification",
        "classification": {
            "class": audit.get("evidence_class"),
            "pipeline_version": PIPELINE_VERSION,
            "reason": normalize_reference_text((record.get("decision") or {}).get("why"), title="Classification reason"),
            "cross_layer_satisfied": audit.get("cross_layer_satisfied"),
            "layers_used": audit.get("layers_used") or [],
            "layer_count": len(audit.get("layers_used") or []),
            "frida_kernel_guard_applied": audit.get("frida_kernel_guard_applied"),
            "dead_flag_checks": checks,
            "class_ready_basis": audit.get("class_ready_basis"),
            "next_missing_layer": audit.get("next_missing_layer"),
            "cross_verification": cross_block,
            "manual_review_required": bool(cross_block.get("cross_conflict")) or bool((record.get("decision") or {}).get("manual_review_required")),
            "doc_source": normalize_doc_source(record),
            "runner_validation": {
                "required": runtime_required,
                "runtime_lane_status": runner_status,
                "runtime_lane_status_raw": runtime_lane_raw_status,
                "procmon_lane_status": lane_capture_status(procmon_lane) if procmon_lane else "not-required",
                "behavior_lane_status": lane_capture_status(behavior_lane) if behavior_lane else "not-required",
                "verdict": runner_status,
            },
            "behavior_significance": {
                "verdict": benchmark.get("significance_verdict", "insufficient"),
                "statistics": benchmark.get("statistics"),
            },
        },
    }


def normalize_artifact_ref_item(item: Any, *, collected_utc: str | None = None) -> dict[str, Any] | None:
    if isinstance(item, str):
        item = {"path": item}
    if not isinstance(item, dict):
        return None
    path_value = repo_relative_text(item.get("path") or item.get("filename")) or str(item.get("path") or item.get("filename") or item.get("id") or "").strip()
    if not path_value:
        return None
    extra = {key: item.get(key) for key in ("id", "filename", "release_url", "exists", "placeholder", "kind") if key in item}
    repo_ref = repo_relative_text(path_value)
    if repo_ref:
        return build_artifact_metadata(REPO_ROOT, repo_ref, collected_utc=collected_utc or item.get("collected_utc"), extra=extra)
    return {
        "path": path_value,
        "sha256": None,
        "size": None,
        "collected_utc": collected_utc or item.get("collected_utc"),
        **extra,
    }


def current_artifact_refs(tweak_id: str, audit: dict[str, Any]) -> list[dict[str, Any]]:
    candidates: list[tuple[Any, str | None]] = []
    full_path = evidence_dir(tweak_id) / "full-evidence.json"
    if full_path.exists():
        payload = load_json(full_path)
        for item in payload.get("artifact_refs") or []:
            candidates.append((item, None))
    for item in audit.get("artifact_refs") or []:
        candidates.append((item, None))
    for filename in ("runtime-lane.json", "procmon-lane.json", "behavior-lane.json"):
        manifest = load_lane_manifest(tweak_id, filename)
        if not manifest:
            continue
        collected = str(manifest.get("generated_utc") or "") or None
        for key in ("result_ref", "log_file"):
            repo_ref = repo_relative_text(manifest.get(key))
            if repo_ref:
                candidates.append(({"path": repo_ref, "id": Path(repo_ref).name}, collected))
        for item in manifest_capture_artifacts(manifest):
            candidates.append((item, collected))

    normalized: list[dict[str, Any]] = []
    seen: set[str] = set()
    for item, collected in candidates:
        normalized_item = normalize_artifact_ref_item(item, collected_utc=collected)
        if not normalized_item:
            continue
        path_key = str(normalized_item.get("path") or normalized_item.get("release_url") or "")
        if not path_key or path_key in seen:
            continue
        seen.add(path_key)
        normalized.append(normalized_item)
    return normalized


def build_timeline(record: dict[str, Any], audit: dict[str, Any], phase: str) -> dict[str, Any]:
    return {
        "$schema": "registry-evidence-v3.2/timeline",
        "timeline": [
            {"step": "bootstrap_record_load", "timestamp": now_utc()},
            {"step": phase, "timestamp": now_utc()},
            {"step": "classification", "timestamp": now_utc()},
            {"step": "last_reviewed", "timestamp": record.get("last_reviewed_utc")},
        ],
    }


def render_verdict(record: dict[str, Any], audit: dict[str, Any], classification: dict[str, Any], artifact_refs: list[dict[str, Any]]) -> str:
    summary = normalize_reference_text(record.get("summary"), title=str(record.get("tweak_id") or "Summary"))
    verdict = normalize_reference_text((record.get("decision") or {}).get("why"), title=str(record.get("tweak_id") or "Verdict"))
    lines = [
        f"# {record.get('tweak_id')}",
        "",
        f"- Class: `{audit.get('evidence_class')}`",
        f"- Pipeline: `{classification['classification'].get('pipeline_version')}`",
        f"- Official doc: `{str(audit.get('official_doc_exists')).lower()}`",
        f"- Cross-layer: `{str(audit.get('cross_layer_satisfied')).lower()}`",
        f"- Cross verification: `{(classification['classification'].get('cross_verification') or {}).get('status', 'insufficient')}`",
        f"- Runner validation: `{(classification['classification'].get('runner_validation') or {}).get('verdict', 'not-required')}`",
        f"- Layer set: `{', '.join(audit.get('layers_used') or []) or 'none'}`",
        f"- Tools: `{', '.join(audit.get('tools_used') or []) or 'none'}`",
        "",
        summary or "",
        "",
        "## Current verdict",
        "",
        verdict or "No decision summary is attached.",
    ]
    if artifact_refs:
        lines.extend(["", "## Artifact refs", ""])
        for item in artifact_refs:
            if isinstance(item, dict):
                lines.append(f"- `{item.get('id') or Path(str(item.get('path') or '')).name}` -> {item.get('release_url') or item.get('path') or item.get('filename')}")
            else:
                lines.append(f"- `{item}`")
    return "\n".join(lines)


def build_full_evidence(record: dict[str, Any], audit: dict[str, Any], phase: str) -> dict[str, Any]:
    metadata = build_metadata(record, audit)
    runtime = build_runtime(record, audit)
    static = build_static(record, audit)
    behavior = build_behavior(record, audit)
    classification = build_classification(record, audit)
    re_audit = build_re_audit(record, audit)
    timeline = build_timeline(record, audit, phase)
    artifact_refs = current_artifact_refs(str(record.get("tweak_id") or ""), audit)
    tweak_id = str(record.get("tweak_id") or "")

    payload: dict[str, Any] = {
        "$schema": "registry-evidence-v3.2/full-evidence",
        "record_id": record.get("record_id"),
        "tweak_id": tweak_id,
        "metadata": metadata,
        "runtime": runtime["runtime"],
        "static": static["static"],
        "behavior": behavior["behavior"],
        "classification": classification["classification"],
        "timeline": timeline["timeline"],
        "artifact_refs": artifact_refs,
        "doc_source": normalize_doc_source(record),
        "cross_verification": normalize_cross_verification(record),
        "interaction_graph": interaction_groups_for_tweak(tweak_id),
        "tweak_dependencies": dependency_entry_for_tweak(tweak_id),
        "anticheat_risk": anticheat_risk_for_tweak(tweak_id),
        "negative_evidence": build_negative_evidence_profile(record, audit, classification["classification"]),
        "reproducibility": reproducibility_manifest(),
        "research_record": str(((RESEARCH_ROOT / "records") / f"{record.get('tweak_id')}.json").relative_to(REPO_ROOT)).replace("\\", "/"),
        "research_record_fallback": str(((RESEARCH_ROOT / "records") / f"{record.get('tweak_id')}.review.json").relative_to(REPO_ROOT)).replace("\\", "/"),
    }
    if re_audit:
        payload["re_audit"] = re_audit["re_audit"]
    return payload


def write_phase_outputs(tweak_id: str, record: dict[str, Any], audit: dict[str, Any], phase: str) -> None:
    root = evidence_dir(tweak_id)
    metadata = build_metadata(record, audit)
    runtime = build_runtime(record, audit)
    static = build_static(record, audit)
    behavior = build_behavior(record, audit)
    classification = build_classification(record, audit)
    re_audit = build_re_audit(record, audit)
    timeline = build_timeline(record, audit, phase)
    full_evidence = build_full_evidence(record, audit, phase)

    write_json(root / "metadata.json", metadata)
    write_json(root / "runtime.json", runtime)
    write_json(root / "static.json", static)
    write_json(root / "behavior.json", behavior)
    write_json(root / "classification.json", classification)
    write_json(root / "timeline.json", timeline)
    if re_audit:
        write_json(root / "re-audit.json", re_audit)
    write_json(root / "full-evidence.json", full_evidence)
    write_text(root / "verdict.md", render_verdict(record, audit, classification, full_evidence.get("artifact_refs") or []))


def invoke_phase_tools(tweak_id: str, phase: str) -> None:
    wrapper_map = {
        "faz1": [
            ("etw-registry-trace.ps1", "runtime-lane.json"),
            ("procmon-registry-trace.ps1", "procmon-lane.json"),
        ],
        "faz3": [
            ("wpr-boot-trace.ps1", "behavior-lane.json"),
        ],
    }
    if phase not in wrapper_map:
        return

    for script_name, output_name in wrapper_map[phase]:
        wrapper_path = REPO_ROOT / "registry-research-framework" / "tools" / script_name
        output_path = evidence_dir(tweak_id) / output_name
        command = [
            "powershell.exe",
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            str(wrapper_path),
            "-OutputFile",
            str(output_path),
            "-TweakId",
            tweak_id,
        ]
        subprocess.run(command, cwd=REPO_ROOT, check=True)


def load_queue(csv_path: Path) -> list[str]:
    if not csv_path.exists():
        return []
    tweak_ids: list[str] = []
    with csv_path.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            tweak_id = str(row.get("tweak_id") or "").strip()
            if tweak_id:
                tweak_ids.append(tweak_id)
    return tweak_ids


def current_queue_ids() -> list[str]:
    if DEFAULT_QUEUE_PATH.exists():
        return load_queue(DEFAULT_QUEUE_PATH)
    payload = load_json(AUDIT_PATH)
    entries = payload.get("entries") or []
    pending = [entry for entry in entries if entry.get("re_audit_required")]
    pending.sort(key=lambda item: (item.get("re_audit_priority", 99), item.get("tweak_id") or ""))
    return [str(item.get("tweak_id") or "") for item in pending if item.get("tweak_id")]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Bootstrap v3.2 evidence outputs.")
    parser.add_argument("--phase", required=True, choices=["faz0", "faz1", "faz2", "faz3", "faz4", "faz5", "faz6", "all"])
    parser.add_argument("--tweak-id")
    parser.add_argument("--queue-only", action="store_true")
    parser.add_argument("--queue-csv", type=Path, default=DEFAULT_QUEUE_PATH)
    parser.add_argument("--execute-tools", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()

    if args.tweak_id:
        tweak_ids = [args.tweak_id]
    elif args.queue_only:
        tweak_ids = load_queue(args.queue_csv) or current_queue_ids()
    else:
        tweak_ids = current_queue_ids()

    if not tweak_ids:
        raise SystemExit("No tweak ids selected.")

    for tweak_id in tweak_ids:
        if args.execute_tools:
            invoke_phase_tools(tweak_id, args.phase)
        record = load_record(tweak_id)
        audit = load_audit_entry(tweak_id)
        write_phase_outputs(tweak_id, record, audit, args.phase)
        print(f"Wrote v3.2 evidence bundle for {tweak_id}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
