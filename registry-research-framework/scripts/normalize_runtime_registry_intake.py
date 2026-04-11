#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"
if str(SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_ROOT))

from runtime_evidence_v36_lib import normalize_runtime_registry_events


def load_events(path: Path) -> list[dict]:
    payload = json.loads(path.read_text(encoding="utf-8-sig"))
    if isinstance(payload, list):
        return [item for item in payload if isinstance(item, dict)]
    if isinstance(payload, dict):
        events = payload.get("events")
        if isinstance(events, list):
            return [item for item in events if isinstance(item, dict)]
    raise ValueError("Input must be a JSON array or an object with an 'events' array.")


def main() -> int:
    parser = argparse.ArgumentParser(description="Normalize Procmon/ETW runtime registry traces for discovery or proof packaging.")
    parser.add_argument("--input", required=True, help="Input JSON file with raw runtime events.")
    parser.add_argument("--mode", required=True, choices=["discovery", "proof"])
    parser.add_argument("--source", required=True, choices=["procmon", "etw"])
    parser.add_argument("--seed-reference", required=True, help="Repo-relative source reference for generated candidates/evidence.")
    parser.add_argument("--output", required=True, help="JSON output path.")
    parser.add_argument("--trigger-start-ms", type=int)
    parser.add_argument("--trigger-end-ms", type=int)
    parser.add_argument("--burst-window-ms", type=int, default=250)
    parser.add_argument("--append-discovery", action="store_true", help="Append discovery candidates to discovery-events.jsonl in discovery mode.")
    args = parser.parse_args()

    events = load_events(Path(args.input))
    payload = normalize_runtime_registry_events(
        events,
        mode=args.mode,
        trace_source=args.source,
        seed_reference=args.seed_reference,
        trigger_start_ms=args.trigger_start_ms,
        trigger_end_ms=args.trigger_end_ms,
        burst_window_ms=args.burst_window_ms,
        append_discovery=args.append_discovery,
    )
    output_path = Path(args.output)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(payload["summary"], ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
