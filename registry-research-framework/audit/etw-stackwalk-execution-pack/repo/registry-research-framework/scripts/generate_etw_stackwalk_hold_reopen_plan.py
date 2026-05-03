#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
BATCH_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-dispatch-batch.json"
RUN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-dispatch-run.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-hold-reopen-plan.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-hold-reopen-plan.md"


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def portable_path(path: Path) -> str:
    resolved = path.resolve()
    try:
        return resolved.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return resolved.as_posix()


def load_json(path: Path) -> dict[str, Any]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError(f"{path} JSON payload is not an object")
    return payload


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text.rstrip() + "\n", encoding="utf-8")


def blocker_prerequisites(blockers: list[str]) -> list[str]:
    results: list[str] = []
    lowered = [str(blocker).lower() for blocker in blockers]
    if any("no-current-build-registry-seeding-path" in blocker for blocker in lowered):
        results.append("Land a current-build boot/init reader or registry seeding caller proof.")
    if any("no-primary-current-build-doc" in blocker for blocker in lowered):
        results.append("Land a primary current-build Microsoft document for the exact value semantics.")
    if any("intentional-hold" == blocker for blocker in lowered):
        results.append("Explicitly reopen the lane before dispatching runtime capture.")
    if not results:
        results.append("Review blockers manually before reopening the lane.")
    return results


def include_holds_command(candidate_id: str) -> str:
    return (
        "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py "
        f"--include-holds --candidate-id {candidate_id}"
    )


def include_holds_run_command(candidate_id: str) -> str:
    return (
        "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py "
        f"--include-holds --candidate-id {candidate_id} --run"
    )


def build_hold_reopen_plan(
    batch_payload: dict[str, Any],
    run_payload: dict[str, Any],
    *,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    items = []
    for item in batch_payload.get("items") or []:
        if item.get("actionability") != "hold":
            continue
        if not item.get("capture_ready"):
            continue
        candidate_id = str(item.get("candidate_id") or "")
        blockers = [str(blocker) for blocker in (item.get("promotion_blockers") or [])]
        items.append(
            {
                "candidate_id": candidate_id,
                "feature_area": item.get("feature_area"),
                "next_missing_layer": item.get("next_missing_layer"),
                "promotion_blockers": blockers,
                "reopen_prerequisites": blocker_prerequisites(blockers),
                "default_dispatch_excluded": not bool(item.get("dispatch_recommended")),
                "effective_config_command": item.get("effective_config_command"),
                "dispatch_command": item.get("dispatch_command"),
                "include_holds_plan_command": include_holds_command(candidate_id),
                "include_holds_run_command": include_holds_run_command(candidate_id),
                "run_id": ((item.get("capture_plan") or {}).get("run") or {}).get("run_id"),
                "host_etl_repo_path": ((item.get("capture_plan") or {}).get("run") or {}).get("host_etl_repo_path"),
                "next_action_hint": item.get("next_action_hint"),
            }
        )
    items.sort(key=lambda item: str(item.get("candidate_id") or ""))
    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "source_batch_path": portable_path(BATCH_PATH),
        "source_run_path": portable_path(RUN_PATH),
        "default_run_mode": run_payload.get("mode"),
        "default_selected_job_count": int(run_payload.get("selected_job_count") or 0),
        "default_skipped_hold_count": int(run_payload.get("skipped_hold_count") or 0),
        "reopen_candidate_count": len(items),
        "items": items,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    lines = [
        "# ETW Stackwalk Hold Reopen Plan",
        "",
        f"- Default run mode: `{payload.get('default_run_mode')}`",
        f"- Default selected jobs: `{payload.get('default_selected_job_count')}`",
        f"- Default skipped hold jobs: `{payload.get('default_skipped_hold_count')}`",
        f"- Reopen candidates: `{payload.get('reopen_candidate_count')}`",
        "",
        "## Candidates",
        "",
    ]
    items = payload.get("items") or []
    if not items:
        lines.append("- none")
        return "\n".join(lines).rstrip() + "\n"
    for item in items:
        lines.extend(
            [
                f"### {item.get('candidate_id')}",
                "",
                f"- Feature area: `{item.get('feature_area')}`",
                f"- Missing layer: `{item.get('next_missing_layer')}`",
                f"- Blockers: `{item.get('promotion_blockers')}`",
                f"- Next action hint: `{item.get('next_action_hint')}`",
                f"- Run id: `{item.get('run_id')}`",
                f"- Host ETL path: `{item.get('host_etl_repo_path')}`",
                "",
                "Prerequisites:",
            ]
        )
        for prereq in item.get("reopen_prerequisites") or []:
            lines.append(f"- {prereq}")
        lines.extend(
            [
                "",
                "```bash",
                str(item.get("effective_config_command") or ""),
                "```",
                "",
                "```bash",
                str(item.get("include_holds_plan_command") or ""),
                "```",
                "",
                "```bash",
                str(item.get("include_holds_run_command") or ""),
                "```",
                "",
            ]
        )
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate a controlled reopen plan for intentional-hold ETW stackwalk candidates.")
    parser.add_argument("--batch", type=Path, default=BATCH_PATH)
    parser.add_argument("--run", type=Path, default=RUN_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    args = parser.parse_args()

    payload = build_hold_reopen_plan(load_json(args.batch), load_json(args.run))
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(json.dumps({"output": portable_path(args.output), "reopen_candidate_count": payload["reopen_candidate_count"]}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
