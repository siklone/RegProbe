#!/usr/bin/env python3
from __future__ import annotations

import argparse
import importlib.util
import json
from datetime import UTC, datetime
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
AUDIT_DIR = REPO_ROOT / "registry-research-framework" / "audit"
REPORT_PATH = AUDIT_DIR / "app-retest-readiness-latest.json"
MARKDOWN_PATH = AUDIT_DIR / "app-retest-readiness-latest.md"
PROMOTION_GATES_PATH = REPO_ROOT / "research" / "promotion-gates.json"
EVIDENCE_INDEX_PATH = REPO_ROOT / "research" / "evidence-index.json"
EVIDENCE_MANIFEST_PATH = REPO_ROOT / "research" / "evidence-manifest.json"
EVIDENCE_AUDIT_PATH = REPO_ROOT / "research" / "evidence-audit.json"
EVIDENCE_ATLAS_PATH = REPO_ROOT / "research" / "evidence-atlas.md"
RECORDS_ROOT = REPO_ROOT / "research" / "records"
VALIDATED_APP_SURFACE_PATH = REPO_ROOT / "Docs" / "research" / "app-surface" / "validated-registry-values.json"
APP_ONLY_SURFACE_PATH = REPO_ROOT / "Docs" / "research" / "app-surface" / "app-only-catalog-tweaks.json"
KVM_APP_SMOKE_PATH = AUDIT_DIR / "kvm-app-publish-deploy-smoke-latest.json"
KVM_LANE_HEALTH_PATH = AUDIT_DIR / "kvm-research-lane-health-latest.json"
PUBLIC_HYGIENE_SCRIPT_PATH = REPO_ROOT / "registry-research-framework" / "scripts" / "check_public_repo_hygiene.py"
TWEAK_TRUTH_SCRIPT_PATH = REPO_ROOT / "scripts" / "check_tweak_catalog_truth.py"


def load_json(path: Path) -> dict[str, Any] | list[Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    spec.loader.exec_module(module)
    return module


def parse_int(value: str) -> int:
    return int(str(value).strip())


def parse_markdown_table(lines: list[str]) -> list[dict[str, str]]:
    if len(lines) < 3:
        return []

    header = [cell.strip() for cell in lines[0].strip().strip("|").split("|")]
    rows: list[dict[str, str]] = []
    for raw_line in lines[2:]:
        if not raw_line.strip().startswith("|"):
            continue
        values = [cell.strip() for cell in raw_line.strip().strip("|").split("|")]
        if len(values) != len(header):
            continue
        rows.append(dict(zip(header, values, strict=True)))
    return rows


def extract_markdown_table(markdown_text: str, heading: str) -> list[dict[str, str]]:
    lines = markdown_text.splitlines()
    in_section = False
    table_lines: list[str] = []

    for raw_line in lines:
        line = raw_line.rstrip("\n")
        if line.strip() == heading:
            in_section = True
            table_lines = []
            continue

        if not in_section:
            continue

        stripped = line.strip()
        if stripped.startswith("## ") and stripped != heading and table_lines:
            break
        if stripped.startswith("### ") and table_lines:
            break
        if not stripped and table_lines:
            break
        if stripped.startswith("|"):
            table_lines.append(line)

    return parse_markdown_table(table_lines)


def build_record_index(records_root: Path) -> tuple[dict[str, dict[str, Any]], dict[str, str]]:
    record_by_id: dict[str, dict[str, Any]] = {}
    record_file_by_id: dict[str, str] = {}

    for path in sorted(records_root.glob("*.json")):
        payload = load_json(path)
        if not isinstance(payload, dict):
            continue
        relative_path = path.relative_to(records_root.parents[1]).as_posix()
        for key in ("record_id", "tweak_id", "legacy_id"):
            value = str(payload.get(key) or "").strip()
            if not value:
                continue
            record_by_id[value] = payload
            record_file_by_id[value] = relative_path

    return record_by_id, record_file_by_id


def collect_gate_ids(entries: list[dict[str, Any]]) -> set[str]:
    ids: set[str] = set()
    for entry in entries:
        for key in ("candidate_id", "record_id", "tweak_id"):
            value = str(entry.get(key) or "").strip()
            if value:
                ids.add(value)
    return ids


def evaluate_app_surface(
    repo_root: Path,
    record_by_id: dict[str, dict[str, Any]],
    record_file_by_id: dict[str, str],
    gate_ids: set[str],
) -> dict[str, Any]:
    payload = load_json(repo_root / "Docs" / "research" / "app-surface" / "validated-registry-values.json")
    categories = payload.get("categories") if isinstance(payload, dict) else {}

    entries: list[dict[str, Any]] = []
    if isinstance(categories, dict):
        for category_key, category_payload in categories.items():
            for entry in (category_payload or {}).get("entries", []):
                if not isinstance(entry, dict):
                    continue
                copied = dict(entry)
                copied["_category_key"] = category_key
                entries.append(copied)

    missing_gate_ids: list[str] = []
    missing_record_ids: list[str] = []
    missing_documentation_paths: list[str] = []
    mismatched_documentation_paths: list[dict[str, str]] = []

    for entry in entries:
        entry_id = str(entry.get("id") or "").strip()
        documentation = str(entry.get("documentation") or "").strip()
        if entry_id and entry_id not in gate_ids:
            missing_gate_ids.append(entry_id)
        if entry_id and entry_id not in record_by_id:
            missing_record_ids.append(entry_id)

        if documentation:
            documentation_path = repo_root / documentation
            if not documentation_path.exists():
                missing_documentation_paths.append(documentation)
            elif entry_id:
                expected_path = record_file_by_id.get(entry_id)
                actual_path = documentation_path.relative_to(repo_root).as_posix()
                if expected_path and actual_path != expected_path:
                    mismatched_documentation_paths.append(
                        {
                            "entry_id": entry_id,
                            "documentation": actual_path,
                            "expected_record_file": expected_path,
                        }
                    )
        else:
            missing_documentation_paths.append(f"{entry_id}:<missing-documentation-field>")

    app_only_payload = load_json(repo_root / "Docs" / "research" / "app-surface" / "app-only-catalog-tweaks.json")
    app_only_tweaks = (app_only_payload.get("tweaks") or []) if isinstance(app_only_payload, dict) else []

    checks = {
        "no_app_only_tweaks": len(app_only_tweaks) == 0,
        "surface_entries_resolve_to_gate_ids": not missing_gate_ids,
        "surface_entries_resolve_to_record_ids": not missing_record_ids,
        "surface_documentation_paths_exist": not missing_documentation_paths,
        "surface_documentation_paths_match_record_files": not mismatched_documentation_paths,
    }

    errors: list[str] = []
    if missing_gate_ids:
        errors.append(
            f"Validated app-surface entries missing promotion-gate coverage: {', '.join(sorted(missing_gate_ids)[:10])}"
        )
    if missing_record_ids:
        errors.append(
            f"Validated app-surface entries missing research record coverage: {', '.join(sorted(missing_record_ids)[:10])}"
        )
    if missing_documentation_paths:
        errors.append(
            f"Validated app-surface documentation links are missing for {len(missing_documentation_paths)} entry path(s)."
        )
    if mismatched_documentation_paths:
        errors.append(
            f"Validated app-surface documentation points at the wrong research record for {len(mismatched_documentation_paths)} entry path(s)."
        )
    if app_only_tweaks:
        errors.append(f"App-only catalog still contains {len(app_only_tweaks)} tweak(s).")

    return {
        "status": "PASS" if not errors else "FAIL",
        "checks": checks,
        "summary": {
            "category_count": len(categories) if isinstance(categories, dict) else 0,
            "entry_count": len(entries),
            "app_only_tweak_count": len(app_only_tweaks),
        },
        "missing_gate_ids": sorted(missing_gate_ids),
        "missing_record_ids": sorted(missing_record_ids),
        "missing_documentation_paths": sorted(missing_documentation_paths),
        "mismatched_documentation_paths": mismatched_documentation_paths,
        "errors": errors,
    }


def evaluate_evidence_surfaces(
    repo_root: Path,
    evidence_index: dict[str, Any],
    evidence_manifest: dict[str, Any],
    evidence_audit: dict[str, Any],
    atlas_path: Path,
    promotion_entries: list[dict[str, Any]],
    record_file_count: int,
) -> dict[str, Any]:
    index_records = evidence_index.get("records") or []
    manifest_records = evidence_manifest.get("records") or []
    audit_entries = evidence_audit.get("entries") or []
    index_summary = evidence_index.get("summary") or {}
    audit_summary = evidence_audit.get("summary") or {}
    atlas_text = atlas_path.read_text(encoding="utf-8")
    atlas_summary_rows = extract_markdown_table(atlas_text, "## Summary")
    atlas_category_rows = extract_markdown_table(atlas_text, "## Category coverage")

    atlas_summary = {row.get("Field", ""): row.get("Value", "") for row in atlas_summary_rows}
    atlas_category_counts = {
        row.get("Category", ""): parse_int(row.get("Records", "0"))
        for row in atlas_category_rows
        if row.get("Category")
    }
    computed_category_counts: dict[str, int] = {}
    for record in index_records:
        if not isinstance(record, dict):
            continue
        category = str(record.get("category") or "").strip()
        if not category:
            continue
        computed_category_counts[category] = computed_category_counts.get(category, 0) + 1

    active_expected = int(index_summary.get("total_records") or 0) - int(index_summary.get("deprecated") or 0)
    active_expected_from_gates = 0
    for entry in promotion_entries:
        state = str(entry.get("promotion_state") or "").strip()
        if state == "rejected":
            continue
        active_expected_from_gates += 1

    summary_expectations = {
        "Total records": int(index_summary.get("total_records") or 0),
        "Validated": int(index_summary.get("validated") or 0),
        "Deprecated": int(index_summary.get("deprecated") or 0),
        "Review required": int(index_summary.get("review_required") or 0),
        "Records with evidence": int(index_summary.get("records_with_evidence") or 0),
        "Records without evidence": int(index_summary.get("records_without_evidence") or 0),
        "Records missing validation proof": int(index_summary.get("records_missing_validation_proof") or 0),
        "Deprecated missing validation proof": int(index_summary.get("deprecated_missing_validation_proof") or 0),
        "Class A": int((index_summary.get("class_counts") or {}).get("A") or 0),
        "Class B": int((index_summary.get("class_counts") or {}).get("B") or 0),
        "Class C": int((index_summary.get("class_counts") or {}).get("C") or 0),
        "Class D": int((index_summary.get("class_counts") or {}).get("D") or 0),
        "Class E": int((index_summary.get("class_counts") or {}).get("E") or 0),
    }

    atlas_summary_mismatches: list[dict[str, Any]] = []
    for field, expected in summary_expectations.items():
        actual_raw = atlas_summary.get(field)
        if actual_raw is None:
            atlas_summary_mismatches.append({"field": field, "expected": expected, "actual": "<missing>"})
            continue
        actual = parse_int(actual_raw)
        if actual != expected:
            atlas_summary_mismatches.append({"field": field, "expected": expected, "actual": actual})

    atlas_category_mismatches: list[dict[str, Any]] = []
    all_categories = sorted(set(computed_category_counts) | set(atlas_category_counts))
    for category in all_categories:
        expected = computed_category_counts.get(category, 0)
        actual = atlas_category_counts.get(category, 0)
        if actual != expected:
            atlas_category_mismatches.append({"category": category, "expected": expected, "actual": actual})

    source_file_violations = [
        record.get("source_file")
        for record in index_records
        if isinstance(record, dict)
        and str(record.get("source_file") or "").strip()
        and not (repo_root / str(record.get("source_file"))).exists()
    ]

    checks = {
        "record_corpus_matches_evidence_counts": record_file_count == len(index_records) == len(manifest_records),
        "evidence_audit_active_count_matches_index_summary": int(audit_summary.get("total_active_records") or 0) == active_expected,
        "evidence_audit_active_count_matches_gate_states": int(audit_summary.get("total_active_records") or 0) == active_expected_from_gates,
        "evidence_audit_validation_proof_count_matches_index_summary": int((audit_summary.get("next_missing_layer_counts") or {}).get("validation-proof") or 0)
        == int(index_summary.get("records_missing_validation_proof") or 0),
        "evidence_index_source_files_exist": not source_file_violations,
        "evidence_atlas_summary_matches_json": not atlas_summary_mismatches,
        "evidence_atlas_category_counts_match_json": not atlas_category_mismatches,
    }

    errors: list[str] = []
    if not checks["record_corpus_matches_evidence_counts"]:
        errors.append(
            "Research record count drifted from evidence-index/evidence-manifest counts."
        )
    if not checks["evidence_audit_active_count_matches_index_summary"]:
        errors.append("Evidence audit active count no longer matches evidence-index summary counts.")
    if not checks["evidence_audit_active_count_matches_gate_states"]:
        errors.append("Evidence audit active count no longer matches non-rejected promotion-gate entries.")
    if not checks["evidence_audit_validation_proof_count_matches_index_summary"]:
        errors.append("Evidence audit validation-proof backlog no longer matches evidence-index summary.")
    if source_file_violations:
        errors.append(f"Evidence index references {len(source_file_violations)} missing source_file path(s).")
    if atlas_summary_mismatches:
        errors.append(f"Evidence atlas summary drifted from evidence-index for {len(atlas_summary_mismatches)} field(s).")
    if atlas_category_mismatches:
        errors.append(f"Evidence atlas category coverage drifted from evidence-index for {len(atlas_category_mismatches)} category row(s).")

    return {
        "status": "PASS" if not errors else "FAIL",
        "checks": checks,
        "summary": {
            "record_file_count": record_file_count,
            "promotion_gate_entry_count": len(promotion_entries),
            "evidence_index_record_count": len(index_records),
            "evidence_manifest_record_count": len(manifest_records),
            "evidence_audit_active_record_count": int(audit_summary.get("total_active_records") or 0),
            "records_missing_validation_proof": int(index_summary.get("records_missing_validation_proof") or 0),
        },
        "atlas_summary_mismatches": atlas_summary_mismatches,
        "atlas_category_mismatches": atlas_category_mismatches,
        "source_file_violations": sorted(source_file_violations),
        "errors": errors,
    }


def evaluate_kvm_statuses(kvm_app_smoke: dict[str, Any], kvm_lane_health: dict[str, Any]) -> dict[str, Any]:
    app_smoke_status = str(kvm_app_smoke.get("status") or "").strip()
    lane_health_status = str(kvm_lane_health.get("status") or "").strip()

    checks = {
        "kvm_app_publish_deploy_smoke_ok": app_smoke_status == "ok",
        "kvm_research_lane_health_ok": lane_health_status == "ok",
    }

    errors: list[str] = []
    if app_smoke_status != "ok":
        errors.append(f"KVM app publish/deploy smoke is {app_smoke_status or 'missing-status'}.")
    if lane_health_status != "ok":
        errors.append(f"KVM research lane health is {lane_health_status or 'missing-status'}.")

    return {
        "status": "PASS" if not errors else "FAIL",
        "checks": checks,
        "summary": {
            "app_smoke_status": app_smoke_status,
            "lane_health_status": lane_health_status,
            "lane_health_generated_utc": str(kvm_lane_health.get("generated_utc") or ""),
        },
        "errors": errors,
    }


def evaluate_rollback_coverage(entries: list[dict[str, Any]]) -> dict[str, Any]:
    apply_allowed_entries = [entry for entry in entries if bool(entry.get("record_promotion_allowed"))]
    missing_story_ids: list[str] = []

    for entry in apply_allowed_entries:
        rollback_status = entry.get("rollback_status") or {}
        has_story = bool(
            rollback_status.get("rollback_value")
            or rollback_status.get("rollback_declared")
            or rollback_status.get("rollback_verified")
        )
        if not has_story:
            missing_story_ids.append(str(entry.get("record_id") or entry.get("candidate_id") or "").strip())

    checks = {
        "apply_allowed_records_have_rollback_story": not missing_story_ids,
    }

    errors: list[str] = []
    if missing_story_ids:
        errors.append(
            f"Apply-allowed records are missing rollback coverage for {len(missing_story_ids)} record(s)."
        )

    return {
        "status": "PASS" if not errors else "FAIL",
        "checks": checks,
        "summary": {
            "apply_allowed_record_count": len(apply_allowed_entries),
            "missing_rollback_story_count": len(missing_story_ids),
        },
        "missing_rollback_story_ids": sorted(missing_story_ids),
        "errors": errors,
    }


def build_app_retest_readiness_report(repo_root: Path | None = None) -> dict[str, Any]:
    root = repo_root or REPO_ROOT
    public_repo_hygiene = load_module("check_public_repo_hygiene", root / "registry-research-framework" / "scripts" / "check_public_repo_hygiene.py")
    tweak_catalog_truth = load_module("check_tweak_catalog_truth", root / "scripts" / "check_tweak_catalog_truth.py")

    promotion_gates = load_json(root / "research" / "promotion-gates.json")
    evidence_index = load_json(root / "research" / "evidence-index.json")
    evidence_manifest = load_json(root / "research" / "evidence-manifest.json")
    evidence_audit = load_json(root / "research" / "evidence-audit.json")
    kvm_app_smoke = load_json(root / "registry-research-framework" / "audit" / "kvm-app-publish-deploy-smoke-latest.json")
    kvm_lane_health = load_json(root / "registry-research-framework" / "audit" / "kvm-research-lane-health-latest.json")

    record_by_id, record_file_by_id = build_record_index(root / "research" / "records")
    promotion_entries = (promotion_gates.get("entries") or []) if isinstance(promotion_gates, dict) else []
    gate_ids = collect_gate_ids(promotion_entries)

    public_hygiene_report = public_repo_hygiene.build_public_repo_hygiene_report(root)
    tweak_truth_report = tweak_catalog_truth.build_tweak_catalog_truth_report(root)
    app_surface_report = evaluate_app_surface(root, record_by_id, record_file_by_id, gate_ids)
    evidence_surface_report = evaluate_evidence_surfaces(
        root,
        evidence_index,
        evidence_manifest,
        evidence_audit,
        root / "research" / "evidence-atlas.md",
        promotion_entries,
        len(list((root / "research" / "records").glob("*.json"))),
    )
    rollback_report = evaluate_rollback_coverage(promotion_entries)
    kvm_report = evaluate_kvm_statuses(kvm_app_smoke, kvm_lane_health)

    checks = {
        "public_repo_hygiene_pass": public_hygiene_report.get("check_status") == "PASS",
        "tweak_catalog_truth_pass": tweak_truth_report.get("check_status") == "PASS",
        **app_surface_report["checks"],
        **evidence_surface_report["checks"],
        **rollback_report["checks"],
        **kvm_report["checks"],
    }

    errors = (
        list(public_hygiene_report.get("errors") or [])
        + list(tweak_truth_report.get("errors") or [])
        + list(app_surface_report.get("errors") or [])
        + list(evidence_surface_report.get("errors") or [])
        + list(rollback_report.get("errors") or [])
        + list(kvm_report.get("errors") or [])
    )

    report = {
        "generated_utc": datetime.now(UTC).isoformat(timespec="seconds").replace("+00:00", "Z"),
        "check_status": "PASS" if not errors else "FAIL",
        "checks": checks,
        "summary": {
            "record_count": len(list((root / "research" / "records").glob("*.json"))),
            "promotion_gate_entry_count": len(promotion_entries),
            "app_surface_entry_count": app_surface_report["summary"]["entry_count"],
            "app_only_tweak_count": app_surface_report["summary"]["app_only_tweak_count"],
            "apply_allowed_record_count": rollback_report["summary"]["apply_allowed_record_count"],
            "missing_rollback_story_count": rollback_report["summary"]["missing_rollback_story_count"],
            "kvm_app_smoke_status": kvm_report["summary"]["app_smoke_status"],
            "kvm_lane_health_status": kvm_report["summary"]["lane_health_status"],
        },
        "reports": {
            "public_repo_hygiene": {
                "status": public_hygiene_report.get("check_status"),
                "report_path": "registry-research-framework/audit/public-repo-hygiene-check.json",
            },
            "tweak_catalog_truth": {
                "status": tweak_truth_report.get("check_status"),
                "catalog_csv": str((root / "Docs" / "tweaks" / "tweak-catalog.csv").relative_to(root)),
            },
            "app_surface": app_surface_report,
            "evidence_surfaces": evidence_surface_report,
            "rollback": rollback_report,
            "kvm": kvm_report,
        },
        "errors": errors,
    }
    return report


def render_markdown(report: dict[str, Any]) -> str:
    lines = [
        "# App Retest Readiness",
        "",
        f"- Status: **{report['check_status']}**",
        f"- Generated UTC: `{report['generated_utc']}`",
        "",
        "## Summary",
    ]
    for key, value in (report.get("summary") or {}).items():
        lines.append(f"- `{key}`: `{value}`")

    lines.extend(["", "## Checks"])
    for key, value in (report.get("checks") or {}).items():
        lines.append(f"- `{key}`: `{value}`")

    if report.get("errors"):
        lines.extend(["", "## Errors"])
        for error in report["errors"]:
            lines.append(f"- {error}")

    return "\n".join(lines) + "\n"


def render_console_report(report: dict[str, Any]) -> str:
    lines = [
        "App retest readiness",
        f"Status: {report.get('check_status')}",
        f"Generated UTC: {report.get('generated_utc')}",
        "",
        "Summary:",
    ]
    summary = report.get("summary") or {}
    lines.append(
        "  app surface: "
        f"{summary.get('app_surface_entry_count')} entries | "
        f"app-only backlog: {summary.get('app_only_tweak_count')}"
    )
    lines.append(
        "  rollback: "
        f"{summary.get('apply_allowed_record_count')} apply-allowed | "
        f"missing story: {summary.get('missing_rollback_story_count')}"
    )
    lines.append(
        "  KVM: "
        f"app smoke={summary.get('kvm_app_smoke_status')} | "
        f"lane health={summary.get('kvm_lane_health_status')}"
    )
    lines.append(
        "  evidence: "
        f"{((report.get('reports') or {}).get('evidence_surfaces') or {}).get('summary', {}).get('evidence_index_record_count')} records"
    )
    lines.append("")
    lines.append("Checks:")
    for key, value in (report.get("checks") or {}).items():
        lines.append(f"  - {key}: {value}")
    lines.append("")
    lines.append(f"Audit JSON: {REPORT_PATH.relative_to(REPO_ROOT).as_posix()}")
    lines.append(f"Audit Markdown: {MARKDOWN_PATH.relative_to(REPO_ROOT).as_posix()}")
    if report.get("errors"):
        lines.append("")
        lines.append("Errors:")
        for error in report["errors"]:
            lines.append(f"  - {error}")
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Check whether the repo is ready for a manual RegProbe app retest across app cards, evidence, rollback coverage, and KVM smoke artifacts."
    )
    parser.add_argument("--json", action="store_true", help="Emit the readiness report as JSON.")
    args = parser.parse_args()

    report = build_app_retest_readiness_report(REPO_ROOT)
    AUDIT_DIR.mkdir(parents=True, exist_ok=True)
    REPORT_PATH.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    MARKDOWN_PATH.write_text(render_markdown(report), encoding="utf-8")

    if args.json:
        print(json.dumps(report, ensure_ascii=False, indent=2))
    else:
        print(render_console_report(report))
    return 0 if report["check_status"] == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
