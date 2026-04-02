#!/usr/bin/env python3
from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from evidence_class_lib import build_class_entry, load_json, load_overrides, load_provenance_map
from research_path_lib import REPO_ROOT, RESEARCH_ROOT


RECORDS_DIR = RESEARCH_ROOT / "records"
PROVENANCE_PATH = REPO_ROOT / "Docs" / "tweaks" / "tweak-provenance.json"
OVERRIDES_PATH = RESEARCH_ROOT / "evidence-class-overrides.json"
AUDIT_PATH = RESEARCH_ROOT / "evidence-audit.json"
OUTPUT_ROOT = RESEARCH_ROOT / "evidence-not-found"


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def load_audit_map() -> dict[str, dict[str, Any]]:
    if not AUDIT_PATH.exists():
        return {}
    payload = load_json(AUDIT_PATH)
    result: dict[str, dict[str, Any]] = {}
    for entry in payload.get("entries", []):
        if not isinstance(entry, dict):
            continue
        tweak_id = str(entry.get("tweak_id") or "").strip()
        if tweak_id:
            result[tweak_id] = entry
    return result


def negative_text(record: dict[str, Any]) -> str:
    values: list[str] = [str(record.get("summary") or "")]
    decision = record.get("decision") or {}
    values.append(str(decision.get("why") or ""))
    for item in record.get("evidence") or []:
        if not isinstance(item, dict):
            continue
        for key in ("title", "summary", "location"):
            values.append(str(item.get(key) or ""))
    return " ".join(values).lower()


def is_negative_evidence_candidate(record: dict[str, Any], class_entry: dict[str, Any]) -> bool:
    if str(class_entry.get("evidence_class") or "") == "E":
        return True
    blob = negative_text(record)
    return any(
        phrase in blob
        for phrase in (
            "did not capture",
            "no exact runtime read",
            "not found",
            "no supporting evidence",
            "did not find",
        )
    )


def build_negative_payload(record: dict[str, Any], class_entry: dict[str, Any], audit_entry: dict[str, Any] | None) -> dict[str, Any]:
    tweak_id = str(record.get("tweak_id") or "")
    audit = audit_entry or {}
    attempted_tools = audit.get("tools_used") or []
    attempted_layers = audit.get("layers_used") or []
    tested_build = class_entry.get("tested_build")
    statement = (
        f"This record remains negative evidence on build {tested_build or 'unknown'}: "
        "the repo did not produce enough supporting proof to promote it into a normal actionable surface."
    )
    return {
        "schema_version": "1.0",
        "generated_utc": now_utc(),
        "record_id": record.get("record_id"),
        "tweak_id": tweak_id,
        "evidence_class": class_entry.get("evidence_class"),
        "record_status": record.get("record_status"),
        "tested_build": tested_build,
        "negative_reason": "class-e" if class_entry.get("evidence_class") == "E" else "no-hit-or-insufficient-proof",
        "statement": statement,
        "attempted_layers": attempted_layers,
        "attempted_tools": attempted_tools,
        "runtime_evidence_found": (class_entry.get("runtime_proof") or {}).get("has_runtime_evidence"),
        "validation_proof_found": (class_entry.get("validated_semantics") or {}).get("has_validation_proof"),
        "anticheat_risk": class_entry.get("anticheat_risk"),
        "gating_reason": class_entry.get("gating_reason"),
        "links": [
            {
                "title": item.get("title"),
                "url": item.get("location"),
                "kind": item.get("kind"),
                "summary": item.get("summary"),
            }
            for item in (record.get("evidence") or [])
            if isinstance(item, dict)
        ][:6],
    }


def render_negative_markdown(payload: dict[str, Any]) -> str:
    lines = [
        f"# {payload.get('tweak_id')}",
        "",
        f"- Class: `{payload.get('evidence_class')}`",
        f"- Record status: `{payload.get('record_status')}`",
        f"- Tested build: `{payload.get('tested_build') or 'unknown'}`",
        f"- Reason: `{payload.get('negative_reason')}`",
        "",
        payload.get("statement") or "",
        "",
        "## Attempted coverage",
        "",
        f"- Layers: `{', '.join(payload.get('attempted_layers') or []) or 'none'}`",
        f"- Tools: `{', '.join(payload.get('attempted_tools') or []) or 'none'}`",
        "",
        "## Why it stays negative",
        "",
        str(payload.get("gating_reason") or "No gating reason attached."),
    ]
    links = payload.get("links") or []
    if links:
        lines.extend(["", "## Attached references", ""])
        for item in links:
            lines.append(f"- `{item.get('kind') or 'reference'}` {item.get('title') or 'Reference'} -> {item.get('url')}")
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    provenance_map = load_provenance_map(PROVENANCE_PATH)
    overrides = load_overrides(OVERRIDES_PATH)
    audit_map = load_audit_map()
    packages: list[dict[str, Any]] = []

    OUTPUT_ROOT.mkdir(parents=True, exist_ok=True)
    for path in sorted(RECORDS_DIR.glob("*.json")):
        record = load_json(path)
        tweak_id = str(record.get("tweak_id") or "")
        class_entry = build_class_entry(
            record,
            provenance_entry=provenance_map.get(tweak_id),
            override=overrides.get(tweak_id),
        )
        if not is_negative_evidence_candidate(record, class_entry):
            continue

        payload = build_negative_payload(record, class_entry, audit_map.get(tweak_id))
        json_path = OUTPUT_ROOT / f"{tweak_id}.json"
        md_path = OUTPUT_ROOT / f"{tweak_id}.md"
        with json_path.open("w", encoding="utf-8", newline="\n") as handle:
            json.dump(payload, handle, ensure_ascii=False, indent=2)
            handle.write("\n")
        md_path.write_text(render_negative_markdown(payload), encoding="utf-8", newline="\n")
        packages.append({"tweak_id": tweak_id, "json": str(json_path.relative_to(REPO_ROOT)).replace("\\", "/"), "markdown": str(md_path.relative_to(REPO_ROOT)).replace("\\", "/")})

    index_payload = {
        "schema_version": "1.0",
        "generated_utc": now_utc(),
        "package_count": len(packages),
        "packages": packages,
    }
    with (OUTPUT_ROOT / "index.json").open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(index_payload, handle, ensure_ascii=False, indent=2)
        handle.write("\n")

    print(f"Wrote {len(packages)} negative evidence package(s) to {OUTPUT_ROOT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
