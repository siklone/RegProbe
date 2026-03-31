#!/usr/bin/env python3
from __future__ import annotations

import json
import hashlib
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from research_path_lib import REPO_ROOT, RESEARCH_ROOT, file_sha256, linkify_reference_text

INDEX_PATH = RESEARCH_ROOT / "evidence-index.json"
JSON_OUTPUT_PATH = RESEARCH_ROOT / "evidence-manifest.json"
MD_OUTPUT_PATH = RESEARCH_ROOT / "evidence-manifest.md"


def escape_md_cell(value: Any) -> str:
    text = "" if value is None else str(value)
    return text.replace("\\", "\\\\").replace("|", "\\|").replace("\n", " ").replace("\r", " ")


def normalize_row_item(value: Any) -> dict[str, Any]:
    if isinstance(value, dict):
        return value
    if value is None:
        return {}
    text = str(value)
    return {
        "id": text,
        "tool": "",
        "filename": text,
        "sha256": "",
        "release_url": "",
    }


def render_table(headers: list[str], rows: list[list[str]]) -> list[str]:
    out = [
        "| " + " | ".join(headers) + " |",
        "| " + " | ".join(["---"] * len(headers)) + " |",
    ]
    for row in rows:
        out.append("| " + " | ".join(row) + " |")
    return out


def load_index() -> dict[str, Any]:
    with INDEX_PATH.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def record_source_hash(source_file: str) -> tuple[str | None, int | None]:
    source_path = REPO_ROOT / source_file
    if not source_path.exists():
        return None, None
    return file_sha256(source_path), source_path.stat().st_size


def normalize_record(index_record: dict[str, Any]) -> dict[str, Any]:
    source_hash, source_size = record_source_hash(index_record["source_file"])
    proof = index_record.get("validation_proof")
    proof_hash = None
    if proof is not None:
        payload = json.dumps(proof, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode("utf-8")
        proof_hash = hashlib.sha256(payload).hexdigest()

    return {
        "record_id": index_record.get("record_id"),
        "tweak_id": index_record.get("tweak_id"),
        "record_status": index_record.get("record_status"),
        "evidence_class": index_record.get("evidence_class"),
        "source_file": index_record.get("source_file"),
        "source_file_sha256": source_hash,
        "source_file_size_bytes": source_size,
        "name": index_record.get("name"),
        "category": index_record.get("category"),
        "area": index_record.get("area"),
        "scope": index_record.get("scope"),
        "summary": index_record.get("summary"),
        "decision": index_record.get("decision"),
        "app_current_implementation": index_record.get("app_current_implementation"),
        "targets": index_record.get("targets", []),
        "windows_defaults": index_record.get("windows_defaults", []),
        "recommended_profiles": index_record.get("recommended_profiles", []),
        "evidence": index_record.get("evidence", []),
        "validation_proof": proof,
        "validation_proof_sha256": proof_hash,
        "provenance": index_record.get("provenance"),
        "v31_evidence_root": index_record.get("v31_evidence_root"),
        "artifact_refs": index_record.get("artifact_refs", []),
        "re_audit": index_record.get("re_audit"),
    }


def render_md(manifest: dict[str, Any]) -> str:
    lines: list[str] = []
    summary = manifest["summary"]
    lines.append("# Evidence Manifest")
    lines.append("")
    lines.append("This file is the index-friendly companion to the atlas. It tracks source hashes, proof hashes, and the normalized evidence links that were pulled into the repo.")
    lines.append("")
    lines.append("## Summary")
    lines.append("")
    rows = [
        ["Total records", str(summary.get("total_records", 0))],
        ["Validated", str(summary.get("validated", 0))],
        ["Deprecated", str(summary.get("deprecated", 0))],
        ["Review required", str(summary.get("review_required", 0))],
        ["Records with evidence roots", str(sum(1 for record in manifest["records"] if record.get("v31_evidence_root")))],
        ["Records with evidence", str(summary.get("records_with_evidence", 0))],
        ["Records without evidence", str(summary.get("records_without_evidence", 0))],
        ["Records missing validation proof", str(summary.get("records_missing_validation_proof", 0))],
        ["Deprecated missing validation proof", str(summary.get("deprecated_missing_validation_proof", 0))],
    ]
    class_counts = summary.get("class_counts") or {}
    for class_id in ["A", "B", "C", "D", "E"]:
        if class_id in class_counts:
            rows.append([f"Class {class_id}", str(class_counts[class_id])])
    lines.extend(render_table(["Field", "Value"], rows))
    lines.append("")
    lines.append("## Record index")
    lines.append("")
    index_rows = []
    for record in manifest["records"]:
        index_rows.append([
            f"`{escape_md_cell(record.get('record_id'))}`",
            escape_md_cell(record.get("record_status")),
            escape_md_cell((record.get("evidence_class") or {}).get("class_label")),
            f"`{escape_md_cell(record.get('source_file'))}`",
            linkify_reference_text(record.get("v31_evidence_root"), MD_OUTPUT_PATH) if record.get("v31_evidence_root") else "-",
            f"`{escape_md_cell(record.get('source_file_sha256'))}`" if record.get("source_file_sha256") else "",
            f"`{escape_md_cell(record.get('validation_proof_sha256'))}`" if record.get("validation_proof_sha256") else "",
            str(len(record.get("evidence", []))),
        ])
    lines.extend(render_table(["Record", "Status", "Class", "Source file", "Evidence root", "Source SHA256", "Proof SHA256", "Evidence"], index_rows))
    lines.append("")
    lines.append("## Per-record details")
    lines.append("")
    for record in manifest["records"]:
        lines.append(f"### `{record.get('record_id')}`")
        lines.append("")
        lines.append(f"- Status: `{escape_md_cell(record.get('record_status'))}`")
        lines.append(f"- Evidence class: `{escape_md_cell((record.get('evidence_class') or {}).get('class_label'))}`")
        lines.append(f"- Source file: `{escape_md_cell(record.get('source_file'))}`")
        if record.get("v31_evidence_root"):
            lines.append(f"- Evidence root: {linkify_reference_text(record.get('v31_evidence_root'), MD_OUTPUT_PATH)}")
        lines.append(f"- Source SHA256: `{escape_md_cell(record.get('source_file_sha256'))}`")
        lines.append(f"- Proof SHA256: `{escape_md_cell(record.get('validation_proof_sha256'))}`")
        lines.append("")
        if record.get("summary"):
            lines.append(f"**Summary:** {escape_md_cell(record.get('summary'))}")
            lines.append("")
        evidence_rows = []
        for evidence in record.get("evidence", []):
            evidence_rows.append([
                f"`{escape_md_cell(evidence.get('evidence_id'))}`",
                f"`{escape_md_cell(evidence.get('kind'))}`",
                escape_md_cell(evidence.get("title")),
                linkify_reference_text(evidence.get("location"), MD_OUTPUT_PATH),
            ])
        if evidence_rows:
            lines.append("**Evidence**")
            lines.append("")
            lines.extend(render_table(["Evidence ID", "Kind", "Title", "Location"], evidence_rows))
            lines.append("")
        proof = record.get("validation_proof")
        if isinstance(proof, dict):
            lines.append("**Validation proof**")
            lines.append("")
            lines.extend(
                render_table(
                    ["Field", "Value"],
                    [
                        ["Source", linkify_reference_text(proof.get("source_url"), MD_OUTPUT_PATH)],
                        ["Exact quote / path", linkify_reference_text(proof.get("exact_quote_or_path"), MD_OUTPUT_PATH)],
                        ["Notes", linkify_reference_text(proof.get("notes"), MD_OUTPUT_PATH)],
                    ],
                )
            )
            lines.append("")
        artifact_refs = record.get("artifact_refs") or []
        if artifact_refs:
            lines.append("**Artifact refs**")
            lines.append("")
            artifact_rows = []
            for artifact in artifact_refs:
                artifact = normalize_row_item(artifact)
                artifact_rows.append(
                    [
                        f"`{escape_md_cell(artifact.get('id'))}`",
                        f"`{escape_md_cell(artifact.get('tool'))}`",
                        escape_md_cell(artifact.get("filename")),
                        escape_md_cell(artifact.get("sha256")),
                        linkify_reference_text(artifact.get("release_url"), MD_OUTPUT_PATH),
                    ]
                )
            lines.extend(render_table(["ID", "Tool", "Filename", "SHA256", "Release URL"], artifact_rows))
            lines.append("")
        if isinstance(record.get("re_audit"), dict):
            lines.append("**Re-audit**")
            lines.append("")
            reaudit = record["re_audit"]
            lines.extend(
                render_table(
                    ["Field", "Value"],
                    [
                        ["Original class", escape_md_cell(reaudit.get("original_class"))],
                        ["Reason", escape_md_cell(reaudit.get("re_audit_reason"))],
                        ["Priority", escape_md_cell(reaudit.get("re_audit_priority"))],
                        ["New pipeline version", escape_md_cell(reaudit.get("new_pipeline_version"))],
                    ],
                )
            )
            lines.append("")
        lines.append("---")
        lines.append("")
    return "\n".join(lines).rstrip()


def main() -> int:
    index = load_index()
    manifest_records = [normalize_record(record) for record in index.get("records", [])]
    manifest = {
        "schema_version": "1.0",
        "generated_utc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "source_index": str(INDEX_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
        "summary": index.get("summary", {}),
        "records": sorted(manifest_records, key=lambda item: (item["record_status"] or "", item["record_id"] or "")),
    }
    manifest["summary"]["records_with_source_hashes"] = sum(1 for record in manifest["records"] if record.get("source_file_sha256"))
    manifest["summary"]["records_without_source_hashes"] = sum(1 for record in manifest["records"] if not record.get("source_file_sha256"))

    JSON_OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    JSON_OUTPUT_PATH.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    MD_OUTPUT_PATH.write_text(render_md(manifest) + "\n", encoding="utf-8")

    print(f"Wrote {JSON_OUTPUT_PATH}")
    print(f"Wrote {MD_OUTPUT_PATH}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
