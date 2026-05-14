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
JSON_OUTPUT = AUDIT_ROOT / "promotion-eligible-review-pack.json"
MARKDOWN_OUTPUT = AUDIT_ROOT / "promotion-eligible-review-pack.md"


DOMAIN_DECISIONS: dict[str, dict[str, Any]] = {
    "power.control.class1-initial-unpark-count": {
        "component": "Kernel Power Manager",
        "affected_processes": ["System", "ntoskrnl.exe"],
        "interaction_surface": "kernel power tuning; user-visible only through latency or power behavior",
        "risk": "medium",
        "side_effects": ["CPU parking behavior can be hardware and power-plan dependent."],
        "vm_impact": "VM vCPU scheduling may hide or distort the real hardware effect.",
        "recommended_action": {
            "verdict": "PROMOTE",
            "apply_allowed": True,
            "condition": "Ship with a hardware-dependent claim boundary; do not claim a universal performance win.",
        },
        "rationale": "Class A evidence, rollback story, app mapping, and a bounded power-tuning blast radius make this decision-ready for promotion with conservative wording.",
    },
    "power.control.hibernate-enabled": {
        "component": "Power Manager / boot hibernation state",
        "affected_processes": ["smss.exe", "System", "powercfg.exe"],
        "interaction_surface": "system sleep/hibernate availability and hiberfil.sys behavior",
        "risk": "low",
        "side_effects": ["No useful effect when firmware reports S4 unsupported."],
        "vm_impact": "Current VM lanes may report hibernation unsupported; a preflight powercfg /a check is required.",
        "recommended_action": {
            "verdict": "CONDITIONAL-PROMOTE",
            "apply_allowed": True,
            "condition": "Requires app mapping plus a runtime pre-check that powercfg /availablesleepstates reports S4 support; otherwise show NotApplicable.",
        },
        "rationale": "The raw value has evidence, but product promotion should be conditional because platform firmware support gates the real behavior.",
        "requires_manual_vm_verification": True,
        "requires_app_mapping": True,
    },
    "power.control.lid-reliability-state": {
        "component": "ACPI lid / Power Policy Manager",
        "affected_processes": ["System", "ntoskrnl.exe"],
        "interaction_surface": "lid-close reliability handling; hardware-dependent on laptops",
        "risk": "low-medium",
        "side_effects": ["Desktop and VM systems without an ACPI lid device may see no effect."],
        "vm_impact": "VMs typically lack a lid device, so product wording must mark the setting hardware-dependent.",
        "recommended_action": {
            "verdict": "PROMOTE",
            "apply_allowed": True,
            "condition": "Ship with a hardware-dependent applicability note for laptop/ACPI lid devices.",
        },
        "rationale": "Evidence and rollback are clean, and inert behavior on non-lid systems can be handled as applicability rather than a blocker.",
        "requires_manual_vm_verification": True,
    },
    "power.control.mf-buffering-threshold": {
        "component": "Power Manager / Media Foundation-adjacent buffering",
        "affected_processes": ["System", "media playback apps"],
        "interaction_surface": "media playback buffering behavior",
        "risk": "low",
        "side_effects": ["Effect depends on the active media stack and workload."],
        "vm_impact": "VM validation can prove read/write safety, but media playback benefit remains workload-specific.",
        "recommended_action": {
            "verdict": "PROMOTE",
            "apply_allowed": True,
            "condition": "Ship as reversible media/power tuning with no universal benchmark claim.",
        },
        "rationale": "Low blast radius, app mapping, rollback, and runtime evidence make this a straightforward conservative promotion candidate.",
    },
    "power.control.perf-calculate-actual-utilization": {
        "component": "Processor performance utilization calculation",
        "affected_processes": ["System", "ntoskrnl.exe"],
        "interaction_surface": "CPU utilization and processor performance-state calculation",
        "risk": "medium-high",
        "side_effects": ["Guest or physical CPU utilization reporting can shift after the override."],
        "vm_impact": "Hypervisor CPU scheduling can make utilization effects misleading inside a VM.",
        "recommended_action": {
            "verdict": "PROMOTE-WITH-WARNINGS",
            "apply_allowed": True,
            "condition": "Ship only with a bare-metal-preferred warning and an explicit rollback path.",
        },
        "rationale": "Evidence is strong and rollback is known, but CPU utilization semantics are broad enough to require warning labels.",
    },
    "system.executive-additional-worker-threads": {
        "component": "NT Executive worker-thread pool",
        "affected_processes": ["System", "ntoskrnl.exe"],
        "interaction_surface": "kernel worker-thread pool sizing",
        "risk": "high",
        "side_effects": [
            "Manual worker-thread overrides can waste context-switch budget on low-core systems.",
            "The default is calculated by Windows and may already fit the machine.",
        ],
        "vm_impact": "VMs with limited vCPUs are especially likely to hide or invert any benefit.",
        "recommended_action": {
            "verdict": "INTENTIONAL-HOLD-CLOSED",
            "apply_allowed": False,
            "reason": "Keep research-only until per-machine profiling proves a safe non-default profile.",
        },
        "rationale": "The value is evidence-full, but the shipping decision should stay closed because the safe preset is machine-dependent and high risk.",
    },
    "system.kernel.disable-exception-chain-validation": {
        "component": "Session Manager Kernel exception-chain policy",
        "affected_processes": ["System", "ntoskrnl.exe", "user-mode process mitigation surface"],
        "interaction_surface": "security mitigation policy; lab-only override surface",
        "risk": "critical-security",
        "side_effects": [
            "Disabling exception-chain validation weakens a security mitigation surface.",
            "The runtime proof establishes the value is read, not that it is safe for users.",
        ],
        "vm_impact": "VM proof is useful for read semantics but does not make this safe outside isolated labs.",
        "recommended_action": {
            "verdict": "INTENTIONAL-HOLD-CLOSED",
            "apply_allowed": False,
            "reason": "Security-sensitive mitigation override; never promote as a normal end-user tweak.",
        },
        "rationale": "This is evidence-full but intentionally non-actionable because it is a security mitigation bypass surface.",
    },
}


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def records_by_id() -> dict[str, dict[str, Any]]:
    result: dict[str, dict[str, Any]] = {}
    for path in list_records():
        record = load_json(path)
        record_id = str(record.get("record_id") or record.get("tweak_id") or "")
        if record_id:
            record["_source_file"] = str(path.relative_to(REPO_ROOT)).replace("\\", "/")
            result[record_id] = record
    return result


def gate_entries_by_id(gate_payload: dict[str, Any]) -> dict[str, dict[str, Any]]:
    result: dict[str, dict[str, Any]] = {}
    for entry in gate_payload.get("entries") or []:
        record_id = str(entry.get("record_id") or entry.get("candidate_id") or entry.get("tweak_id") or "")
        if record_id:
            result[record_id] = entry
    return result


def primary_target(record: dict[str, Any]) -> dict[str, Any]:
    targets = ((record.get("setting") or {}).get("targets") or [])
    return targets[0] if targets and isinstance(targets[0], dict) else {}


def evidence_items(record: dict[str, Any]) -> list[dict[str, Any]]:
    return [item for item in (record.get("evidence") or []) if isinstance(item, dict)]


def evidence_kind_set(record: dict[str, Any]) -> set[str]:
    return {str(item.get("kind") or "").lower() for item in evidence_items(record)}


def has_evidence_kind(record: dict[str, Any], *needles: str) -> bool:
    kinds = evidence_kind_set(record)
    return any(any(needle in kind for kind in kinds) for needle in needles)


def evidence_refs(record: dict[str, Any]) -> list[dict[str, Any]]:
    refs: list[dict[str, Any]] = []
    for item in evidence_items(record):
        refs.append(
            {
                "id": str(item.get("id") or item.get("evidence_id") or item.get("title") or ""),
                "kind": str(item.get("kind") or ""),
                "title": str(item.get("title") or ""),
                "location": str(item.get("location") or item.get("url") or ""),
                "strength": str(item.get("strength") or ""),
                "supports": list(item.get("supports") or []),
            }
        )
    return refs


def official_doc_ref(record: dict[str, Any]) -> str:
    for item in evidence_items(record):
        kind = str(item.get("kind") or "").lower()
        if kind in {"official-doc", "policy-csp", "troubleshoot-doc"}:
            return str(item.get("location") or item.get("url") or item.get("title") or "")
    for item in evidence_items(record):
        supports = {str(value).lower() for value in (item.get("supports") or [])}
        if "version-scope" in supports or "behavior" in supports:
            return str(item.get("location") or item.get("url") or item.get("title") or "")
    return ""


def rollback_mechanism(target: dict[str, Any], rollback_status: dict[str, Any]) -> str:
    rollback_value = rollback_status.get("rollback_value") if isinstance(rollback_status, dict) else {}
    state_kind = str((rollback_value or {}).get("state_kind") or "")
    path = str(target.get("path") or "")
    value_name = str(target.get("value_name") or "")
    if state_kind == "missing":
        return f"Delete `{value_name}` under `{path}`."
    value = (rollback_value or {}).get("value")
    return f"Restore `{value_name}` under `{path}` to `{value}`."


def decision_options(domain: dict[str, Any]) -> list[dict[str, Any]]:
    recommended = dict(domain.get("recommended_action") or {})
    return [
        {
            "verdict": "PROMOTE",
            "apply_allowed": True,
            "condition": "Use only if risk, app mapping, rollback, and claim-boundary checks remain acceptable.",
        },
        {
            "verdict": "PROMOTE-WITH-WARNINGS",
            "apply_allowed": True,
            "condition": "Use when evidence is complete but the card needs hardware, VM, security, or workload warnings.",
        },
        {
            "verdict": "CONDITIONAL-PROMOTE",
            "apply_allowed": True,
            "condition": "Use when runtime preflight or missing app mapping must gate the card.",
        },
        {
            "verdict": "INTENTIONAL-HOLD-CLOSED",
            "apply_allowed": False,
            "reason": "Use when the evidence is complete but the product should not expose the action yet.",
        },
        {
            "verdict": "EVIDENCE-BACKED-REJECTED",
            "apply_allowed": False,
            "reason": "Use when the correct evidence lane proves a platform limit, deprecation, or NotApplicable result.",
        },
        {
            "verdict": "RECOMMENDED",
            "apply_allowed": bool(recommended.get("apply_allowed")),
            "condition": recommended.get("condition"),
            "reason": recommended.get("reason"),
            "maps_to": recommended.get("verdict"),
        },
    ]


def analyze_record(record_id: str, record: dict[str, Any], gate: dict[str, Any]) -> dict[str, Any]:
    if record_id not in DOMAIN_DECISIONS:
        raise KeyError(f"No domain decision overlay for promotion-eligible record: {record_id}")
    domain = DOMAIN_DECISIONS[record_id]
    target = primary_target(record)
    rollback_status = gate.get("rollback_status") or {}
    recommended = dict(domain["recommended_action"])
    requires_manual_vm = bool(domain.get("requires_manual_vm_verification"))
    requires_app_mapping = bool(domain.get("requires_app_mapping") or gate.get("app_mapping_status") == "not-mapped")

    return {
        "record_id": record_id,
        "current_state": gate.get("promotion_state"),
        "source_file": record.get("_source_file"),
        "evidence_status": {
            "registry_path": target.get("path"),
            "value_name": target.get("value_name"),
            "value_type": target.get("value_type"),
            "evidence_count": (gate.get("evidence_status") or {}).get("evidence_count"),
            "has_etw_bundle": has_evidence_kind(record, "etw"),
            "has_ghidra_xref": bool((gate.get("evidence_status") or {}).get("has_ghidra_evidence")) or has_evidence_kind(record, "ghidra", "decompilation"),
            "has_procmon_trace": bool((gate.get("evidence_status") or {}).get("has_procmon_evidence")) or has_evidence_kind(record, "procmon"),
            "has_reboot_evidence": bool((gate.get("evidence_status") or {}).get("has_reboot_evidence")),
            "official_doc_ref": official_doc_ref(record),
            "evidence_refs": evidence_refs(record),
        },
        "app_mapping": {
            "status": gate.get("app_mapping_status"),
            "tweak_origin": gate.get("tweak_origin"),
            "component": domain["component"],
            "affected_processes": list(domain.get("affected_processes") or []),
            "interaction_surface": domain["interaction_surface"],
        },
        "risk_assessment": {
            "classification": domain["risk"],
            "reversibility": bool(rollback_status.get("rollback_verified")),
            "known_side_effects": list(domain.get("side_effects") or []),
            "vm_specific_considerations": domain.get("vm_impact"),
            "security_flag": "security-sensitive" if "security" in str(domain.get("risk") or "") else "standard",
            "score_breakdown": gate.get("score_breakdown") or {},
        },
        "rollback_plan": {
            "mechanism": rollback_mechanism(target, rollback_status),
            "validation": rollback_status.get("rollback_verification_method"),
            "rollback_value": rollback_status.get("rollback_value"),
        },
        "decision_options": decision_options(domain),
        "recommended_action": recommended,
        "requires_manual_vm_verification": requires_manual_vm,
        "requires_app_mapping": requires_app_mapping,
        "rationale": domain["rationale"],
    }


def build_pack(
    gate_payload: dict[str, Any],
    records: dict[str, dict[str, Any]],
    *,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or utc_now_iso()
    gates = gate_entries_by_id(gate_payload)
    eligible_ids = sorted(
        record_id
        for record_id, gate in gates.items()
        if str(gate.get("promotion_state") or "") == "promotion-eligible"
    )

    records_payload: list[dict[str, Any]] = []
    verdict_counts: Counter[str] = Counter()
    risk_counts: Counter[str] = Counter()
    requires_manual_vm_check: list[str] = []
    requires_app_mapping: list[str] = []

    for record_id in eligible_ids:
        record = records.get(record_id)
        if not record:
            raise KeyError(f"Missing research record for promotion-eligible id: {record_id}")
        analyzed = analyze_record(record_id, record, gates[record_id])
        records_payload.append(analyzed)
        verdict = str((analyzed.get("recommended_action") or {}).get("verdict") or "UNKNOWN")
        verdict_counts[verdict] += 1
        risk_counts[str((analyzed.get("risk_assessment") or {}).get("classification") or "unknown")] += 1
        if analyzed.get("requires_manual_vm_verification"):
            requires_manual_vm_check.append(record_id)
        if analyzed.get("requires_app_mapping"):
            requires_app_mapping.append(record_id)

    blocked_count = int((gate_payload.get("summary") or {}).get("promotion_state_counts", {}).get("blocked") or 0)
    unclassified_rejected = 0
    rejected_ledger_path = AUDIT_ROOT / "rejected-closure-ledger.json"
    if rejected_ledger_path.exists():
        rejected_ledger = load_json(rejected_ledger_path)
        unclassified_rejected = int((rejected_ledger.get("summary") or {}).get("unclassified_rejected") or 0)

    return {
        "schema_version": "1.0",
        "metadata": {
            "generated_utc": generated_utc,
            "campaign_id": "promotion-eligible-final-decision-wave-1",
            "total_records": len(records_payload),
            "source_gate": str(PROMOTION_GATES_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
            "preconditions": {
                "blocked_count": blocked_count,
                "unclassified_rejected": unclassified_rejected,
                "all_records_confidence_high": all(
                    str(((gates[item.get("record_id")] or {}).get("documentation_status") or {}).get("confidence") or "") == "high"
                    for item in records_payload
                ),
                "all_records_next_missing_layer_none": all(
                    str((gates[item.get("record_id")] or {}).get("next_missing_layer") or "") == "none"
                    for item in records_payload
                ),
            },
        },
        "summary_stats": {
            "promote_candidates": int(verdict_counts.get("PROMOTE") or 0),
            "promote_with_warnings_candidates": int(verdict_counts.get("PROMOTE-WITH-WARNINGS") or 0),
            "conditional_promote_candidates": int(verdict_counts.get("CONDITIONAL-PROMOTE") or 0),
            "hold_candidates": int(verdict_counts.get("INTENTIONAL-HOLD-CLOSED") or 0),
            "reject_candidates": int(verdict_counts.get("EVIDENCE-BACKED-REJECTED") or 0),
            "verdict_counts": dict(sorted(verdict_counts.items())),
            "risk_counts": dict(sorted(risk_counts.items())),
            "requires_manual_vm_check": requires_manual_vm_check,
            "requires_app_mapping": requires_app_mapping,
        },
        "records": records_payload,
    }


def markdown_cell(value: str) -> str:
    return str(value or "").replace("|", "\\|").replace("\n", " ").replace("`", "\\`")


def render_markdown(pack: dict[str, Any]) -> str:
    metadata = pack.get("metadata") or {}
    stats = pack.get("summary_stats") or {}
    preconditions = metadata.get("preconditions") or {}
    lines = [
        "# Promotion-Eligible Review Pack",
        "",
        f"Generated: `{metadata.get('generated_utc')}`",
        "",
        "This pack covers records that have no active evidence blocker but still need a final shipping decision.",
        "",
        "| Metric | Value |",
        "|---|---:|",
        f"| Total records | {int(metadata.get('total_records') or 0)} |",
        f"| Promote | {int(stats.get('promote_candidates') or 0)} |",
        f"| Promote with warnings | {int(stats.get('promote_with_warnings_candidates') or 0)} |",
        f"| Conditional promote | {int(stats.get('conditional_promote_candidates') or 0)} |",
        f"| Hold closed | {int(stats.get('hold_candidates') or 0)} |",
        f"| Reject | {int(stats.get('reject_candidates') or 0)} |",
        "",
        "## Preconditions",
        "",
        "| Check | Value |",
        "|---|---:|",
    ]
    for key, value in preconditions.items():
        lines.append(f"| `{markdown_cell(str(key))}` | `{markdown_cell(str(value))}` |")

    lines.extend(
        [
            "",
            "## Decision Matrix",
            "",
            "| Record | Risk | Recommended action | App mapping | Evidence | Manual checks | Rationale |",
            "|---|---|---|---|---:|---|---|",
        ]
    )
    for item in pack.get("records") or []:
        record_id = markdown_cell(str(item.get("record_id") or ""))
        risk = markdown_cell(str((item.get("risk_assessment") or {}).get("classification") or ""))
        action = markdown_cell(str((item.get("recommended_action") or {}).get("verdict") or ""))
        app_mapping = markdown_cell(str((item.get("app_mapping") or {}).get("status") or ""))
        evidence_count = int((item.get("evidence_status") or {}).get("evidence_count") or 0)
        checks = []
        if item.get("requires_manual_vm_verification"):
            checks.append("vm")
        if item.get("requires_app_mapping"):
            checks.append("app-mapping")
        checks_text = ", ".join(checks) if checks else "none"
        rationale = markdown_cell(str(item.get("rationale") or ""))
        lines.append(f"| `{record_id}` | `{risk}` | `{action}` | `{app_mapping}` | {evidence_count} | {checks_text} | {rationale} |")
    lines.append("")
    return "\n".join(lines)


def write_outputs(pack: dict[str, Any]) -> None:
    write_json(JSON_OUTPUT, pack)
    MARKDOWN_OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    MARKDOWN_OUTPUT.write_text(render_markdown(pack), encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate the promotion-eligible final decision review pack.")
    parser.add_argument("--emit-json", action="store_true", help="Print summary JSON.")
    args = parser.parse_args()

    pack = build_pack(load_json(PROMOTION_GATES_PATH), records_by_id())
    write_outputs(pack)

    if args.emit_json:
        print(json.dumps(pack["summary_stats"], ensure_ascii=False, indent=2))
    else:
        print(f"Wrote {JSON_OUTPUT}")
        print(f"Wrote {MARKDOWN_OUTPUT}")
        print(json.dumps(pack["summary_stats"], ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
