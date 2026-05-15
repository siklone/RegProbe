#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import subprocess
from collections import Counter
from datetime import UTC, datetime
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
AUDIT_DIR = REPO_ROOT / "registry-research-framework" / "audit"
DEFAULT_JSON_OUTPUT = AUDIT_DIR / "cleanup-quarantine-ledger-20260510.json"
DEFAULT_MARKDOWN_OUTPUT = AUDIT_DIR / "cleanup-quarantine-ledger-20260510.md"

REPLACEMENT_ARTIFACTS = [
    "registry-research-framework/audit/operator96-value-campaign-tranche-20260509.json",
    "registry-research-framework/audit/registry-value-experiment-analysis.json",
    "registry-research-framework/audit/operator96-enriched-value-matrix-20260510.json",
]

LEDGER_OUTPUT_GLOBS = {
    "registry-research-framework/audit/cleanup-quarantine-ledger-20260510.json",
    "registry-research-framework/audit/cleanup-quarantine-ledger-20260510.md",
}

STAGING_CANONICAL_REPLACEMENTS: dict[str, list[str]] = {
    "evidence/files/vm-tooling-staging/showinfotip-1-hits.csv..md": [
        "evidence/raw/procmon/explorer-show-info-tips-validation-20260324/showinfotip-1-hits.csv"
    ],
    "evidence/files/vm-tooling-staging/showsuperhidden-1-hits.csv..md": [
        "evidence/raw/procmon/explorer-show-protected-operating-system-files-validation-20260324/showsuperhidden-1-hits.csv"
    ],
    "evidence/files/vm-tooling-staging/beep_start_toggle_out.txt": [
        "evidence/raw/runtime-diff/audio.disable-beep/beep-start-toggle-20260327.json"
    ],
    "evidence/files/vm-tooling-staging/hags_toggle_out.txt": [
        "evidence/raw/runtime-diff/system.enable-hags/hags-toggle-20260327.json"
    ],
    "evidence/files/vm-tooling-staging/vm-batch-probe-20260320.json..md": [
        "evidence/raw/runtime-diff/vm-batch-probe-20260320/vm-batch-probe-20260320.json"
    ],
    "evidence/files/vm-tooling-staging/defender-threat-file-hash-mpengine-1-20260325-100039": [
        "evidence/raw/procmon/security.threat-file-hash-logging/defender-threat-file-hash-mpengine-reboot-no-read-20260325.txt"
    ],
    "evidence/files/vm-tooling-staging/defender-cloud-demo-extracted": [
        "evidence/raw/external/security.threat-file-hash-logging/defender-cloud-demo-sample-metadata-20260325.json"
    ],
    "evidence/files/vm-tooling-staging/thread-dpc-enable-0-cpu3.etl.md": [
        "evidence/raw/procmon/thread-dpc-enable-vm-suite-20260324/thread-dpc-enable-0-cpu3-runtime-summary.json"
    ],
    "evidence/files/vm-tooling-staging/thread-dpc-enable-0-mem2.etl.md": [
        "evidence/raw/procmon/thread-dpc-enable-vm-suite-20260324/thread-dpc-enable-0-mem2-runtime-summary.json"
    ],
}

CLEANUP_TOOL_REFERENCE_PATHS = {
    "registry-research-framework/scripts/generate_cleanup_retained_inventory_plan.py",
    "tests/python/test_cleanup_quarantine_ledger.py",
    "tests/python/test_cleanup_retained_inventory_plan.py",
}


def is_audit_trail_reference(path: str) -> bool:
    name = Path(path).name
    return (
        path.startswith("registry-research-framework/audit/cleanup-quarantine-ledger-")
        or path.startswith("registry-research-framework/audit/cleanup-retained-inventory-plan-")
        or path.startswith("registry-research-framework/audit/branch-cleanup-ledger-")
        or name in {"research-artifact-map-latest.json", "artifact-map.md"}
    )


def cleanup_status(*, delete_eligible: bool, blocking_reference_count: int, audit_reference_count: int) -> str:
    if delete_eligible:
        return "delete-candidate"
    if blocking_reference_count > 0:
        return "retained-live-reference"
    if audit_reference_count > 0:
        return "retained-audit-trail-reference"
    return "retained-pending-review"


def now_utc() -> str:
    return datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def relative(path: Path, repo_root: Path = REPO_ROOT) -> str:
    try:
        return str(path.resolve().relative_to(repo_root.resolve())).replace("\\", "/")
    except ValueError:
        return str(path).replace("\\", "/")


def path_size(path: Path) -> int:
    if not path.exists():
        return 0
    if path.is_file():
        return path.stat().st_size
    total = 0
    for child in path.rglob("*"):
        if child.is_file():
            try:
                total += child.stat().st_size
            except OSError:
                continue
    return total


def file_sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def markdown_cell(value: Any) -> str:
    return str(value or "").replace("|", "\\|").replace("\n", " ").replace("`", "\\`")


def first_sentence(value: str, max_length: int = 160) -> str:
    text = " ".join(str(value or "").split())
    if len(text) <= max_length:
        return text
    return text[: max_length - 1].rstrip() + "..."


def reference_terms_for(path: Path, repo_root: Path = REPO_ROOT) -> list[str]:
    rel = relative(path, repo_root)
    terms = {rel, path.name}
    if path.is_file():
        terms.add(path.stem)
    return sorted(term for term in terms if term)


def is_relative_to(child: Path, parent: Path) -> bool:
    try:
        child.resolve().relative_to(parent.resolve())
        return True
    except ValueError:
        return False


def rg_files_for_term(term: str, repo_root: Path = REPO_ROOT) -> list[Path]:
    if not term:
        return []
    # The explicit "." search root is intentional: without it, rg can miss
    # matches when called through Python in this mounted workspace.
    completed = subprocess.run(
        ["rg", "-l", "--fixed-strings", "--", term, "."],
        cwd=repo_root,
        capture_output=True,
        text=True,
        check=False,
    )
    if completed.returncode not in (0, 1):
        raise RuntimeError(f"rg failed for {term!r}: {completed.stderr.strip()}")
    refs: list[Path] = []
    for line in completed.stdout.splitlines():
        cleaned = line[2:] if line.startswith("./") else line
        refs.append(repo_root / cleaned)
    return refs


def live_references(
    path: Path,
    *,
    reference_terms: list[str],
    repo_root: Path = REPO_ROOT,
    output_paths: set[str] | None = None,
    ignore_reference_paths: set[str] | None = None,
) -> list[str]:
    output_paths = output_paths or set()
    ignore_reference_paths = ignore_reference_paths or set()
    refs: set[str] = set()
    for term in reference_terms:
        for ref_path in rg_files_for_term(term, repo_root):
            rel = relative(ref_path, repo_root)
            if rel in output_paths:
                continue
            if rel in ignore_reference_paths:
                continue
            if any(rel.startswith(f"{ignored}/") for ignored in ignore_reference_paths):
                continue
            if path.is_dir() and is_relative_to(ref_path, path):
                continue
            if path.is_file() and ref_path.resolve() == path.resolve():
                continue
            refs.add(rel)
    return sorted(refs)


def is_replacement_resolved_reference(
    ref: str,
    *,
    target_rel: str,
    replacement_artifacts: list[str],
    repo_root: Path = REPO_ROOT,
) -> bool:
    if not replacement_artifacts:
        return False
    if ref in replacement_artifacts:
        return True

    replacement_dirs = {str(Path(replacement).parent).replace("\\", "/") for replacement in replacement_artifacts}
    if any(ref.startswith(f"{directory}/") for directory in replacement_dirs if directory and directory != "."):
        ref_path = repo_root / ref
        try:
            text = ref_path.read_text(encoding="utf-8-sig")
        except (OSError, UnicodeDecodeError):
            return False
        return target_rel not in text

    ref_path = repo_root / ref
    try:
        text = ref_path.read_text(encoding="utf-8-sig")
    except (OSError, UnicodeDecodeError):
        return False
    return target_rel not in text and any(replacement in text for replacement in replacement_artifacts)


def filter_replacement_resolved_references(
    refs: list[str],
    *,
    target_rel: str,
    replacement_artifacts: list[str],
    repo_root: Path = REPO_ROOT,
) -> tuple[list[str], list[str]]:
    resolved = [
        ref
        for ref in refs
        if is_replacement_resolved_reference(
            ref,
            target_rel=target_rel,
            replacement_artifacts=replacement_artifacts,
            repo_root=repo_root,
        )
    ]
    resolved_set = set(resolved)
    return [ref for ref in refs if ref not in resolved_set], resolved


def item_for(
    path: Path,
    *,
    category: str,
    stale_reason: str,
    replacement_artifacts: list[str] | None = None,
    recommended_action: str,
    repo_root: Path = REPO_ROOT,
    output_paths: set[str] | None = None,
    ignore_reference_paths: set[str] | None = None,
) -> dict[str, Any]:
    reference_terms = reference_terms_for(path, repo_root)
    refs = live_references(
        path,
        reference_terms=reference_terms,
        repo_root=repo_root,
        output_paths=output_paths,
        ignore_reference_paths=ignore_reference_paths,
    )
    refs, replacement_resolved_refs = filter_replacement_resolved_references(
        refs,
        target_rel=relative(path, repo_root),
        replacement_artifacts=replacement_artifacts or [],
        repo_root=repo_root,
    )
    audit_refs = [ref for ref in refs if is_audit_trail_reference(ref)]
    blocking_refs = [ref for ref in refs if ref not in audit_refs]
    can_delete = (
        recommended_action == "delete-after-review"
        and len(refs) == 0
        and bool(stale_reason)
        and bool(replacement_artifacts)
    )
    status = cleanup_status(
        delete_eligible=can_delete,
        blocking_reference_count=len(blocking_refs),
        audit_reference_count=len(audit_refs),
    )
    return {
        "path": relative(path, repo_root),
        "kind": "directory" if path.is_dir() else "file",
        "cleanup_status": status,
        "category": category,
        "size_bytes": path_size(path),
        "stale_reason": stale_reason,
        "replacement_artifacts": replacement_artifacts or [],
        "reference_terms": reference_terms,
        "live_reference_count": len(refs),
        "audit_reference_count": len(audit_refs),
        "blocking_reference_count": len(blocking_refs),
        "references_sample": refs[:8],
        "audit_references_sample": audit_refs[:8],
        "blocking_references_sample": blocking_refs[:8],
        "replacement_resolved_reference_count": len(replacement_resolved_refs),
        "replacement_resolved_references_sample": replacement_resolved_refs[:8],
        "delete_eligible": can_delete,
        "recommended_action": recommended_action,
    }


def refresh_item_references(
    item: dict[str, Any],
    *,
    repo_root: Path,
    output_paths: set[str],
    ignore_reference_paths: set[str],
) -> None:
    path = repo_root / str(item.get("path") or "")
    refs = live_references(
        path,
        reference_terms=list(item.get("reference_terms") or []),
        repo_root=repo_root,
        output_paths=output_paths,
        ignore_reference_paths=ignore_reference_paths,
    )
    refs, replacement_resolved_refs = filter_replacement_resolved_references(
        refs,
        target_rel=str(item.get("path") or ""),
        replacement_artifacts=list(item.get("replacement_artifacts") or []),
        repo_root=repo_root,
    )
    audit_refs = [ref for ref in refs if is_audit_trail_reference(ref)]
    blocking_refs = [ref for ref in refs if ref not in audit_refs]
    item["live_reference_count"] = len(refs)
    item["audit_reference_count"] = len(audit_refs)
    item["blocking_reference_count"] = len(blocking_refs)
    item["references_sample"] = refs[:8]
    item["audit_references_sample"] = audit_refs[:8]
    item["blocking_references_sample"] = blocking_refs[:8]
    item["replacement_resolved_reference_count"] = len(replacement_resolved_refs)
    item["replacement_resolved_references_sample"] = replacement_resolved_refs[:8]
    item["delete_eligible"] = (
        item.get("recommended_action") == "delete-after-review"
        and len(refs) == 0
        and bool(item.get("stale_reason"))
        and bool(item.get("replacement_artifacts"))
    )
    item["cleanup_status"] = cleanup_status(
        delete_eligible=bool(item["delete_eligible"]),
        blocking_reference_count=len(blocking_refs),
        audit_reference_count=len(audit_refs),
    )


def pilot_items(repo_root: Path = REPO_ROOT, output_paths: set[str] | None = None) -> list[dict[str, Any]]:
    experiments = repo_root / "registry-research-framework" / "audit" / "registry-value-experiments"
    paths = sorted(experiments.glob("pilot-perf-calculate-actual-utilization-0*"))
    items = [
        item_for(
            path,
            category="custom-value-superseded-pilot",
            stale_reason="pilot artifact superseded by the full custom registry value baseline, but referenced as safety example",
            replacement_artifacts=REPLACEMENT_ARTIFACTS,
            recommended_action="keep-referenced",
            repo_root=repo_root,
            output_paths=output_paths,
        )
        for path in paths
        if path.is_file()
    ]
    pilot_bench = repo_root / "registry-research-framework" / "audit" / "operator-regadd-value-missing-bench-pilot-20260509.json"
    if pilot_bench.exists():
        items.append(
            item_for(
                pilot_bench,
                category="custom-value-superseded-pilot",
                stale_reason="early custom value pilot superseded by full 179/179 matrix and enriched matrix",
                replacement_artifacts=REPLACEMENT_ARTIFACTS,
                recommended_action="delete-after-review",
                repo_root=repo_root,
                output_paths=output_paths,
            )
        )
    return items


def oldest_staging_items(
    repo_root: Path = REPO_ROOT,
    *,
    limit: int = 25,
    output_paths: set[str] | None = None,
) -> list[dict[str, Any]]:
    staging = repo_root / "evidence" / "files" / "vm-tooling-staging"
    if not staging.exists():
        return []
    paths = sorted((path for path in staging.iterdir()), key=lambda path: (path.stat().st_mtime, path.name))[:limit]
    items: list[dict[str, Any]] = []
    for path in paths:
        path_rel = relative(path, repo_root)
        duplicate_replacements = duplicate_raw_replacements(path, repo_root=repo_root)
        canonical_replacements = STAGING_CANONICAL_REPLACEMENTS.get(path_rel, [])
        replacements = list(dict.fromkeys([*duplicate_replacements, *canonical_replacements]))
        is_duplicate = bool(replacements)
        items.append(item_for(
            path,
            category="vm-tooling-staging-oldest-sample",
            stale_reason=(
                "staging diagnostic bundle duplicated by canonical evidence/raw artifact"
                if is_duplicate
                else "staging diagnostic bundle; verify no record/evidence-index dependency before deletion"
            ),
            replacement_artifacts=replacements,
            recommended_action="delete-after-review" if is_duplicate else "keep-pending-review",
            repo_root=repo_root,
            output_paths=output_paths,
        ))
    return items


def duplicate_raw_replacements(path: Path, *, repo_root: Path = REPO_ROOT) -> list[str]:
    raw_root = repo_root / "evidence" / "raw"
    if not raw_root.exists():
        return []
    files = [path] if path.is_file() else [child for child in path.rglob("*") if child.is_file()]
    if not files:
        return []
    replacements: list[str] = []
    for source in files:
        try:
            source_hash = file_sha256(source)
        except OSError:
            return []
        replacement = None
        for candidate in raw_root.rglob(source.name):
            if not candidate.is_file():
                continue
            try:
                if file_sha256(candidate) == source_hash:
                    replacement = relative(candidate, repo_root)
                    break
            except OSError:
                continue
        if replacement is None:
            return []
        replacements.append(replacement)
    return sorted(replacements)


def largest_raw_trace_items(
    repo_root: Path = REPO_ROOT,
    *,
    limit: int = 25,
    output_paths: set[str] | None = None,
) -> list[dict[str, Any]]:
    roots = [repo_root / "evidence" / "raw", repo_root / "evidence" / "files" / "vm-tooling-staging"]
    paths: list[Path] = []
    for root in roots:
        if not root.exists():
            continue
        paths.extend(path for path in root.rglob("*") if path.is_file() and path.suffix.lower() in {".etl", ".pml"})
    selected = sorted(paths, key=lambda path: (path.stat().st_size, str(path)), reverse=True)[:limit]
    return [
        item_for(
            path,
            category="large-raw-trace-sample",
            stale_reason="large raw trace; keep until indexed replacement/derived parse is confirmed",
            replacement_artifacts=[],
            recommended_action="keep-pending-review",
            repo_root=repo_root,
            output_paths=output_paths,
        )
        for path in selected
    ]


def audit_archive_named_items(
    repo_root: Path = REPO_ROOT,
    *,
    limit: int = 25,
    output_paths: set[str] | None = None,
) -> list[dict[str, Any]]:
    audit_dir = repo_root / "registry-research-framework" / "audit"
    if not audit_dir.exists():
        return []
    markers = ("archive", "obsolete", "superseded", "stale")
    paths = [
        path
        for path in audit_dir.iterdir()
        if path.is_file()
        and relative(path, repo_root) not in (output_paths or set())
        and any(marker in path.name.lower() for marker in markers)
    ]
    selected = sorted(paths, key=lambda path: path.name)[:limit]
    return [
        item_for(
            path,
            category="audit-archive-named-sample",
            stale_reason="audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed",
            replacement_artifacts=[],
            recommended_action="keep-pending-review",
            repo_root=repo_root,
            output_paths=output_paths,
        )
        for path in selected
    ]


def old_dated_audit_items(
    repo_root: Path = REPO_ROOT,
    *,
    limit: int = 25,
    output_paths: set[str] | None = None,
) -> list[dict[str, Any]]:
    audit_dir = repo_root / "registry-research-framework" / "audit"
    if not audit_dir.exists():
        return []
    paths = [
        path
        for path in audit_dir.iterdir()
        if path.is_file()
        and relative(path, repo_root) not in (output_paths or set())
        and any(token in path.name for token in ("202601", "202602", "202603", "202604"))
    ]
    selected = sorted(paths, key=lambda path: (path.stat().st_mtime, path.name))[:limit]
    return [
        item_for(
            path,
            category="old-dated-audit-output-sample",
            stale_reason="older dated audit output; keep until a current index, report, or historical replacement is confirmed",
            replacement_artifacts=[],
            recommended_action="keep-pending-review",
            repo_root=repo_root,
            output_paths=output_paths,
        )
        for path in selected
    ]


def build_ledger(
    *,
    repo_root: Path = REPO_ROOT,
    generated_utc: str | None = None,
    staging_limit: int = 25,
    raw_trace_limit: int = 25,
    audit_archive_limit: int = 25,
    old_audit_limit: int = 25,
    output_paths: set[str] | None = None,
) -> dict[str, Any]:
    output_paths = output_paths or set(LEDGER_OUTPUT_GLOBS)
    items: list[dict[str, Any]] = []
    items.extend(pilot_items(repo_root, output_paths=output_paths))
    items.extend(oldest_staging_items(repo_root, limit=staging_limit, output_paths=output_paths))
    items.extend(largest_raw_trace_items(repo_root, limit=raw_trace_limit, output_paths=output_paths))
    items.extend(audit_archive_named_items(repo_root, limit=audit_archive_limit, output_paths=output_paths))
    items.extend(old_dated_audit_items(repo_root, limit=old_audit_limit, output_paths=output_paths))

    deduped_items: list[dict[str, Any]] = []
    seen_paths: set[str] = set()
    for item in items:
        path = str(item.get("path") or "")
        if not path or path in seen_paths:
            continue
        seen_paths.add(path)
        deduped_items.append(item)
    items = deduped_items

    ignore_reference_paths = {
        relative(Path(__file__), repo_root),
        *CLEANUP_TOOL_REFERENCE_PATHS,
        *output_paths,
        *(str(item.get("path") or "") for item in items),
    }
    for item in items:
        refresh_item_references(
            item,
            repo_root=repo_root,
            output_paths=output_paths,
            ignore_reference_paths=ignore_reference_paths,
        )

    category_counts = Counter(str(item.get("category")) for item in items)
    status_counts = Counter(str(item.get("cleanup_status")) for item in items)
    delete_candidate_count = status_counts.get("delete-candidate", 0)
    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc or now_utc(),
        "purpose": "Quarantine ledger for cleanup review inventory. Only delete-candidate rows are cleanup candidates; retained rows are not deletion candidates.",
        "deletion_policy": [
            "No file is deleted by this generator.",
            "Delete only when live_reference_count is 0.",
            "Delete only when a replacement artifact or explicit obsolete reason is recorded.",
            "Manual review is required for raw ETL/PML and vm-tooling-staging bundles.",
            "Rows with cleanup_status other than delete-candidate are retained inventory, not deletion candidates.",
        ],
        "summary": {
            "total_items": len(items),
            "delete_candidate_count": delete_candidate_count,
            "retained_inventory_count": len(items) - delete_candidate_count,
            "delete_eligible_count": sum(1 for item in items if item.get("delete_eligible")),
            "referenced_count": sum(1 for item in items if int(item.get("live_reference_count") or 0) > 0),
            "blocking_referenced_count": sum(1 for item in items if int(item.get("blocking_reference_count") or 0) > 0),
            "audit_only_referenced_count": sum(
                1
                for item in items
                if int(item.get("live_reference_count") or 0) > 0
                and int(item.get("blocking_reference_count") or 0) == 0
            ),
            "total_size_bytes": sum(int(item.get("size_bytes") or 0) for item in items),
            "categories": dict(sorted(category_counts.items())),
            "cleanup_status_counts": dict(sorted(status_counts.items())),
        },
        "items": items,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    summary = payload.get("summary") or {}
    lines = [
        "# Cleanup Quarantine Ledger",
        "",
        f"Generated: `{payload.get('generated_utc')}`",
        "",
        str(payload.get("purpose") or ""),
        "",
        "## Deletion Policy",
        "",
    ]
    for policy in payload.get("deletion_policy") or []:
        lines.append(f"- {policy}")
    lines.extend(
        [
            "",
            "## Summary",
            "",
            "| Metric | Value |",
            "|---|---:|",
            f"| Review inventory items | {int(summary.get('total_items') or 0)} |",
            f"| Delete candidates | {int(summary.get('delete_candidate_count') or 0)} |",
            f"| Retained inventory items | {int(summary.get('retained_inventory_count') or 0)} |",
            f"| Referenced items | {int(summary.get('referenced_count') or 0)} |",
            f"| Blocking referenced items | {int(summary.get('blocking_referenced_count') or 0)} |",
            f"| Audit-only referenced items | {int(summary.get('audit_only_referenced_count') or 0)} |",
            f"| Delete eligible after review | {int(summary.get('delete_eligible_count') or 0)} |",
            f"| Total sampled size bytes | {int(summary.get('total_size_bytes') or 0)} |",
            "",
            "## Categories",
            "",
            "| Category | Count |",
            "|---|---:|",
        ]
    )
    for category, count in (summary.get("categories") or {}).items():
        lines.append(f"| `{markdown_cell(category)}` | {count} |")

    lines.extend(
        [
            "",
            "## Cleanup Statuses",
            "",
            "| Status | Count | Meaning |",
            "|---|---:|---|",
        ]
    )
    status_meanings = {
        "delete-candidate": "Eligible for deletion review because live references are zero and replacement/obsolete rationale exists.",
        "retained-live-reference": "Not a deletion candidate; real blocking references still point at it.",
        "retained-audit-trail-reference": "Not a deletion candidate yet; only audit/history references point at it.",
        "retained-pending-review": "Not a deletion candidate; more replacement or obsolete proof is required.",
    }
    for status, count in (summary.get("cleanup_status_counts") or {}).items():
        lines.append(f"| `{markdown_cell(status)}` | {count} | {markdown_cell(status_meanings.get(status, ''))} |")

    delete_items = [item for item in payload.get("items") or [] if item.get("cleanup_status") == "delete-candidate"]
    retained_items = [item for item in payload.get("items") or [] if item.get("cleanup_status") != "delete-candidate"]

    lines.extend(
        [
            "",
            "## Delete Candidates",
            "",
            "Only rows in this section are deletion candidates.",
            "",
        ]
    )
    if not delete_items:
        lines.append("_No delete candidates in this ledger._")
    else:
        lines.extend([
            "| Path | Category | Live refs | Blocking refs | Audit refs | Action | Reason |",
            "|---|---|---:|---:|---:|---|---|",
        ])
    for item in delete_items:
        lines.append(
            "| "
            f"`{markdown_cell(item.get('path'))}` | "
            f"`{markdown_cell(item.get('category'))}` | "
            f"{int(item.get('live_reference_count') or 0)} | "
            f"{int(item.get('blocking_reference_count') or 0)} | "
            f"{int(item.get('audit_reference_count') or 0)} | "
            f"`{markdown_cell(item.get('recommended_action'))}` | "
            f"{markdown_cell(first_sentence(str(item.get('stale_reason') or '')))} |"
        )

    lines.extend(
        [
            "",
            "## Retained Inventory",
            "",
            "Rows here were inspected by the cleanup scanner but are not deletion candidates.",
            "",
            "| Path | Status | Category | Live refs | Blocking refs | Audit refs | Action | Reason |",
            "|---|---|---|---:|---:|---:|---|---|",
        ]
    )
    for item in retained_items:
        lines.append(
            "| "
            f"`{markdown_cell(item.get('path'))}` | "
            f"`{markdown_cell(item.get('cleanup_status'))}` | "
            f"`{markdown_cell(item.get('category'))}` | "
            f"{int(item.get('live_reference_count') or 0)} | "
            f"{int(item.get('blocking_reference_count') or 0)} | "
            f"{int(item.get('audit_reference_count') or 0)} | "
            f"`{markdown_cell(item.get('recommended_action'))}` | "
            f"{markdown_cell(first_sentence(str(item.get('stale_reason') or '')))} |"
        )
    lines.append("")
    return "\n".join(lines)


def write_outputs(payload: dict[str, Any], json_output: Path, markdown_output: Path) -> None:
    json_output.parent.mkdir(parents=True, exist_ok=True)
    json_output.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    markdown_output.parent.mkdir(parents=True, exist_ok=True)
    markdown_output.write_text(render_markdown(payload), encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate cleanup quarantine ledger artifacts.")
    parser.add_argument("--json-output", type=Path, default=DEFAULT_JSON_OUTPUT)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_OUTPUT)
    parser.add_argument("--staging-limit", type=int, default=25)
    parser.add_argument("--raw-trace-limit", type=int, default=25)
    parser.add_argument("--audit-archive-limit", type=int, default=25)
    parser.add_argument("--old-audit-limit", type=int, default=25)
    parser.add_argument("--emit-json", action="store_true", help="Print summary JSON.")
    args = parser.parse_args()

    output_paths = {relative(args.json_output), relative(args.markdown_output)}
    payload = build_ledger(
        staging_limit=args.staging_limit,
        raw_trace_limit=args.raw_trace_limit,
        audit_archive_limit=args.audit_archive_limit,
        old_audit_limit=args.old_audit_limit,
        output_paths=output_paths,
    )
    write_outputs(payload, args.json_output, args.markdown_output)
    if args.emit_json:
        print(json.dumps(payload["summary"], indent=2))
    else:
        print(f"Wrote {args.json_output}")
        print(f"Wrote {args.markdown_output}")
        print(json.dumps(payload["summary"], indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
