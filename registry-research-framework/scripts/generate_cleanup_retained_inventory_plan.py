#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from collections import Counter
from datetime import UTC, datetime
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
AUDIT_DIR = REPO_ROOT / "registry-research-framework" / "audit"
DEFAULT_LEDGER = AUDIT_DIR / "cleanup-quarantine-ledger-20260514.json"
DEFAULT_JSON_OUTPUT = AUDIT_DIR / "cleanup-retained-inventory-plan-20260514.json"
DEFAULT_MARKDOWN_OUTPUT = AUDIT_DIR / "cleanup-retained-inventory-plan-20260514.md"


def now_utc() -> str:
    return datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def rel(path: Path, repo_root: Path = REPO_ROOT) -> str:
    try:
        return path.resolve().relative_to(repo_root.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def load_json(path: Path) -> dict[str, Any]:
    payload = json.loads(path.read_text(encoding="utf-8-sig"))
    return payload if isinstance(payload, dict) else {}


def markdown_cell(value: Any) -> str:
    return str(value if value is not None else "").replace("|", "\\|").replace("\n", " ")


def first_sentence(value: str, max_length: int = 150) -> str:
    text = " ".join(str(value or "").split())
    if len(text) <= max_length:
        return text
    return text[: max_length - 1].rstrip() + "..."


def classify_reference(path: str) -> str:
    if path in {"README.md", "CONTRIBUTING.md"} or path.startswith("Docs/"):
        return "public-doc-reference"
    if path.startswith("research/records/"):
        return "research-record-reference"
    if path in {
        "research/evidence-atlas.md",
        "research/evidence-classes.json",
        "research/evidence-index.json",
        "research/evidence-manifest.json",
        "research/evidence-manifest.md",
    }:
        return "research-index-reference"
    if path.startswith("research/notes/"):
        return "research-note-reference"
    if path.startswith("registry-research-framework/discovery/"):
        return "discovery-index-reference"
    if path.startswith("registry-research-framework/audit/"):
        return "audit-cross-reference"
    if path.startswith("evidence/raw/") or path.startswith("evidence/files/"):
        return "evidence-tree-reference"
    return "other-reference"


def release_state(item: dict[str, Any]) -> str:
    if item.get("cleanup_status") == "delete-candidate" or item.get("delete_eligible"):
        return "delete-ready"
    if item.get("cleanup_status") == "retained-audit-trail-reference":
        return "audit-only-retained"
    if (
        item.get("recommended_action") == "delete-after-review"
        and item.get("replacement_artifacts")
        and int(item.get("blocking_reference_count") or 0) > 0
    ):
        return "reference-migration-needed"
    if item.get("recommended_action") == "keep-referenced":
        return "intentional-reference-keep"
    if not item.get("replacement_artifacts"):
        return "needs-replacement-or-retention-decision"
    return "retained-pending-review"


DECISION_TRACKS: dict[str, dict[str, str]] = {
    "delete-ready": {
        "decision_track": "delete-ready",
        "decision_status": "ready-for-dedicated-delete-pr",
        "evidence_role": "obsolete artifact with replacement proof and no live references",
        "retention_owner": "cleanup",
        "exit_criteria": "Delete in a dedicated cleanup PR after reviewer confirms the replacement/obsolete reason.",
        "app_surface_policy": "not-app-surface",
    },
    "reference-migration-needed": {
        "decision_track": "reference-migration",
        "decision_status": "blocked-by-live-references-to-old-path",
        "evidence_role": "old artifact with known replacement",
        "retention_owner": "cleanup",
        "exit_criteria": "Move live references to replacement artifacts, rerun the ledger, then reclassify as delete-ready or audit-only retained.",
        "app_surface_policy": "not-app-surface",
    },
    "audit-only-retained": {
        "decision_track": "audit-trail-retained",
        "decision_status": "retained-for-history-only",
        "evidence_role": "historical cleanup/audit trail reference",
        "retention_owner": "audit",
        "exit_criteria": "Keep unless a future cleanup PR explicitly chooses to drop audit-history references.",
        "app_surface_policy": "not-app-surface",
    },
    "intentional-reference-keep": {
        "decision_track": "intentional-reference-keep",
        "decision_status": "explicitly-retained",
        "evidence_role": "referenced example or historical safety artifact",
        "retention_owner": "research",
        "exit_criteria": "Keep until maintainers replace the current reference with a newer source-of-record artifact.",
        "app_surface_policy": "not-app-surface",
    },
    "retained-pending-review": {
        "decision_track": "pending-retention-review",
        "decision_status": "needs-owner-review",
        "evidence_role": "retained artifact without enough cleanup metadata",
        "retention_owner": "cleanup",
        "exit_criteria": "Add replacement proof, explicit retention rationale, or an obsolete reason.",
        "app_surface_policy": "not-app-surface",
    },
}


NEEDS_DECISION_TRACKS: dict[str, dict[str, str]] = {
    "large-raw-trace-sample": {
        "decision_track": "raw-trace-source-of-record",
        "decision_status": "retain-until-derived-parse-reviewed",
        "evidence_role": "raw ETL/PML backing derived evidence, records, or evidence indexes",
        "retention_owner": "evidence",
        "exit_criteria": "Create or verify a derived CSV/JSON/summary replacement, move live record/index references off the raw trace if appropriate, then rerun cleanup.",
        "app_surface_policy": "technical-evidence-only; never normal app card copy",
    },
    "vm-tooling-staging-oldest-sample": {
        "decision_track": "staging-bundle-canonicalization",
        "decision_status": "needs-canonical-evidence-or-retention-note",
        "evidence_role": "legacy staging output that may still be the only referenced proof for a record",
        "retention_owner": "evidence",
        "exit_criteria": "Find a canonical evidence/raw replacement or add an explicit keep rationale naming the owning record/note.",
        "app_surface_policy": "technical-evidence-only until canonicalized",
    },
    "old-dated-audit-output-sample": {
        "decision_track": "historical-audit-output",
        "decision_status": "retain-until-supersession-is-explicit",
        "evidence_role": "dated audit output still referenced by records, notes, indexes, or closure ledgers",
        "retention_owner": "audit",
        "exit_criteria": "Name the superseding report/index and migrate live references, or mark as explicit historical retention.",
        "app_surface_policy": "not-app-surface",
    },
    "audit-archive-named-sample": {
        "decision_track": "archive-history-anchor",
        "decision_status": "retain-history-anchor",
        "evidence_role": "archive/history bundle used by archive checkers, manifests, or checksum ledgers",
        "retention_owner": "audit",
        "exit_criteria": "Keep unless archive checkers and checksum ledgers are retired or moved to a replacement archive.",
        "app_surface_policy": "not-app-surface",
    },
}


def decision_track_for(item: dict[str, Any], state: str) -> dict[str, str]:
    if state == "needs-replacement-or-retention-decision":
        category = str(item.get("category") or "")
        track = NEEDS_DECISION_TRACKS.get(category)
        if track:
            return track
    return DECISION_TRACKS.get(
        state,
        {
            "decision_track": "unknown-retention-state",
            "decision_status": "needs-cleanup-generator-review",
            "evidence_role": "unknown",
            "retention_owner": "cleanup",
            "exit_criteria": "Update cleanup retained inventory classification logic.",
            "app_surface_policy": "not-app-surface",
        },
    )


def next_action_for(item: dict[str, Any], state: str) -> str:
    category = str(item.get("category") or "")
    if state == "delete-ready":
        return "Review the replacement/obsolete reason, then delete in a dedicated cleanup PR."
    if state == "reference-migration-needed":
        return "Move blocking docs/records to the listed replacement artifacts, rerun the ledger, then promote to delete-candidate if live refs reach zero."
    if state == "audit-only-retained":
        return "No live blocking references remain; keep for audit trail or handle in a dedicated deletion PR that explicitly accepts audit-only history references."
    if state == "intentional-reference-keep":
        return "Keep as a historical example unless a maintainer explicitly rewrites the current docs/record to the replacement artifacts."
    if category == "large-raw-trace-sample":
        return "Keep until a derived parse or current index replaces the raw ETL/PML reference."
    if category == "vm-tooling-staging-oldest-sample":
        return "Decide whether this staging bundle has a canonical evidence/raw replacement before attempting deletion."
    if category in {"audit-archive-named-sample", "old-dated-audit-output-sample"}:
        return "Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed."
    return "Add a replacement artifact or explicit retention decision before changing this file."


def planned_item(item: dict[str, Any]) -> dict[str, Any]:
    refs = list(item.get("blocking_references_sample") or [])
    classes = Counter(classify_reference(ref) for ref in refs)
    state = release_state(item)
    decision = decision_track_for(item, state)
    return {
        "path": item.get("path"),
        "category": item.get("category"),
        "cleanup_status": item.get("cleanup_status"),
        "release_state": state,
        "delete_candidate_state": "delete-candidate" if state == "delete-ready" else "not-a-delete-candidate",
        "decision_track": decision["decision_track"],
        "decision_status": decision["decision_status"],
        "evidence_role": decision["evidence_role"],
        "retention_owner": decision["retention_owner"],
        "exit_criteria": decision["exit_criteria"],
        "app_surface_policy": decision["app_surface_policy"],
        "can_become_delete_candidate": state == "reference-migration-needed",
        "blocking_reference_count": int(item.get("blocking_reference_count") or 0),
        "audit_reference_count": int(item.get("audit_reference_count") or 0),
        "blocking_reference_classes": dict(sorted(classes.items())),
        "blocking_references_sample": refs,
        "replacement_artifacts": list(item.get("replacement_artifacts") or []),
        "recommended_action": item.get("recommended_action"),
        "next_action": next_action_for(item, state),
        "stale_reason": item.get("stale_reason"),
    }


def build_plan(ledger_path: Path = DEFAULT_LEDGER) -> dict[str, Any]:
    ledger = load_json(ledger_path)
    items = [planned_item(item) for item in ledger.get("items") or [] if isinstance(item, dict)]
    release_state_counts = Counter(str(item.get("release_state")) for item in items)
    decision_track_counts = Counter(str(item.get("decision_track")) for item in items)
    decision_status_counts = Counter(str(item.get("decision_status")) for item in items)
    category_counts = Counter(str(item.get("category")) for item in items)
    blocker_counts: Counter[str] = Counter()
    blocker_path_counts: Counter[str] = Counter()
    for item in items:
        blocker_counts.update(item.get("blocking_reference_classes") or {})
        blocker_path_counts.update(item.get("blocking_references_sample") or [])

    return {
        "schema_version": "1.0",
        "generated_utc": now_utc(),
        "ledger": rel(ledger_path),
        "purpose": "Action plan for cleanup retained inventory. It does not delete files and it does not redefine delete eligibility.",
        "rules": {
            "delete_candidate_rule": "Only release_state=delete-ready rows may enter a deletion PR.",
            "retained_rule": "retained rows are not stale-delete candidates; they need reference migration, replacement proof, or an explicit retention decision.",
            "operator_rule": "Use this plan to reduce blocking references before regenerating cleanup-quarantine-ledger.",
        },
        "summary": {
            "item_count": len(items),
            "delete_ready_count": release_state_counts.get("delete-ready", 0),
            "reference_migration_needed_count": release_state_counts.get("reference-migration-needed", 0),
            "audit_only_retained_count": release_state_counts.get("audit-only-retained", 0),
            "intentional_reference_keep_count": release_state_counts.get("intentional-reference-keep", 0),
            "needs_replacement_or_retention_decision_count": release_state_counts.get("needs-replacement-or-retention-decision", 0),
            "retention_decision_queue_count": release_state_counts.get("needs-replacement-or-retention-decision", 0),
            "retained_pending_review_count": release_state_counts.get("retained-pending-review", 0),
            "release_state_counts": dict(sorted(release_state_counts.items())),
            "decision_track_counts": dict(sorted(decision_track_counts.items())),
            "decision_status_counts": dict(sorted(decision_status_counts.items())),
            "category_counts": dict(sorted(category_counts.items())),
            "blocking_reference_class_counts": dict(sorted(blocker_counts.items())),
            "top_blocking_reference_paths": [
                {"path": path, "count": count}
                for path, count in blocker_path_counts.most_common(15)
            ],
        },
        "reference_migration_queue": [
            item for item in items if item.get("release_state") == "reference-migration-needed"
        ],
        "retention_decision_queue": [
            item for item in items if item.get("release_state") == "needs-replacement-or-retention-decision"
        ],
        "retained_inventory": items,
    }


def render_markdown(plan: dict[str, Any]) -> str:
    summary = plan.get("summary") or {}
    lines = [
        "# Cleanup Retained Inventory Plan",
        "",
        f"Generated: `{plan.get('generated_utc')}`",
        f"Ledger: `{plan.get('ledger')}`",
        "",
        str(plan.get("purpose") or ""),
        "",
        "## Rules",
        "",
    ]
    for key, value in (plan.get("rules") or {}).items():
        lines.append(f"- `{key}`: {value}")

    lines.extend(
        [
            "",
            "## Summary",
            "",
            "| Metric | Value |",
            "|---|---:|",
            f"| Retained inventory items | {int(summary.get('item_count') or 0)} |",
            f"| Delete-ready rows | {int(summary.get('delete_ready_count') or 0)} |",
            f"| Reference migration needed | {int(summary.get('reference_migration_needed_count') or 0)} |",
            f"| Audit-only retained | {int(summary.get('audit_only_retained_count') or 0)} |",
            f"| Intentional reference keep | {int(summary.get('intentional_reference_keep_count') or 0)} |",
            f"| Needs replacement/retention decision | {int(summary.get('needs_replacement_or_retention_decision_count') or 0)} |",
            f"| Retention decision queue | {int(summary.get('retention_decision_queue_count') or 0)} |",
            f"| Retained pending review | {int(summary.get('retained_pending_review_count') or 0)} |",
            "",
            "## Release States",
            "",
            "| State | Count |",
            "|---|---:|",
        ]
    )
    for state, count in (summary.get("release_state_counts") or {}).items():
        lines.append(f"| `{markdown_cell(state)}` | {count} |")

    lines.extend(["", "## Decision Tracks", "", "| Track | Count |", "|---|---:|"])
    for track, count in (summary.get("decision_track_counts") or {}).items():
        lines.append(f"| `{markdown_cell(track)}` | {count} |")

    lines.extend(["", "## Top Blocking Reference Paths", "", "| Path | Count |", "|---|---:|"])
    for item in summary.get("top_blocking_reference_paths") or []:
        lines.append(f"| `{markdown_cell(item.get('path'))}` | {int(item.get('count') or 0)} |")

    lines.extend(
        [
            "",
            "## Reference Migration Queue",
            "",
            "These rows are the only retained items that already have replacement artifacts and can plausibly become delete-candidates after references are migrated.",
            "",
        ]
    )
    queue = plan.get("reference_migration_queue") or []
    if not queue:
        lines.append("_No reference migration rows currently exist._")
    else:
        lines.extend(
            [
                "| Path | Category | Blocking refs | Replacement artifacts | Next action |",
                "|---|---|---:|---|---|",
            ]
        )
    for item in queue:
        replacements = ", ".join(f"`{markdown_cell(path)}`" for path in item.get("replacement_artifacts") or [])
        lines.append(
            "| "
            f"`{markdown_cell(item.get('path'))}` | "
            f"`{markdown_cell(item.get('category'))}` | "
            f"{int(item.get('blocking_reference_count') or 0)} | "
            f"{replacements or 'none'} | "
            f"{markdown_cell(item.get('next_action'))} |"
        )

    lines.extend(
        [
            "",
            "## Retention Decision Queue",
            "",
            "These rows are not delete candidates. They need a replacement artifact, an explicit retention rationale, or a source-of-record decision before any deletion work.",
            "",
        ]
    )
    retention_queue = plan.get("retention_decision_queue") or []
    if not retention_queue:
        lines.append("_No retention decision rows currently exist._")
    else:
        lines.extend(
            [
                "| Path | Decision track | Category | Blocking refs | Evidence role | Exit criteria |",
                "|---|---|---|---:|---|---|",
            ]
        )
    for item in retention_queue:
        lines.append(
            "| "
            f"`{markdown_cell(item.get('path'))}` | "
            f"`{markdown_cell(item.get('decision_track'))}` | "
            f"`{markdown_cell(item.get('category'))}` | "
            f"{int(item.get('blocking_reference_count') or 0)} | "
            f"{markdown_cell(first_sentence(str(item.get('evidence_role') or ''), 110))} | "
            f"{markdown_cell(first_sentence(str(item.get('exit_criteria') or ''), 130))} |"
        )

    lines.extend(
        [
            "",
            "## Retained Inventory Worklist",
            "",
            "| Path | Release state | Decision track | Category | Blocking refs | Next action |",
            "|---|---|---|---|---:|---|",
        ]
    )
    for item in plan.get("retained_inventory") or []:
        lines.append(
            "| "
            f"`{markdown_cell(item.get('path'))}` | "
            f"`{markdown_cell(item.get('release_state'))}` | "
            f"`{markdown_cell(item.get('decision_track'))}` | "
            f"`{markdown_cell(item.get('category'))}` | "
            f"{int(item.get('blocking_reference_count') or 0)} | "
            f"{markdown_cell(first_sentence(str(item.get('next_action') or '')))} |"
        )

    lines.append("")
    return "\n".join(lines)


def write_outputs(plan: dict[str, Any], json_output: Path, markdown_output: Path) -> None:
    json_output.parent.mkdir(parents=True, exist_ok=True)
    json_output.write_text(json.dumps(plan, indent=2) + "\n", encoding="utf-8")
    markdown_output.parent.mkdir(parents=True, exist_ok=True)
    markdown_output.write_text(render_markdown(plan), encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate an action plan for cleanup retained inventory.")
    parser.add_argument("--ledger", type=Path, default=DEFAULT_LEDGER)
    parser.add_argument("--json-output", type=Path, default=DEFAULT_JSON_OUTPUT)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_OUTPUT)
    parser.add_argument("--emit-json", action="store_true")
    args = parser.parse_args()

    plan = build_plan(args.ledger.resolve())
    write_outputs(plan, args.json_output.resolve(), args.markdown_output.resolve())
    if args.emit_json:
        print(json.dumps(plan["summary"], indent=2))
    else:
        print(f"Wrote {args.json_output}")
        print(f"Wrote {args.markdown_output}")
        print(json.dumps(plan["summary"], indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
