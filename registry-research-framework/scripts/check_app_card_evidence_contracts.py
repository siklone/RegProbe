#!/usr/bin/env python3
from __future__ import annotations

import argparse
import importlib.util
import json
from collections import defaultdict
from datetime import UTC, datetime
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_SCRIPTS = REPO_ROOT / "registry-research-framework" / "scripts"
AUDIT_DIR = REPO_ROOT / "registry-research-framework" / "audit"
REPORT_PATH = AUDIT_DIR / "app-card-evidence-contracts-latest.json"
MARKDOWN_PATH = AUDIT_DIR / "app-card-evidence-contracts-latest.md"

REQUIRED_CARD_FIELDS = [
    "TweakId",
    "Name",
    "Category",
    "EvidenceClass",
    "ResearchStatus",
    "RollbackSnapshotState",
    "HasClaimBoundary",
    "WhatWeKnowSummary",
    "WhatWeDoNotClaimSummary",
    "ProofLanes",
]
REQUIRED_PROOF_LANES = ["docs", "runtime", "source", "rollback"]


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    spec.loader.exec_module(module)
    return module


promoted_app_qa = load_module(
    "check_promoted_tweak_app_qa_batch_for_card_contracts",
    FRAMEWORK_SCRIPTS / "check_promoted_tweak_app_qa_batch.py",
)


def normalize_text(value: Any) -> str:
    return str(value or "").strip()


def local_path_exists(repo_root: Path, path_text: str) -> bool:
    path_text = normalize_text(path_text)
    if not path_text or path_text.startswith(("http://", "https://")):
        return True
    return (repo_root / path_text).exists()


def validate_candidate_plan(plan: dict[str, Any], *, repo_root: Path) -> list[str]:
    failures: list[str] = []
    tweak_id = normalize_text(plan.get("tweak_id")) or normalize_text(plan.get("candidate_id")) or "<unknown>"
    card = plan.get("card_expectations") or {}
    expected = plan.get("expected_report") or {}
    expected_skip = plan.get("expected_report_skip_rollback") or {}
    snapshot = expected.get("required_card_snapshot") or {}
    snapshot_skip = expected_skip.get("required_card_snapshot") or {}
    evidence = plan.get("evidence_expectations") or {}

    for field in ("name", "category", "documentation"):
        if not normalize_text(card.get(field)):
            failures.append(f"{tweak_id}: missing card {field}")

    documentation = normalize_text(card.get("documentation"))
    if documentation and not local_path_exists(repo_root, documentation):
        failures.append(f"{tweak_id}: documentation path does not exist: {documentation}")

    required_fields = {normalize_text(item) for item in snapshot.get("required_fields") or []}
    missing_fields = [field for field in REQUIRED_CARD_FIELDS if field not in required_fields]
    if missing_fields:
        failures.append(f"{tweak_id}: missing required card fields: {', '.join(missing_fields)}")

    required_lanes = {normalize_text(item) for item in snapshot.get("required_proof_lanes") or []}
    missing_lanes = [lane for lane in REQUIRED_PROOF_LANES if lane not in required_lanes]
    if missing_lanes:
        failures.append(f"{tweak_id}: missing required proof lanes: {', '.join(missing_lanes)}")

    if snapshot.get("claim_boundary_required") is not True:
        failures.append(f"{tweak_id}: claim boundary is not required in the normal contract")

    skip_required_fields = {normalize_text(item) for item in snapshot_skip.get("required_fields") or []}
    skip_missing_fields = [field for field in REQUIRED_CARD_FIELDS if field not in skip_required_fields]
    if skip_missing_fields:
        failures.append(f"{tweak_id}: skip-rollback contract missing fields: {', '.join(skip_missing_fields)}")

    skip_required_lanes = {normalize_text(item) for item in snapshot_skip.get("required_proof_lanes") or []}
    skip_missing_lanes = [lane for lane in REQUIRED_PROOF_LANES if lane not in skip_required_lanes]
    if skip_missing_lanes:
        failures.append(f"{tweak_id}: skip-rollback contract missing proof lanes: {', '.join(skip_missing_lanes)}")

    if snapshot_skip.get("claim_boundary_required") is not True:
        failures.append(f"{tweak_id}: claim boundary is not required in the skip-rollback contract")

    if expected.get("rollback_requested") is True and "rollback" not in [normalize_text(item) for item in expected.get("required_stages") or []]:
        failures.append(f"{tweak_id}: rollback is requested but the normal contract does not require a rollback stage")

    if int(evidence.get("linked_evidence_count") or 0) <= 0:
        failures.append(f"{tweak_id}: no linked evidence locations are planned for the drawer")

    if not (plan.get("operator_checklist") or []):
        failures.append(f"{tweak_id}: operator checklist is missing")

    return failures


def build_report(repo_root: Path) -> dict[str, Any]:
    candidates = promoted_app_qa.collect_promoted_candidates(repo_root)
    records: list[dict[str, Any]] = []
    errors: list[str] = []
    categories: defaultdict[str, int] = defaultdict(int)

    for candidate in candidates:
        tweak_id = normalize_text(candidate.get("tweak_id"))
        if not tweak_id:
            continue
        plan = promoted_app_qa.build_candidate_plan(repo_root, tweak_id)
        if plan.get("status") != "ok":
            error = f"{tweak_id}: could not build app QA plan: {plan.get('error') or plan.get('status')}"
            errors.append(error)
            records.append(
                {
                    "tweak_id": tweak_id,
                    "status": "FAIL",
                    "category": normalize_text(candidate.get("category")),
                    "failures": [error],
                }
            )
            continue

        failures = validate_candidate_plan(plan, repo_root=repo_root)
        category = normalize_text((plan.get("card_expectations") or {}).get("category")) or normalize_text(candidate.get("category"))
        categories[category or "Uncategorized"] += 1
        if failures:
            errors.extend(failures)
        records.append(
            {
                "tweak_id": tweak_id,
                "qa_tweak_id": normalize_text(plan.get("qa_tweak_id")) or tweak_id,
                "candidate_id": normalize_text(plan.get("candidate_id")) or tweak_id,
                "record_id": normalize_text(plan.get("record_id")),
                "category": category,
                "card_name": normalize_text((plan.get("card_expectations") or {}).get("name")),
                "documentation": normalize_text((plan.get("card_expectations") or {}).get("documentation")),
                "promotion_state": normalize_text(plan.get("promotion_state")),
                "apply_allowed": bool(plan.get("apply_allowed")),
                "restore_default_supported": bool((plan.get("rollback_expectations") or {}).get("restore_default_supported")),
                "restore_previous_supported": bool((plan.get("rollback_expectations") or {}).get("restore_previous_supported")),
                "linked_evidence_count": int((plan.get("evidence_expectations") or {}).get("linked_evidence_count") or 0),
                "runtime_read_signal_count": int((plan.get("evidence_expectations") or {}).get("runtime_read_signal_count") or 0),
                "required_card_fields": (plan.get("expected_report") or {}).get("required_card_snapshot", {}).get("required_fields") or [],
                "required_proof_lanes": (plan.get("expected_report") or {}).get("required_card_snapshot", {}).get("required_proof_lanes") or [],
                "claim_boundary_required": bool(
                    (plan.get("expected_report") or {}).get("required_card_snapshot", {}).get("claim_boundary_required")
                ),
                "status": "PASS" if not failures else "FAIL",
                "failures": failures,
            }
        )

    pass_count = sum(1 for item in records if item.get("status") == "PASS")
    fail_count = len(records) - pass_count
    return {
        "generated_utc": datetime.now(UTC).isoformat(timespec="seconds").replace("+00:00", "Z"),
        "status": "PASS" if not errors else "FAIL",
        "sources": {
            "promotion_gates": "research/promotion-gates.json",
            "app_surface": "Docs/research/app-surface/validated-registry-values.json",
            "qa_plan_script": "registry-research-framework/scripts/check_single_tweak_app_qa.py",
            "batch_script": "registry-research-framework/scripts/check_promoted_tweak_app_qa_batch.py",
        },
        "summary": {
            "candidate_count": len(records),
            "pass_count": pass_count,
            "fail_count": fail_count,
            "category_counts": dict(sorted(categories.items())),
            "required_card_fields": REQUIRED_CARD_FIELDS,
            "required_proof_lanes": REQUIRED_PROOF_LANES,
        },
        "errors": errors,
        "records": records,
    }


def render_markdown(report: dict[str, Any]) -> str:
    summary = report.get("summary") or {}
    lines = [
        "# App Card Evidence Contract Sweep",
        "",
        f"- Status: {report.get('status')}",
        f"- Generated UTC: {report.get('generated_utc')}",
        f"- Candidates: {summary.get('candidate_count', 0)}",
        f"- Passing: {summary.get('pass_count', 0)}",
        f"- Failing: {summary.get('fail_count', 0)}",
        "- Required card fields: " + ", ".join(summary.get("required_card_fields") or []),
        "- Required proof lanes: " + ", ".join(summary.get("required_proof_lanes") or []),
        "",
        "## Categories",
        "",
    ]
    for category, count in (summary.get("category_counts") or {}).items():
        lines.append(f"- {category}: {count}")

    failures = [item for item in report.get("records") or [] if item.get("status") != "PASS"]
    lines.extend(["", "## Failures", ""])
    if failures:
        for item in failures:
            lines.append(f"- `{item.get('tweak_id')}` | {item.get('category')}")
            for failure in item.get("failures") or []:
                lines.append(f"  {failure}")
    else:
        lines.append("- No card/evidence contract failures.")

    lines.extend(["", "## Sample Passing Records", ""])
    for item in (report.get("records") or [])[:20]:
        lines.append(
            f"- `{item.get('tweak_id')}` | {item.get('card_name')} | "
            f"{item.get('category')} | evidence={item.get('linked_evidence_count')} | "
            f"runtime={item.get('runtime_read_signal_count')}"
        )

    return "\n".join(lines) + "\n"


def write_artifacts(report: dict[str, Any], repo_root: Path) -> None:
    audit_dir = repo_root / "registry-research-framework" / "audit"
    audit_dir.mkdir(parents=True, exist_ok=True)
    (audit_dir / REPORT_PATH.name).write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    (audit_dir / MARKDOWN_PATH.name).write_text(render_markdown(report), encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Check promoted app cards for evidence drawer and QA card snapshot contract readiness."
    )
    parser.add_argument("--json", action="store_true", help="Emit JSON instead of markdown.")
    parser.add_argument("--no-write", action="store_true", help="Do not refresh latest audit artifacts.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    report = build_report(REPO_ROOT)
    if not args.no_write:
        write_artifacts(report, REPO_ROOT)
    if args.json:
        print(json.dumps(report, indent=2))
    else:
        print(render_markdown(report))
    return 0 if report.get("status") == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
