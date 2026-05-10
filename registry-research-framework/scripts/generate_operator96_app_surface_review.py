#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from collections import Counter
from datetime import UTC, datetime
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
AUDIT_DIR = REPO_ROOT / "registry-research-framework" / "audit"
DEFAULT_MATRIX = AUDIT_DIR / "operator96-enriched-value-matrix-20260510.json"
JSON_OUTPUT = AUDIT_DIR / "operator96-app-surface-review-20260510.json"
MARKDOWN_OUTPUT = AUDIT_DIR / "operator96-app-surface-review-20260510.md"

SAFETY_VERDICTS = {"boot_failure", "rollback_failure", "app_breakage"}
NOISY_OR_LOW_CONFIDENCE_VERDICTS = {"harmful", "low_confidence", "noisy"}


def normalize_text(value: Any) -> str:
    return str(value or "").strip()


def candidate_proofs(record: dict[str, Any]) -> list[dict[str, Any]]:
    return [
        candidate.get("baseline_proof") or {}
        for candidate in record.get("candidates") or []
        if isinstance(candidate, dict) and isinstance(candidate.get("baseline_proof"), dict)
    ]


def classify_record(record: dict[str, Any]) -> dict[str, Any]:
    gate = record.get("app_surface_gate") or {}
    proofs = candidate_proofs(record)
    blockers = list(gate.get("blockers") or [])
    reasons: list[str] = []

    if not bool(gate.get("eligible_for_app_card")):
        reasons.extend(blockers or ["app-surface-gate-not-eligible"])
        bucket = "blocked_by_gate"
    elif any(normalize_text(proof.get("status")) != "ok" for proof in proofs):
        reasons.append("one-or-more-vm-proofs-did-not-finish-ok")
        bucket = "blocked_by_safety"
    elif any(normalize_text(proof.get("verdict")) in SAFETY_VERDICTS for proof in proofs):
        reasons.append("safety-finding-present")
        bucket = "blocked_by_safety"
    elif any(
        normalize_text(proof.get("host_noise")).lower() in {"unknown", "noisy"}
        or normalize_text(proof.get("confidence")).lower() == "low"
        or normalize_text(proof.get("verdict")).lower() in NOISY_OR_LOW_CONFIDENCE_VERDICTS
        for proof in proofs
    ):
        reasons.append("low-noise-repeat-required-before-app-card")
        bucket = "needs_low_noise_rerun"
    else:
        reasons.append("bounded-card-ready")
        bucket = "ready_for_bounded_app_card"

    proof_verdicts = Counter(normalize_text(proof.get("verdict")) or "unknown" for proof in proofs)
    proof_noise = Counter(normalize_text(proof.get("host_noise")) or "unknown" for proof in proofs)
    proof_confidence = Counter(normalize_text(proof.get("confidence")) or "unknown" for proof in proofs)

    return {
        "index": record.get("index"),
        "value_name": normalize_text(record.get("value_name")),
        "registry_path": normalize_text(record.get("registry_path")),
        "default_status": normalize_text(record.get("default_status")),
        "source_quality": normalize_text(record.get("source_quality")),
        "app_surface_bucket": bucket,
        "app_surface_ready": bucket == "ready_for_bounded_app_card",
        "reasons": reasons,
        "gate": gate,
        "candidate_value_count": len(record.get("candidates") or []),
        "vm_validated_value_count": sum(1 for candidate in record.get("candidates") or [] if bool(candidate.get("vm_validated"))),
        "proof_verdict_counts": dict(sorted(proof_verdicts.items())),
        "proof_host_noise_counts": dict(sorted(proof_noise.items())),
        "proof_confidence_counts": dict(sorted(proof_confidence.items())),
        "claim_boundary": normalize_text(gate.get("claim_boundary")),
        "recommended_action": {
            "blocked_by_gate": "do-not-surface-unless-blocker-is-researched-away",
            "blocked_by_safety": "keep-out-of-app-surface-and-open-targeted-safety-review",
            "needs_low_noise_rerun": "rerun-low-noise-before-any-app-card-or-performance-claim",
            "ready_for_bounded_app_card": "eligible-for-bounded-card-copy-review",
        }[bucket],
    }


def build_review(matrix_path: Path = DEFAULT_MATRIX) -> dict[str, Any]:
    matrix = json.loads(matrix_path.read_text(encoding="utf-8"))
    records = [classify_record(record) for record in matrix.get("records") or []]
    bucket_counts = Counter(record["app_surface_bucket"] for record in records)
    return {
        "schema_version": "1.0",
        "generated_utc": datetime.now(UTC).isoformat(timespec="seconds").replace("+00:00", "Z"),
        "matrix": str(matrix_path.relative_to(REPO_ROOT)),
        "campaign_id": "operator96-app-surface-review-20260510",
        "status": "PASS",
        "summary": {
            "record_count": len(records),
            "ready_for_bounded_app_card": bucket_counts.get("ready_for_bounded_app_card", 0),
            "needs_low_noise_rerun": bucket_counts.get("needs_low_noise_rerun", 0),
            "blocked_by_gate": bucket_counts.get("blocked_by_gate", 0),
            "blocked_by_safety": bucket_counts.get("blocked_by_safety", 0),
            "bucket_counts": dict(sorted(bucket_counts.items())),
        },
        "policy": {
            "ship_rule": "Only ready_for_bounded_app_card may enter the app surface without another VM campaign.",
            "no_performance_claim_rule": "Low-confidence, noisy, or host-noise-unknown experiments are observations only.",
            "rerun_rule": "needs_low_noise_rerun records require repeated low-noise VM runs before card copy or performance claims.",
        },
        "records": records,
    }


def render_markdown(review: dict[str, Any]) -> str:
    summary = review.get("summary") or {}
    lines = [
        "# Operator96 App Surface Review",
        "",
        f"- Generated UTC: `{review.get('generated_utc')}`",
        f"- Matrix: `{review.get('matrix')}`",
        f"- Records: `{summary.get('record_count')}`",
        f"- Ready for bounded app card: `{summary.get('ready_for_bounded_app_card')}`",
        f"- Needs low-noise rerun: `{summary.get('needs_low_noise_rerun')}`",
        f"- Blocked by gate: `{summary.get('blocked_by_gate')}`",
        f"- Blocked by safety: `{summary.get('blocked_by_safety')}`",
        "",
        "## Policy",
        "",
    ]
    for value in (review.get("policy") or {}).values():
        lines.append(f"- {value}")

    lines.extend(["", "## Buckets", ""])
    for bucket, count in (summary.get("bucket_counts") or {}).items():
        lines.append(f"- `{bucket}`: `{count}`")

    lines.extend(["", "## Records", ""])
    lines.append("| # | Value | Bucket | Reason | Action |")
    lines.append("|---:|---|---|---|---|")
    for record in review.get("records") or []:
        reasons = ", ".join(record.get("reasons") or [])
        lines.append(
            f"| {record.get('index')} | `{record.get('value_name')}` | "
            f"`{record.get('app_surface_bucket')}` | {reasons} | {record.get('recommended_action')} |"
        )

    return "\n".join(lines) + "\n"


def write_outputs(review: dict[str, Any], json_output: Path, markdown_output: Path) -> None:
    json_output.parent.mkdir(parents=True, exist_ok=True)
    json_output.write_text(json.dumps(review, indent=2) + "\n", encoding="utf-8")
    markdown_output.write_text(render_markdown(review), encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Review Operator96 enriched records for app-surface eligibility.")
    parser.add_argument("--matrix", default=str(DEFAULT_MATRIX))
    parser.add_argument("--output", default=str(JSON_OUTPUT))
    parser.add_argument("--markdown-output", default=str(MARKDOWN_OUTPUT))
    parser.add_argument("--json", action="store_true", help="Print JSON to stdout instead of markdown.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    review = build_review(Path(args.matrix).resolve())
    write_outputs(review, Path(args.output).resolve(), Path(args.markdown_output).resolve())
    if args.json:
        print(json.dumps(review, indent=2))
    else:
        print(render_markdown(review))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
