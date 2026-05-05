#!/usr/bin/env python3
from __future__ import annotations

import argparse
import csv
import importlib.util
import json
import subprocess
import sys
from collections import defaultdict
from datetime import UTC, datetime
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_SCRIPTS = REPO_ROOT / "registry-research-framework" / "scripts"
AUDIT_DIR = REPO_ROOT / "registry-research-framework" / "audit"
REPORT_PATH = AUDIT_DIR / "promoted-app-qa-batch-latest.json"
MARKDOWN_PATH = AUDIT_DIR / "promoted-app-qa-batch-latest.md"
DEFAULT_BATCH_RUNNER = REPO_ROOT / "scripts" / "vm-kvm" / "run-guest-app-tweak-qa-batch.py"


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    spec.loader.exec_module(module)
    return module


single_tweak_app_qa = load_module(
    "check_single_tweak_app_qa_batch_support",
    FRAMEWORK_SCRIPTS / "check_single_tweak_app_qa.py",
)


def normalize_text(value: Any) -> str:
    return str(value or "").strip()


def load_catalog(repo_root: Path) -> dict[str, dict[str, str]]:
    catalog_path = repo_root / "Docs" / "tweaks" / "tweak-catalog.csv"
    with catalog_path.open(newline="", encoding="utf-8") as handle:
        return {row["id"]: row for row in csv.DictReader(handle)}


def load_app_surface_entries(repo_root: Path) -> dict[str, dict[str, str]]:
    surface_path = repo_root / "Docs" / "research" / "app-surface" / "validated-registry-values.json"
    surface = json.loads(surface_path.read_text(encoding="utf-8"))
    entries: dict[str, dict[str, str]] = {}
    for category_key, category in (surface.get("categories") or {}).items():
        category_name = normalize_text((category or {}).get("name")) or category_key
        for entry in (category or {}).get("entries") or []:
            tweak_id = normalize_text((entry or {}).get("id"))
            if not tweak_id:
                continue
            entries[tweak_id] = {
                "category": category_name,
                "name": normalize_text((entry or {}).get("name")) or tweak_id,
                "description": normalize_text((entry or {}).get("description")),
                "documentation": normalize_text((entry or {}).get("documentation")),
            }
    return entries


def load_promotion_entries(repo_root: Path) -> list[dict[str, Any]]:
    gates_path = repo_root / "research" / "promotion-gates.json"
    gates = json.loads(gates_path.read_text(encoding="utf-8"))
    return list(gates.get("entries") or [])


def collect_promoted_candidates(repo_root: Path) -> list[dict[str, Any]]:
    catalog = load_catalog(repo_root)
    app_surface = load_app_surface_entries(repo_root)
    candidates: list[dict[str, Any]] = []
    for entry in load_promotion_entries(repo_root):
        tweak_id = normalize_text(entry.get("tweak_id"))
        if not tweak_id:
            continue
        surface_row = app_surface.get(tweak_id)
        catalog_row = catalog.get(tweak_id)
        if not surface_row and not catalog_row:
            continue
        if normalize_text(entry.get("promotion_state")) != "promoted":
            continue
        if not bool(entry.get("apply_allowed")):
            continue
        if normalize_text(entry.get("app_mapping_status")) != "matches-research":
            continue

        rollback_status = entry.get("rollback_status") or {}
        if not bool(rollback_status.get("rollback_verified")):
            continue

        candidates.append(
            {
                "tweak_id": tweak_id,
                "record_id": normalize_text(entry.get("record_id")) or tweak_id,
                "category": normalize_text((surface_row or {}).get("category") or (catalog_row or {}).get("category")),
                "name": normalize_text((surface_row or {}).get("name") or (catalog_row or {}).get("name")) or tweak_id,
                "description": normalize_text((surface_row or {}).get("description") or (catalog_row or {}).get("description")),
                "docs": normalize_text((surface_row or {}).get("documentation") or (catalog_row or {}).get("docs")),
            }
        )

    candidates.sort(key=lambda item: (item["category"].lower(), item["tweak_id"].lower()))
    return candidates


def select_candidates(
    candidates: list[dict[str, Any]],
    *,
    tweak_ids: list[str],
    categories: list[str],
    limit_per_category: int,
    total_limit: int,
) -> list[dict[str, Any]]:
    by_id = {item["tweak_id"].lower(): item for item in candidates}
    if tweak_ids:
        selected: list[dict[str, Any]] = []
        seen: set[str] = set()
        for tweak_id in tweak_ids:
            key = tweak_id.lower()
            item = by_id.get(key)
            if item and key not in seen:
                selected.append(item)
                seen.add(key)
        return selected

    normalized_categories = {value.strip().lower() for value in categories if value.strip()}
    selected = []
    category_counts: defaultdict[str, int] = defaultdict(int)
    for item in candidates:
        category_key = item["category"].lower()
        if normalized_categories and category_key not in normalized_categories:
            continue
        if category_counts[category_key] >= limit_per_category:
            continue
        selected.append(item)
        category_counts[category_key] += 1
        if len(selected) >= total_limit:
            break
    return selected


def build_candidate_plan(repo_root: Path, tweak_id: str) -> dict[str, Any]:
    report = single_tweak_app_qa.build_single_tweak_app_qa_report(
        tweak_id,
        exact=True,
        limit=1,
        repo_root=repo_root,
    )
    candidates = report.get("candidates") or []
    if report.get("status") != "ok" or not candidates:
        return {
            "tweak_id": tweak_id,
            "status": "error",
            "error": report.get("error") or report.get("status") or "qa-plan generation failed",
            "report": report,
        }
    candidate = dict(candidates[0])
    candidate["status"] = "ok"
    return candidate


def run_kvm_batch(repo_root: Path, tweak_ids: list[str], wait_timeout: int) -> list[dict[str, Any]]:
    cmd = [sys.executable, str(DEFAULT_BATCH_RUNNER)]
    for tweak_id in tweak_ids:
        cmd.extend(["--id", tweak_id])
    cmd.extend(["--wait-timeout", str(wait_timeout)])
    proc = subprocess.run(cmd, cwd=repo_root, capture_output=True, text=True)
    if proc.returncode not in {0, 2}:
        raise RuntimeError(proc.stderr.strip() or proc.stdout.strip() or f"KVM batch failed with exit code {proc.returncode}")
    try:
        payload = json.loads(proc.stdout)
    except json.JSONDecodeError as exc:
        raise RuntimeError(f"Could not parse KVM batch JSON: {exc}") from exc
    if not isinstance(payload, list):
        raise RuntimeError("KVM batch did not return a result list.")
    return payload


def build_report(
    *,
    repo_root: Path,
    tweak_ids: list[str],
    categories: list[str],
    limit_per_category: int,
    total_limit: int,
    run_live_kvm: bool,
    wait_timeout: int,
) -> dict[str, Any]:
    candidates = collect_promoted_candidates(repo_root)
    selected = select_candidates(
        candidates,
        tweak_ids=tweak_ids,
        categories=categories,
        limit_per_category=limit_per_category,
        total_limit=total_limit,
    )

    report: dict[str, Any] = {
        "generated_utc": datetime.now(UTC).isoformat(timespec="seconds").replace("+00:00", "Z"),
        "status": "PASS",
        "selection": {
            "requested_ids": tweak_ids,
            "requested_categories": categories,
            "limit_per_category": limit_per_category,
            "total_limit": total_limit,
            "run_kvm": run_live_kvm,
            "wait_timeout": wait_timeout,
        },
        "sources": {
            "promotion_gates": "research/promotion-gates.json",
            "catalog_csv": "Docs/tweaks/tweak-catalog.csv",
            "single_tweak_app_qa": "registry-research-framework/scripts/check_single_tweak_app_qa.py",
            "kvm_batch_runner": "scripts/vm-kvm/run-guest-app-tweak-qa-batch.py",
        },
        "catalog_candidate_count": len(candidates),
        "selected_candidate_count": len(selected),
        "errors": [],
        "candidates": [],
        "run_results": [],
        "summary": {},
    }

    if not selected:
        report["status"] = "FAIL"
        report["errors"].append("No promoted app-QA batch candidates matched the requested filters.")
        report["summary"] = {
            "planned_count": 0,
            "planned_apply_allowed_count": 0,
            "live_success_count": 0,
            "live_failure_count": 0,
        }
        return report

    for item in selected:
        plan = build_candidate_plan(repo_root, item["tweak_id"])
        if plan.get("status") != "ok":
            report["status"] = "FAIL"
            report["errors"].append(f"Could not build QA plan for {item['tweak_id']}: {plan.get('error')}")
            report["candidates"].append(plan)
            continue

        report["candidates"].append(
            {
                "tweak_id": plan.get("tweak_id"),
                "record_id": plan.get("record_id"),
                "category": (plan.get("card_expectations") or {}).get("category"),
                "name": (plan.get("card_expectations") or {}).get("name"),
                "documentation": (plan.get("card_expectations") or {}).get("documentation"),
                "promotion_state": plan.get("promotion_state"),
                "apply_allowed": bool(plan.get("apply_allowed")),
                "restore_default_supported": bool(plan.get("restore_default_supported")),
                "restore_previous_supported": bool(plan.get("restore_previous_supported")),
                "commands": plan.get("commands"),
                "expected_report": plan.get("expected_report"),
            }
        )

    if run_live_kvm and report["candidates"]:
        live_results = run_kvm_batch(
            repo_root,
            [item["tweak_id"] for item in report["candidates"]],
            wait_timeout,
        )
        report["run_results"] = live_results
        for result in live_results:
            if not bool(result.get("report_success")):
                report["status"] = "FAIL"
                report["errors"].append(
                    f"{result.get('tweak_id')}: live app QA returned {result.get('report_status') or 'unknown status'}."
                )

    live_success_count = sum(1 for item in report["run_results"] if bool(item.get("report_success")))
    live_failure_count = sum(1 for item in report["run_results"] if not bool(item.get("report_success")))
    report["summary"] = {
        "planned_count": len(report["candidates"]),
        "planned_apply_allowed_count": sum(1 for item in report["candidates"] if bool(item.get("apply_allowed"))),
        "live_success_count": live_success_count,
        "live_failure_count": live_failure_count,
        "categories": sorted({normalize_text(item.get("category")) for item in report["candidates"] if normalize_text(item.get("category"))}),
    }
    return report


def render_markdown(report: dict[str, Any]) -> str:
    lines = [
        "# Promoted App QA Batch",
        "",
        f"- Status: {report.get('status')}",
        f"- Generated UTC: {report.get('generated_utc')}",
        f"- Catalog candidates: {report.get('catalog_candidate_count')}",
        f"- Selected candidates: {report.get('selected_candidate_count')}",
        f"- Planned apply-allowed candidates: {(report.get('summary') or {}).get('planned_apply_allowed_count', 0)}",
        f"- Live successes: {(report.get('summary') or {}).get('live_success_count', 0)}",
        f"- Live failures: {(report.get('summary') or {}).get('live_failure_count', 0)}",
        "",
        "## Selected Candidates",
        "",
    ]

    for item in report.get("candidates") or []:
        lines.extend(
            [
                f"- `{item.get('tweak_id')}` | {item.get('name')} | {item.get('category')}",
                f"  docs: `{item.get('documentation')}`",
                "  rollback: "
                + f"default={str(bool(item.get('restore_default_supported'))).lower()} | "
                + f"previous={str(bool(item.get('restore_previous_supported'))).lower()}",
            ]
        )

    if report.get("run_results"):
        lines.extend(["", "## Live Results", ""])
        for result in report["run_results"]:
            lines.extend(
                [
                    f"- `{result.get('tweak_id')}` | success={str(bool(result.get('report_success'))).lower()} | status={result.get('report_status')}",
                    f"  summary: {result.get('report_summary')}",
                ]
            )

    if report.get("errors"):
        lines.extend(["", "## Errors", ""])
        for error in report["errors"]:
            lines.append(f"- {error}")

    return "\n".join(lines) + "\n"


def write_artifacts(report: dict[str, Any]) -> None:
    AUDIT_DIR.mkdir(parents=True, exist_ok=True)
    REPORT_PATH.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    MARKDOWN_PATH.write_text(render_markdown(report), encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Plan or run a promoted desktop-app QA batch across shipped apply-allowed tweaks."
    )
    parser.add_argument("--id", action="append", default=[], help="Explicit tweak id to include. Repeat for multiple ids.")
    parser.add_argument("--category", action="append", default=[], help="Category filter when auto-selecting a batch.")
    parser.add_argument("--limit-per-category", type=int, default=1, help="Auto-selection cap per category.")
    parser.add_argument("--total-limit", type=int, default=6, help="Maximum number of auto-selected tweaks.")
    parser.add_argument("--run-kvm", action="store_true", help="Run the selected batch through the KVM guest app-QA runner.")
    parser.add_argument("--wait-timeout", type=int, default=900, help="Wait timeout for the live KVM batch.")
    parser.add_argument("--json", action="store_true", help="Emit JSON instead of markdown.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    if args.limit_per_category <= 0:
        raise SystemExit("--limit-per-category must be greater than 0")
    if args.total_limit <= 0:
        raise SystemExit("--total-limit must be greater than 0")
    if args.wait_timeout <= 0:
        raise SystemExit("--wait-timeout must be greater than 0")

    report = build_report(
        repo_root=REPO_ROOT,
        tweak_ids=[normalize_text(value) for value in args.id if normalize_text(value)],
        categories=[normalize_text(value) for value in args.category if normalize_text(value)],
        limit_per_category=args.limit_per_category,
        total_limit=args.total_limit,
        run_live_kvm=args.run_kvm,
        wait_timeout=args.wait_timeout,
    )
    write_artifacts(report)

    if args.json:
        print(json.dumps(report, indent=2))
    else:
        print(render_markdown(report))

    return 0 if report.get("status") == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
