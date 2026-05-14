#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
from collections import defaultdict
from datetime import UTC, datetime
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
AUDIT_DIR = REPO_ROOT / "registry-research-framework" / "audit"
DEFAULT_PLAN = AUDIT_DIR / "operator96-value-campaign-20260509.json"
DEFAULT_CAMPAIGN = AUDIT_DIR / "operator96-value-campaign-tranche-20260509.json"

REFERENCE_CATALOG = [
    {
        "id": "microsoft-powercfg",
        "source_kind": "microsoft-doc",
        "title": "Powercfg command-line options",
        "url": "https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options",
        "use_for": ["power states", "sleep states", "power setting query/write semantics"],
    },
    {
        "id": "microsoft-cfg",
        "source_kind": "microsoft-doc",
        "title": "Control Flow Guard for platform security",
        "url": "https://learn.microsoft.com/en-us/windows/win32/secbp/control-flow-guard",
        "use_for": ["CFG/security mitigation boundaries"],
    },
    {
        "id": "microsoft-fast-startup",
        "source_kind": "microsoft-doc",
        "title": "Distinguishing fast startup from wake-from-hibernation",
        "url": "https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/distinguishing-fast-startup-from-wake-from-hibernation",
        "use_for": ["hibernation", "fast startup", "hiberfil.sys"],
    },
    {
        "id": "sysinternals-procmon",
        "source_kind": "microsoft-sysinternals-doc",
        "title": "Process Monitor",
        "url": "https://learn.microsoft.com/en-us/sysinternals/downloads/procmon",
        "use_for": ["Procmon boot logging", "registry write trace"],
    },
    {
        "id": "local-vm-defaults",
        "source_kind": "local-runtime",
        "title": "Win11 25H2 VM observed defaults",
        "url": "",
        "use_for": ["default value", "observed absence", "rollback proof"],
    },
    {
        "id": "runtime-evidence",
        "source_kind": "local-runtime",
        "title": "ETW/Procmon/Ghidra/operator96 VM artifacts",
        "url": "",
        "use_for": ["runtime validation", "static/runtime hints"],
    },
    {
        "id": "reactos-static",
        "source_kind": "static-hint",
        "title": "ReactOS/static string hints",
        "url": "https://github.com/reactos/reactos",
        "use_for": ["non-authoritative static hinting only"],
    },
]

SOURCE_KIND_PROOF_WEIGHT = {
    "vm-validated": 0,
    "local-default": 1,
    "microsoft-doc": 2,
    "source-backed": 3,
    "name-rule": 4,
    "static-hint": 5,
    "community-hint": 6,
}

PERCENT_NAME_RE = re.compile(r"(percent|percentage|frequencyoverride)", re.IGNORECASE)
TIMEOUT_NAME_RE = re.compile(r"(timeout|msec|millisecond|watchdog)", re.IGNORECASE)
THRESHOLD_NAME_RE = re.compile(r"(threshold|interval|delay|grace|depth|width)", re.IGNORECASE)
COUNT_NAME_RE = re.compile(r"(count|threads|classes)", re.IGNORECASE)


def now_utc() -> str:
    return datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def load_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def relative(path: Path) -> str:
    try:
        return str(path.resolve().relative_to(REPO_ROOT))
    except ValueError:
        return str(path)


def parse_int(value: Any) -> int | None:
    if value is None:
        return None
    try:
        return int(str(value), 0)
    except (TypeError, ValueError):
        return None


def record_key(row: dict[str, Any]) -> tuple[int, str, str]:
    return (
        int(row.get("index") or 0),
        str(row.get("registry_path") or ""),
        str(row.get("value_name") or ""),
    )


def source_rank(source: dict[str, Any]) -> int:
    return SOURCE_KIND_PROOF_WEIGHT.get(str(source.get("source_kind") or ""), 99)


def candidate_rank(candidate: dict[str, Any]) -> tuple[int, int]:
    sources = candidate.get("sources") if isinstance(candidate.get("sources"), list) else []
    best_source = min((source_rank(source) for source in sources if isinstance(source, dict)), default=99)
    return (best_source, int(candidate.get("value") or 0))


def add_candidate(candidates: dict[int, dict[str, Any]], value: int | None, source: dict[str, Any]) -> None:
    if value is None:
        return
    candidate = candidates.setdefault(
        value,
        {
            "value": value,
            "sources": [],
            "already_tested": False,
            "vm_validated": False,
            "requires_vm_validation": True,
            "community_only": False,
        },
    )
    if source not in candidate["sources"]:
        candidate["sources"].append(source)


def name_rules(value_name: str) -> set[str]:
    rules: set[str] = set()
    lower_name = value_name.lower()
    if (
        "enable" in lower_name
        or "disable" in lower_name
        or lower_name.endswith("off")
        or lower_name.endswith("on")
        or lower_name.startswith("off")
        or lower_name.startswith("on")
    ):
        rules.add("boolean-toggle")
    if PERCENT_NAME_RE.search(value_name):
        rules.add("percent-range")
    if TIMEOUT_NAME_RE.search(value_name):
        rules.add("timeout-boundary")
    if THRESHOLD_NAME_RE.search(value_name):
        rules.add("threshold-boundary")
    if COUNT_NAME_RE.search(value_name):
        rules.add("count-boundary")
    return rules


def source_matches_hint(row: dict[str, Any], hint: dict[str, Any]) -> bool:
    hint_name = hint.get("value_name")
    hint_path = hint.get("registry_path")
    if hint_name and str(hint_name).lower() != str(row.get("value_name") or "").lower():
        return False
    if hint_path and str(hint_path).lower() != str(row.get("registry_path") or "").lower():
        return False
    return bool(hint_name or hint_path)


def load_source_hints(path: Path | None) -> list[dict[str, Any]]:
    if not path:
        return []
    payload = load_json(path)
    if isinstance(payload, dict):
        hints = payload.get("hints", [])
    else:
        hints = payload
    return [hint for hint in hints if isinstance(hint, dict)]


def baseline_rows(plan_payload: dict[str, Any]) -> list[dict[str, Any]]:
    by_key: dict[tuple[int, str, str], dict[str, Any]] = {}
    for row in plan_payload.get("plan") or []:
        if not isinstance(row, dict):
            continue
        key = record_key(row)
        by_key.setdefault(key, row)
    return [by_key[key] for key in sorted(by_key)]


def tested_map(campaign_payload: dict[str, Any]) -> dict[tuple[int, str, str], dict[int, dict[str, Any]]]:
    tested: dict[tuple[int, str, str], dict[int, dict[str, Any]]] = defaultdict(dict)
    for result in campaign_payload.get("results") or []:
        if not isinstance(result, dict):
            continue
        value = parse_int(result.get("value_data"))
        if value is None:
            continue
        key = record_key(result)
        observations = result.get("observations") if isinstance(result.get("observations"), dict) else {}
        tested[key][value] = {
            "experiment_id": result.get("experiment_id"),
            "artifact_json": result.get("artifact_json"),
            "status": result.get("status"),
            "verdict": observations.get("verdict"),
            "confidence": observations.get("confidence"),
            "host_noise": observations.get("host_noise"),
            "restore_action": ((observations.get("post_reboot") or {}).get("restore_action") if isinstance(observations.get("post_reboot"), dict) else None),
        }
    return tested


def default_status(row: dict[str, Any]) -> str:
    kind = str(row.get("default_kind") or "")
    if kind == "observed-present":
        return "known-present"
    if kind == "observed-absent":
        return "known-absent"
    return "unknown"


def build_candidates(
    row: dict[str, Any],
    tested_values: dict[int, dict[str, Any]] | None = None,
    source_hints: list[dict[str, Any]] | None = None,
) -> list[dict[str, Any]]:
    tested_values = tested_values or {}
    source_hints = source_hints or []
    value_name = str(row.get("value_name") or "")
    candidates: dict[int, dict[str, Any]] = {}

    requested = parse_int(row.get("requested_data"))
    default_value = parse_int(row.get("default_value"))

    add_candidate(candidates, requested, {"source_kind": "source-backed", "source_label": "requested-script-value", "reason": "value requested by operator input"})
    if default_status(row) == "known-present":
        add_candidate(candidates, default_value, {"source_kind": "local-default", "source_label": "observed-default", "reason": "default value observed in Win11 25H2 VM"})
    elif default_status(row) == "known-absent":
        add_candidate(candidates, None, {"source_kind": "local-default", "source_label": "observed-absent", "reason": "value absent in Win11 25H2 VM"})

    for value, proof in tested_values.items():
        add_candidate(
            candidates,
            value,
            {
                "source_kind": "vm-validated",
                "source_label": proof.get("experiment_id") or "operator96-baseline",
                "reason": f"operator96 baseline VM experiment status={proof.get('status')} verdict={proof.get('verdict')}",
            },
        )

    rules = name_rules(value_name)
    if "boolean-toggle" in rules:
        for value in (0, 1):
            add_candidate(candidates, value, {"source_kind": "name-rule", "source_label": "boolean-toggle", "reason": "enable/disable/off/on naming convention"})
    if "percent-range" in rules:
        for value in (0, 1, 50, 100):
            add_candidate(candidates, value, {"source_kind": "name-rule", "source_label": "percent-range", "reason": "percent-like value name"})
    if rules.intersection({"timeout-boundary", "threshold-boundary", "count-boundary"}):
        for value in (requested, default_value, 0, 1):
            add_candidate(candidates, value, {"source_kind": "name-rule", "source_label": ",".join(sorted(rules)), "reason": "boundary candidate for count/timeout/threshold-style value"})

    for hint in source_hints:
        if not source_matches_hint(row, hint):
            continue
        value = parse_int(hint.get("value"))
        source_kind = str(hint.get("source_kind") or "source-backed")
        add_candidate(
            candidates,
            value,
            {
                "source_kind": source_kind,
                "source_label": hint.get("source_label") or hint.get("url") or "external-hint",
                "reason": hint.get("reason") or "external value hint; requires VM validation before proof",
                "url": hint.get("url") or "",
            },
        )

    for value, candidate in candidates.items():
        proof = tested_values.get(value)
        if proof:
            candidate["already_tested"] = True
            candidate["vm_validated"] = proof.get("status") == "ok"
            candidate["requires_vm_validation"] = False
            candidate["baseline_proof"] = proof
        source_kinds = {str(source.get("source_kind") or "") for source in candidate["sources"] if isinstance(source, dict)}
        candidate["community_only"] = source_kinds == {"community-hint"}
        if candidate["community_only"]:
            candidate["requires_vm_validation"] = True
        candidate["primary_source_kind"] = min(candidate["sources"], key=source_rank).get("source_kind") if candidate["sources"] else "unknown"
        candidate["sources"] = sorted(candidate["sources"], key=source_rank)

    return sorted(candidates.values(), key=candidate_rank)


def evidence_review(row: dict[str, Any]) -> dict[str, Any]:
    record_class = str(row.get("record_class") or "")
    source_quality = str(row.get("source_quality") or "")
    no_evidence = record_class == "key-missing" and "no-authoritative-evidence" in source_quality
    return {
        "lanes": [
            {"lane": "local-repo-search", "status": "covered-by-existing-audit" if source_quality else "pending"},
            {"lane": "admx-adml", "status": "pending-if-not-already-covered"},
            {"lane": "microsoft-docs", "status": "pending-if-not-already-covered"},
            {"lane": "reactos-static", "status": "hint-only"},
            {"lane": "etw-procmon-ghidra", "status": "runtime/static-proof-required-for-promotion"},
        ],
        "outcome": "no-evidence-found-on-win11-25h2" if no_evidence else "evidence-lanes-open-or-covered",
    }


def risk_review_flags(row: dict[str, Any]) -> list[str]:
    value_name = str(row.get("value_name") or "").lower()
    flags = list(row.get("risk_flags") or [])
    if "disableexceptionchainvalidation" in value_name or "disablecontrolflowguard" in value_name:
        flags.append("security-mitigation-override")
    if "workerthreads" in value_name or "kernelworker" in value_name:
        flags.append("kernel-worker-thread-override")
    if "watchdog" in value_name:
        flags.append("watchdog-timeout-sensitive")
    return sorted(set(str(flag) for flag in flags if flag))


def app_surface_gate(row: dict[str, Any], candidates: list[dict[str, Any]], tested_values: dict[int, dict[str, Any]]) -> dict[str, Any]:
    reasons: list[str] = []
    risk_flags = risk_review_flags(row)
    if default_status(row) == "unknown":
        reasons.append("default-not-known")
    if str(row.get("record_class") or "") == "key-missing":
        reasons.append("key-missing-in-target-vm")
    if not (row.get("registry_path") and row.get("value_name")):
        reasons.append("app-write-not-explicit")
    rollback_tested = any(proof.get("restore_action") for proof in tested_values.values())
    if not rollback_tested:
        reasons.append("rollback-not-tested")
    if any(proof.get("verdict") in {"boot_failure", "rollback_failure", "app_breakage"} for proof in tested_values.values()):
        reasons.append("safety-finding-present")
    if any(candidate.get("community_only") for candidate in candidates):
        reasons.append("community-hints-require-vm-validation")
    if "security-mitigation-override" in risk_flags:
        reasons.append("security-mitigation-override")
    if "kernel-worker-thread-override" in risk_flags:
        reasons.append("kernel-worker-thread-override")
    return {
        "eligible_for_app_card": not reasons,
        "default_status": default_status(row),
        "rollback_tested": rollback_tested,
        "app_write_explicit": bool(row.get("registry_path") and row.get("value_name")),
        "claim_boundary": "VM smoke/default/rollback evidence only; no performance claim without repeated low-noise runs",
        "risk_flags": risk_flags,
        "blockers": reasons,
    }


def build_matrix(plan_payload: dict[str, Any], campaign_payload: dict[str, Any], source_hints: list[dict[str, Any]] | None = None) -> dict[str, Any]:
    tested = tested_map(campaign_payload)
    records: list[dict[str, Any]] = []
    candidate_count = 0
    app_eligible_count = 0
    community_hint_count = 0

    for row in baseline_rows(plan_payload):
        key = record_key(row)
        tested_values = tested.get(key, {})
        candidates = build_candidates(row, tested_values, source_hints)
        gate = app_surface_gate(row, candidates, tested_values)
        candidate_count += len(candidates)
        app_eligible_count += 1 if gate["eligible_for_app_card"] else 0
        community_hint_count += sum(1 for candidate in candidates if candidate.get("community_only"))
        records.append(
            {
                "index": key[0],
                "registry_path": key[1],
                "value_name": key[2],
                "requested_data": row.get("requested_data"),
                "default_kind": row.get("default_kind"),
                "default_value": row.get("default_value"),
                "default_status": default_status(row),
                "vm_status": row.get("vm_status"),
                "record_class": row.get("record_class"),
                "source_quality": row.get("source_quality"),
                "name_rules": sorted(name_rules(key[2])),
                "risk_review_flags": risk_review_flags(row),
                "evidence_review": evidence_review(row),
                "candidates": candidates,
                "app_surface_gate": gate,
            }
        )

    return {
        "schema_version": "1.0",
        "generated_utc": now_utc(),
        "campaign_id": "operator96-enriched-values-20260510",
        "inputs": {
            "plan_records": len(records),
            "campaign_results": len(campaign_payload.get("results") or []),
        },
        "reference_catalog": REFERENCE_CATALOG,
        "rules": {
            "boolean": "enable/disable/off/on names receive 0/1 candidates",
            "percent": "percent/frequency override names receive 0/1/50/100 candidates",
            "timeout_threshold_count": "requested/default/0/1 are included; extra small/large values must come from source hints",
            "community": "community-hint values never count as proof without VM validation",
        },
        "summary": {
            "records": len(records),
            "candidate_values": candidate_count,
            "app_card_eligible_records": app_eligible_count,
            "community_only_candidates": community_hint_count,
        },
        "records": records,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    lines = [
        "# Operator96 Enriched Value Matrix",
        "",
        f"- Generated UTC: `{payload.get('generated_utc')}`",
        f"- Campaign: `{payload.get('campaign_id')}`",
        f"- Records: `{(payload.get('summary') or {}).get('records')}`",
        f"- Candidate values: `{(payload.get('summary') or {}).get('candidate_values')}`",
        f"- App-card eligible records: `{(payload.get('summary') or {}).get('app_card_eligible_records')}`",
        "",
        "## Reference Catalog",
        "",
    ]
    for ref in payload.get("reference_catalog") or []:
        url = ref.get("url")
        suffix = f" ({url})" if url else ""
        lines.append(f"- `{ref.get('id')}`: {ref.get('title')}{suffix}")

    lines.extend(
        [
            "",
            "## Records",
            "",
            "| # | Value | Default | Rules | Candidates | App gate | Notes |",
            "|---:|---|---|---|---|---|---|",
        ]
    )
    for row in payload.get("records") or []:
        candidates = ", ".join(f"`{candidate.get('value')}`:{candidate.get('primary_source_kind')}" for candidate in row.get("candidates") or [])
        rules = ", ".join(f"`{rule}`" for rule in row.get("name_rules") or []) or "-"
        gate = row.get("app_surface_gate") or {}
        blockers = ", ".join(f"`{blocker}`" for blocker in gate.get("blockers") or []) or "none"
        notes = str((row.get("evidence_review") or {}).get("outcome") or "").replace("|", "\\|")
        lines.append(
            f"| {row.get('index')} | `{row.get('value_name')}` | `{row.get('default_status')}` | {rules} | "
            f"{candidates} | `eligible={gate.get('eligible_for_app_card')}` {blockers} | {notes} |"
        )
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate enriched Operator96 value candidates with proof boundaries.")
    parser.add_argument("--plan", default=str(DEFAULT_PLAN))
    parser.add_argument("--campaign", default=str(DEFAULT_CAMPAIGN))
    parser.add_argument("--source-hints", default="")
    parser.add_argument("--output", default="")
    parser.add_argument("--markdown-output", default="")
    args = parser.parse_args()

    plan_path = Path(args.plan).resolve()
    campaign_path = Path(args.campaign).resolve()
    date = now_utc()[:10].replace("-", "")
    output = Path(args.output).resolve() if args.output else AUDIT_DIR / f"operator96-enriched-value-matrix-{date}.json"
    markdown_output = Path(args.markdown_output).resolve() if args.markdown_output else output.with_suffix(".md")
    hints_path = Path(args.source_hints).resolve() if args.source_hints else None

    payload = build_matrix(load_json(plan_path), load_json(campaign_path), load_source_hints(hints_path))
    payload["inputs"].update(
        {
            "plan": relative(plan_path),
            "campaign": relative(campaign_path),
            "source_hints": relative(hints_path) if hints_path else None,
        }
    )
    write_json(output, payload)
    markdown_output.parent.mkdir(parents=True, exist_ok=True)
    markdown_output.write_text(render_markdown(payload), encoding="utf-8")
    print(json.dumps({"status": "ok", "json": relative(output), "markdown": relative(markdown_output), "records": payload["summary"]["records"]}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
