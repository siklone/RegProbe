#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
QUEUE_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-job-queue.jsonl"
OUTPUT_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-autotrigger-seeds.jsonl"

UNKNOWN_FRAME_MARKERS = ("unknown", "<unknown>", "??", "???")


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def portable_path(path: Path) -> str:
    resolved = path.resolve()
    try:
        return resolved.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return resolved.as_posix()


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def load_jsonl(path: Path) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    if not path.exists():
        return rows
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line:
            continue
        rows.append(json.loads(line))
    return rows


def write_jsonl(path: Path, rows: list[dict[str, Any]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        "".join(json.dumps(row, sort_keys=True) + "\n" for row in rows),
        encoding="utf-8",
    )


def normalize_registry_path(value: Any) -> str:
    text = str(value or "").strip().replace("/", "\\")
    text = re.sub(r"^\\\\REGISTRY\\\\MACHINE\\\\", lambda _: "HKLM\\", text, flags=re.IGNORECASE)
    text = re.sub(r"^HKEY_LOCAL_MACHINE\\\\", lambda _: "HKLM\\", text, flags=re.IGNORECASE)
    text = re.sub(r"^\\\\REGISTRY\\\\USER\\\\", lambda _: "HKU\\", text, flags=re.IGNORECASE)
    text = re.sub(r"^HKEY_USERS\\\\", lambda _: "HKU\\", text, flags=re.IGNORECASE)
    return re.sub(r"\\{2,}", r"\\", text).lower()


def split_value_patterns(value: Any) -> list[str]:
    patterns: list[str] = []
    for part in re.split(r"\s*/\s*", str(value or "")):
        cleaned = part.strip()
        if cleaned and cleaned.lower() not in [item.lower() for item in patterns]:
            patterns.append(cleaned)
    return patterns


def infer_target_binary(key_path: Any) -> str | None:
    normalized = normalize_registry_path(key_path)
    kernel_prefixes = (
        "hklm\\system\\currentcontrolset\\control\\power",
        "hklm\\system\\currentcontrolset\\control\\session manager\\power",
        "hklm\\system\\currentcontrolset\\control\\session manager\\kernel",
        "hklm\\system\\controlset001\\control\\power",
        "hklm\\system\\controlset001\\control\\session manager\\power",
        "hklm\\system\\controlset001\\control\\session manager\\kernel",
    )
    if normalized.startswith(kernel_prefixes):
        return "ntoskrnl.exe"
    if normalized.startswith("hklm\\software\\microsoft\\windows\\currentversion\\explorer"):
        return "explorer.exe"
    return None


def frame_resolution_kind(frame: Any) -> str:
    text = str(frame or "").strip()
    lowered = text.lower()
    if not text:
        return "empty"
    if lowered in UNKNOWN_FRAME_MARKERS or "unknown" in lowered or lowered.startswith("??"):
        return "unknown_marker"
    if re.fullmatch(r"(?:0x)?[0-9a-f]{8,16}", lowered):
        return "raw_address"
    if "+0x" in lowered:
        return "module_offset"
    if "!" in text:
        return "resolved_symbol"
    return "plain_text"


def event_matches_job(event: dict[str, Any], job: dict[str, Any]) -> bool:
    if normalize_registry_path(event.get("key_path")) != normalize_registry_path(job.get("key_path")):
        return False

    event_value = str(event.get("value_name") or "").strip().lower()
    job_patterns = [item.lower() for item in split_value_patterns(job.get("value_name"))]
    if not job_patterns:
        return True
    return event_value in job_patterns


def suggested_patterns_for_event(event: dict[str, Any], job: dict[str, Any]) -> list[str]:
    value_name = str(event.get("value_name") or "").strip()
    if value_name:
        return [value_name]
    return split_value_patterns(job.get("value_name"))


def autotrigger_seeds_from_bundle(
    bundle: dict[str, Any],
    *,
    bundle_path: str,
    queue_rows: list[dict[str, Any]],
    generated_utc: str | None = None,
) -> list[dict[str, Any]]:
    generated_utc = generated_utc or now_utc()
    seeds: list[dict[str, Any]] = []
    seen: set[tuple[str, str, str, str]] = set()
    events = bundle.get("events") or []
    source_fields = ((bundle.get("stack_capture") or {}).get("source_fields")) or []

    for index, event in enumerate(events, start=1):
        frames = [str(frame).strip() for frame in (event.get("caller_stack") or []) if str(frame).strip()]
        if not frames:
            continue

        frame_details = [{"frame": frame, "resolution_kind": frame_resolution_kind(frame)} for frame in frames]
        unresolved_frames = [item["frame"] for item in frame_details if item["resolution_kind"] != "resolved_symbol"]
        if not unresolved_frames:
            continue

        matching_jobs = [row for row in queue_rows if event_matches_job(event, row)]
        if not matching_jobs:
            continue

        resolved_frames = [item["frame"] for item in frame_details if item["resolution_kind"] == "resolved_symbol"]

        for job in matching_jobs:
            candidate_id = str(job.get("candidate_id") or "")
            dedupe_key = (
                candidate_id,
                normalize_registry_path(event.get("key_path")),
                str(event.get("value_name") or "").strip().lower(),
                unresolved_frames[0].lower(),
            )
            if dedupe_key in seen:
                continue
            seen.add(dedupe_key)

            target_binary = infer_target_binary(event.get("key_path"))
            seed = {
                "schema_version": "1.0",
                "job_type": "ghidra-autotrigger-seed",
                "status": "queued",
                "created_utc": generated_utc,
                "trigger": "caller-stack-unresolved-frame",
                "source_bundle_path": bundle_path,
                "source_run_id": bundle.get("run_id"),
                "capture_phase": bundle.get("capture_phase"),
                "source_tool": bundle.get("source_tool"),
                "candidate_id": candidate_id,
                "feature_area": job.get("feature_area"),
                "event_index": index,
                "key_path": event.get("key_path"),
                "value_name": event.get("value_name"),
                "operation": event.get("operation"),
                "target_binary": target_binary,
                "suggested_patterns": suggested_patterns_for_event(event, job),
                "resolved_frames": resolved_frames,
                "unresolved_frames": unresolved_frames,
                "caller_stack": frames,
                "frame_resolution": frame_details,
                "stack_source_fields": source_fields,
                "promotion_blockers": job.get("promotion_blockers") or [],
                "next_action_hint": f"Pivot static RE from unresolved caller stack for {candidate_id}.",
            }
            seeds.append(seed)

    return seeds


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate Ghidra auto-trigger seeds from caller_stack events in a normalized bundle.")
    parser.add_argument("--bundle", type=Path, required=True)
    parser.add_argument("--queue", type=Path, default=QUEUE_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    args = parser.parse_args()

    bundle = load_json(args.bundle)
    queue_rows = load_jsonl(args.queue)
    seeds = autotrigger_seeds_from_bundle(
        bundle,
        bundle_path=portable_path(args.bundle),
        queue_rows=queue_rows,
    )
    write_jsonl(args.output, seeds)
    print(json.dumps({"output": portable_path(args.output), "seed_count": len(seeds)}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
