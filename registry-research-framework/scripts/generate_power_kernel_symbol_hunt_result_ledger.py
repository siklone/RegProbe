#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
AUDIT_ROOT = REPO_ROOT / "registry-research-framework" / "audit"
RUBRIC_PATH = AUDIT_ROOT / "power-kernel-symbol-hunt-review-rubric-20260422.json"
TEMPLATE_PATH = AUDIT_ROOT / "power-kernel-symbol-hunt-result-ledger-template-20260422.json"


def load_json(path: Path) -> dict[str, Any]:
    payload = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(payload, dict):
        raise ValueError(f"{path} JSON payload is not an object")
    return payload


def portable_path(path: Path, *, repo_root: Path = REPO_ROOT) -> str:
    try:
        return str(path.resolve().relative_to(repo_root)).replace("\\", "/")
    except ValueError:
        return str(path.resolve()).replace("\\", "/")


def artifact_display_path(path: Path, *, repo_root: Path = REPO_ROOT) -> str:
    try:
        return str(path.resolve().relative_to(repo_root)).replace("\\", "/")
    except ValueError:
        parent = path.parent.name or "external"
        return f"external-artifacts/{parent}/{path.name}"


def collect_markers(text: str, markers: list[str]) -> list[str]:
    return [marker for marker in markers if marker in text]


def build_artifact_review(
    *,
    artifact_id: str,
    stdout_path: Path,
    summary_path: Path,
    command_file: str,
    rubric_entry: dict[str, Any],
) -> tuple[dict[str, Any], list[str]]:
    red_flags: list[str] = []
    stdout_text = ""
    if stdout_path.exists():
        stdout_text = stdout_path.read_text(encoding="utf-8-sig", errors="ignore")
    else:
        red_flags.append(f"missing stdout: {artifact_display_path(stdout_path)}")

    required_markers = list(rubric_entry.get("required_markers") or [])
    strong_markers = list(rubric_entry.get("good_signal_markers") or [])
    weak_markers = list(rubric_entry.get("weak_signal_markers") or [])

    required_seen = collect_markers(stdout_text, required_markers)
    strong_seen = collect_markers(stdout_text, strong_markers)
    weak_seen = collect_markers(stdout_text, weak_markers)
    required_present = len(required_seen) == len(required_markers)
    if stdout_path.exists() and not required_present:
        red_flags.append(f"required markers missing for {artifact_id}")

    return (
        {
            "artifact_id": artifact_id,
            "stdout_path": artifact_display_path(stdout_path),
            "summary_path": artifact_display_path(summary_path),
            "command_file": command_file,
            "required_markers_present": required_present,
            "required_markers_seen": required_seen,
            "strong_markers_seen": strong_seen,
            "weak_markers_seen": weak_seen,
            "summary_exists": summary_path.exists(),
        },
        red_flags,
    )


def infer_outcome(reviews: dict[str, dict[str, Any]], red_flags: list[str]) -> str:
    if red_flags:
        return "symbol-regression-or-wrapper-fog"

    init_review = reviews["execution_required_init_walker"]
    consumer_review = reviews["execution_required_consumers"]
    callback_review = reviews["execution_required_setting_callback"]
    timer_review = reviews["global_timer_resolution_reader"]

    if (
        init_review["required_markers_present"]
        and consumer_review["required_markers_present"]
        and (init_review["strong_markers_seen"] or consumer_review["strong_markers_seen"])
    ):
        return "execution-required-seeding-retained"

    if callback_review["required_markers_present"] and callback_review["strong_markers_seen"]:
        return "timeout-branch-separated"

    if timer_review["required_markers_present"] and not timer_review["strong_markers_seen"]:
        return "timer-anchor-retained-without-reader"

    return "symbol-regression-or-wrapper-fog"


def render_markdown(payload: dict[str, Any]) -> str:
    fill = payload["fill_after_run"]
    review = fill["review_outcome"]
    next_move = fill["next_move"]

    lines = [
        "# Power / Kernel Symbol Hunt Result Ledger",
        "",
        f"- Run ID: `{fill['run_id']}`",
        f"- Suggested outcome: `{review['chosen_outcome']}`",
        f"- Stop condition triggered: `{review['stop_condition_triggered']}`",
        "",
    ]

    artifact_order = [
        ("execution_required_init_walker", "Execution-Required Init Walker"),
        ("execution_required_consumers", "Execution-Required Consumers"),
        ("execution_required_setting_callback", "Execution-Required Setting Callback"),
        ("global_timer_resolution_reader", "Global Timer Resolution Reader"),
    ]
    for key, title in artifact_order:
        artifact = fill["artifacts"][key]
        lines.extend(
            [
                f"## {title}",
                f"- Stdout: `{artifact['stdout_path']}`",
                f"- Summary: `{artifact['summary_path']}`",
                f"- Required markers present: `{artifact['required_markers_present']}`",
                f"- Strong markers: `{artifact['strong_markers_seen']}`",
                f"- Weak markers: `{artifact['weak_markers_seen']}`",
                "",
            ]
        )

    lines.extend(
        [
            "## Review",
            f"- Why: {review['why']}",
            f"- Red flags: `{review['red_flags']}`",
            "",
            "## Next Move",
            f"- Lane: `{next_move['lane']}`",
            f"- Exact target: `{next_move['exact_target']}`",
        ]
    )
    return "\n".join(lines) + "\n"


def build_prefilled_payload(
    *,
    run_id: str,
    init_stdout: Path,
    init_summary: Path,
    consumers_stdout: Path,
    consumers_summary: Path,
    callback_stdout: Path,
    callback_summary: Path,
    timer_stdout: Path,
    timer_summary: Path,
) -> dict[str, Any]:
    rubric = load_json(RUBRIC_PATH)
    template = load_json(TEMPLATE_PATH)
    artifact_checks = {entry["artifact_id"]: entry for entry in (rubric.get("artifact_checks") or [])}

    artifact_configs = [
        (
            "execution_required_init_walker",
            "local-kd-execution-required-init-walker",
            init_stdout,
            init_summary,
            "registry-research-framework/audit/execution-required-init-walker-reacquire-local-kd-20260422.txt",
        ),
        (
            "execution_required_consumers",
            "local-kd-execution-required-consumers",
            consumers_stdout,
            consumers_summary,
            "registry-research-framework/audit/execution-required-consumers-reacquire-local-kd-20260422.txt",
        ),
        (
            "execution_required_setting_callback",
            "local-kd-execution-required-setting-callback",
            callback_stdout,
            callback_summary,
            "registry-research-framework/audit/execution-required-setting-callback-reacquire-local-kd-20260422.txt",
        ),
        (
            "global_timer_resolution_reader",
            "local-kd-global-timer-resolution-reader",
            timer_stdout,
            timer_summary,
            "registry-research-framework/audit/global-timer-resolution-reader-reacquire-local-kd-20260422.txt",
        ),
    ]

    reviews: dict[str, dict[str, Any]] = {}
    red_flags: list[str] = []
    for payload_key, artifact_id, stdout_path, summary_path, command_file in artifact_configs:
        review, review_red_flags = build_artifact_review(
            artifact_id=artifact_id,
            stdout_path=stdout_path,
            summary_path=summary_path,
            command_file=command_file,
            rubric_entry=artifact_checks[artifact_id],
        )
        reviews[payload_key] = review
        red_flags.extend(review_red_flags)

    chosen_outcome = infer_outcome(reviews, red_flags)
    lane_map = {
        "execution-required-seeding-retained": ("execution-required", "retained init walker and consumer chain"),
        "timeout-branch-separated": ("execution-required", "keep timeout callback separate from boolean seeding"),
        "timer-anchor-retained-without-reader": ("global-timer", "KiGlobalTimerResolutionRequests active-narrow"),
        "symbol-regression-or-wrapper-fog": ("hold", "reacquire exact retained anchor before new claims"),
    }
    lane, exact_target = lane_map[chosen_outcome]

    payload = template
    fill = payload["fill_after_run"]
    fill["run_id"] = run_id
    fill["artifacts"]["execution_required_init_walker"] = reviews["execution_required_init_walker"]
    fill["artifacts"]["execution_required_consumers"] = reviews["execution_required_consumers"]
    fill["artifacts"]["execution_required_setting_callback"] = reviews["execution_required_setting_callback"]
    fill["artifacts"]["global_timer_resolution_reader"] = reviews["global_timer_resolution_reader"]
    fill["review_outcome"]["chosen_outcome"] = chosen_outcome
    fill["review_outcome"]["why"] = (
        "Auto-suggested from retained marker hits; confirm against the reacquired stdout before promoting this result."
    )
    fill["review_outcome"]["red_flags"] = red_flags
    fill["review_outcome"]["stop_condition_triggered"] = bool(red_flags)
    fill["next_move"]["lane"] = lane
    fill["next_move"]["exact_target"] = exact_target
    payload["generated_utc"] = datetime.now(timezone.utc).isoformat()
    payload["generator"] = portable_path(Path(__file__))
    return payload


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate a prefilled Power/Kernel symbol-hunt result ledger from retained KD artifacts.")
    parser.add_argument("--run-id", required=True)
    parser.add_argument("--init-stdout", type=Path, required=True)
    parser.add_argument("--init-summary", type=Path, required=True)
    parser.add_argument("--consumers-stdout", type=Path, required=True)
    parser.add_argument("--consumers-summary", type=Path, required=True)
    parser.add_argument("--callback-stdout", type=Path, required=True)
    parser.add_argument("--callback-summary", type=Path, required=True)
    parser.add_argument("--timer-stdout", type=Path, required=True)
    parser.add_argument("--timer-summary", type=Path, required=True)
    parser.add_argument("--output-json", type=Path, required=True)
    parser.add_argument("--output-md", type=Path, required=True)
    args = parser.parse_args()

    payload = build_prefilled_payload(
        run_id=args.run_id,
        init_stdout=args.init_stdout,
        init_summary=args.init_summary,
        consumers_stdout=args.consumers_stdout,
        consumers_summary=args.consumers_summary,
        callback_stdout=args.callback_stdout,
        callback_summary=args.callback_summary,
        timer_stdout=args.timer_stdout,
        timer_summary=args.timer_summary,
    )
    args.output_json.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    args.output_md.write_text(render_markdown(payload), encoding="utf-8")
    print(json.dumps({"output_json": portable_path(args.output_json), "output_md": portable_path(args.output_md)}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
