#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
PLAN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-capture-plan.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-capture-plan-check.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-capture-plan-check.md"
REQUIRED_STACKWALK_EVENTS = {"RegQueryValue", "RegSetValue"}


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


def command_contains(command: list[Any], needle: str) -> bool:
    return any(str(part) == needle or needle in str(part) for part in command)


def check_plan(plan: dict[str, Any], *, generated_utc: str | None = None) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    commands = plan.get("commands") or {}
    start_command = commands.get("start") or []
    repo_parse_command = commands.get("repo_parse") or []
    stack_capture = plan.get("stack_capture") or {}
    stackwalk_events = set(str(event) for event in (stack_capture.get("stackwalk_events") or []))
    errors: list[str] = []

    if plan.get("plan_status") != "ready":
        errors.append("plan_status must be ready.")
    if not command_contains(start_command, "-stackwalk"):
        errors.append("start command must include -stackwalk.")
    if not command_contains(start_command, "REGISTRY"):
        errors.append("start command must include the REGISTRY kernel flag.")
    missing_stackwalk_events = sorted(REQUIRED_STACKWALK_EVENTS - stackwalk_events)
    if missing_stackwalk_events:
        errors.append(f"missing required stackwalk events: {', '.join(missing_stackwalk_events)}")
    if stack_capture.get("expected") is not True:
        errors.append("stack_capture.expected must be true.")
    if stack_capture.get("normalized_bundle_field") != "caller_stack":
        errors.append("stack_capture.normalized_bundle_field must be caller_stack.")
    if "Stack" not in set(str(field) for field in (stack_capture.get("source_fields") or [])):
        errors.append("stack_capture.source_fields must include Stack.")
    if not repo_parse_command or "parse_etl_registry_touches.py" not in " ".join(str(part) for part in repo_parse_command):
        errors.append("repo_parse command must call parse_etl_registry_touches.py.")
    if not repo_parse_command or not str(repo_parse_command[-1]).startswith("evidence/raw/etw-stackwalk/"):
        errors.append("repo_parse command must point at an evidence/raw/etw-stackwalk ETL handoff path.")

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "check_status": "ok" if not errors else "error",
        "errors": errors,
        "plan_status": plan.get("plan_status"),
        "profile_id": plan.get("profile_id"),
        "run_id": (plan.get("run") or {}).get("run_id"),
        "stackwalk_events": sorted(stackwalk_events),
        "commands_checked": sorted(commands.keys()),
    }


def render_markdown(payload: dict[str, Any]) -> str:
    lines = [
        "# ETW Stackwalk Capture Plan Check",
        "",
        f"- Status: `{payload.get('check_status')}`",
        f"- Profile: `{payload.get('profile_id')}`",
        f"- Run id: `{payload.get('run_id')}`",
        f"- Stackwalk events: `{', '.join(payload.get('stackwalk_events') or [])}`",
        "",
        "## Errors",
        "",
    ]
    errors = payload.get("errors") or []
    if not errors:
        lines.append("- none")
    for error in errors:
        lines.append(f"- {error}")
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate the ETW registry stackwalk capture plan surface.")
    parser.add_argument("--plan", type=Path, default=PLAN_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    args = parser.parse_args()

    plan = json.loads(args.plan.read_text(encoding="utf-8"))
    if not isinstance(plan, dict):
        raise ValueError(f"{args.plan} JSON payload is not an object")
    payload = check_plan(plan)
    payload["plan_path"] = portable_path(args.plan)
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(json.dumps({"check": portable_path(args.output), "status": payload.get("check_status")}, indent=2))
    return 0 if payload.get("check_status") == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
