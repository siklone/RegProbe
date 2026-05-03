#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
CURRENT_DIR = Path(__file__).resolve().parent
PROFILE_CONFIG_PATH = FRAMEWORK_ROOT / "config" / "etw-stackwalk-profiles.json"
RUNNER_CONFIG_PATH = FRAMEWORK_ROOT / "config" / "tweak-vm-runners.json"
QUEUE_PATH = FRAMEWORK_ROOT / "queue" / "research-queue.json"
PROMOTION_GATES_PATH = REPO_ROOT / "research" / "promotion-gates.json"
OUTPUT_JSON = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-dispatch-batch.json"
OUTPUT_MD = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-dispatch-batch.md"

if str(CURRENT_DIR) not in sys.path:
    sys.path.insert(0, str(CURRENT_DIR))

from generate_etw_stackwalk_capture_plan import build_capture_plan  # noqa: E402
from generate_etw_stackwalk_capture_plan import load_config  # noqa: E402
from generate_etw_stackwalk_capture_plan import load_runner_config  # noqa: E402
from generate_etw_stackwalk_capture_plan import profile_by_id  # noqa: E402


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
    path.write_text(text.rstrip() + "\n", encoding="utf-8")


def load_json(path: Path) -> dict[str, Any]:
    payload = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(payload, dict):
        raise ValueError(f"{path} JSON payload is not an object")
    return payload


def queue_entry_map(queue_payload: dict[str, Any]) -> dict[str, dict[str, Any]]:
    return {
        str(entry.get("candidate_id") or ""): entry
        for entry in queue_payload.get("entries") or []
        if str(entry.get("candidate_id") or "").strip()
    }


def gate_entry_map(gates_payload: dict[str, Any]) -> dict[str, dict[str, Any]]:
    entries = gates_payload.get("entries")
    if entries is None:
        entries = gates_payload.get("gates") or []
    return {
        str(entry.get("candidate_id") or ""): entry
        for entry in entries
        if str(entry.get("candidate_id") or "").strip()
    }


def mapped_runner_entries(
    runner_config: dict[str, Any],
    *,
    candidate_ids: set[str] | None = None,
) -> list[dict[str, Any]]:
    selected: list[dict[str, Any]] = []
    for lane_name, lane_entries in runner_config.items():
        if not isinstance(lane_entries, dict):
            continue
        for candidate_id, entry in lane_entries.items():
            if not isinstance(entry, dict):
                continue
            profile_id = str(entry.get("etw_stackwalk_profile_id") or "").strip()
            if not profile_id:
                continue
            if candidate_ids and candidate_id not in candidate_ids:
                continue
            selected.append(
                {
                    "candidate_id": candidate_id,
                    "lane": str(lane_name),
                    "profile_id": profile_id,
                    "runner_script": entry.get("script"),
                    "runner_args": entry.get("args") or [],
                    "required_capabilities": entry.get("required_capabilities") or [],
                    "supported_backend_types": entry.get("supported_backend_types") or [],
                    "execution_context_defaults": entry.get("execution_context_defaults") or {},
                }
            )
    selected.sort(key=lambda item: str(item.get("candidate_id") or ""))
    return selected


def shell_join(argv: list[str]) -> str:
    return " ".join(f'"{part}"' if " " in part else part for part in argv)


def actionability_for(gate_entry: dict[str, Any]) -> str:
    blockers = [str(item).lower() for item in (gate_entry.get("promotion_blockers") or [])]
    if str(gate_entry.get("next_missing_layer") or "") == "intentional-hold":
        return "hold"
    if "intentional-hold" in blockers:
        return "hold"
    promotion_state = str(gate_entry.get("promotion_state") or "")
    if promotion_state == "blocked":
        return "active"
    if promotion_state == "promoted":
        return "complete"
    return "active"


def next_action_hint(gate_entry: dict[str, Any], capture_ready: bool) -> str:
    blockers = [str(item) for item in (gate_entry.get("promotion_blockers") or [])]
    lowered = " | ".join(item.lower() for item in blockers)
    if not capture_ready:
        return "Fix the ETW stackwalk profile or runner config before dispatching the guest lane."
    if "intentional-hold" in lowered and "seeding-path" in lowered:
        return "Reopen only when a boot/init reader or registry seeding caller pivot becomes available."
    if "intentional-hold" in lowered and "primary-current-build-doc" in lowered:
        return "Reopen only if we land a primary current-build Microsoft source for the value semantics."
    if "intentional-hold" in lowered:
        return "Candidate is on intentional hold; run this lane only when we explicitly reopen the investigation."
    if str(gate_entry.get("promotion_state") or "") == "blocked":
        return "Ready to dispatch when we want another focused ETW caller-stack capture."
    return "Capture plan is ready."


def build_dispatch_item(
    *,
    mapping: dict[str, Any],
    profile_config: dict[str, Any],
    queue_map: dict[str, dict[str, Any]],
    gate_map: dict[str, dict[str, Any]],
    generated_utc: str,
) -> dict[str, Any]:
    candidate_id = str(mapping.get("candidate_id") or "")
    profile = profile_by_id(profile_config, str(mapping.get("profile_id") or ""))
    plan = build_capture_plan(
        profile,
        run_id=str(profile.get("default_run_id") or candidate_id),
        candidate_id=candidate_id,
        generated_utc=generated_utc,
    )
    queue_entry = queue_map.get(candidate_id, {})
    gate_entry = gate_map.get(candidate_id, {})
    actionability = actionability_for(gate_entry)
    capture_ready = str(plan.get("plan_status") or "") == "ready"
    dispatch_recommended = capture_ready and actionability != "hold"
    runner_cmd = [
        "python3",
        "scripts/vm-kvm/run-guest-etw-stackwalk-capture.py",
        "--candidate-id",
        candidate_id,
        "--ingest-to-repo",
        "--refresh-ghidra",
    ]
    config_cmd = [
        "python3",
        "scripts/vm-kvm/run-guest-etw-stackwalk-capture.py",
        "--candidate-id",
        candidate_id,
        "--print-effective-config",
    ]
    return {
        "candidate_id": candidate_id,
        "feature_area": queue_entry.get("feature_area"),
        "queue_state": queue_entry.get("state"),
        "promotion_state": gate_entry.get("promotion_state"),
        "next_missing_layer": gate_entry.get("next_missing_layer"),
        "actionability": actionability,
        "promotion_blockers": gate_entry.get("promotion_blockers") or [],
        "key_path": queue_entry.get("key_path"),
        "value_name": queue_entry.get("value_name"),
        "lane": mapping.get("lane"),
        "runner_script": mapping.get("runner_script"),
        "runner_args": mapping.get("runner_args") or [],
        "required_capabilities": mapping.get("required_capabilities") or [],
        "supported_backend_types": mapping.get("supported_backend_types") or [],
        "execution_context_defaults": mapping.get("execution_context_defaults") or {},
        "profile_id": profile.get("profile_id"),
        "profile_description": profile.get("description"),
        "capture_ready": capture_ready,
        "dispatch_recommended": dispatch_recommended,
        "next_action_hint": next_action_hint(gate_entry, capture_ready),
        "effective_config_command_argv": config_cmd,
        "effective_config_command": shell_join(config_cmd),
        "dispatch_command_argv": runner_cmd,
        "dispatch_command": shell_join(runner_cmd),
        "capture_plan": {
            "plan_status": plan.get("plan_status"),
            "errors": plan.get("errors") or [],
            "run": plan.get("run") or {},
            "target": plan.get("target") or {},
            "stack_capture": plan.get("stack_capture") or {},
        },
    }


def build_dispatch_batch(
    *,
    profile_config: dict[str, Any],
    runner_config: dict[str, Any],
    queue_payload: dict[str, Any],
    gates_payload: dict[str, Any],
    candidate_ids: set[str] | None = None,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    queue_map = queue_entry_map(queue_payload)
    gate_map = gate_entry_map(gates_payload)
    mapped = mapped_runner_entries(runner_config, candidate_ids=candidate_ids)
    items = [
        build_dispatch_item(
            mapping=item,
            profile_config=profile_config,
            queue_map=queue_map,
            gate_map=gate_map,
            generated_utc=generated_utc,
        )
        for item in mapped
    ]
    ready_capture_count = sum(1 for item in items if item.get("capture_ready"))
    dispatch_recommended_count = sum(1 for item in items if item.get("dispatch_recommended"))
    hold_count = sum(1 for item in items if item.get("actionability") == "hold")
    active_count = sum(1 for item in items if item.get("actionability") == "active")
    errors = [
        error
        for item in items
        for error in (item.get("capture_plan") or {}).get("errors", [])
    ]
    batch_status = "empty"
    if items:
        batch_status = "ready" if not errors else "blocked"
    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "batch_status": batch_status,
        "profile_config_path": portable_path(PROFILE_CONFIG_PATH),
        "runner_config_path": portable_path(RUNNER_CONFIG_PATH),
        "queue_path": portable_path(QUEUE_PATH),
        "promotion_gates_path": portable_path(PROMOTION_GATES_PATH),
        "mapped_candidate_count": len(items),
        "ready_capture_count": ready_capture_count,
        "dispatch_recommended_count": dispatch_recommended_count,
        "active_candidate_count": active_count,
        "hold_candidate_count": hold_count,
        "profiles_used": sorted({str(item.get("profile_id") or "") for item in items if str(item.get("profile_id") or "")}),
        "errors": errors,
        "items": items,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    lines = [
        "# ETW Stackwalk Dispatch Batch",
        "",
        f"- Batch status: `{payload.get('batch_status')}`",
        f"- Mapped candidates: `{payload.get('mapped_candidate_count')}`",
        f"- Ready capture configs: `{payload.get('ready_capture_count')}`",
        f"- Dispatch recommended now: `{payload.get('dispatch_recommended_count')}`",
        f"- Active candidates: `{payload.get('active_candidate_count')}`",
        f"- Intentional-hold candidates: `{payload.get('hold_candidate_count')}`",
        f"- Profiles used: `{', '.join(payload.get('profiles_used') or [])}`",
        "",
        "## Candidates",
        "",
    ]
    for item in payload.get("items") or []:
        run = (item.get("capture_plan") or {}).get("run") or {}
        target = (item.get("capture_plan") or {}).get("target") or {}
        stack_capture = (item.get("capture_plan") or {}).get("stack_capture") or {}
        lines.extend(
            [
                f"### {item.get('candidate_id')}",
                "",
                f"- Queue state: `{item.get('queue_state')}`",
                f"- Promotion state: `{item.get('promotion_state')}`",
                f"- Missing layer: `{item.get('next_missing_layer')}`",
                f"- Actionability: `{item.get('actionability')}`",
                f"- Blockers: `{item.get('promotion_blockers')}`",
                f"- Profile: `{item.get('profile_id')}`",
                f"- Registry target: `{target.get('registry_path')}` / `{target.get('value_name')}`",
                f"- Run id: `{run.get('run_id')}`",
                f"- Host ETL path: `{run.get('host_etl_repo_path')}`",
                f"- Stackwalk events: `{', '.join(stack_capture.get('stackwalk_events') or [])}`",
                f"- Dispatch recommended: `{item.get('dispatch_recommended')}`",
                f"- Next action hint: `{item.get('next_action_hint')}`",
                "",
                "```bash",
                str(item.get("effective_config_command") or ""),
                "```",
                "",
                "```bash",
                str(item.get("dispatch_command") or ""),
                "```",
                "",
            ]
        )
    if payload.get("errors"):
        lines.extend(["## Errors", ""])
        for error in payload.get("errors") or []:
            lines.append(f"- {error}")
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Generate a queue-aware ETW stackwalk dispatch batch for candidate-driven guest captures."
    )
    parser.add_argument("--profile-config", type=Path, default=PROFILE_CONFIG_PATH)
    parser.add_argument("--runner-config", type=Path, default=RUNNER_CONFIG_PATH)
    parser.add_argument("--queue", type=Path, default=QUEUE_PATH)
    parser.add_argument("--promotion-gates", type=Path, default=PROMOTION_GATES_PATH)
    parser.add_argument("--candidate-id", action="append", default=[], help="Limit output to one or more mapped candidates.")
    parser.add_argument("--output", type=Path, default=OUTPUT_JSON)
    parser.add_argument("--markdown-output", type=Path, default=OUTPUT_MD)
    args = parser.parse_args()

    candidate_ids = {item for item in args.candidate_id if str(item).strip()} or None
    payload = build_dispatch_batch(
        profile_config=load_config(args.profile_config),
        runner_config=load_runner_config(args.runner_config),
        queue_payload=load_json(args.queue),
        gates_payload=load_json(args.promotion_gates),
        candidate_ids=candidate_ids,
    )
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(json.dumps({"output": portable_path(args.output), "mapped_candidate_count": payload["mapped_candidate_count"]}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
