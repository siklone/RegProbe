#!/usr/bin/env python3
from __future__ import annotations

import json
import os
from collections import Counter, defaultdict
from pathlib import Path
from typing import Any

from research_path_lib import REPO_ROOT, RESEARCH_ROOT, display_reference_label, linkify_reference_text, normalize_reference

EVIDENCE_INDEX_PATH = RESEARCH_ROOT / "evidence-index.json"
OUTPUT_PATH = RESEARCH_ROOT / "evidence-atlas.md"


def md_escape(value: Any) -> str:
    text = "" if value is None else str(value)
    return text.replace("|", "\\|").replace("\r", "").replace("\n", "<br>")


def md_code(value: Any) -> str:
    text = "" if value is None else str(value)
    return "`" + text.replace("`", "\\`") + "`"


def normalize_row_item(value: Any) -> dict[str, Any]:
    if isinstance(value, dict):
        return value
    if value is None:
        return {}
    text = str(value)
    return {
        "evidence_id": text,
        "kind": "",
        "origin": "",
        "title": text,
        "location": text,
        "strength": "",
        "supports": [],
        "id": text,
        "tool": "",
        "filename": text,
        "sha256": "",
        "release_url": "",
    }


def md_link(target: str, label: str | None = None) -> str:
    normalized = normalize_reference(target)
    if not normalized:
        return md_escape(label or target)

    if normalized.startswith("http://") or normalized.startswith("https://"):
        return f"[{md_escape(label or normalized)}]({normalized})"

    absolute = REPO_ROOT / normalized.replace("/", os.sep)
    relative = os.path.relpath(absolute, OUTPUT_PATH.parent).replace("\\", "/")
    return f"[{md_escape(label or display_reference_label(normalized))}]({relative})"


def format_value(value: Any) -> str:
    if value is None:
        return "-"
    if isinstance(value, bool):
        return "true" if value else "false"
    return md_code(value)


def render_table(headers: list[str], rows: list[list[str]]) -> list[str]:
    out = [
        "| " + " | ".join(headers) + " |",
        "| " + " | ".join(["---"] * len(headers)) + " |",
    ]
    for row in rows:
        out.append("| " + " | ".join(row) + " |")
    return out


def load_index() -> dict[str, Any]:
    with EVIDENCE_INDEX_PATH.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def group_records(records: list[dict[str, Any]]) -> dict[str, dict[str, list[dict[str, Any]]]]:
    grouped: dict[str, dict[str, list[dict[str, Any]]]] = defaultdict(lambda: defaultdict(list))
    for record in records:
        status = record.get("record_status", "unknown")
        category = record.get("category", "Uncategorized") or "Uncategorized"
        grouped[status][category].append(record)
    for status_map in grouped.values():
        for rows in status_map.values():
            rows.sort(key=lambda item: item.get("record_id") or "")
    return grouped


def write_record(lines: list[str], record: dict[str, Any]) -> None:
    record_id = record.get("record_id") or "unknown"
    lines.append(f"### {md_code(record_id)}")
    lines.append("")
    meta_rows = [
        ["Status", md_code(record.get("record_status"))],
        ["Evidence class", md_code((record.get("evidence_class") or {}).get("class_label"))],
        ["Category", md_code(record.get("category"))],
        ["Area", md_code(record.get("area"))],
        ["Scope", md_code(record.get("scope"))],
        ["Source file", md_link(str(record.get("source_file") or ""))],
        ["V3.1 evidence root", md_link(str(record.get("v31_evidence_root") or "")) if record.get("v31_evidence_root") else "-"],
        ["Apply allowed", md_code((record.get("decision") or {}).get("apply_allowed"))],
        ["Confidence", md_code((record.get("decision") or {}).get("confidence"))],
        ["Needs VM validation", md_code((record.get("decision") or {}).get("needs_vm_validation"))],
    ]
    lines.extend(render_table(["Field", "Value"], meta_rows))
    lines.append("")
    lines.append(f"**Summary:** {md_escape(record.get('summary'))}")
    lines.append("")

    impl = record.get("app_current_implementation") or {}
    lines.append("**Current implementation**")
    lines.append("")
    impl_rows = [
        ["Status", md_code(impl.get("status"))],
        ["Provider source", md_escape(impl.get("provider_source"))],
        ["Notes", md_escape(impl.get("notes"))],
    ]
    lines.extend(render_table(["Field", "Value"], impl_rows))
    lines.append("")
    writes = impl.get("writes") or []
    if writes:
        write_rows = []
        for write in writes:
            write_rows.append([
                md_code(write.get("target_id")),
                md_code(write.get("path")),
                md_code(write.get("value_name")),
                format_value(write.get("value")),
                md_code(write.get("state_kind")),
                md_escape(write.get("notes")),
            ])
        lines.append("Current writes")
        lines.append("")
        lines.extend(render_table(["Target", "Path", "Value", "State", "Kind", "Notes"], write_rows))
        lines.append("")

    evidence_class = record.get("evidence_class") or {}
    lines.append("**Evidence class**")
    lines.append("")
    class_rows = [
        ["Label", md_code(evidence_class.get("class_label"))],
        ["Title", md_escape(evidence_class.get("class_title"))],
        ["Action state", md_code(evidence_class.get("action_state"))],
        ["Gating reason", md_escape(evidence_class.get("gating_reason"))],
    ]
    lines.extend(render_table(["Field", "Value"], class_rows))
    lines.append("")

    provenance = record.get("provenance") or {}
    lines.append("**Sources**")
    lines.append("")
    provenance_rows = [
        ["Coverage state", md_code(provenance.get("coverage_state"))],
        ["Has nohuto lineage", md_code(provenance.get("has_nohuto_evidence"))],
        ["Has Windows Internals notes", md_code(provenance.get("has_windows_internals_context"))],
        ["Needs review", md_code(provenance.get("needs_review"))],
        ["Source repositories", md_escape(", ".join(provenance.get("source_repositories", []) or []))],
        ["Matched tokens", md_escape(", ".join(provenance.get("matched_tokens", []) or []))],
        ["Lineage note", md_escape(provenance.get("lineage_note"))],
    ]
    lines.extend(render_table(["Field", "Value"], provenance_rows))
    lines.append("")
    for label, key in (
        ("Nohuto lineage references", "nohuto_references"),
        ("Windows Internals references", "windows_internals_references"),
        ("Other source references", "other_references"),
    ):
        refs = provenance.get(key) or []
        if not refs:
            continue
        lines.append(f"{label}:")
        lines.append("")
        ref_rows = []
        for ref in refs:
            row = [md_escape(ref.get("Title"))]
            if key == "other_references":
                row.insert(0, md_escape(ref.get("Kind")))
            row.extend([
                linkify_reference_text(ref.get("Url"), OUTPUT_PATH),
                md_escape(ref.get("Summary")),
            ])
            ref_rows.append(row)
        headers = ["Title", "Location", "Summary"] if key != "other_references" else ["Kind", "Title", "Location", "Summary"]
        lines.extend(render_table(headers, ref_rows))
        lines.append("")

    targets = record.get("targets", []) or []
    lines.append("**Targets**")
    lines.append("")
    for target in targets:
        lines.append(f"#### {md_code(target.get('target_id'))}")
        lines.append("")
        target_rows = [
            ["Location kind", md_code(target.get("location_kind"))],
            ["Path", md_code(target.get("path"))],
            ["Value name", md_code(target.get("value_name"))],
            ["Value type", md_code(target.get("value_type"))],
        ]
        if target.get("notes"):
            target_rows.append(["Notes", linkify_reference_text(str(target.get("notes")), OUTPUT_PATH)])
        lines.extend(render_table(["Field", "Value"], target_rows))
        lines.append("")
        allowed_rows = []
        for allowed in target.get("allowed_values", []) or []:
            allowed_rows.append([
                md_code(allowed.get("state_kind")),
                format_value(allowed.get("value")),
                md_escape(allowed.get("label")),
                md_escape(allowed.get("meaning")),
                md_escape(", ".join(allowed.get("evidence_ids", []) or [])),
            ])
        if allowed_rows:
            lines.extend(render_table(["State", "Value", "Label", "Meaning", "Evidence IDs"], allowed_rows))
            lines.append("")

    defaults = record.get("windows_defaults", []) or []
    if defaults:
        lines.append("**Windows defaults**")
        lines.append("")
        default_rows = []
        for default in defaults:
            states = []
            for state in default.get("states", []) or []:
                states.append(
                    f"{state.get('target_id')}: {state.get('state_kind')} {state.get('value')!r} - {state.get('rationale')}"
                )
            default_rows.append([
                md_escape(default.get("label")),
                md_escape(default.get("applies_to")),
                md_escape("; ".join(states)),
            ])
        lines.extend(render_table(["Label", "Applies to", "States"], default_rows))
        lines.append("")

    profiles = record.get("recommended_profiles", []) or []
    if profiles:
        lines.append("**Recommended profiles**")
        lines.append("")
        profile_rows = []
        for profile in profiles:
            profile_rows.append([
                md_code(profile.get("profile_id")),
                md_escape(profile.get("label")),
                md_escape(profile.get("intended_for")),
                md_escape(profile.get("avoid_for")),
                md_code(profile.get("apply_allowed")),
            ])
        lines.extend(render_table(["Profile", "Label", "Intended for", "Avoid for", "Apply allowed"], profile_rows))
        lines.append("")

    evidence = record.get("evidence", []) or []
    if evidence:
        lines.append("**Evidence**")
        lines.append("")
        evidence_rows = []
        for item in evidence:
            item = normalize_row_item(item)
            evidence_rows.append([
                md_code(item.get("evidence_id")),
                md_code(item.get("kind")),
                md_code(item.get("origin")),
                md_escape(item.get("title")),
                linkify_reference_text(item.get("location"), OUTPUT_PATH),
                md_code(item.get("strength")),
                md_escape(", ".join(item.get("supports", []) or [])),
            ])
        lines.extend(render_table(["Evidence ID", "Kind", "Origin", "Title", "Location", "Strength", "Supports"], evidence_rows))
        lines.append("")

    artifact_refs = record.get("artifact_refs") or []
    if artifact_refs:
        lines.append("**Artifact refs**")
        lines.append("")
        artifact_rows = []
        for item in artifact_refs:
            item = normalize_row_item(item)
            artifact_rows.append(
                [
                    md_code(item.get("id")),
                    md_code(item.get("tool")),
                    md_escape(item.get("filename")),
                    md_code(item.get("sha256")),
                    linkify_reference_text(item.get("release_url"), OUTPUT_PATH),
                ]
            )
        lines.extend(render_table(["ID", "Tool", "Filename", "SHA256", "Release URL"], artifact_rows))
        lines.append("")

    proof = record.get("validation_proof")
    if proof:
        lines.append("**Validation proof**")
        lines.append("")
        proof_rows = [
            ["Source", linkify_reference_text(proof.get("source_url"), OUTPUT_PATH)],
            ["Exact quote / path", linkify_reference_text(proof.get("exact_quote_or_path"), OUTPUT_PATH)],
            ["Key found on page", md_code(proof.get("key_found_on_page"))],
        ]
        if proof.get("notes"):
            proof_rows.append(["Notes", linkify_reference_text(str(proof.get("notes")), OUTPUT_PATH)])
        lines.extend(render_table(["Field", "Value"], proof_rows))
        lines.append("")

    decision = record.get("decision") or {}
    if decision:
        lines.append("**Decision**")
        lines.append("")
        decision_rows = [
            ["Apply allowed", md_code(decision.get("apply_allowed"))],
            ["Recommended for general users", md_code(decision.get("recommended_for_general_users"))],
            ["Restore default supported", md_code(decision.get("restore_default_supported"))],
            ["Restore previous supported", md_code(decision.get("restore_previous_supported"))],
            ["Needs VM validation", md_code(decision.get("needs_vm_validation"))],
            ["Why", md_escape(decision.get("why"))],
        ]
        lines.extend(render_table(["Field", "Value"], decision_rows))
        lines.append("")
        blocking = decision.get("blocking_issues") or []
        if blocking:
            lines.append("Blocking issues:")
            for item in blocking:
                lines.append(f"- {md_escape(item)}")
            lines.append("")


def main() -> int:
    if not EVIDENCE_INDEX_PATH.exists():
        raise FileNotFoundError(f"Missing evidence index: {EVIDENCE_INDEX_PATH}")

    data = load_index()
    records = data.get("records", []) or []
    summary = data.get("summary", {}) or {}
    grouped = group_records(records)

    category_counts = Counter()
    for record in records:
        category_counts[record.get("category", "Uncategorized") or "Uncategorized"] += 1

    lines: list[str] = []
    lines.append("# Evidence Atlas")
    lines.append("")
    lines.append("This report collects every tweak record into one place: keys, values, evidence, runtime proof, and source links.")
    lines.append("Nohuto references only show upstream dump or naming links. Value semantics come from the record evidence and validation proof.")
    lines.append("")
    lines.append("## Summary")
    lines.append("")
    summary_rows = [
        ["Total records", str(summary.get("total_records", len(records)))],
        ["Validated", str(summary.get("validated", 0))],
        ["Deprecated", str(summary.get("deprecated", 0))],
        ["Review required", str(summary.get("review_required", 0))],
        ["Records with evidence", str(summary.get("records_with_evidence", 0))],
        ["Records without evidence", str(summary.get("records_without_evidence", 0))],
        ["Records missing validation proof", str(summary.get("records_missing_validation_proof", 0))],
        ["Deprecated missing validation proof", str(summary.get("deprecated_missing_validation_proof", 0))],
    ]
    class_counts = summary.get("class_counts") or {}
    for class_id in ["A", "B", "C", "D", "E"]:
        if class_id in class_counts:
            summary_rows.append([f"Class {class_id}", str(class_counts[class_id])])
    lines.extend(render_table(["Field", "Value"], summary_rows))
    lines.append("")
    lines.append("## Category coverage")
    lines.append("")
    category_rows = [[md_escape(category), str(count)] for category, count in sorted(category_counts.items())]
    lines.extend(render_table(["Category", "Records"], category_rows))
    lines.append("")

    for status in ["validated", "deprecated", "review-required"]:
        status_records = grouped.get(status, {})
        if not status_records:
            continue
        lines.append(f"## {status.replace('-', ' ').title()}")
        lines.append("")
        for category in sorted(status_records):
            lines.append(f"### {md_escape(category)}")
            lines.append("")
            for record in status_records[category]:
                write_record(lines, record)
                lines.append("---")
                lines.append("")

    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT_PATH.write_text("\n".join(lines).rstrip() + "\n", encoding="utf-8")
    print(f"Wrote {OUTPUT_PATH}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
