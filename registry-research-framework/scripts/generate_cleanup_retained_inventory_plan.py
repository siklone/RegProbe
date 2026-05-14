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

STAGING_ACTION_TRACKS: dict[str, dict[str, str]] = {
    "rerun-needed": {
        "decision_track": "vm-rerun-required",
        "decision_status": "blocked-until-fresh-vm-evidence",
        "evidence_role": "legacy staging pointer with no canonical raw replacement",
        "retention_owner": "evidence",
        "exit_criteria": "Run a fresh VM capture or replace with a checked-in evidence/raw artifact, then migrate live references.",
        "app_surface_policy": "technical-evidence-only until rerun produces canonical artifacts",
    },
    "partial-derived-replacement-known": {
        "decision_track": "partial-derived-needs-raw-trace",
        "decision_status": "derived-summary-exists-but-raw-trace-missing",
        "evidence_role": "placeholder for raw ETL where only derived perf evidence is checked in",
        "retention_owner": "evidence",
        "exit_criteria": "Capture or check in a canonical raw ETL/summary pair, or explicitly retire the ETL placeholder from live records.",
        "app_surface_policy": "technical-evidence-only; do not surface as app-card proof",
    },
    "staging-source-of-record": {
        "decision_track": "staging-source-promote-to-raw",
        "decision_status": "staging-artifact-is-current-source",
        "evidence_role": "staging artifact still carries distinct proof that must be promoted to evidence/raw",
        "retention_owner": "evidence",
        "exit_criteria": "Copy or regenerate the distinct proof under evidence/raw and migrate live references.",
        "app_surface_policy": "technical-evidence-only until promoted to raw",
    },
}


STAGING_CANONICALIZATION_DECISIONS: dict[str, dict[str, Any]] = {
    "evidence/files/vm-tooling-staging/defender-cloud-demo-extracted": {
        "canonicalization_state": "rerun-needed",
        "owning_records": ["security.threat-file-hash-logging"],
        "canonical_replacement_candidates": [],
        "retention_rationale": "Only a Defender cloud demo extraction note currently remains; no canonical evidence/raw replacement was found in this repo.",
        "next_canonicalization_step": "Rerun or replace the Defender cloud validation under evidence/raw before migrating the note reference.",
    },
    "evidence/files/vm-tooling-staging/showinfotip-1-hits.csv..md": {
        "canonicalization_state": "canonical-raw-replacement-known",
        "owning_records": ["explorer.show-info-tips"],
        "canonical_replacement_candidates": [
            "evidence/raw/procmon/explorer-show-info-tips-validation-20260324/showinfotip-1-hits.csv"
        ],
        "retention_rationale": "The repo now has the checked-in Procmon CSV under evidence/raw; the staging placeholder is only a legacy pointer.",
        "next_canonicalization_step": "Keep live record/index references on the evidence/raw CSV; leave the staging placeholder only as audit history.",
    },
    "evidence/files/vm-tooling-staging/showsuperhidden-1-hits.csv..md": {
        "canonicalization_state": "canonical-raw-replacement-known",
        "owning_records": ["explorer.show-protected-operating-system-files"],
        "canonical_replacement_candidates": [
            "evidence/raw/procmon/explorer-show-protected-operating-system-files-validation-20260324/showsuperhidden-1-hits.csv"
        ],
        "retention_rationale": "The repo now has the checked-in Procmon CSV under evidence/raw; the staging placeholder is only a legacy pointer.",
        "next_canonicalization_step": "Keep live record/index references on the evidence/raw CSV; leave the staging placeholder only as audit history.",
    },
    "evidence/files/vm-tooling-staging/thread-dpc-enable-0-cpu3.etl.md": {
        "canonicalization_state": "partial-derived-replacement-known",
        "owning_records": ["system.kernel-thread-dpc-enable"],
        "canonical_replacement_candidates": [
            "evidence/raw/procmon/thread-dpc-enable-vm-suite-20260324/thread-dpc-enable-0-cpu3.perf.csv"
        ],
        "retention_rationale": "A derived perf CSV exists, but it is not a byte-for-byte replacement for the original ETL placeholder.",
        "next_canonicalization_step": "Retain until a new raw ETL/summary pair or explicit source-of-record decision replaces this placeholder.",
    },
    "evidence/files/vm-tooling-staging/thread-dpc-enable-0-mem2.etl.md": {
        "canonicalization_state": "partial-derived-replacement-known",
        "owning_records": ["system.kernel-thread-dpc-enable"],
        "canonical_replacement_candidates": [
            "evidence/raw/procmon/thread-dpc-enable-vm-suite-20260324/thread-dpc-enable-0-mem2.perf.csv"
        ],
        "retention_rationale": "A derived perf CSV exists, but it is not a byte-for-byte replacement for the original ETL placeholder.",
        "next_canonicalization_step": "Retain until a new raw ETL/summary pair or explicit source-of-record decision replaces this placeholder.",
    },
    "evidence/files/vm-tooling-staging/vm-batch-probe-20260320.json..md": {
        "canonicalization_state": "canonical-raw-replacement-known",
        "owning_records": [
            "security.trusted-path-credential-prompting",
            "system.disable-auto-maintenance",
            "system.memory-large-system-cache-client",
        ],
        "canonical_replacement_candidates": [
            "evidence/raw/runtime-diff/vm-batch-probe-20260320/vm-batch-probe-20260320.json"
        ],
        "retention_rationale": "A canonical evidence/raw runtime-diff JSON now carries the multi-record VM batch apply/rollback proof.",
        "next_canonicalization_step": "Keep live record/index references on the evidence/raw runtime-diff JSON; leave the staging placeholder only as audit history.",
    },
    "evidence/files/vm-tooling-staging/ghidra-probes": {
        "canonicalization_state": "active-tool-output-root",
        "owning_records": [],
        "canonical_replacement_candidates": [],
        "retention_rationale": "Current static-probe scripts still name this path as the host output root, so it is not a stale evidence bundle.",
        "next_canonicalization_step": "Move script defaults only if a broader tooling-path cleanup is planned; do not delete as evidence cleanup.",
    },
    "evidence/files/vm-tooling-staging/beep_start_toggle_out.txt": {
        "canonicalization_state": "canonical-raw-replacement-known",
        "owning_records": ["audio.disable-beep"],
        "canonical_replacement_candidates": [
            "evidence/raw/runtime-diff/audio.disable-beep/beep-start-toggle-20260327.json",
        ],
        "retention_rationale": "A canonical evidence/raw runtime-diff JSON now carries the reversible Beep value proof.",
        "next_canonicalization_step": "Keep live record/index references on the evidence/raw runtime-diff JSON; leave the staging TXT only as audit history.",
    },
    "evidence/files/vm-tooling-staging/defender-threat-file-hash-mpengine-1-20260325-100039": {
        "canonicalization_state": "canonical-raw-replacement-known",
        "owning_records": ["security.threat-file-hash-logging"],
        "canonical_replacement_candidates": [
            "evidence/raw/procmon/security.threat-file-hash-logging/defender-threat-file-hash-mpengine-reboot-no-read-20260325.txt",
        ],
        "retention_rationale": "A canonical evidence/raw Procmon text artifact now carries the distinct rebooted MPENGINE no-read proof.",
        "next_canonicalization_step": "Keep live record/index references on the evidence/raw Procmon text; leave the staging directory only as audit history.",
    },
    "evidence/files/vm-tooling-staging/hags_toggle_out.txt": {
        "canonicalization_state": "canonical-raw-replacement-known",
        "owning_records": ["system.enable-hags"],
        "canonical_replacement_candidates": [
            "evidence/raw/runtime-diff/system.enable-hags/hags-toggle-20260327.json",
        ],
        "retention_rationale": "A canonical evidence/raw runtime-diff JSON now carries the reversible HwSchMode value proof.",
        "next_canonicalization_step": "Keep live record/index references on the evidence/raw runtime-diff JSON; leave the staging TXT only as audit history.",
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
    planned = {
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
    staging_decision = STAGING_CANONICALIZATION_DECISIONS.get(str(item.get("path") or ""))
    if staging_decision:
        if staging_decision["canonicalization_state"] == "active-tool-output-root":
            planned["release_state"] = "intentional-reference-keep"
            planned["decision_track"] = "tooling-output-root"
            planned["decision_status"] = "active-tool-output-root"
            planned["evidence_role"] = "active static-analysis output root used by current scripts"
            planned["retention_owner"] = "tooling"
            planned["exit_criteria"] = (
                "Keep until a broader tooling-path migration changes script defaults and migrates any generated outputs."
            )
            planned["next_action"] = (
                "Keep as an active tooling output root; do not treat as evidence cleanup or delete-candidate work."
            )
        elif staging_decision["canonicalization_state"] in STAGING_ACTION_TRACKS:
            action_track = STAGING_ACTION_TRACKS[staging_decision["canonicalization_state"]]
            planned["decision_track"] = action_track["decision_track"]
            planned["decision_status"] = action_track["decision_status"]
            planned["evidence_role"] = action_track["evidence_role"]
            planned["retention_owner"] = action_track["retention_owner"]
            planned["exit_criteria"] = action_track["exit_criteria"]
            planned["app_surface_policy"] = action_track["app_surface_policy"]
            planned["next_action"] = staging_decision["next_canonicalization_step"]
        planned["staging_canonicalization"] = staging_decision
        planned["canonicalization_state"] = staging_decision["canonicalization_state"]
        planned["owning_records"] = staging_decision["owning_records"]
        planned["canonical_replacement_candidates"] = staging_decision["canonical_replacement_candidates"]
        planned["retention_rationale"] = staging_decision["retention_rationale"]
        planned["next_canonicalization_step"] = staging_decision["next_canonicalization_step"]
    return planned


def build_plan(ledger_path: Path = DEFAULT_LEDGER) -> dict[str, Any]:
    ledger = load_json(ledger_path)
    items = [planned_item(item) for item in ledger.get("items") or [] if isinstance(item, dict)]
    release_state_counts = Counter(str(item.get("release_state")) for item in items)
    decision_track_counts = Counter(str(item.get("decision_track")) for item in items)
    decision_status_counts = Counter(str(item.get("decision_status")) for item in items)
    canonicalization_state_counts = Counter(
        str(item.get("canonicalization_state"))
        for item in items
        if item.get("canonicalization_state")
    )
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
            "staging_canonicalization_state_counts": dict(sorted(canonicalization_state_counts.items())),
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

    lines.extend(["", "## Staging Canonicalization States", "", "| State | Count |", "|---|---:|"])
    staging_counts = summary.get("staging_canonicalization_state_counts") or {}
    if not staging_counts:
        lines.append("| `_none_` | 0 |")
    else:
        for state, count in staging_counts.items():
            lines.append(f"| `{markdown_cell(state)}` | {count} |")

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
                "| Path | Decision track | Category | Blocking refs | Canonicalization | Owner records | Next step |",
                "|---|---|---|---:|---|---|---|",
            ]
        )
    for item in retention_queue:
        owners = ", ".join(f"`{markdown_cell(owner)}`" for owner in item.get("owning_records") or [])
        canonicalization = item.get("canonicalization_state") or item.get("evidence_role")
        next_step = item.get("next_canonicalization_step") or item.get("exit_criteria")
        lines.append(
            "| "
            f"`{markdown_cell(item.get('path'))}` | "
            f"`{markdown_cell(item.get('decision_track'))}` | "
            f"`{markdown_cell(item.get('category'))}` | "
            f"{int(item.get('blocking_reference_count') or 0)} | "
            f"`{markdown_cell(canonicalization)}` | "
            f"{owners or '_none_'} | "
            f"{markdown_cell(first_sentence(str(next_step or ''), 150))} |"
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
