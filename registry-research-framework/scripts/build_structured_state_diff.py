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

from runtime_evidence_v36_lib import (
    build_structured_state_diff_from_registry_files,
    build_structured_state_diff_from_snapshots,
    build_structured_state_diff_from_state_payload,
)


def load_json(path: Path):
    payload = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(payload, dict):
        raise ValueError(f"{path} JSON payload is not an object")
    return payload


def main() -> int:
    parser = argparse.ArgumentParser(description="Build a structured registry state diff from snapshots, state.json, or registry exports.")
    parser.add_argument("--state-json", help="Path to state.json containing baseline_values and candidate_values.")
    parser.add_argument("--baseline-json", help="Baseline snapshot JSON path.")
    parser.add_argument("--candidate-json", help="Candidate snapshot JSON path.")
    parser.add_argument("--before-reg", help="Before registry export/dump path.")
    parser.add_argument("--after-reg", help="After registry export/dump path.")
    parser.add_argument("--output", required=True, help="Structured diff JSON output path.")
    args = parser.parse_args()

    if args.state_json:
        payload = build_structured_state_diff_from_state_payload(load_json(Path(args.state_json)))
    elif args.baseline_json and args.candidate_json:
        payload = build_structured_state_diff_from_snapshots(
            load_json(Path(args.baseline_json)),
            load_json(Path(args.candidate_json)),
        )
    elif args.before_reg and args.after_reg:
        payload = build_structured_state_diff_from_registry_files(Path(args.before_reg), Path(args.after_reg))
    else:
        raise SystemExit("Provide --state-json, or both --baseline-json/--candidate-json, or both --before-reg/--after-reg.")

    output_path = Path(args.output)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(payload["summary_counts"], ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
