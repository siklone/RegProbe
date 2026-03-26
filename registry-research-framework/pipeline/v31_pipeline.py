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

from research_path_lib import V31_EVIDENCE_ROOT  # noqa: E402

RESEARCH_ROOT = REPO_ROOT / "research"
RECORDS_DIR = RESEARCH_ROOT / "records"
AUDIT_PATH = RESEARCH_ROOT / "evidence-audit.json"
DEFAULT_QUEUE_PATH = REPO_ROOT / "registry-research-framework" / "audit" / "re-audit-queue.csv"


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
    return {
        "$schema": "registry-evidence-v3.1/metadata",
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
    }


def build_runtime(record: dict[str, Any], audit: dict[str, Any]) -> dict[str, Any]:
    runtime_items = collect_evidence(record, {"procmon-trace", "runtime-diff", "runtime-trace", "vm-test", "registry-observation", "etw-trace"})
    kernelish = audit.get("suspected_layer") in {"kernel", "boot", "driver"}
    procmon_item = next((item for item in runtime_items if item.get("kind") == "procmon-trace"), None)
    etw_item = next((item for item in runtime_items if item.get("kind") == "etw-trace"), None)
    return {
        "$schema": "registry-evidence-v3.1/runtime-evidence",
        "runtime": {
            "etw": {
                "executed": etw_item is not None,
                "events_found": etw_item is not None,
                "reading_process": None,
                "reading_pid": None,
                "operation": None,
                "timestamp": None,
                "boot_phase_included": audit.get("boot_phase_relevant") and bool(audit.get("wpr") or audit.get("reboot_tested")),
                "trace_file": etw_item.get("location") if etw_item else None,
            },
            "procmon": {
                "executed": procmon_item is not None,
                "events_found": procmon_item is not None,
                "reading_process": None,
                "trace_file": procmon_item.get("location") if procmon_item else None,
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
    return {
        "$schema": "registry-evidence-v3.1/static-evidence",
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
            "ghidra": {
                "executed": ghidra_item is not None,
                "trigger": "existing_record_evidence" if ghidra_item else None,
                "pdb_loaded": False,
                "pdb_source": None,
                "string_xref_found": ghidra_item is not None,
                "function_name": None,
                "decompile_snippet": ghidra_item.get("summary") if ghidra_item else None,
                "call_graph_depth": None,
                "output_file": ghidra_item.get("location") if ghidra_item else None,
                "ghidra_no_function_fallback": audit.get("ghidra_no_function_fallback"),
            },
        },
        "source_evidence_ids": [item.get("evidence_id") for item in static_items if item.get("evidence_id")],
    }


def build_behavior(record: dict[str, Any], audit: dict[str, Any]) -> dict[str, Any]:
    behavior_items = collect_evidence(record, {"wpr-trace", "etw-trace", "runtime-benchmark"})
    wpr_item = next((item for item in behavior_items if item.get("kind") in {"wpr-trace", "etw-trace"}), None)
    bench_item = next((item for item in behavior_items if item.get("kind") == "runtime-benchmark"), None)
    return {
        "$schema": "registry-evidence-v3.1/behavior-evidence",
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
                "executed": wpr_item is not None,
                "boot_time_before_ms": None,
                "boot_time_after_ms": None,
                "cpu_delta": None,
                "trace_file": wpr_item.get("location") if wpr_item else None,
            },
            "benchmark": {
                "executed": bench_item is not None,
                "summary": bench_item.get("summary") if bench_item else None,
                "output_file": bench_item.get("location") if bench_item else None,
            },
            "registry_sideeffects": {
                "executed": False,
                "sideeffect_count": None,
                "diff_file": None,
            },
        },
        "source_evidence_ids": [item.get("evidence_id") for item in behavior_items if item.get("evidence_id")],
    }


def build_re_audit(record: dict[str, Any], audit: dict[str, Any]) -> dict[str, Any] | None:
    if not audit.get("re_audit_required"):
        return None
    return {
        "$schema": "registry-evidence-v3.1/re-audit",
        "re_audit": {
            "is_re_audit": True,
            "original_class": audit.get("original_class") or audit.get("evidence_class"),
            "original_evidence_tools": audit.get("tools_used") or [],
            "original_cross_layer": audit.get("cross_layer_satisfied"),
            "original_pipeline_version": audit.get("original_pipeline_version") or "pre-v3.1",
            "re_audit_date": now_utc(),
            "re_audit_reason": audit.get("re_audit_reason"),
            "re_audit_priority": audit.get("re_audit_priority"),
            "new_tools_applied": [],
            "new_cross_layer": audit.get("cross_layer_satisfied"),
            "new_class": audit.get("evidence_class"),
            "class_changed": False,
            "frida_kernel_guard_applied": audit.get("frida_kernel_guard_applied"),
            "dead_flag_four_conditions_met": all((audit.get("dead_flag_checks") or {}).values()),
            "notes": "Bootstrapped from the current research record and evidence audit. Replace with fresh v3.1 runtime output after re-audit runs.",
            "new_pipeline_version": "v3.1",
        },
    }


def build_classification(record: dict[str, Any], audit: dict[str, Any]) -> dict[str, Any]:
    return {
        "$schema": "registry-evidence-v3.1/classification",
        "classification": {
            "class": audit.get("evidence_class"),
            "pipeline_version": "v3.1",
            "reason": (record.get("decision") or {}).get("why"),
            "cross_layer_satisfied": audit.get("cross_layer_satisfied"),
            "layers_used": audit.get("layers_used") or [],
            "layer_count": len(audit.get("layers_used") or []),
            "frida_kernel_guard_applied": audit.get("frida_kernel_guard_applied"),
            "dead_flag_checks": audit.get("dead_flag_checks") or {},
            "class_ready_basis": audit.get("class_ready_basis"),
            "next_missing_layer": audit.get("next_missing_layer"),
        },
    }


def current_artifact_refs(tweak_id: str) -> list[dict[str, Any]]:
    full_path = evidence_dir(tweak_id) / "full-evidence.json"
    if not full_path.exists():
        return []
    payload = load_json(full_path)
    return payload.get("artifact_refs") or []


def build_timeline(record: dict[str, Any], audit: dict[str, Any], phase: str) -> dict[str, Any]:
    return {
        "$schema": "registry-evidence-v3.1/timeline",
        "timeline": [
            {"step": "bootstrap_record_load", "timestamp": now_utc()},
            {"step": phase, "timestamp": now_utc()},
            {"step": "classification", "timestamp": now_utc()},
            {"step": "last_reviewed", "timestamp": record.get("last_reviewed_utc")},
        ],
    }


def render_verdict(record: dict[str, Any], audit: dict[str, Any], classification: dict[str, Any], artifact_refs: list[dict[str, Any]]) -> str:
    lines = [
        f"# {record.get('tweak_id')}",
        "",
        f"- Class: `{audit.get('evidence_class')}`",
        f"- Pipeline: `{classification['classification'].get('pipeline_version')}`",
        f"- Official doc: `{str(audit.get('official_doc_exists')).lower()}`",
        f"- Cross-layer: `{str(audit.get('cross_layer_satisfied')).lower()}`",
        f"- Layer set: `{', '.join(audit.get('layers_used') or []) or 'none'}`",
        f"- Tools: `{', '.join(audit.get('tools_used') or []) or 'none'}`",
        "",
        record.get("summary") or "",
        "",
        "## Current verdict",
        "",
        (record.get("decision") or {}).get("why") or "No decision summary is attached.",
    ]
    if artifact_refs:
        lines.extend(["", "## Artifact refs", ""])
        for item in artifact_refs:
            lines.append(f"- `{item.get('id')}` -> {item.get('release_url') or item.get('filename')}")
    return "\n".join(lines)


def build_full_evidence(record: dict[str, Any], audit: dict[str, Any], phase: str) -> dict[str, Any]:
    metadata = build_metadata(record, audit)
    runtime = build_runtime(record, audit)
    static = build_static(record, audit)
    behavior = build_behavior(record, audit)
    classification = build_classification(record, audit)
    re_audit = build_re_audit(record, audit)
    timeline = build_timeline(record, audit, phase)
    artifact_refs = current_artifact_refs(str(record.get("tweak_id") or ""))

    payload: dict[str, Any] = {
        "$schema": "registry-evidence-v3.1/full-evidence",
        "record_id": record.get("record_id"),
        "tweak_id": record.get("tweak_id"),
        "metadata": metadata,
        "runtime": runtime["runtime"],
        "static": static["static"],
        "behavior": behavior["behavior"],
        "classification": classification["classification"],
        "timeline": timeline["timeline"],
        "artifact_refs": artifact_refs,
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
        "faz1": ("etw-registry-trace.ps1", "runtime-lane.json"),
        "faz3": ("wpr-boot-trace.ps1", "behavior-lane.json"),
    }
    if phase not in wrapper_map:
        return

    script_name, output_name = wrapper_map[phase]
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
    parser = argparse.ArgumentParser(description="Bootstrap v3.1 evidence outputs.")
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
        record = load_record(tweak_id)
        audit = load_audit_entry(tweak_id)
        write_phase_outputs(tweak_id, record, audit, args.phase)
        if args.execute_tools:
            invoke_phase_tools(tweak_id, args.phase)
        print(f"Wrote v3.1 evidence bundle for {tweak_id}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
