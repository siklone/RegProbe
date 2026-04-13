#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"
FRAMEWORK_SCRIPTS = REPO_ROOT / "registry-research-framework" / "scripts"
for path in (SCRIPTS_ROOT, FRAMEWORK_SCRIPTS):
    if str(path) not in sys.path:
        sys.path.insert(0, str(path))

from metrics_publish_v36_lib import GATE_METRICS_PATH, PUBLISH_METRICS_PATH, build_gate_metrics, load_json_dict, utc_now_iso  # noqa: E402
from research_v36_lib import PROMOTION_GATES_PATH, RESEARCH_ROOT  # noqa: E402
from validate_research_batch import build_validation_summary  # noqa: E402

AUDIT_PATH = RESEARCH_ROOT / "evidence-audit.json"


def load_or_build_gate_metrics() -> dict:
    existing = load_json_dict(GATE_METRICS_PATH)
    if existing:
        return existing
    return build_gate_metrics(
        load_json_dict(PROMOTION_GATES_PATH),
        load_json_dict(AUDIT_PATH),
        build_validation_summary(),
        generated_at=utc_now_iso(),
    )


def main() -> int:
    parser = argparse.ArgumentParser(description="Check RegProbe gate metric thresholds.")
    parser.add_argument("--emit-json", action="store_true", help="Emit JSON payload.")
    args = parser.parse_args()

    payload = load_or_build_gate_metrics()
    publish_metrics = load_json_dict(PUBLISH_METRICS_PATH)
    threshold_violations = list(payload.get("threshold_violations") or [])
    blocked_worklist_status = str(publish_metrics.get("blocked_worklist_status") or "UNKNOWN")
    blocked_worklist_errors = list(publish_metrics.get("blocked_worklist_errors") or [])
    if publish_metrics and blocked_worklist_status != "PASS":
        threshold_violations.append("blocked_worklist")
    output = {
        "status": "PASS" if not threshold_violations else "FAIL",
        "threshold_violations": threshold_violations,
        "thresholds": payload.get("thresholds") or {},
        "schema_complete_ratio": payload.get("schema_complete_ratio"),
        "invalid_gate_entries": payload.get("invalid_gate_entries"),
        "stale_promoted_count": payload.get("stale_promoted_count"),
        "blocked_worklist_status": blocked_worklist_status,
        "blocked_worklist_errors": blocked_worklist_errors,
    }
    print(json.dumps(output, ensure_ascii=False, indent=2))
    return 0 if output["status"] == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
