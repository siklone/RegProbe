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

from metrics_publish_v36_lib import (  # noqa: E402
    GATE_METRICS_PATH,
    OPERATIONAL_METRICS_PATH,
    PUBLISH_METRICS_PATH,
    README_PATH,
    build_final_audit_payload,
    build_gate_metrics,
    build_operational_metrics,
    build_publish_metrics,
    load_json_dict,
    research_health_markdown,
    update_readme_summary_block,
    utc_now_iso,
    write_json,
)
from generate_blocked_worklist import (  # noqa: E402
    JSON_OUTPUT as BLOCKED_WORKLIST_JSON_PATH,
    MARKDOWN_OUTPUT as BLOCKED_WORKLIST_MARKDOWN_PATH,
    write_outputs as write_blocked_worklist_outputs,
)
from research_v36_lib import (  # noqa: E402
    PROMOTION_GATES_PATH,
    QUEUE_ROOT,
    RESEARCH_ROOT,
    URL_VALIDATION_REPORT_PATH,
)
from validate_research_batch import build_validation_summary  # noqa: E402

QUEUE_PATH = QUEUE_ROOT / "research-queue.json"
AUDIT_PATH = RESEARCH_ROOT / "evidence-audit.json"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate RegProbe publish, gate, and operational metrics.")
    parser.add_argument("--emit-json", action="store_true", help="Emit JSON payload.")
    args = parser.parse_args()

    generated_at = utc_now_iso()
    queue_payload = load_json_dict(QUEUE_PATH)
    gate_payload = load_json_dict(PROMOTION_GATES_PATH)
    audit_payload = load_json_dict(AUDIT_PATH)
    url_report = load_json_dict(URL_VALIDATION_REPORT_PATH)
    validation_summary = build_validation_summary()

    gate_metrics = build_gate_metrics(gate_payload, audit_payload, validation_summary, generated_at=generated_at)
    if int(url_report.get("dead_link_count") or 0) > int(gate_metrics.get("dead_link_count") or 0):
        gate_metrics["dead_link_count"] = int(url_report.get("dead_link_count") or 0)
    blocked_worklist = write_blocked_worklist_outputs()
    operational_metrics = build_operational_metrics(
        queue_payload,
        gate_payload,
        audit_payload,
        validation_summary,
        gate_metrics,
        generated_at=generated_at,
    )
    publish_metrics = build_publish_metrics(
        gate_payload,
        audit_payload,
        validation_summary,
        gate_metrics,
        blocked_worklist=blocked_worklist,
        generated_at=generated_at,
    )
    final_audit = build_final_audit_payload(audit_payload, gate_metrics, validation_summary)

    write_json(OPERATIONAL_METRICS_PATH, operational_metrics)
    write_json(GATE_METRICS_PATH, gate_metrics)
    publish_metrics["blocked_worklist_json"] = str(BLOCKED_WORKLIST_JSON_PATH.relative_to(REPO_ROOT)).replace("\\", "/")
    publish_metrics["blocked_worklist_markdown"] = str(BLOCKED_WORKLIST_MARKDOWN_PATH.relative_to(REPO_ROOT)).replace("\\", "/")
    write_json(PUBLISH_METRICS_PATH, publish_metrics)
    write_json(AUDIT_PATH, final_audit)

    readme_block = research_health_markdown(
        publish_metrics,
        gate_metrics,
        validation_summary,
        str(final_audit.get("summary", {}).get("gate_health") or "green"),
    )
    update_readme_summary_block(README_PATH, readme_block)

    payload = {
        "generated_at": generated_at,
        "operational_metrics": str(OPERATIONAL_METRICS_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
        "gate_metrics": str(GATE_METRICS_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
        "publish_metrics": str(PUBLISH_METRICS_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
        "evidence_audit": str(AUDIT_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
        "blocked_worklist_json": str(BLOCKED_WORKLIST_JSON_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
        "blocked_worklist_markdown": str(BLOCKED_WORKLIST_MARKDOWN_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
        "readme": str(README_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
        "gate_health": final_audit.get("summary", {}).get("gate_health"),
        "missing_docs_count": validation_summary.get("missing_docs_count"),
        "threshold_violations": gate_metrics.get("threshold_violations"),
        "blocked_count": blocked_worklist.get("blocked_count"),
        "blocked_actionability_counts": publish_metrics.get("blocked_actionability_counts"),
        "top_actionable_blocked_candidates": publish_metrics.get("top_actionable_blocked_candidates"),
        "top_hold_blocked_candidates": publish_metrics.get("top_hold_blocked_candidates"),
    }

    if args.emit_json:
        print(json.dumps(payload, ensure_ascii=False, indent=2))
    else:
        print(json.dumps(payload, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
