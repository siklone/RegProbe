#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
CONFIG_PATH = FRAMEWORK_ROOT / "config" / "etw-stackwalk-profiles.json"
RUNNER_CONFIG_PATH = FRAMEWORK_ROOT / "config" / "tweak-vm-runners.json"
OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-capture-plan.json"
MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-capture-plan.md"


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


def load_config(path: Path = CONFIG_PATH) -> dict[str, Any]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError(f"{path} JSON payload is not an object")
    return payload


def load_runner_config(path: Path = RUNNER_CONFIG_PATH) -> dict[str, Any]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError(f"{path} JSON payload is not an object")
    return payload


def profile_by_id(config: dict[str, Any], profile_id: str | None = None) -> dict[str, Any]:
    selected_id = profile_id or str(config.get("default_profile") or "")
    for profile in config.get("profiles") or []:
        if str(profile.get("profile_id") or "") == selected_id:
            return profile
    raise ValueError(f"Unknown ETW stackwalk profile: {selected_id}")


def profile_id_for_candidate(candidate_id: str, runner_config: dict[str, Any]) -> str | None:
    target_id = str(candidate_id or "").strip()
    if not target_id:
        return None
    for lane_entries in runner_config.values():
        if not isinstance(lane_entries, dict):
            continue
        entry = lane_entries.get(target_id)
        if not isinstance(entry, dict):
            continue
        profile_id = str(entry.get("etw_stackwalk_profile_id") or "").strip()
        if profile_id:
            return profile_id
    return None


def sanitize_run_id(value: str) -> str:
    cleaned = re.sub(r"[^A-Za-z0-9_.-]+", "-", value.strip())
    return cleaned.strip("-") or "registry-stackwalk"


def unique_strings(values: list[Any]) -> list[str]:
    seen: set[str] = set()
    result: list[str] = []
    for value in values:
        text = str(value or "").strip()
        if not text or text.lower() in seen:
            continue
        seen.add(text.lower())
        result.append(text)
    return result


def validate_profile(profile: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    if str(profile.get("tool") or "").lower() != "xperf":
        errors.append("Only xperf stackwalk profiles are supported by this planner.")
    if not unique_strings(profile.get("kernel_flags") or []):
        errors.append("kernel_flags must include at least one xperf kernel flag.")
    if not unique_strings(profile.get("stackwalk_events") or []):
        errors.append("stackwalk_events must include at least one registry event.")
    if not str(profile.get("default_output_root") or "").strip():
        errors.append("default_output_root is required.")
    return errors


def windows_join(*parts: str) -> str:
    cleaned = [part.strip("\\") for part in parts if str(part or "").strip()]
    if not cleaned:
        return ""
    first = cleaned[0]
    if re.match(r"^[A-Za-z]:$", first):
        first = first + "\\"
    return first.rstrip("\\") + "\\" + "\\".join(part for part in cleaned[1:])


def build_capture_plan(
    profile: dict[str, Any],
    *,
    run_id: str,
    output_root: str | None = None,
    duration_seconds: int | None = None,
    candidate_id: str | None = None,
    registry_path: str | None = None,
    value_name: str | None = None,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    errors = validate_profile(profile)
    target_defaults = profile.get("target_defaults") or {}
    normalized_run_id = sanitize_run_id(run_id or str(profile.get("default_run_id") or "registry-stackwalk-operator"))
    base_output_root = str(output_root or profile.get("default_output_root") or "").rstrip("\\")
    run_output_root = windows_join(base_output_root, normalized_run_id)
    raw_etl_path = windows_join(run_output_root, f"{normalized_run_id}.raw.etl")
    etl_path = windows_join(run_output_root, f"{normalized_run_id}.etl")
    xml_path = windows_join(run_output_root, f"{normalized_run_id}.xml")
    host_etl_ref = f"evidence/raw/etw-stackwalk/{normalized_run_id}/{normalized_run_id}.etl"
    target_registry_path = registry_path or target_defaults.get("registry_path")
    target_value_name = value_name or target_defaults.get("value_name")
    kernel_flags = unique_strings(profile.get("kernel_flags") or [])
    stackwalk_events = unique_strings(profile.get("stackwalk_events") or [])
    buffer = profile.get("buffer") or {}
    duration = int(duration_seconds or profile.get("default_duration_seconds") or 60)
    xperf_start = [
        "xperf",
        "-on",
        "+".join(kernel_flags),
        "-stackwalk",
        "+".join(stackwalk_events),
        "-BufferSize",
        str(int(buffer.get("size_kb") or 1024)),
        "-MinBuffers",
        str(int(buffer.get("min_buffers") or 64)),
        "-MaxBuffers",
        str(int(buffer.get("max_buffers") or 256)),
        "-f",
        raw_etl_path,
    ]
    plan_status = "ready" if not errors else "blocked"
    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "plan_status": plan_status,
        "errors": errors,
        "profile_id": profile.get("profile_id"),
        "description": profile.get("description"),
        "capture_phase": profile.get("capture_phase"),
        "provider": profile.get("provider") or {},
        "target": {
            "candidate_id": candidate_id,
            "registry_path": target_registry_path,
            "value_name": target_value_name,
        },
        "run": {
            "run_id": normalized_run_id,
            "duration_seconds": duration,
            "output_root": run_output_root,
            "raw_etl_path": raw_etl_path,
            "etl_path": etl_path,
            "xml_path": xml_path,
            "host_etl_repo_path": host_etl_ref,
        },
        "stack_capture": {
            "expected": bool((profile.get("stack_capture") or {}).get("expected")),
            "stackwalk_events": stackwalk_events,
            "source_fields": (profile.get("stack_capture") or {}).get("source_fields") or [],
            "normalized_bundle_field": (profile.get("postprocess") or {}).get("normalized_bundle_field"),
        },
        "commands": {
            "preflight": [
                ["where", "xperf.exe"],
                ["where", "tracerpt.exe"],
            ],
            "prepare": [
                ["powershell", "-NoProfile", "-Command", f"New-Item -ItemType Directory -Force -Path '{run_output_root}' | Out-Null"],
                ["xperf", "-stop"],
            ],
            "start": xperf_start,
            "wait": ["powershell", "-NoProfile", "-Command", f"Start-Sleep -Seconds {duration}"],
            "stop": ["xperf", "-d", etl_path],
            "parse_xml": ["tracerpt", etl_path, "-o", xml_path, "-of", "XML"],
            "repo_parse": [
                "python3",
                "registry-research-framework/scripts/parse_etl_registry_touches.py",
                "--input",
                host_etl_ref,
            ],
            "repo_guest_capture": [
                "python3",
                "scripts/vm-kvm/run-guest-etw-stackwalk-capture.py",
                "--run-id",
                normalized_run_id,
                "--duration-seconds",
                str(duration),
                "--registry-path",
                str(target_registry_path or ""),
                "--value-name",
                str(target_value_name or ""),
                "--ingest-to-repo",
                "--refresh-ghidra",
            ],
        },
        "operator_notes": [
            "Run from an elevated Windows shell with Windows Performance Toolkit installed.",
            f"Copy the final ETL into {host_etl_ref} before running repo_parse.",
            "The start command enables registry stack walking; the parser expects tracerpt XML fields such as Stack or CallStack.",
            "The repo_guest_capture command is the preferred host-side lane when the focused KVM guest is available because it launches the guest helper, ingests the ETL/XML into the repo, and refreshes caller-stack follow-up automatically.",
            "If caller_stack remains empty, rerun with a narrower trigger window or move the ETL to WPA/xperf for stack inspection.",
        ],
    }


def shell_join(command: list[str]) -> str:
    return " ".join(f'"{part}"' if re.search(r"\s", part) else part for part in command)


def render_markdown(payload: dict[str, Any]) -> str:
    run = payload.get("run") or {}
    stack_capture = payload.get("stack_capture") or {}
    commands = payload.get("commands") or {}
    lines = [
        "# ETW Stackwalk Capture Plan",
        "",
        f"- Status: `{payload.get('plan_status')}`",
        f"- Profile: `{payload.get('profile_id')}`",
        f"- Capture phase: `{payload.get('capture_phase')}`",
        f"- Run id: `{run.get('run_id')}`",
        f"- Duration seconds: `{run.get('duration_seconds')}`",
        f"- Candidate id: `{(payload.get('target') or {}).get('candidate_id')}`",
        f"- Registry path: `{(payload.get('target') or {}).get('registry_path')}`",
        f"- Value name: `{(payload.get('target') or {}).get('value_name')}`",
        f"- Stack expected: `{stack_capture.get('expected')}`",
        f"- Stackwalk events: `{', '.join(stack_capture.get('stackwalk_events') or [])}`",
        "",
        "## Commands",
        "",
    ]
    for name in ("preflight", "prepare", "start", "wait", "stop", "parse_xml", "repo_parse", "repo_guest_capture"):
        command = commands.get(name)
        if not command:
            continue
        lines.append(f"### {name}")
        lines.append("")
        if command and isinstance(command[0], list):
            for item in command:
                lines.append(f"```powershell\n{shell_join(item)}\n```")
        else:
            lines.append(f"```powershell\n{shell_join(command)}\n```")
        lines.append("")
    notes = payload.get("operator_notes") or []
    lines.extend(["## Notes", ""])
    for note in notes:
        lines.append(f"- {note}")
    if payload.get("errors"):
        lines.extend(["", "## Errors", ""])
        for error in payload.get("errors") or []:
            lines.append(f"- {error}")
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate an operator-ready ETW registry stackwalk capture plan.")
    parser.add_argument("--config", type=Path, default=CONFIG_PATH)
    parser.add_argument("--runner-config", type=Path, default=RUNNER_CONFIG_PATH)
    parser.add_argument("--profile-id")
    parser.add_argument("--candidate-id")
    parser.add_argument("--run-id")
    parser.add_argument("--output-root")
    parser.add_argument("--duration-seconds", type=int)
    parser.add_argument("--registry-path")
    parser.add_argument("--value-name")
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=MARKDOWN_PATH)
    args = parser.parse_args()

    config = load_config(args.config)
    resolved_profile_id = args.profile_id
    if not resolved_profile_id and args.candidate_id:
        resolved_profile_id = profile_id_for_candidate(args.candidate_id, load_runner_config(args.runner_config))
        if not resolved_profile_id:
            raise ValueError(f"No ETW stackwalk profile mapping found for candidate: {args.candidate_id}")
    profile = profile_by_id(config, resolved_profile_id)
    payload = build_capture_plan(
        profile,
        run_id=args.run_id,
        output_root=args.output_root,
        duration_seconds=args.duration_seconds,
        candidate_id=args.candidate_id,
        registry_path=args.registry_path,
        value_name=args.value_name,
    )
    payload["config_path"] = portable_path(args.config)
    payload["runner_config_path"] = portable_path(args.runner_config)
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(json.dumps({"plan": portable_path(args.output), "status": payload.get("plan_status")}, indent=2))
    return 0 if payload.get("plan_status") == "ready" else 1


if __name__ == "__main__":
    raise SystemExit(main())
