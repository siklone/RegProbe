#!/usr/bin/env python3
from __future__ import annotations

import json
import re
from datetime import datetime, timezone
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
RESEARCH_ROOT = REPO_ROOT / "research"
DOCS_FIRST_BACKLOG = RESEARCH_ROOT / "docs-first-backlog.json"
WITH_URL_OUTPUT = RESEARCH_ROOT / "docs-first-admx-policy-with-url.json"
WITHOUT_URL_OUTPUT = RESEARCH_ROOT / "docs-first-admx-policy-without-url.json"
BATCH1_OUTPUT = RESEARCH_ROOT / "docs-first-recovery-batch-1.json"

TRACK_PATTERN = re.compile(r"(admx|policy csp)", re.IGNORECASE)
URL_PATTERN = re.compile(r"https?://[^\s)]+")


def generated_utc() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def extract_url(text: str | None) -> str | None:
    if not text:
        return None
    match = URL_PATTERN.search(text)
    if match:
        return match.group(0)
    return None


def load_docs_first_entries() -> list[dict]:
    data = json.loads(DOCS_FIRST_BACKLOG.read_text(encoding="utf-8-sig"))
    return data.get("entries", [])


def matching_tracks(entry: dict) -> list[dict]:
    matches = []
    for track in entry.get("suggested_evidence_tracks", []):
        name = track.get("track", "")
        if not TRACK_PATTERN.search(name):
            continue
        matches.append(
            {
                "track": name,
                "why_not_validated": track.get("why_not_validated"),
                "next_source": track.get("next_source"),
                "source_url": extract_url(track.get("next_source")),
            }
        )
    return matches


def recovery_entry(entry: dict) -> dict:
    tracks = matching_tracks(entry)
    urls = [track["source_url"] for track in tracks if track.get("source_url")]
    return {
        "record_id": entry["record_id"],
        "source_file": entry["source_file"],
        "current_state": entry.get("current_state"),
        "notes": entry.get("notes"),
        "matching_tracks": tracks,
        "has_url_source": bool(urls),
        "source_urls": urls,
    }


def write_json(path: Path, payload: dict) -> None:
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    entries = [recovery_entry(entry) for entry in load_docs_first_entries() if matching_tracks(entry)]
    with_url = [entry for entry in entries if entry["has_url_source"]]
    without_url = [entry for entry in entries if not entry["has_url_source"]]
    batch1 = with_url[:20]

    workflow = [
        "Open the cited source_url.",
        "Capture validation_proof.exact_quote_or_path from the page.",
        "Set validation_proof.key_found_on_page = true only if the exact key, path, or control is explicitly present on the page.",
        "If key_found_on_page is true, the record can move toward validated after the proof is stored.",
        "If key_found_on_page is false, keep the record in docs-first and update why_not_validated.",
        "Do not bulk-validate; each record must be proven individually.",
    ]

    common_meta = {
        "schema_version": "1.0",
        "generated_utc": generated_utc(),
        "source_backlog": str(DOCS_FIRST_BACKLOG.relative_to(REPO_ROOT)).replace("\\", "/"),
        "filter": "docs-first entries whose suggested_evidence_tracks include ADMX or Policy CSP",
    }

    write_json(
        WITH_URL_OUTPUT,
        {
            **common_meta,
            "group": "with-url",
            "entry_count": len(with_url),
            "entries": with_url,
        },
    )
    write_json(
        WITHOUT_URL_OUTPUT,
        {
            **common_meta,
            "group": "without-url",
            "entry_count": len(without_url),
            "entries": without_url,
        },
    )
    write_json(
        BATCH1_OUTPUT,
        {
            **common_meta,
            "group": "recovery-batch-1",
            "selection_rule": "First 20 entries from the with-url group, preserving docs-first backlog order",
            "entry_count": len(batch1),
            "agent_workflow": workflow,
            "entries": batch1,
        },
    )

    print(
        json.dumps(
            {
                "with_url": len(with_url),
                "without_url": len(without_url),
                "batch_1_count": len(batch1),
                "batch_1_output": str(BATCH1_OUTPUT),
            },
            indent=2,
        )
    )


if __name__ == "__main__":
    main()
