#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from datetime import UTC, datetime
from pathlib import Path
from typing import Any

from vm_env import vm_connect, vm_domain, vm_snapshot


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_SCRIPTS = REPO_ROOT / "registry-research-framework" / "scripts"
if str(FRAMEWORK_SCRIPTS) not in sys.path:
    sys.path.insert(0, str(FRAMEWORK_SCRIPTS))

from registry_value_verdict import compute_registry_value_verdict  # noqa: E402


def now_utc() -> str:
    return datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def slug(value: str) -> str:
    import re

    return re.sub(r"[^A-Za-z0-9_.-]+", "-", value).strip("-").lower() or "registry-value"


def load_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def parse_int(value: object) -> int | None:
    if value is None:
        return None
    try:
        return int(str(value), 0)
    except (TypeError, ValueError):
        return None


def append_unique(values: list[int], value: int | None) -> None:
    if value is None:
        return
    if value not in values:
        values.append(value)


def planned_values(row: dict[str, Any], *, include_default_tests: bool, max_values: int) -> list[int]:
    requested = parse_int(row.get("requested_data"))
    default_value = parse_int(row.get("default_value"))
    values: list[int] = []

    append_unique(values, requested)
    append_unique(values, 0)
    append_unique(values, 1)

    if include_default_tests:
        append_unique(values, default_value)
    elif row.get("default_kind") == "observed-present":
        values = [value for value in values if value != default_value]

    if not values and requested is not None:
        values.append(requested)
    if max_values > 0:
        values = values[:max_values]
    return values


def key_missing_rows(report: dict[str, Any]) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    for item in report.get("key_missing_audit", []):
        if not isinstance(item, dict):
            continue
        rows.append(
            {
                "index": item.get("index"),
                "registry_path": item.get("registry_path"),
                "value_name": item.get("value_name"),
                "requested_data": item.get("requested_data"),
                "vm_status": "key-missing",
                "default_kind": "observed-absent",
                "default_value": None,
                "source_quality": item.get("verdict"),
                "risk_flags": [],
                "record_class": "key-missing",
            }
        )
    return rows


def load_campaign_rows(report_path: Path) -> list[dict[str, Any]]:
    report = load_json(report_path)
    rows = [dict(row, record_class="value") for row in report.get("default_value_matrix", []) if isinstance(row, dict)]
    rows.extend(key_missing_rows(report))
    rows.sort(key=lambda row: int(row.get("index") or 0))
    return rows


def build_plan(
    rows: list[dict[str, Any]],
    *,
    include_default_tests: bool,
    max_values_per_record: int,
    start_index: int,
    only_index: set[int],
) -> list[dict[str, Any]]:
    plan: list[dict[str, Any]] = []
    for row in rows:
        index = int(row.get("index") or 0)
        if start_index and index < start_index:
            continue
        if only_index and index not in only_index:
            continue
        for value in planned_values(row, include_default_tests=include_default_tests, max_values=max_values_per_record):
            experiment_id = f"operator96-{index:03d}-{slug(str(row.get('value_name') or 'value'))}-{value}"
            plan.append(
                {
                    "index": index,
                    "experiment_id": experiment_id,
                    "registry_path": row.get("registry_path"),
                    "value_name": row.get("value_name"),
                    "value_data": value,
                    "requested_data": row.get("requested_data"),
                    "default_kind": row.get("default_kind"),
                    "default_value": row.get("default_value"),
                    "vm_status": row.get("vm_status"),
                    "record_class": row.get("record_class"),
                    "risk_flags": row.get("risk_flags", []),
                    "source_quality": row.get("source_quality"),
                }
            )
    return plan


def run(cmd: list[str], *, timeout: int | None = None) -> subprocess.CompletedProcess[str]:
    return subprocess.run(cmd, cwd=str(REPO_ROOT), capture_output=True, text=True, timeout=timeout)


def existing_status(path: Path) -> str | None:
    if not path.exists():
        return None
    try:
        payload = load_json(path)
    except (OSError, json.JSONDecodeError):
        return "unreadable"
    return str(payload.get("status") or "unknown")


def numeric(value: Any) -> float | None:
    try:
        return float(value)
    except (TypeError, ValueError):
        return None


def percent_delta(after: Any, before: Any) -> float | None:
    before_number = numeric(before)
    after_number = numeric(after)
    if before_number is None or after_number is None or before_number == 0:
        return None
    return round(((after_number - before_number) / before_number) * 100, 2)


def stage_result(payload: dict[str, Any], stage: str) -> dict[str, Any]:
    stages = payload.get("stages")
    if not isinstance(stages, dict):
        return {}
    stage_payload = stages.get(stage)
    if not isinstance(stage_payload, dict):
        return {}
    result = stage_payload.get("result")
    return result if isinstance(result, dict) else {}


def benchmarks_from(stage_payload: dict[str, Any], key: str = "smoke") -> dict[str, Any]:
    smoke = stage_payload.get(key)
    if not isinstance(smoke, dict):
        return {}
    benchmarks = smoke.get("benchmarks")
    return benchmarks if isinstance(benchmarks, dict) else {}


def interactive_summary(stage_payload: dict[str, Any]) -> dict[str, Any]:
    smoke = stage_payload.get("smoke")
    if not isinstance(smoke, dict):
        return {"status": "missing"}
    interactive = smoke.get("interactive_user_smoke")
    if not isinstance(interactive, dict):
        return {"status": "missing"}
    return {
        "status": interactive.get("status"),
        "failure_count": interactive.get("failure_count"),
        "user": interactive.get("user"),
    }


def read_artifact_observations(path: Path) -> dict[str, Any]:
    if not path.exists():
        return {"status": "missing-artifact"}
    try:
        payload = load_json(path)
    except (OSError, json.JSONDecodeError) as error:
        return {"status": "unreadable-artifact", "error": str(error)}

    apply = stage_result(payload, "apply")
    post_reboot = stage_result(payload, "post_reboot_rollback")
    post_rollback = stage_result(payload, "post_rollback")
    baseline_bench = benchmarks_from(apply, "baseline_smoke")
    apply_bench = benchmarks_from(apply)
    post_reboot_bench = benchmarks_from(post_reboot)
    verdict = compute_registry_value_verdict(payload)

    return {
        "status": payload.get("status"),
        "verdict": verdict.get("overall"),
        "confidence": verdict.get("confidence"),
        "host_noise": verdict.get("host_noise"),
        "primary_delta_pct": verdict.get("delta_pct"),
        "verdict_reason": verdict.get("reason"),
        "metrics_used": verdict.get("metrics_used") or [],
        "safety_findings": verdict.get("safety_findings") or [],
        "smoke_hard_success": payload.get("smoke"),
        "apply": {
            "original": apply.get("original"),
            "after_apply": apply.get("after_apply"),
            "hard_failure_count": (apply.get("smoke") or {}).get("hard_failure_count") if isinstance(apply.get("smoke"), dict) else None,
            "best_effort_failure_count": (apply.get("smoke") or {}).get("best_effort_failure_count") if isinstance(apply.get("smoke"), dict) else None,
            "interactive_user_smoke": interactive_summary(apply),
        },
        "post_reboot": {
            "after_reboot": post_reboot.get("after_reboot"),
            "restore_action": post_reboot.get("restore_action"),
            "after_restore": post_reboot.get("after_restore"),
            "hard_failure_count": (post_reboot.get("smoke") or {}).get("hard_failure_count") if isinstance(post_reboot.get("smoke"), dict) else None,
            "best_effort_failure_count": (post_reboot.get("smoke") or {}).get("best_effort_failure_count") if isinstance(post_reboot.get("smoke"), dict) else None,
            "interactive_user_smoke": interactive_summary(post_reboot),
        },
        "post_rollback": {
            "final": post_rollback.get("final"),
            "hard_failure_count": (post_rollback.get("smoke") or {}).get("hard_failure_count") if isinstance(post_rollback.get("smoke"), dict) else None,
            "best_effort_failure_count": (post_rollback.get("smoke") or {}).get("best_effort_failure_count") if isinstance(post_rollback.get("smoke"), dict) else None,
            "interactive_user_smoke": interactive_summary(post_rollback),
        },
        "benchmark_delta_percent": {
            "apply_vs_baseline": {
                "cpu_single_seconds": percent_delta(apply_bench.get("cpu_single_seconds"), baseline_bench.get("cpu_single_seconds")),
                "cpu_multi_seconds": percent_delta(apply_bench.get("cpu_multi_seconds"), baseline_bench.get("cpu_multi_seconds")),
                "io_write_read_mib_per_second": percent_delta(apply_bench.get("io_write_read_mib_per_second"), baseline_bench.get("io_write_read_mib_per_second")),
            },
            "post_reboot_vs_baseline": {
                "cpu_single_seconds": percent_delta(post_reboot_bench.get("cpu_single_seconds"), baseline_bench.get("cpu_single_seconds")),
                "cpu_multi_seconds": percent_delta(post_reboot_bench.get("cpu_multi_seconds"), baseline_bench.get("cpu_multi_seconds")),
                "io_write_read_mib_per_second": percent_delta(post_reboot_bench.get("io_write_read_mib_per_second"), baseline_bench.get("io_write_read_mib_per_second")),
            },
        },
    }


def run_experiment(
    item: dict[str, Any],
    *,
    domain: str,
    connect: str,
    snapshot_name: str,
    smoke_profile: str,
    output_dir: Path,
    stage_wait_timeout: int,
    reboot_wait_timeout: int,
    post_reboot_delay_seconds: int,
    host_noise_max_retries: int,
    host_noise_retry_interval_seconds: float,
    host_noise_busy_threshold_pct: float,
    host_noise_load1_per_cpu_threshold: float,
    host_noise_sample_interval_seconds: float,
    abort_on_noisy_host: bool,
) -> dict[str, Any]:
    cmd = [
        sys.executable,
        str(REPO_ROOT / "scripts" / "vm-kvm" / "run-guest-registry-value-experiment.py"),
        "--domain",
        domain,
        "--connect",
        connect,
        "--registry-path",
        str(item["registry_path"]),
        "--value-name",
        str(item["value_name"]),
        "--value-data",
        str(item["value_data"]),
        "--output-name",
        str(item["experiment_id"]),
        "--output-dir",
        str(output_dir),
        "--smoke-profile",
        smoke_profile,
        "--require-domain-snapshot",
        "--auto-revert-snapshot-on-boot-failure",
        "--revert-snapshot-name",
        snapshot_name,
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
    ]
    if abort_on_noisy_host:
        cmd.append("--abort-on-noisy-host")
    completed = run(cmd, timeout=(stage_wait_timeout * 4) + (reboot_wait_timeout * 2) + 300)
    try:
        stdout_payload = json.loads(completed.stdout)
    except json.JSONDecodeError:
        stdout_payload = {"status": "parse-error", "stdout": completed.stdout, "stderr": completed.stderr}
    return {
        "returncode": completed.returncode,
        "command": cmd,
        "stdout_payload": stdout_payload,
        "stderr": completed.stderr,
    }


def write_plan_markdown(payload: dict[str, Any], path: Path) -> None:
    lines = [
        "# Operator 96 Registry Value Campaign",
        "",
        f"- Generated UTC: `{payload['generated_utc']}`",
        f"- Status: **{payload['status']}**",
        f"- Planned experiments: `{len(payload['plan'])}`",
        f"- Completed in this run: `{len(payload.get('results', []))}`",
        "",
        "## Plan",
        "",
        "| # | Experiment | Target | Value | Default | Source quality |",
        "|---:|---|---|---:|---|---|",
    ]
    for item in payload["plan"]:
        target = f"{item.get('registry_path')}\\{item.get('value_name')}"
        default = "absent" if item.get("default_kind") == "observed-absent" else item.get("default_value")
        lines.append(
            f"| {item.get('index')} | `{item.get('experiment_id')}` | `{target}` | `{item.get('value_data')}` | `{default}` | `{item.get('source_quality')}` |"
        )
    if payload.get("results"):
        lines.extend(
            [
                "",
                "## Results",
                "",
                "| Experiment | Verdict | Confidence | Host noise | Status | Hard smoke | Interactive | Primary Δ% | Post-reboot IO Δ% | Artifact |",
                "|---|---|---|---|---|---|---|---:|---:|---|",
            ]
        )
        for item in payload["results"]:
            observations = item.get("observations") if isinstance(item.get("observations"), dict) else {}
            smoke = observations.get("smoke_hard_success") if isinstance(observations.get("smoke_hard_success"), dict) else {}
            post_reboot = observations.get("post_reboot") if isinstance(observations.get("post_reboot"), dict) else {}
            interactive = post_reboot.get("interactive_user_smoke") if isinstance(post_reboot.get("interactive_user_smoke"), dict) else {}
            deltas = observations.get("benchmark_delta_percent") if isinstance(observations.get("benchmark_delta_percent"), dict) else {}
            post_reboot_delta = deltas.get("post_reboot_vs_baseline") if isinstance(deltas.get("post_reboot_vs_baseline"), dict) else {}
            lines.append(
                f"| `{item.get('experiment_id')}` | `{observations.get('verdict')}` | "
                f"`{observations.get('confidence')}` | `{observations.get('host_noise')}` | "
                f"`{item.get('status')}` | "
                f"`{smoke.get('post_reboot_smoke_hard_success')}` | "
                f"`{interactive.get('status')}`/`{interactive.get('failure_count')}` | "
                f"`{observations.get('primary_delta_pct')}` | "
                f"`{post_reboot_delta.get('io_write_read_mib_per_second')}` | "
                f"`{item.get('artifact_json')}` |"
            )
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Run a resumable registry value experiment campaign from the operator 96 follow-up report.")
    parser.add_argument("--domain", default=vm_domain("regprobe-win11-25h2-session"))
    parser.add_argument("--connect", default=vm_connect("qemu:///session"))
    parser.add_argument("--snapshot-name", default=vm_snapshot("clean-25h2-qga"))
    parser.add_argument(
        "--report",
        default=str(REPO_ROOT / "registry-research-framework" / "audit" / "operator-regadd-followup-source-default-report-20260509.json"),
    )
    parser.add_argument("--output-dir", default=str(REPO_ROOT / "registry-research-framework" / "audit" / "registry-value-experiments"))
    parser.add_argument("--campaign-output", default=str(REPO_ROOT / "registry-research-framework" / "audit" / "operator96-value-campaign-20260509.json"))
    parser.add_argument("--markdown-output", default="")
    parser.add_argument("--smoke-profile", choices=["none", "core", "gui"], default="gui")
    parser.add_argument("--max-values-per-record", type=int, default=2)
    parser.add_argument("--include-default-tests", action="store_true")
    parser.add_argument("--start-index", type=int, default=0)
    parser.add_argument("--only-index", action="append", type=int, default=[])
    parser.add_argument("--limit-records", type=int, default=0)
    parser.add_argument("--limit-experiments", type=int, default=0)
    parser.add_argument("--run", action="store_true")
    parser.add_argument("--rerun", action="store_true")
    parser.add_argument("--stop-on-failure", action="store_true")
    parser.add_argument("--stage-wait-timeout", type=int, default=420)
    parser.add_argument("--reboot-wait-timeout", type=int, default=420)
    parser.add_argument("--post-reboot-delay-seconds", type=int, default=25)
    parser.add_argument("--host-noise-max-retries", type=int, default=5)
    parser.add_argument("--host-noise-retry-interval-seconds", type=float, default=5.0)
    parser.add_argument("--host-noise-busy-threshold-pct", type=float, default=20.0)
    parser.add_argument("--host-noise-load1-per-cpu-threshold", type=float, default=0.75)
    parser.add_argument("--host-noise-sample-interval-seconds", type=float, default=0.5)
    parser.add_argument(
        "--abort-on-noisy-host",
        action="store_true",
        help="Forward fail-fast host-noise preflight to each experiment so low-noise campaigns do not mutate the guest under noisy host conditions.",
    )
    args = parser.parse_args()

    report_path = Path(args.report).resolve()
    output_dir = Path(args.output_dir).resolve()
    output_dir.mkdir(parents=True, exist_ok=True)
    campaign_output = Path(args.campaign_output).resolve()
    markdown_output = Path(args.markdown_output).resolve() if args.markdown_output else campaign_output.with_suffix(".md")

    rows = load_campaign_rows(report_path)
    if args.limit_records:
        rows = rows[: args.limit_records]
    plan = build_plan(
        rows,
        include_default_tests=args.include_default_tests,
        max_values_per_record=args.max_values_per_record,
        start_index=args.start_index,
        only_index=set(args.only_index),
    )
    if args.limit_experiments:
        plan = plan[: args.limit_experiments]

    payload: dict[str, Any] = {
        "generated_utc": now_utc(),
        "status": "planned" if not args.run else "running",
        "report": str(report_path.relative_to(REPO_ROOT)),
        "smoke_profile": args.smoke_profile,
        "max_values_per_record": args.max_values_per_record,
        "include_default_tests": args.include_default_tests,
        "plan": plan,
        "results": [],
    }

    if args.run:
        for item in plan:
            artifact_json = output_dir / f"{item['experiment_id']}.json"
            status = existing_status(artifact_json)
            if status == "ok" and not args.rerun:
                result = {
                    **item,
                    "status": "skipped-existing-ok",
                    "artifact_json": str(artifact_json.relative_to(REPO_ROOT)),
                    "observations": read_artifact_observations(artifact_json),
                }
                payload["results"].append(result)
                continue
            execution = run_experiment(
                item,
                domain=args.domain,
                connect=args.connect,
                snapshot_name=args.snapshot_name,
                smoke_profile=args.smoke_profile,
                output_dir=output_dir,
                stage_wait_timeout=args.stage_wait_timeout,
                reboot_wait_timeout=args.reboot_wait_timeout,
                post_reboot_delay_seconds=args.post_reboot_delay_seconds,
                host_noise_max_retries=args.host_noise_max_retries,
                host_noise_retry_interval_seconds=args.host_noise_retry_interval_seconds,
                host_noise_busy_threshold_pct=args.host_noise_busy_threshold_pct,
                host_noise_load1_per_cpu_threshold=args.host_noise_load1_per_cpu_threshold,
                host_noise_sample_interval_seconds=args.host_noise_sample_interval_seconds,
                abort_on_noisy_host=args.abort_on_noisy_host,
            )
            stdout_payload = execution.get("stdout_payload") if isinstance(execution, dict) else {}
            result_status = str((stdout_payload or {}).get("status") or ("error" if execution.get("returncode") else "unknown"))
            result = {
                **item,
                "status": result_status,
                "returncode": execution.get("returncode"),
                "artifact_json": str(artifact_json.relative_to(REPO_ROOT)),
                "stdout_payload": stdout_payload,
                "stderr": execution.get("stderr"),
                "observations": read_artifact_observations(artifact_json),
            }
            payload["results"].append(result)
            campaign_output.write_text(json.dumps({**payload, "status": "running"}, indent=2), encoding="utf-8")
            write_plan_markdown({**payload, "status": "running"}, markdown_output)
            if result_status != "ok" and args.stop_on_failure:
                payload["status"] = "error"
                break
        else:
            payload["status"] = "ok"

    campaign_output.parent.mkdir(parents=True, exist_ok=True)
    campaign_output.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    write_plan_markdown(payload, markdown_output)
    print(
        json.dumps(
            {
                "status": payload["status"],
                "planned": len(plan),
                "results": len(payload["results"]),
                "json": str(campaign_output),
                "markdown": str(markdown_output),
            },
            indent=2,
        )
    )
    return 0 if payload["status"] in {"planned", "ok"} else 1


if __name__ == "__main__":
    raise SystemExit(main())
