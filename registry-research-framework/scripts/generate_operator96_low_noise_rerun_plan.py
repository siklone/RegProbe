#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from datetime import UTC, datetime
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
AUDIT_DIR = REPO_ROOT / "registry-research-framework" / "audit"
DEFAULT_REVIEW = AUDIT_DIR / "operator96-app-surface-review-20260510.json"
JSON_OUTPUT = AUDIT_DIR / "operator96-low-noise-rerun-plan-20260510.json"
MARKDOWN_OUTPUT = AUDIT_DIR / "operator96-low-noise-rerun-plan-20260510.md"

DEFAULT_OUTPUT_DIR = "registry-research-framework/audit/registry-value-experiments-low-noise-20260510"
DEFAULT_CAMPAIGN_OUTPUT = "registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260510.json"
DEFAULT_MARKDOWN_OUTPUT = "registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260510.md"


def normalize_text(value: Any) -> str:
    return str(value or "").strip()


def low_noise_records(review: dict[str, Any]) -> list[dict[str, Any]]:
    records = [
        record
        for record in review.get("records") or []
        if isinstance(record, dict) and normalize_text(record.get("app_surface_bucket")) == "needs_low_noise_rerun"
    ]
    return sorted(records, key=lambda record: int(record.get("index") or 0))


def build_campaign_command(
    indexes: list[int],
    *,
    run: bool,
    output_dir: str = DEFAULT_OUTPUT_DIR,
    campaign_output: str = DEFAULT_CAMPAIGN_OUTPUT,
    markdown_output: str = DEFAULT_MARKDOWN_OUTPUT,
    max_values_per_record: int = 2,
    stage_wait_timeout: int = 420,
    reboot_wait_timeout: int = 420,
    post_reboot_delay_seconds: int = 90,
    host_noise_max_retries: int = 18,
    host_noise_retry_interval_seconds: float = 10.0,
    host_noise_busy_threshold_pct: float = 12.5,
    host_noise_load1_per_cpu_threshold: float = 0.5,
    host_noise_sample_interval_seconds: float = 1.0,
) -> list[str]:
    cmd = [
        "python3",
        "scripts/vm-kvm/run-guest-registry-value-campaign.py",
        "--output-dir",
        output_dir,
        "--campaign-output",
        campaign_output,
        "--markdown-output",
        markdown_output,
        "--max-values-per-record",
        str(max_values_per_record),
        "--smoke-profile",
        "gui",
        "--stage-wait-timeout",
        str(stage_wait_timeout),
        "--reboot-wait-timeout",
        str(reboot_wait_timeout),
        "--post-reboot-delay-seconds",
        str(post_reboot_delay_seconds),
        "--host-noise-max-retries",
        str(host_noise_max_retries),
        "--host-noise-retry-interval-seconds",
        str(host_noise_retry_interval_seconds),
        "--host-noise-busy-threshold-pct",
        str(host_noise_busy_threshold_pct),
        "--host-noise-load1-per-cpu-threshold",
        str(host_noise_load1_per_cpu_threshold),
        "--host-noise-sample-interval-seconds",
        str(host_noise_sample_interval_seconds),
        "--rerun",
    ]
    for index in indexes:
        cmd.extend(["--only-index", str(index)])
    if run:
        cmd.append("--run")
    return cmd


def build_plan(
    review_path: Path = DEFAULT_REVIEW,
    *,
    tranche_size: int = 5,
    start_offset: int = 0,
    output_dir: str = DEFAULT_OUTPUT_DIR,
    campaign_output: str = DEFAULT_CAMPAIGN_OUTPUT,
    campaign_markdown_output: str = DEFAULT_MARKDOWN_OUTPUT,
) -> dict[str, Any]:
    review = json.loads(review_path.read_text(encoding="utf-8"))
    records = low_noise_records(review)
    tranche = records[start_offset : start_offset + tranche_size]
    tranche_indexes = [int(record.get("index") or 0) for record in tranche]
    first_tranche = records[:tranche_size]
    first_indexes = [int(record.get("index") or 0) for record in first_tranche]
    return {
        "schema_version": "1.0",
        "generated_utc": datetime.now(UTC).isoformat(timespec="seconds").replace("+00:00", "Z"),
        "review": str(review_path.relative_to(REPO_ROOT)),
        "campaign_id": "operator96-low-noise-rerun-20260510",
        "status": "PASS",
        "summary": {
            "candidate_record_count": len(records),
            "start_offset": start_offset,
            "tranche_record_count": len(tranche),
            "tranche_expected_experiment_count": len(tranche) * 2,
            "tranche_indexes": tranche_indexes,
            "first_tranche_record_count": len(first_tranche),
            "first_tranche_expected_experiment_count": len(first_tranche) * 2,
            "first_tranche_indexes": first_indexes,
        },
        "low_noise_policy": {
            "output_dir": output_dir,
            "max_values_per_record": 2,
            "host_noise_max_retries": 18,
            "host_noise_retry_interval_seconds": 10.0,
            "host_noise_busy_threshold_pct": 12.5,
            "host_noise_load1_per_cpu_threshold": 0.5,
            "host_noise_sample_interval_seconds": 1.0,
            "post_reboot_delay_seconds": 90,
            "claim_rule": "Rerun results may support app-card copy only if host_noise=ok and confidence is not low.",
        },
        "first_tranche": first_tranche,
        "tranche": tranche,
        "commands": {
            "plan_only": build_campaign_command(
                tranche_indexes,
                run=False,
                output_dir=output_dir,
                campaign_output=campaign_output,
                markdown_output=campaign_markdown_output,
            ),
            "run": build_campaign_command(
                tranche_indexes,
                run=True,
                output_dir=output_dir,
                campaign_output=campaign_output,
                markdown_output=campaign_markdown_output,
            ),
        },
        "records": records,
    }


def render_markdown(plan: dict[str, Any]) -> str:
    summary = plan.get("summary") or {}
    commands = plan.get("commands") or {}
    lines = [
        "# Operator96 Low-Noise Rerun Plan",
        "",
        f"- Generated UTC: `{plan.get('generated_utc')}`",
        f"- Review: `{plan.get('review')}`",
        f"- Candidate records: `{summary.get('candidate_record_count')}`",
        f"- Start offset: `{summary.get('start_offset')}`",
        f"- Tranche records: `{summary.get('tranche_record_count')}`",
        f"- Tranche expected experiments: `{summary.get('tranche_expected_experiment_count')}`",
        f"- Tranche indexes: `{', '.join(str(item) for item in summary.get('tranche_indexes') or [])}`",
        "",
        "## Commands",
        "",
        "- Plan only:",
        f"  `{' '.join(commands.get('plan_only') or [])}`",
        "- Run:",
        f"  `{' '.join(commands.get('run') or [])}`",
        "",
        "## Policy",
        "",
    ]
    for key, value in (plan.get("low_noise_policy") or {}).items():
        lines.append(f"- `{key}`: `{value}`")

    lines.extend(["", "## Tranche", ""])
    lines.append("| # | Value | Reason | Action |")
    lines.append("|---:|---|---|---|")
    for record in plan.get("tranche") or []:
        reasons = ", ".join(record.get("reasons") or [])
        lines.append(
            f"| {record.get('index')} | `{record.get('value_name')}` | {reasons} | {record.get('recommended_action')} |"
        )

    return "\n".join(lines) + "\n"


def write_outputs(plan: dict[str, Any], json_output: Path, markdown_output: Path) -> None:
    json_output.parent.mkdir(parents=True, exist_ok=True)
    json_output.write_text(json.dumps(plan, indent=2) + "\n", encoding="utf-8")
    markdown_output.write_text(render_markdown(plan), encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate the next Operator96 low-noise rerun tranche plan.")
    parser.add_argument("--review", default=str(DEFAULT_REVIEW))
    parser.add_argument("--tranche-size", type=int, default=5)
    parser.add_argument("--start-offset", type=int, default=0)
    parser.add_argument("--experiment-output-dir", default=DEFAULT_OUTPUT_DIR)
    parser.add_argument("--campaign-output", default=DEFAULT_CAMPAIGN_OUTPUT)
    parser.add_argument("--campaign-markdown-output", default=DEFAULT_MARKDOWN_OUTPUT)
    parser.add_argument("--output", default=str(JSON_OUTPUT))
    parser.add_argument("--markdown-output", default=str(MARKDOWN_OUTPUT))
    parser.add_argument("--json", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    if args.tranche_size <= 0:
        raise SystemExit("--tranche-size must be greater than 0")
    if args.start_offset < 0:
        raise SystemExit("--start-offset must be greater than or equal to 0")
    plan = build_plan(
        Path(args.review).resolve(),
        tranche_size=args.tranche_size,
        start_offset=args.start_offset,
        output_dir=args.experiment_output_dir,
        campaign_output=args.campaign_output,
        campaign_markdown_output=args.campaign_markdown_output,
    )
    write_outputs(plan, Path(args.output).resolve(), Path(args.markdown_output).resolve())
    if args.json:
        print(json.dumps(plan, indent=2))
    else:
        print(render_markdown(plan))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
