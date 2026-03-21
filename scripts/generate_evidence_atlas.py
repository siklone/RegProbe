#!/usr/bin/env python3
from __future__ import annotations

import json
from collections import Counter, defaultdict
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[1]
EVIDENCE_INDEX_PATH = REPO_ROOT / "Docs" / "tweaks" / "research" / "evidence-index.json"
OUTPUT_PATH = REPO_ROOT / "Docs" / "tweaks" / "research" / "evidence-atlas.md"
REDACTED_USER = "<USER>"
HOME_PATH = str(Path.home())
HOME_PATH_FWD = HOME_PATH.replace("\\", "/")
USER_PATH_REPLACEMENTS = {
    HOME_PATH: HOME_PATH.replace(Path.home().name, REDACTED_USER),
    HOME_PATH_FWD: HOME_PATH_FWD.replace(Path.home().name, REDACTED_USER),
}


def sanitize_text(value: Any) -> str:
    text = "" if value is None else str(value)
    for source, replacement in USER_PATH_REPLACEMENTS.items():
        text = text.replace(source, replacement)
    return text


def md_escape(value: Any) -> str:
    text = sanitize_text(value)
    return text.replace("|", "\\|").replace("\n", "<br>")


def md_code(value: Any) -> str:
    text = sanitize_text(value)
    return "`" + text.replace("`", "\\`") + "`"


def format_value(value: Any) -> str:
    if value is None:
        return "—"
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


def as_lines(text: str) -> list[str]:
    return text.splitlines() if text else []


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
        for category, rows in status_map.items():
            rows.sort(key=lambda item: item.get("record_id") or "")
    return grouped


def write_record(lines: list[str], record: dict[str, Any]) -> None:
    record_id = record.get("record_id") or "unknown"
    lines.append(f"### {md_code(record_id)}")
    lines.append("")
    meta_rows = [
        ["Status", md_code(record.get("record_status"))],
        ["Category", md_code(record.get("category"))],
        ["Area", md_code(record.get("area"))],
        ["Scope", md_code(record.get("scope"))],
        ["Source file", md_code(record.get("source_file"))],
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
        ["Provider source", md_code(impl.get("provider_source"))],
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
        lines.append("Current write(s):")
        lines.append("")
        lines.extend(render_table(["Target", "Path", "Value", "State", "Kind", "Notes"], write_rows))
        lines.append("")

    targets = record.get("setting", {}).get("targets", []) or []
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
            target_rows.append(["Notes", md_escape(target.get("notes"))])
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
        for default in defaults:
            lines.append(f"- {md_escape(default.get('label'))} ({md_escape(default.get('applies_to'))})")
            for state in default.get("states", []) or []:
                lines.append(
                    f"  - {md_escape(state.get('target_id'))}: {md_escape(state.get('state_kind'))} "
                    f"{format_value(state.get('value'))} — {md_escape(state.get('rationale'))}"
                )
        lines.append("")

    profiles = record.get("recommended_profiles", []) or []
    if profiles:
        lines.append("**Recommended profiles**")
        lines.append("")
        for profile in profiles:
            lines.append(
                f"- {md_code(profile.get('profile_id'))}: {md_escape(profile.get('label'))} "
                f"(apply_allowed={md_escape(profile.get('apply_allowed'))})"
            )
        lines.append("")

    evidence = record.get("evidence", []) or []
    if evidence:
        lines.append("**Evidence**")
        lines.append("")
        evidence_rows = []
        for item in evidence:
            evidence_rows.append([
                md_code(item.get("evidence_id")),
                md_code(item.get("kind")),
                md_escape(item.get("title")),
                md_escape(item.get("location")),
                md_code(item.get("strength")),
                md_escape(", ".join(item.get("supports", []) or [])),
            ])
        lines.extend(render_table(["Evidence ID", "Kind", "Title", "Location", "Strength", "Supports"], evidence_rows))
        lines.append("")

    proof = record.get("validation_proof")
    if proof:
        lines.append("**Validation proof**")
        lines.append("")
        proof_rows = [
            ["Source URL", md_escape(proof.get("source_url"))],
            ["Exact quote / path", md_escape(proof.get("exact_quote_or_path"))],
            ["Key found on page", md_code(proof.get("key_found_on_page"))],
        ]
        if proof.get("notes"):
            proof_rows.append(["Notes", md_escape(proof.get("notes"))])
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
    lines.append("This report consolidates every tweak record into a single human-readable atlas of key/value mappings, allowed values, evidence, and validation proof.")
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
    lines.extend(render_table(["Field", "Value"], summary_rows))
    lines.append("")
    lines.append("## Category Coverage")
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
