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

from runtime_evidence_v36_lib import evaluate_rollback_verification


def load_json(path: Path):
    payload = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(payload, dict):
        raise ValueError(f"{path} JSON payload is not an object")
    return payload


def main() -> int:
    parser = argparse.ArgumentParser(description="Evaluate rollback verification from baseline/candidate/restored state snapshots.")
    parser.add_argument("--baseline-json", required=True)
    parser.add_argument("--candidate-json", required=True)
    parser.add_argument("--restored-json")
    parser.add_argument("--rollback-declared", action="store_true")
    parser.add_argument("--rollback-executed", action="store_true")
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    payload = evaluate_rollback_verification(
        baseline_state=load_json(Path(args.baseline_json)),
        candidate_state=load_json(Path(args.candidate_json)),
        restored_state=load_json(Path(args.restored_json)) if args.restored_json else None,
        rollback_declared=args.rollback_declared,
        rollback_executed=args.rollback_executed,
    )
    output_path = Path(args.output)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(
        json.dumps(
            {
                "rollback_verified": payload["rollback_verified"],
                "rollback_failure_reason": payload["rollback_failure_reason"],
            },
            ensure_ascii=False,
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
