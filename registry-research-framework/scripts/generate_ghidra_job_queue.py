#!/usr/bin/env python3
from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
WORKLIST_PATH = FRAMEWORK_ROOT / "audit" / "blocked-worklist.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-job-queue.jsonl"


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def ghidra_jobs_from_worklist(payload: dict[str, Any], generated_utc: str | None = None) -> list[dict[str, Any]]:
    generated_utc = generated_utc or now_utc()
    jobs: list[dict[str, Any]] = []
    for item in payload.get("items") or []:
        if str(item.get("next_missing_layer") or "") != "ghidra":
            continue
        if str(item.get("actionability") or "") == "hold":
            continue

        jobs.append(
            {
                "schema_version": "1.0",
                "job_type": "ghidra-decompile-context",
                "status": "queued",
                "created_utc": generated_utc,
                "priority_rank": len(jobs) + 1,
                "candidate_id": item.get("candidate_id"),
                "feature_area": item.get("feature_area"),
                "key_path": item.get("key_path"),
                "value_name": item.get("value_name"),
                "promotion_blockers": item.get("promotion_blockers") or [],
                "trigger": "blocked-worklist-ghidra-lane",
                "suggested_command": item.get("suggested_command"),
                "next_action_hint": item.get("next_action_hint"),
            }
        )
    return jobs


def write_jsonl(path: Path, rows: list[dict[str, Any]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        "".join(json.dumps(row, sort_keys=True) + "\n" for row in rows),
        encoding="utf-8",
    )


def main() -> int:
    payload = json.loads(WORKLIST_PATH.read_text(encoding="utf-8"))
    jobs = ghidra_jobs_from_worklist(payload)
    write_jsonl(OUTPUT_PATH, jobs)
    print(json.dumps({"output": str(OUTPUT_PATH), "job_count": len(jobs)}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
