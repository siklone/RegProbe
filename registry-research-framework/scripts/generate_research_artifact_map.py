#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from datetime import UTC, datetime
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_JSON_OUTPUT = REPO_ROOT / "registry-research-framework" / "audit" / "research-artifact-map-latest.json"
DEFAULT_MARKDOWN_OUTPUT = REPO_ROOT / "Docs" / "research" / "artifact-map.md"


def utc_now() -> str:
    return datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def rel(path: Path, repo_root: Path = REPO_ROOT) -> str:
    return path.relative_to(repo_root).as_posix()


def load_json(path: Path) -> dict[str, Any]:
    if not path.exists():
        return {}
    try:
        payload = json.loads(path.read_text(encoding="utf-8-sig"))
    except json.JSONDecodeError as exc:
        return {"_load_error": str(exc)}
    return payload if isinstance(payload, dict) else {"_load_error": "top-level JSON is not an object"}


def status_from_bool(ok: bool, *, ok_label: str = "ok", fail_label: str = "attention") -> str:
    return ok_label if ok else fail_label


def summarize_app_readiness(repo_root: Path) -> tuple[str, dict[str, Any]]:
    path = repo_root / "registry-research-framework" / "audit" / "app-retest-readiness-latest.json"
    payload = load_json(path)
    summary = payload.get("summary") if isinstance(payload.get("summary"), dict) else {}
    status = str(payload.get("check_status") or payload.get("status") or "missing").lower()
    ok = status == "pass" and not payload.get("_load_error")
    return status_from_bool(ok), {
        "check_status": payload.get("check_status") or payload.get("status"),
        "app_surface_entry_count": summary.get("app_surface_entry_count"),
        "apply_allowed_record_count": summary.get("apply_allowed_record_count"),
        "missing_rollback_story_count": summary.get("missing_rollback_story_count"),
        "kvm_app_smoke_status": summary.get("kvm_app_smoke_status"),
        "kvm_lane_health_status": summary.get("kvm_lane_health_status"),
    }


def summarize_card_contracts(repo_root: Path) -> tuple[str, dict[str, Any]]:
    path = repo_root / "registry-research-framework" / "audit" / "app-card-evidence-contracts-latest.json"
    payload = load_json(path)
    summary = payload.get("summary") if isinstance(payload.get("summary"), dict) else {}
    ok = str(payload.get("status") or "").upper() == "PASS" and int(summary.get("fail_count") or 0) == 0
    return status_from_bool(ok), {
        "status": payload.get("status"),
        "candidate_count": summary.get("candidate_count"),
        "pass_count": summary.get("pass_count"),
        "fail_count": summary.get("fail_count"),
    }


def summarize_promoted_qa(repo_root: Path) -> tuple[str, dict[str, Any]]:
    path = repo_root / "registry-research-framework" / "audit" / "promoted-app-qa-batch-latest.json"
    payload = load_json(path)
    summary = payload.get("summary") if isinstance(payload.get("summary"), dict) else {}
    ok = str(payload.get("status") or "").upper() == "PASS" and int(summary.get("live_failure_count") or 0) == 0
    return status_from_bool(ok), {
        "status": payload.get("status"),
        "planned_count": summary.get("planned_count"),
        "live_success_count": summary.get("live_success_count"),
        "live_failure_count": summary.get("live_failure_count"),
    }


def summarize_promoted_coverage(repo_root: Path) -> tuple[str, dict[str, Any]]:
    path = repo_root / "registry-research-framework" / "audit" / "promoted-app-qa-coverage-latest.json"
    payload = load_json(path)
    summary = payload.get("summary") if isinstance(payload.get("summary"), dict) else {}
    uncovered = summary.get("uncovered_categories") if isinstance(summary.get("uncovered_categories"), dict) else {}
    coverage = float(summary.get("coverage_percent") or 0.0)
    ok = coverage >= 100.0 and not uncovered and not payload.get("_load_error")
    return status_from_bool(ok), {
        "coverage_percent": coverage,
        "uncovered_categories": uncovered,
    }


def summarize_operator96_aggregate(repo_root: Path) -> tuple[str, dict[str, Any]]:
    path = repo_root / "registry-research-framework" / "audit" / "operator96-low-noise-rerun-aggregate-20260512.json"
    payload = load_json(path)
    summary = payload.get("summary") if isinstance(payload.get("summary"), dict) else {}
    non_ok = int(summary.get("non_ok_count") or 0)
    noisy = int(summary.get("noisy_result_count") or 0)
    ok = str(payload.get("status") or "").lower() == "ok" and non_ok == 0 and noisy == 0
    return status_from_bool(ok), {
        "status": payload.get("status"),
        "result_count": summary.get("result_count"),
        "non_ok_count": non_ok,
        "noisy_result_count": noisy,
        "host_noise_counts": summary.get("host_noise_counts"),
        "confidence_counts": summary.get("confidence_counts"),
        "verdict_counts": summary.get("verdict_counts"),
    }


def summarize_operator96_surface(repo_root: Path) -> tuple[str, dict[str, Any]]:
    path = repo_root / "registry-research-framework" / "audit" / "operator96-app-surface-review-20260510.json"
    payload = load_json(path)
    summary = payload.get("summary") if isinstance(payload.get("summary"), dict) else {}
    aggregate_blocked = bool(summary.get("aggregate_surface_blocked"))
    ok = str(payload.get("status") or "").upper() == "PASS" and not aggregate_blocked
    return status_from_bool(ok, ok_label="research-only-ok"), {
        "status": payload.get("status"),
        "record_count": summary.get("record_count"),
        "ready_for_bounded_app_card": summary.get("ready_for_bounded_app_card"),
        "needs_low_noise_rerun": summary.get("needs_low_noise_rerun"),
        "aggregate_surface_blocked": aggregate_blocked,
        "bucket_counts": summary.get("bucket_counts"),
    }


def summarize_cleanup_ledger(repo_root: Path) -> tuple[str, dict[str, Any]]:
    candidates = sorted((repo_root / "registry-research-framework" / "audit").glob("cleanup-quarantine-ledger-*.json"))
    path = candidates[-1] if candidates else repo_root / "registry-research-framework" / "audit" / "cleanup-quarantine-ledger-20260514.json"
    payload = load_json(path)
    summary = payload.get("summary") if isinstance(payload.get("summary"), dict) else {}
    delete_eligible = int(summary.get("delete_eligible_count") or 0)
    ok = delete_eligible == 0 and not payload.get("_load_error")
    return status_from_bool(ok, ok_label="no-delete-eligible", fail_label="review-delete-eligible"), {
        "ledger_path": rel(path, repo_root) if path.exists() else rel(path, repo_root),
        "total_items": summary.get("total_items"),
        "delete_candidate_count": summary.get("delete_candidate_count"),
        "retained_inventory_count": summary.get("retained_inventory_count"),
        "referenced_count": summary.get("referenced_count"),
        "blocking_referenced_count": summary.get("blocking_referenced_count"),
        "audit_only_referenced_count": summary.get("audit_only_referenced_count"),
        "delete_eligible_count": delete_eligible,
        "total_size_bytes": summary.get("total_size_bytes"),
    }


def summarize_cleanup_retained_plan(repo_root: Path) -> tuple[str, dict[str, Any]]:
    path = repo_root / "registry-research-framework" / "audit" / "cleanup-retained-inventory-plan-20260514.json"
    payload = load_json(path)
    summary = payload.get("summary") if isinstance(payload.get("summary"), dict) else {}
    delete_ready = int(summary.get("delete_ready_count") or 0)
    migration_needed = int(summary.get("reference_migration_needed_count") or 0)
    ok = path.exists() and not payload.get("_load_error") and delete_ready == 0
    return status_from_bool(ok, ok_label="retained-plan-ready", fail_label="review-delete-ready"), {
        "plan_path": rel(path, repo_root) if path.exists() else rel(path, repo_root),
        "item_count": summary.get("item_count"),
        "delete_ready_count": delete_ready,
        "reference_migration_needed_count": migration_needed,
        "retention_decision_queue_count": summary.get("retention_decision_queue_count"),
        "audit_only_retained_count": summary.get("audit_only_retained_count"),
        "intentional_reference_keep_count": summary.get("intentional_reference_keep_count"),
        "needs_replacement_or_retention_decision_count": summary.get("needs_replacement_or_retention_decision_count"),
        "retained_pending_review_count": summary.get("retained_pending_review_count"),
        "release_state_counts": summary.get("release_state_counts"),
        "decision_track_counts": summary.get("decision_track_counts"),
    }


def summarize_vm_health(repo_root: Path) -> tuple[str, dict[str, Any]]:
    path = repo_root / "registry-research-framework" / "audit" / "vm-health-check-latest.json"
    payload = load_json(path)
    ok = str(payload.get("status") or "").lower() == "ok" and not payload.get("failed_checks")
    return status_from_bool(ok), {
        "status": payload.get("status"),
        "guest_health": payload.get("guest_health"),
        "failed_checks": payload.get("failed_checks"),
        "transport_blocker": payload.get("transport_blocker"),
    }


def summarize_kvm_smoke(repo_root: Path) -> tuple[str, dict[str, Any]]:
    path = repo_root / "registry-research-framework" / "audit" / "kvm-app-publish-deploy-smoke-latest.json"
    payload = load_json(path)
    ok = str(payload.get("status") or "").lower() == "ok" and payload.get("deploy_smoke_returncode") == 0
    return status_from_bool(ok), {
        "status": payload.get("status"),
        "self_contained": payload.get("self_contained"),
        "publish_returncode": payload.get("publish_returncode"),
        "deploy_smoke_returncode": payload.get("deploy_smoke_returncode"),
        "guest_health": payload.get("guest_health"),
    }


def summarize_contributor_lab_smoke(repo_root: Path) -> tuple[str, dict[str, Any]]:
    path = repo_root / "registry-research-framework" / "audit" / "kvm-app-contributor-lab-smoke-latest.json"
    payload = load_json(path)
    deploy_payload = payload.get("deploy_smoke_payload") if isinstance(payload.get("deploy_smoke_payload"), dict) else {}
    smoke_payload = deploy_payload.get("smoke_payload") if isinstance(deploy_payload.get("smoke_payload"), dict) else {}
    app_args = payload.get("app_args") if isinstance(payload.get("app_args"), list) else []
    ok = (
        str(payload.get("status") or "").lower() == "ok"
        and str(smoke_payload.get("status") or "").lower() == "ok"
        and "--contributor-lab" in app_args
        and not smoke_payload.get("new_crash_log_detected")
    )
    return status_from_bool(ok), {
        "status": payload.get("status"),
        "app_args": app_args,
        "smoke_status": smoke_payload.get("status"),
        "new_crash_log_detected": smoke_payload.get("new_crash_log_detected"),
        "guest_health": payload.get("guest_health"),
    }


def static_artifact(
    *,
    artifact_id: str,
    path: str,
    audience: str,
    tier: str,
    purpose: str,
    use_when: str,
    avoid_when: str,
    status: str = "reference",
    details: dict[str, Any] | None = None,
) -> dict[str, Any]:
    return {
        "id": artifact_id,
        "path": path,
        "audience": audience,
        "tier": tier,
        "purpose": purpose,
        "use_when": use_when,
        "avoid_when": avoid_when,
        "status": status,
        "details": details or {},
    }


def build_artifact_map(repo_root: Path = REPO_ROOT) -> dict[str, Any]:
    app_readiness_status, app_readiness = summarize_app_readiness(repo_root)
    card_status, card_contracts = summarize_card_contracts(repo_root)
    promoted_qa_status, promoted_qa = summarize_promoted_qa(repo_root)
    coverage_status, coverage = summarize_promoted_coverage(repo_root)
    op96_aggregate_status, op96_aggregate = summarize_operator96_aggregate(repo_root)
    op96_surface_status, op96_surface = summarize_operator96_surface(repo_root)
    cleanup_status, cleanup = summarize_cleanup_ledger(repo_root)
    retained_plan_status, retained_plan = summarize_cleanup_retained_plan(repo_root)
    vm_status, vm_health = summarize_vm_health(repo_root)
    kvm_status, kvm_smoke = summarize_kvm_smoke(repo_root)
    contributor_smoke_status, contributor_smoke = summarize_contributor_lab_smoke(repo_root)

    artifacts = [
        static_artifact(
            artifact_id="app-surface-cards",
            path="Docs/research/app-surface/validated-registry-values.json",
            audience="end-user app, contributor",
            tier="canonical",
            purpose="Source of shipped app-card registry surfaces.",
            use_when="You need to know which cards normal users may see.",
            avoid_when="You are evaluating unshipped Operator96 or raw experiment records.",
            status="reference",
        ),
        static_artifact(
            artifact_id="app-retest-readiness",
            path="registry-research-framework/audit/app-retest-readiness-latest.json",
            audience="contributor, release QA",
            tier="canonical-latest",
            purpose="Checks public repo hygiene, app-surface resolution, evidence surfaces, rollback stories, and KVM smoke references.",
            use_when="Before manual app retest or after changing cards/evidence/rollback mapping.",
            avoid_when="You need a per-card live apply report; use promoted-app-qa instead.",
            status=app_readiness_status,
            details=app_readiness,
        ),
        static_artifact(
            artifact_id="app-card-contracts",
            path="registry-research-framework/audit/app-card-evidence-contracts-latest.json",
            audience="contributor, release QA",
            tier="canonical-latest",
            purpose="Verifies shipped card snapshots expose required trust fields and proof lanes.",
            use_when="After changing card copy, evidence drawer data, or app-surface records.",
            avoid_when="You need to prove registry mutation works; use promoted-app-qa.",
            status=card_status,
            details=card_contracts,
        ),
        static_artifact(
            artifact_id="promoted-app-qa-live-batch",
            path="registry-research-framework/audit/promoted-app-qa-batch-latest.json",
            audience="contributor, release QA",
            tier="canonical-latest",
            purpose="Newest live VM app harness batch for promoted apply/verify/rollback cards.",
            use_when="You need evidence that a representative shipped-card batch still applies and rolls back.",
            avoid_when="You need total coverage across every card; use promoted-app-qa-coverage.",
            status=promoted_qa_status,
            details=promoted_qa,
        ),
        static_artifact(
            artifact_id="promoted-app-qa-coverage",
            path="registry-research-framework/audit/promoted-app-qa-coverage-latest.json",
            audience="contributor, release QA",
            tier="canonical-latest",
            purpose="Cumulative app-QA coverage summary over promoted app-card candidates.",
            use_when="You need to know whether any promoted app-QA category remains uncovered.",
            avoid_when="You need newest run detail; use promoted-app-qa-live-batch.",
            status=coverage_status,
            details=coverage,
        ),
        static_artifact(
            artifact_id="single-tweak-lookup",
            path="registry-research-framework/scripts/check_single_tweak.py",
            audience="contributor, agentic AI",
            tier="canonical-script",
            purpose="Host-safe lookup for one tweak, value name, path, or expected value.",
            use_when="A user asks whether a key/value exists, is read, or is written by the app.",
            avoid_when="You need live app mutation; use check_single_tweak_app_qa.py or KVM app QA.",
            status="reference",
        ),
        static_artifact(
            artifact_id="operator96-low-noise-aggregate",
            path="registry-research-framework/audit/operator96-low-noise-rerun-aggregate-20260512.json",
            audience="contributor, research",
            tier="canonical-research",
            purpose="Low-noise aggregate for Operator96 registry value experiments.",
            use_when="You need to know whether noisy/non-ok reruns remain.",
            avoid_when="You are building normal end-user app cards.",
            status=op96_aggregate_status,
            details=op96_aggregate,
        ),
        static_artifact(
            artifact_id="operator96-app-surface-review",
            path="registry-research-framework/audit/operator96-app-surface-review-20260510.json",
            audience="contributor, research",
            tier="canonical-research",
            purpose="Explains why Operator96 records remain Contributor Lab observations instead of normal cards.",
            use_when="You need to decide whether any Operator96 record may enter app cards.",
            avoid_when="You want an optimization claim; this review blocks unbounded claims.",
            status=op96_surface_status,
            details=op96_surface,
        ),
        static_artifact(
            artifact_id="cleanup-quarantine-ledger",
            path=str(cleanup.get("ledger_path") or "registry-research-framework/audit/cleanup-quarantine-ledger-20260514.json"),
            audience="maintainer, contributor",
            tier="canonical-safety-ledger",
            purpose="Deletion-first safety gate for stale reports, raw traces, old parses, and staging bundles.",
            use_when="Before deleting or moving any archived/raw evidence or historical parse artifact.",
            avoid_when="You are looking for shipped app state; use app-surface/readiness artifacts.",
            status=cleanup_status,
            details=cleanup,
        ),
        static_artifact(
            artifact_id="cleanup-retained-inventory-plan",
            path=str(retained_plan.get("plan_path") or "registry-research-framework/audit/cleanup-retained-inventory-plan-20260514.json"),
            audience="maintainer, contributor",
            tier="canonical-action-plan",
            purpose="Action plan for retained cleanup inventory that is not currently delete-eligible.",
            use_when="After the quarantine ledger reports retained inventory and you need to reduce references or decide explicit retention.",
            avoid_when="You need the deletion safety contract itself; use cleanup-quarantine-ledger.",
            status=retained_plan_status,
            details=retained_plan,
        ),
        static_artifact(
            artifact_id="vm-health",
            path="registry-research-framework/audit/vm-health-check-latest.json",
            audience="contributor, VM operator",
            tier="canonical-latest",
            purpose="Latest QGA/domstate/guest-exec health contract.",
            use_when="Before ETW, Ghidra, app deploy smoke, or registry mutation experiments.",
            avoid_when="You need historical VM incident context.",
            status=vm_status,
            details=vm_health,
        ),
        static_artifact(
            artifact_id="kvm-app-publish-deploy-smoke",
            path="registry-research-framework/audit/kvm-app-publish-deploy-smoke-latest.json",
            audience="contributor, release QA",
            tier="canonical-latest",
            purpose="Latest self-contained publish, VM upload, expand, launch, and crash-log smoke.",
            use_when="After WPF/app-shell changes or before manual app retesting.",
            avoid_when="You need card-level apply/rollback evidence.",
            status=kvm_status,
            details=kvm_smoke,
        ),
        static_artifact(
            artifact_id="kvm-contributor-lab-smoke",
            path="registry-research-framework/audit/kvm-app-contributor-lab-smoke-latest.json",
            audience="contributor, release QA",
            tier="canonical-latest",
            purpose="Latest self-contained VM launch smoke for the gated Contributor Lab startup path.",
            use_when="After Contributor Lab, startup navigation, or contributor readiness UI changes.",
            avoid_when="You need normal end-user card apply/rollback evidence.",
            status=contributor_smoke_status,
            details=contributor_smoke,
        ),
        static_artifact(
            artifact_id="rejected-closure-ledger",
            path="registry-research-framework/audit/rejected-closure-ledger.md",
            audience="contributor, audit",
            tier="historical-archive",
            purpose="Frozen closure story for rejected/deprecated records.",
            use_when="You need to understand why rejected does not mean evidence missing.",
            avoid_when="You are looking for active backlog.",
            status="archive",
        ),
        static_artifact(
            artifact_id="v36-clean-state-report",
            path="registry-research-framework/audit/v36-clean-state-report.md",
            audience="contributor, audit",
            tier="historical-checkpoint",
            purpose="Zero-pending checkpoint for the v36 classification campaign.",
            use_when="You need the historical clean-state audit snapshot.",
            avoid_when="You need today's app/VM retest state.",
            status="archive",
        ),
    ]

    attention_statuses = {"attention", "review-delete-eligible", "review-delete-ready"}
    attention = [item for item in artifacts if item.get("status") in attention_statuses]
    summary = {
        "artifact_count": len(artifacts),
        "attention_count": len(attention),
        "normal_app_cards_source": "Docs/research/app-surface/validated-registry-values.json",
        "operator96_normal_app_card_ready": op96_surface.get("ready_for_bounded_app_card"),
        "operator96_non_ok_count": op96_aggregate.get("non_ok_count"),
        "operator96_noisy_result_count": op96_aggregate.get("noisy_result_count"),
        "cleanup_delete_eligible_count": cleanup.get("delete_eligible_count"),
        "cleanup_retained_inventory_count": cleanup.get("retained_inventory_count"),
        "cleanup_reference_migration_needed_count": retained_plan.get("reference_migration_needed_count"),
        "cleanup_retention_decision_queue_count": retained_plan.get("retention_decision_queue_count"),
        "cleanup_audit_only_retained_count": retained_plan.get("audit_only_retained_count"),
        "app_card_contract_pass_count": card_contracts.get("pass_count"),
        "app_card_contract_fail_count": card_contracts.get("fail_count"),
        "promoted_app_qa_live_success_count": promoted_qa.get("live_success_count"),
        "promoted_app_qa_live_failure_count": promoted_qa.get("live_failure_count"),
        "contributor_lab_smoke_status": contributor_smoke.get("status"),
    }

    return {
        "schema_version": "1.0",
        "generated_utc": utc_now(),
        "summary": summary,
        "rules": {
            "end_user_surface": "Normal users start from the WPF app and validated app-surface records, not raw audit folders.",
            "operator96_surface": "Operator96 remains Contributor Lab / research observation unless ready_for_bounded_app_card is positive and all gates stay clean.",
            "cleanup": "Do not delete archived/raw evidence unless the cleanup quarantine ledger reports live_reference_count=0 and a replacement or explicit obsolete reason exists. Use the retained inventory plan to reduce references before deletion.",
            "performance_claims": "No benchmark/performance claim ships from a single noisy, low-confidence, or community-only observation.",
        },
        "raw_parse_do_not_start_here": [
            "registry-research-framework/audit/registry-value-experiments*",
            "registry-research-framework/audit/operator96-low-noise-rerun-tranche-*",
            "evidence/raw/**",
            "evidence/files/vm-tooling-staging/**",
        ],
        "artifacts": artifacts,
    }


def markdown_cell(value: Any) -> str:
    return str(value if value is not None else "").replace("|", "\\|")


def render_markdown(payload: dict[str, Any]) -> str:
    summary = payload.get("summary") or {}
    lines = [
        "# Research Artifact Map",
        "",
        f"Generated: `{payload.get('generated_utc')}`",
        "",
        "This is the contributor entrypoint for current research artifacts. Use it",
        "instead of browsing raw audit folders first.",
        "",
        "## Current Gates",
        "",
        f"- App-card contracts: `{summary.get('app_card_contract_pass_count')}` pass, `{summary.get('app_card_contract_fail_count')}` fail.",
        f"- Promoted app QA latest: `{summary.get('promoted_app_qa_live_success_count')}` live success, `{summary.get('promoted_app_qa_live_failure_count')}` live failure.",
        f"- Contributor Lab VM smoke: `{summary.get('contributor_lab_smoke_status')}`.",
        f"- Operator96 app-card ready: `{summary.get('operator96_normal_app_card_ready')}`.",
        f"- Operator96 noisy results: `{summary.get('operator96_noisy_result_count')}`; non-ok results: `{summary.get('operator96_non_ok_count')}`.",
        f"- Cleanup delete-eligible items: `{summary.get('cleanup_delete_eligible_count')}`.",
        f"- Cleanup retained inventory: `{summary.get('cleanup_retained_inventory_count')}`; reference migration needed: `{summary.get('cleanup_reference_migration_needed_count')}`; retention decision queue: `{summary.get('cleanup_retention_decision_queue_count')}`; audit-only retained: `{summary.get('cleanup_audit_only_retained_count')}`.",
        "",
        "## Rules",
        "",
    ]
    for key, value in (payload.get("rules") or {}).items():
        lines.append(f"- `{key}`: {value}")
    lines.extend(
        [
            "",
            "## Canonical Artifacts",
            "",
            "| ID | Tier | Status | Audience | Path | Use when | Avoid when |",
            "|---|---|---|---|---|---|---|",
        ]
    )
    for item in payload.get("artifacts") or []:
        lines.append(
            "| "
            f"`{markdown_cell(item.get('id'))}` | "
            f"`{markdown_cell(item.get('tier'))}` | "
            f"`{markdown_cell(item.get('status'))}` | "
            f"{markdown_cell(item.get('audience'))} | "
            f"`{markdown_cell(item.get('path'))}` | "
            f"{markdown_cell(item.get('use_when'))} | "
            f"{markdown_cell(item.get('avoid_when'))} |"
        )
    lines.extend(
        [
            "",
            "## Do Not Start From Raw Parse Folders",
            "",
            "These paths are valid historical evidence, but they are not the first stop",
            "for normal app QA or contributor onboarding:",
            "",
        ]
    )
    for path in payload.get("raw_parse_do_not_start_here") or []:
        lines.append(f"- `{path}`")
    lines.extend(
        [
            "",
            "If one of those files appears stale, add it to the cleanup quarantine",
            "ledger and prove zero live references before deleting it.",
            "",
        ]
    )
    return "\n".join(lines)


def write_outputs(payload: dict[str, Any], json_output: Path, markdown_output: Path) -> None:
    json_output.parent.mkdir(parents=True, exist_ok=True)
    json_output.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    markdown_output.parent.mkdir(parents=True, exist_ok=True)
    markdown_output.write_text(render_markdown(payload), encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate the research artifact map.")
    parser.add_argument("--json-output", type=Path, default=DEFAULT_JSON_OUTPUT)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_OUTPUT)
    parser.add_argument("--emit-json", action="store_true")
    args = parser.parse_args()

    payload = build_artifact_map(REPO_ROOT)
    write_outputs(payload, args.json_output, args.markdown_output)
    if args.emit_json:
        print(json.dumps(payload["summary"], indent=2))
    else:
        print(f"Wrote {args.json_output}")
        print(f"Wrote {args.markdown_output}")
        print(json.dumps(payload["summary"], indent=2))
    return 0 if int(payload["summary"].get("attention_count") or 0) == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())
