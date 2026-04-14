#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
DEFAULT_OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "research-quality-gate.json"
DEFAULT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "research-quality-gate.md"


@dataclass(frozen=True)
class GateStep:
    step_id: str
    label: str
    command: list[str]
    skipped: bool = False
    skip_reason: str | None = None


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def portable_path(path: Path) -> str:
    resolved = path.resolve()
    try:
        return resolved.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return resolved.as_posix()


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def tail(value: str, limit: int = 4000) -> str:
    return value[-limit:] if len(value) > limit else value


def run_step(step: GateStep) -> dict[str, Any]:
    if step.skipped:
        return {
            "step_id": step.step_id,
            "label": step.label,
            "status": "skipped",
            "skip_reason": step.skip_reason,
            "command": step.command,
            "returncode": None,
            "duration_ms": 0,
            "stdout_tail": "",
            "stderr_tail": "",
        }

    started = time.monotonic()
    completed = subprocess.run(
        step.command,
        cwd=REPO_ROOT,
        text=True,
        capture_output=True,
        check=False,
    )
    duration_ms = int((time.monotonic() - started) * 1000)
    return {
        "step_id": step.step_id,
        "label": step.label,
        "status": "pass" if completed.returncode == 0 else "fail",
        "skip_reason": None,
        "command": step.command,
        "returncode": completed.returncode,
        "duration_ms": duration_ms,
        "stdout_tail": tail(completed.stdout or ""),
        "stderr_tail": tail(completed.stderr or ""),
    }


def default_steps(args: argparse.Namespace) -> list[GateStep]:
    python = sys.executable
    return [
        GateStep(
            "python-tests",
            "Python pipeline tests",
            [python, "-m", "unittest", "discover", "-s", "tests/python", "-p", "test*.py"],
            skipped=args.skip_python_tests,
            skip_reason="--skip-python-tests",
        ),
        GateStep(
            "ghidra-smoke",
            "Ghidra autotrigger smoke gate",
            [python, "registry-research-framework/scripts/run_ghidra_autotrigger_smoke.py"],
            skipped=args.skip_ghidra_smoke,
            skip_reason="--skip-ghidra-smoke",
        ),
        GateStep(
            "etw-stackwalk-plan",
            "ETW stackwalk capture plan check",
            [python, "registry-research-framework/scripts/check_etw_stackwalk_capture_plan.py"],
        ),
        GateStep(
            "etw-stackwalk-dispatch-batch",
            "ETW stackwalk dispatch batch check",
            [python, "registry-research-framework/scripts/check_etw_stackwalk_dispatch_batch.py"],
        ),
        GateStep(
            "etw-stackwalk-dispatch-run",
            "ETW stackwalk dispatch run check",
            [python, "registry-research-framework/scripts/check_etw_stackwalk_dispatch_run.py"],
        ),
        GateStep(
            "etw-stackwalk-hold-reopen-plan",
            "ETW stackwalk hold reopen plan check",
            [python, "registry-research-framework/scripts/check_etw_stackwalk_hold_reopen_plan.py"],
        ),
        GateStep(
            "etw-stackwalk-hold-reopen-pack",
            "ETW stackwalk hold reopen pack check",
            [python, "registry-research-framework/scripts/check_etw_stackwalk_hold_reopen_pack.py"],
        ),
        GateStep(
            "etw-stackwalk-execution-manifest",
            "ETW stackwalk execution manifest check",
            [python, "registry-research-framework/scripts/check_etw_stackwalk_execution_manifest.py"],
        ),
        GateStep(
            "etw-stackwalk-execution-pack",
            "ETW stackwalk execution pack check",
            [python, "registry-research-framework/scripts/check_etw_stackwalk_execution_pack.py"],
        ),
        GateStep(
            "publish-metrics",
            "Generate research publish metrics",
            [python, "registry-research-framework/scripts/generate_publish_metrics.py"],
            skipped=args.skip_publish_metrics,
            skip_reason="--skip-publish-metrics",
        ),
        GateStep(
            "gate-thresholds",
            "Gate metrics threshold check",
            [python, "registry-research-framework/scripts/check_gate_thresholds.py"],
            skipped=args.skip_gate_thresholds,
            skip_reason="--skip-gate-thresholds",
        ),
        GateStep(
            "source-url-validation",
            "Source URL validation",
            [python, "registry-research-framework/scripts/validate_source_urls.py"],
            skipped=args.skip_url_validation,
            skip_reason="--skip-url-validation",
        ),
        GateStep(
            "mcp-readiness",
            "MCP readiness",
            [python, "registry-research-framework/scripts/check_mcp_readiness.py"],
            skipped=args.skip_mcp_readiness,
            skip_reason="--skip-mcp-readiness",
        ),
    ]


def quality_gate_payload(
    steps: list[GateStep],
    *,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    results = [run_step(step) for step in steps]
    failed = [result for result in results if result.get("status") == "fail"]
    skipped = [result for result in results if result.get("status") == "skipped"]
    passed = [result for result in results if result.get("status") == "pass"]
    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "quality_gate_status": "PASS" if not failed else "FAIL",
        "counts": {
            "step_count": len(results),
            "passed": len(passed),
            "failed": len(failed),
            "skipped": len(skipped),
        },
        "failed_step_ids": [str(result.get("step_id") or "") for result in failed],
        "steps": results,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    counts = payload.get("counts") or {}
    lines = [
        "# Research Quality Gate",
        "",
        f"- Status: `{payload.get('quality_gate_status')}`",
        f"- Steps: `{counts.get('step_count')}`",
        f"- Passed: `{counts.get('passed')}`",
        f"- Failed: `{counts.get('failed')}`",
        f"- Skipped: `{counts.get('skipped')}`",
        "",
        "## Steps",
        "",
    ]
    for result in payload.get("steps") or []:
        lines.append(
            f"- `{result.get('step_id')}`: `{result.get('status')}`"
            + (f" returncode=`{result.get('returncode')}`" if result.get("returncode") is not None else "")
        )
    failures = [result for result in payload.get("steps") or [] if result.get("status") == "fail"]
    lines.extend(["", "## Failures", ""])
    if not failures:
        lines.append("- none")
    for result in failures:
        lines.append(f"- `{result.get('step_id')}`")
        stderr_tail = str(result.get("stderr_tail") or "").strip()
        stdout_tail = str(result.get("stdout_tail") or "").strip()
        if stderr_tail:
            lines.append(f"  stderr: `{stderr_tail}`")
        elif stdout_tail:
            lines.append(f"  stdout: `{stdout_tail}`")
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Run the RegProbe research quality gate checks as one command.")
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_PATH)
    parser.add_argument("--skip-python-tests", action="store_true")
    parser.add_argument("--skip-ghidra-smoke", action="store_true")
    parser.add_argument("--skip-publish-metrics", action="store_true")
    parser.add_argument("--skip-gate-thresholds", action="store_true")
    parser.add_argument("--skip-url-validation", action="store_true")
    parser.add_argument("--skip-mcp-readiness", action="store_true")
    args = parser.parse_args()

    payload = quality_gate_payload(default_steps(args))
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(json.dumps(payload, indent=2))
    return 0 if payload.get("quality_gate_status") == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
