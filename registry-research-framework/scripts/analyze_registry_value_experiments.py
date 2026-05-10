#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from collections import Counter
from datetime import UTC, datetime
from pathlib import Path
from typing import Any

from registry_value_verdict import compute_registry_value_verdict


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_INPUT_DIR = REPO_ROOT / "registry-research-framework" / "audit" / "registry-value-experiments"
DEFAULT_OUTPUT = REPO_ROOT / "registry-research-framework" / "audit" / "registry-value-experiment-analysis.json"


def now_utc() -> str:
    return datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def load_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def relative(path: Path) -> str:
    try:
        return str(path.resolve().relative_to(REPO_ROOT))
    except ValueError:
        return str(path)


def iter_artifact_paths(input_dir: Path, pattern: str) -> list[Path]:
    return sorted(path for path in input_dir.glob(pattern) if path.is_file() and path.suffix.lower() == ".json")


def read_stage(payload: dict[str, Any], stage: str) -> dict[str, Any]:
    stages = payload.get("stages")
    if not isinstance(stages, dict):
        return {}
    wrapper = stages.get(stage)
    if not isinstance(wrapper, dict):
        return {}
    result = wrapper.get("result")
    return result if isinstance(result, dict) else {}


def smoke_count(stage: dict[str, Any], field: str) -> int | None:
    smoke = stage.get("smoke")
    if not isinstance(smoke, dict):
        return None
    value = smoke.get(field)
    return value if isinstance(value, int) else None


def extract_summary(path: Path, payload: dict[str, Any]) -> dict[str, Any]:
    verdict = compute_registry_value_verdict(payload)
    apply = read_stage(payload, "apply")
    post_reboot = read_stage(payload, "post_reboot_rollback")
    post_rollback = read_stage(payload, "post_rollback")
    target = f"{payload.get('registry_path')}\\{payload.get('value_name')}"
    return {
        "artifact": relative(path),
        "experiment_id": payload.get("experiment_id") or path.stem,
        "status": payload.get("status"),
        "target": target,
        "registry_path": payload.get("registry_path"),
        "value_name": payload.get("value_name"),
        "value_data": payload.get("value_data"),
        "verdict": verdict.get("overall"),
        "confidence": verdict.get("confidence"),
        "host_noise": verdict.get("host_noise"),
        "delta_pct": verdict.get("delta_pct"),
        "reason": verdict.get("reason"),
        "metrics_used": verdict.get("metrics_used") or [],
        "safety_findings": verdict.get("safety_findings") or [],
        "performance_findings": verdict.get("performance_findings") or [],
        "apply_hard_failures": smoke_count(apply, "hard_failure_count"),
        "post_reboot_hard_failures": smoke_count(post_reboot, "hard_failure_count"),
        "post_rollback_hard_failures": smoke_count(post_rollback, "hard_failure_count"),
        "restore_action": post_reboot.get("restore_action"),
    }


def analyze(input_dir: Path, pattern: str) -> dict[str, Any]:
    rows: list[dict[str, Any]] = []
    errors: list[dict[str, Any]] = []
    for path in iter_artifact_paths(input_dir, pattern):
        try:
            payload = load_json(path)
            if not isinstance(payload, dict):
                raise ValueError("artifact JSON root is not an object")
            rows.append(extract_summary(path, payload))
        except (OSError, json.JSONDecodeError, ValueError) as error:
            errors.append({"artifact": relative(path), "error": str(error)})

    verdict_counts = Counter(str(row.get("verdict") or "unknown") for row in rows)
    confidence_counts = Counter(str(row.get("confidence") or "unknown") for row in rows)
    host_noise_counts = Counter(str(row.get("host_noise") or "unknown") for row in rows)
    return {
        "schema_version": "1.0",
        "generated_utc": now_utc(),
        "input_dir": relative(input_dir),
        "pattern": pattern,
        "artifact_count": len(rows),
        "error_count": len(errors),
        "counts": {
            "by_verdict": dict(sorted(verdict_counts.items())),
            "by_confidence": dict(sorted(confidence_counts.items())),
            "by_host_noise": dict(sorted(host_noise_counts.items())),
        },
        "results": rows,
        "errors": errors,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    lines = [
        "# Registry Value Experiment Analysis",
        "",
        f"- Generated UTC: `{payload.get('generated_utc')}`",
        f"- Input: `{payload.get('input_dir')}`",
        f"- Pattern: `{payload.get('pattern')}`",
        f"- Artifacts analyzed: `{payload.get('artifact_count')}`",
        f"- Errors: `{payload.get('error_count')}`",
        "",
        "## Verdict Counts",
        "",
    ]
    for verdict, count in (payload.get("counts") or {}).get("by_verdict", {}).items():
        lines.append(f"- `{verdict}`: `{count}`")

    lines.extend(
        [
            "",
            "## Results",
            "",
            "| Experiment | Verdict | Confidence | Host noise | Δ% | Reason | Artifact |",
            "|---|---|---|---|---:|---|---|",
        ]
    )
    for row in payload.get("results") or []:
        delta = row.get("delta_pct")
        delta_text = "" if delta is None else f"{delta:.3f}" if isinstance(delta, float) else str(delta)
        reason = str(row.get("reason") or "").replace("|", "\\|")
        lines.append(
            f"| `{row.get('experiment_id')}` | `{row.get('verdict')}` | `{row.get('confidence')}` | "
            f"`{row.get('host_noise')}` | `{delta_text}` | {reason} | `{row.get('artifact')}` |"
        )

    if payload.get("errors"):
        lines.extend(["", "## Errors", ""])
        for error in payload.get("errors") or []:
            lines.append(f"- `{error.get('artifact')}`: {error.get('error')}")

    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Retro-analyze registry value experiment artifacts and emit verdict summaries.")
    parser.add_argument("--input-dir", default=str(DEFAULT_INPUT_DIR))
    parser.add_argument("--pattern", default="operator96-*.json")
    parser.add_argument("--output", default=str(DEFAULT_OUTPUT))
    parser.add_argument("--markdown-output", default="")
    args = parser.parse_args()

    input_dir = Path(args.input_dir).resolve()
    output = Path(args.output).resolve()
    markdown_output = Path(args.markdown_output).resolve() if args.markdown_output else output.with_suffix(".md")
    payload = analyze(input_dir, args.pattern)
    write_json(output, payload)
    markdown_output.parent.mkdir(parents=True, exist_ok=True)
    markdown_output.write_text(render_markdown(payload), encoding="utf-8")
    print(json.dumps({"status": "ok", "json": relative(output), "markdown": relative(markdown_output), "artifact_count": payload["artifact_count"]}, indent=2))
    return 0 if not payload.get("errors") else 1


if __name__ == "__main__":
    raise SystemExit(main())
