#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[1]
INDEX_PATH = REPO_ROOT / "Docs" / "tweaks" / "research" / "evidence-index.json"
JSON_OUTPUT_PATH = REPO_ROOT / "Docs" / "tweaks" / "research" / "evidence-manifest.json"
MD_OUTPUT_PATH = REPO_ROOT / "Docs" / "tweaks" / "research" / "evidence-manifest.md"


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha256_json(value: Any) -> str:
    payload = json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode("utf-8")
    return sha256_bytes(payload)


def escape_md_cell(value: Any) -> str:
    text = "" if value is None else str(value)
    return text.replace("\\", "\\\\").replace("|", "\\|").replace("\n", " ").replace("\r", " ")


def load_index() -> dict[str, Any]:
    with INDEX_PATH.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def record_source_hash(source_file: str) -> tuple[str | None, int | None]:
    source_path = REPO_ROOT / source_file
    if not source_path.exists():
        return None, None
    data = source_path.read_bytes()
    return sha256_bytes(data), len(data)


def normalize_record(index_record: dict[str, Any]) -> dict[str, Any]:
    source_hash, source_size = record_source_hash(index_record["source_file"])
    proof = index_record.get("validation_proof")
    proof_hash = sha256_json(proof) if proof is not None else None

    return {
        "record_id": index_record.get("record_id"),
        "tweak_id": index_record.get("tweak_id"),
        "record_status": index_record.get("record_status"),
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
    }


def render_allowed_value(item: dict[str, Any]) -> str:
    state = item.get("state_kind", "unknown")
    value = item.get("value")
    label = item.get("label")
    meaning = item.get("meaning")
    pieces = [f"{state}"]
    if value is not None:
        pieces.append(f"value={json.dumps(value, ensure_ascii=False)}")
    if label:
        pieces.append(f"label={label}")
    if meaning:
        pieces.append(f"meaning={meaning}")
    return " | ".join(escape_md_cell(piece) for piece in pieces)


def render_md(manifest: dict[str, Any]) -> str:
    lines: list[str] = []
    summary = manifest["summary"]
    lines.append("# Evidence Manifest")
    lines.append("")
    lines.append("This manifest is the forensic companion to the evidence atlas.")
    lines.append("Each record includes the raw source-file SHA256 and the exact validation proof block.")
    lines.append("")
    lines.append("## Summary")
    lines.append("")
    lines.append("| Field | Value |")
    lines.append("| --- | --- |")
    lines.append(f"| Total records | {summary['total_records']} |")
    lines.append(f"| Validated | {summary['validated']} |")
    lines.append(f"| Deprecated | {summary['deprecated']} |")
    lines.append(f"| Review required | {summary['review_required']} |")
    lines.append(f"| Records with evidence | {summary['records_with_evidence']} |")
    lines.append(f"| Records without evidence | {summary['records_without_evidence']} |")
    lines.append(f"| Records missing validation proof | {summary['records_missing_validation_proof']} |")
    lines.append(f"| Deprecated missing validation proof | {summary['deprecated_missing_validation_proof']} |")
    lines.append("")
    lines.append("## Record Index")
    lines.append("")
    lines.append("| Record | Status | Source file | Source SHA256 | Proof SHA256 | Targets |")
    lines.append("| --- | --- | --- | --- | --- | --- |")
    total_records = len(manifest["records"])
    for index, record in enumerate(manifest["records"]):
        lines.append(
            "| "
            + " | ".join(
                [
                    f"`{escape_md_cell(record.get('record_id'))}`",
                    escape_md_cell(record.get("record_status", "")),
                    f"`{escape_md_cell(record.get('source_file'))}`",
                    f"`{escape_md_cell(record.get('source_file_sha256'))}`" if record.get("source_file_sha256") else "",
                    f"`{escape_md_cell(record.get('validation_proof_sha256'))}`" if record.get("validation_proof_sha256") else "",
                    str(len(record.get("targets", []))),
                ]
            )
            + " |"
        )
    lines.append("")
    lines.append("## Per-Record Details")
    lines.append("")
    for record in manifest["records"]:
        lines.append(f"### `{record.get('record_id')}`")
        lines.append("")
        lines.append(f"- Status: `{record.get('record_status')}`")
        lines.append(f"- Category: `{escape_md_cell(record.get('category'))}`")
        lines.append(f"- Area: `{escape_md_cell(record.get('area'))}`")
        lines.append(f"- Scope: `{escape_md_cell(record.get('scope'))}`")
        lines.append(f"- Source file: `{escape_md_cell(record.get('source_file'))}`")
        lines.append(f"- Source SHA256: `{escape_md_cell(record.get('source_file_sha256'))}`")
        lines.append(f"- Proof SHA256: `{escape_md_cell(record.get('validation_proof_sha256'))}`")
        lines.append("")
        summary_text = record.get("summary")
        if summary_text:
            lines.append(f"**Summary:** {summary_text}")
            lines.append("")
        lines.append("**Targets**")
        lines.append("")
        for target in record.get("targets", []):
            lines.append(
                f"- `{escape_md_cell(target.get('path'))}` / "
                f"`{escape_md_cell(target.get('value_name'))}` / "
                f"`{escape_md_cell(target.get('value_type'))}`"
            )
            notes = target.get("notes")
            if notes:
                lines.append(f"  - Notes: {escape_md_cell(notes)}")
            for allowed in target.get("allowed_values", []) or []:
                lines.append(f"  - {render_allowed_value(allowed)}")
        lines.append("")
        lines.append("**Evidence**")
        lines.append("")
        for evidence in record.get("evidence", []):
            lines.append(
                f"- `{escape_md_cell(evidence.get('evidence_id'))}` | `{escape_md_cell(evidence.get('kind'))}` | "
                f"{escape_md_cell(evidence.get('title'))} | `{escape_md_cell(evidence.get('strength'))}`"
            )
        lines.append("")
        proof = record.get("validation_proof")
        lines.append("**Validation proof**")
        lines.append("")
        if isinstance(proof, dict):
            lines.append("| Field | Value |")
            lines.append("| --- | --- |")
            for key in ["source_url", "exact_quote_or_path", "key_found_on_page", "notes"]:
                value = proof.get(key)
                if value is None:
                    continue
                text = json.dumps(value, ensure_ascii=False) if isinstance(value, (dict, list)) else str(value)
                lines.append(f"| {escape_md_cell(key)} | {escape_md_cell(text)} |")
        else:
            lines.append("_No validation proof present._")
        if index < total_records - 1:
            lines.append("")
            lines.append("---")
            lines.append("")
    return "\n".join(lines)


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
    with JSON_OUTPUT_PATH.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(manifest, handle, ensure_ascii=False, indent=2)
        handle.write("\n")

    with MD_OUTPUT_PATH.open("w", encoding="utf-8", newline="\n") as handle:
        handle.write(render_md(manifest))
        handle.write("\n")

    print(f"Wrote {JSON_OUTPUT_PATH}")
    print(f"Wrote {MD_OUTPUT_PATH}")
    print(
        "Summary: "
        f"{manifest['summary']['total_records']} records, "
        f"{manifest['summary']['validated']} validated, "
        f"{manifest['summary']['deprecated']} deprecated, "
        f"{manifest['summary']['review_required']} review-required."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
