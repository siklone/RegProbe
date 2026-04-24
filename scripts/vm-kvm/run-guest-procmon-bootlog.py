#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
from datetime import datetime
from pathlib import Path

from guest_bridge import ensure_guest_bridge
from summary_contract_lib import apply_summary_contract, read_json_object, write_summary_contract
from vm_env import bridge_base_url, upload_dir as default_upload_dir, vm_connect, vm_domain


def quote_ps(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


def quote_ps_array(values: list[str]) -> str:
    return ", ".join(quote_ps(value) for value in values)


def run(cmd: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(cmd, cwd=str(cwd), check=True, capture_output=True, text=True)


def format_process_error(exc: subprocess.CalledProcessError) -> str:
    details = [f"command exited with code {exc.returncode}"]
    stdout = (exc.output or "").strip()
    stderr = (exc.stderr or "").strip()
    if stdout:
        details.append(f"stdout: {stdout}")
    if stderr:
        details.append(f"stderr: {stderr}")
    return " | ".join(details)


def emit_host_step_error(
    *,
    summary_path: Path,
    arm_summary_path: Path,
    collect_summary_path: Path,
    hits_path: Path,
    output_name: str,
    host_step: str,
    exc: subprocess.CalledProcessError,
) -> dict[str, object]:
    return write_summary_contract(
        summary_path,
        {
            "summary_arm_path": str(arm_summary_path),
            "summary_collect_path": str(collect_summary_path),
            "summary_path": str(summary_path),
            "hits_path": str(hits_path),
            "output_name": output_name,
            "status": "error",
            "host_step": host_step,
            "exit_code": exc.returncode,
            "command": [str(part) for part in exc.cmd] if isinstance(exc.cmd, list) else str(exc.cmd),
            "error": format_process_error(exc),
            "summary_source": "host-step-failure",
        },
        default_error_kind="procmon-host-step-error",
        default_recovery_action="rerun-procmon-bootlog",
        default_transport_blocker="host-step-error",
        default_guest_health="unknown",
    )


def wait_for_file(path: Path, timeout_seconds: int) -> bool:
    deadline = time.time() + timeout_seconds
    while time.time() < deadline:
        if path.exists():
            return True
        time.sleep(2)
    return path.exists()


def load_summary_or_error(
    summary_path: Path,
    *,
    arm_summary_path: Path,
    collect_summary_path: Path,
    output_name: str,
) -> tuple[dict[str, object], bool]:
    try:
        return apply_summary_contract(read_json_object(summary_path, context="procmon summary")), False
    except (OSError, json.JSONDecodeError, ValueError) as exc:
        return (
            write_summary_contract(
                summary_path,
                {
                    "summary_arm_path": str(arm_summary_path),
                    "summary_collect_path": str(collect_summary_path),
                    "summary_path": str(summary_path),
                    "output_name": output_name,
                    "status": "error",
                    "summary_parse_error": str(exc),
                },
                default_error_kind="procmon-summary-parse-error",
                default_recovery_action="rerun-procmon-bootlog",
                default_transport_blocker="summary-parse-error",
                default_guest_health="unknown",
            ),
            True,
        )


def load_stage_or_error(
    stage_path: Path,
    *,
    summary_path: Path,
    arm_summary_path: Path,
    collect_summary_path: Path,
    hits_path: Path,
    output_name: str,
) -> tuple[dict[str, object] | None, dict[str, object] | None]:
    try:
        return read_json_object(stage_path, context="procmon stage"), None
    except (OSError, json.JSONDecodeError, ValueError) as exc:
        return None, write_summary_contract(
            summary_path,
            {
                "summary_arm_path": str(arm_summary_path),
                "summary_collect_path": str(collect_summary_path),
                "summary_path": str(summary_path),
                "stage_path": str(stage_path),
                "hits_path": str(hits_path),
                "output_name": output_name,
                "status": "error",
                "summary_source": "stage-parse-error",
                "summary_parse_error": str(exc),
            },
            default_error_kind="procmon-stage-parse-error",
            default_recovery_action="rerun-procmon-bootlog",
            default_transport_blocker="summary-parse-error",
            default_guest_health="unknown",
        )


def parse_generated_utc_timestamp(value: object) -> float | None:
    if not isinstance(value, str) or not value.strip():
        return None
    normalized = value.strip().replace("Z", "+00:00")
    try:
        return datetime.fromisoformat(normalized).timestamp()
    except ValueError:
        return None


def stage_started_timestamp(stage_payload: dict[str, object], stage_path: Path) -> float | None:
    generated_utc = parse_generated_utc_timestamp(stage_payload.get("generated_utc"))
    if generated_utc is not None:
        return generated_utc
    try:
        return stage_path.stat().st_mtime
    except OSError:
        return None


def emit_stage_stall_timeout(
    *,
    summary_path: Path,
    arm_summary_path: Path,
    collect_summary_path: Path,
    stage_path: Path,
    hits_path: Path,
    output_name: str,
    stage_payload: dict[str, object] | None,
    stall_seconds: int,
) -> dict[str, object]:
    return write_summary_contract(
        summary_path,
        {
            "summary_arm_path": str(arm_summary_path),
            "summary_collect_path": str(collect_summary_path),
            "summary_path": str(summary_path),
            "stage_path": str(stage_path),
            "hits_path": str(hits_path),
            "output_name": output_name,
            "status": "timeout",
            "stage": stage_payload,
            "stall_seconds": stall_seconds,
            "summary_source": "stage-timeout",
        },
        default_error_kind="guest-stage-stall",
        default_recovery_action="inspect-procmon-stage",
        default_transport_blocker="stage-stall",
        default_guest_health="degraded",
    )


def emit_first_artifact_timeout(
    *,
    summary_path: Path,
    arm_summary_path: Path,
    collect_summary_path: Path,
    stage_path: Path,
    hits_path: Path,
    output_name: str,
    wait_seconds: int,
) -> dict[str, object]:
    return write_summary_contract(
        summary_path,
        {
            "summary_arm_path": str(arm_summary_path),
            "summary_collect_path": str(collect_summary_path),
            "summary_path": str(summary_path),
            "stage_path": str(stage_path),
            "hits_path": str(hits_path),
            "output_name": output_name,
            "status": "timeout",
            "first_artifact_timeout_seconds": wait_seconds,
            "summary_source": "first-artifact-timeout",
        },
        default_error_kind="bridge-artifact-timeout",
        default_recovery_action="inspect-bridge-upload",
        default_transport_blocker="bridge-artifact-timeout",
        default_guest_health="unknown",
    )


def build_guest_launcher(guest_scripts_root: str, bridge: str, generated_name: str) -> str:
    return "\n".join(
        [
            f"New-Item -ItemType Directory -Path {quote_ps(guest_scripts_root)} -Force | Out-Null",
            (
                f"Invoke-WebRequest -UseBasicParsing -Uri "
                f"{quote_ps(bridge + '/dist/kvm-generated/' + generated_name)} "
                f"-OutFile {quote_ps(guest_scripts_root + '\\\\' + generated_name)}"
            ),
            f"powershell -NoProfile -ExecutionPolicy Bypass -File {quote_ps(guest_scripts_root + '\\\\' + generated_name)}",
        ]
    )


def main() -> int:
    parser = argparse.ArgumentParser(description="Stage and run a Procmon boot-log capture inside the KVM guest.")
    parser.add_argument("--repo-root", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--domain", default=vm_domain("regprobe-win11-25h2-session"))
    parser.add_argument("--connect", default=vm_connect("qemu:///session"))
    parser.add_argument("--bridge-base-url", default=bridge_base_url("http://10.0.2.2:8766"))
    parser.add_argument("--upload-dir", default=default_upload_dir("/tmp/regprobe-bridge"))
    parser.add_argument("--guest-scripts-root", default=r"C:\RegProbe-Diag\bootstrap")
    parser.add_argument("--delay-ms", default="18")
    parser.add_argument("--wake-key", default="KEY_ENTER")
    parser.add_argument("--prepare-timeout-seconds", type=int, default=180)
    parser.add_argument("--timeout-seconds", type=int, default=420)
    parser.add_argument("--first-artifact-timeout-seconds", type=int, default=120)
    parser.add_argument("--reboot-settle-seconds", type=int, default=55)
    parser.add_argument("--host-reboot-mode", choices=["reboot", "reset"], default="reboot")
    parser.add_argument("--registry-path", required=True)
    parser.add_argument("--value-name", required=True)
    parser.add_argument("--output-name", required=True)
    parser.add_argument("--match-fragment", action="append", default=[])
    parser.add_argument("--process-name", action="append", default=[])
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    upload_dir = Path(args.upload_dir).resolve()
    upload_dir.mkdir(parents=True, exist_ok=True)
    generated_dir = repo_root / "dist" / "kvm-generated"
    generated_dir.mkdir(parents=True, exist_ok=True)

    arm_summary_path = upload_dir / f"{args.output_name}-summary-arm.json"
    collect_summary_path = upload_dir / f"{args.output_name}-summary-collect.json"
    summary_path = upload_dir / f"{args.output_name}-summary.json"
    stage_path = upload_dir / f"{args.output_name}-stage.json"
    hits_path = upload_dir / f"{args.output_name}.hits.csv"
    for path in (arm_summary_path, collect_summary_path, summary_path, stage_path, hits_path):
        if path.exists():
            path.unlink()

    ensure_guest_bridge(repo_root=repo_root, bridge_base_url=args.bridge_base_url, upload_root=upload_dir)
    guest_scripts_root = args.guest_scripts_root
    try:
        run(
            [
                sys.executable,
                str(repo_root / "scripts" / "vm-kvm" / "ensure-guest-admin-shell.py"),
                "--repo-root",
                str(repo_root),
                "--domain",
                args.domain,
                "--connect",
                args.connect,
                "--bridge-base-url",
                args.bridge_base_url,
                "--upload-dir",
                str(upload_dir),
                "--guest-scripts-root",
                guest_scripts_root,
                "--delay-ms",
                args.delay_ms,
                "--marker-name",
                f"{args.output_name}-bootlog-arm-ready",
            ],
            cwd=repo_root,
        )
    except subprocess.CalledProcessError as exc:
        print(
            json.dumps(
                emit_host_step_error(
                    summary_path=summary_path,
                    arm_summary_path=arm_summary_path,
                    collect_summary_path=collect_summary_path,
                    hits_path=hits_path,
                    output_name=args.output_name,
                    host_step="ensure-admin-shell-arm",
                    exc=exc,
                ),
                indent=2,
            )
        )
        return 1

    bridge = args.bridge_base_url.rstrip("/")
    arm_name = f"guest-procmon-bootlog-arm-{args.output_name}.ps1"
    arm_path = generated_dir / arm_name
    arm_lines = [
        "$ErrorActionPreference = 'Stop'",
        f"New-Item -ItemType Directory -Path {quote_ps(guest_scripts_root)} -Force | Out-Null",
        (
            f"Invoke-WebRequest -UseBasicParsing -Uri "
            f"{quote_ps(bridge + '/scripts/vm/guest-tools/run-procmon-bootlog-probe.ps1')} "
            f"-OutFile {quote_ps(guest_scripts_root + r'\\run-procmon-bootlog-probe.ps1')}"
        ),
    ]
    arm_command = [
        "&",
        quote_ps(guest_scripts_root + r"\run-procmon-bootlog-probe.ps1"),
        "-Stage",
        quote_ps("arm"),
        "-RegistryPath",
        quote_ps(args.registry_path),
        "-ValueName",
        quote_ps(args.value_name),
        "-OutputName",
        quote_ps(args.output_name),
        "-UploadBaseUrl",
        quote_ps(bridge),
    ]
    if args.match_fragment:
        arm_command.extend(["-MatchFragments", quote_ps_array(args.match_fragment)])
    if args.process_name:
        arm_command.extend(["-ProcessNames", quote_ps_array(args.process_name)])
    arm_lines.append(" ".join(arm_command))
    arm_path.write_text("\n".join(arm_lines) + "\n", encoding="utf-8")

    try:
        run(
            [
                sys.executable,
                str(repo_root / "scripts" / "vm-kvm" / "type-to-guest.py"),
                args.domain,
                "--connect",
                args.connect,
                "--delay-ms",
                args.delay_ms,
                "--wake-key",
                args.wake_key,
                "--enter",
                build_guest_launcher(guest_scripts_root, bridge, arm_name),
            ],
            cwd=repo_root,
        )
    except subprocess.CalledProcessError as exc:
        print(
            json.dumps(
                emit_host_step_error(
                    summary_path=summary_path,
                    arm_summary_path=arm_summary_path,
                    collect_summary_path=collect_summary_path,
                    hits_path=hits_path,
                    output_name=args.output_name,
                    host_step="launch-arm-script",
                    exc=exc,
                ),
                indent=2,
            )
        )
        return 1

    if not wait_for_file(arm_summary_path, args.prepare_timeout_seconds):
        print(
            json.dumps(
                apply_summary_contract(
                    {
                        "summary_arm_path": str(arm_summary_path),
                        "output_name": args.output_name,
                        "status": "prepare-timeout",
                        "summary_source": "procmon-prepare-timeout",
                        "error_kind": "runner-timeout",
                        "recovery_action": "rerun-procmon-bootlog",
                        "transport_blocker": "timeout",
                        "guest_health": "unknown",
                    }
                ),
                indent=2,
            )
        )
        return 2

    try:
        run(["virsh", "-c", args.connect, args.host_reboot_mode, args.domain], cwd=repo_root)
    except subprocess.CalledProcessError as exc:
        print(
            json.dumps(
                emit_host_step_error(
                    summary_path=summary_path,
                    arm_summary_path=arm_summary_path,
                    collect_summary_path=collect_summary_path,
                    hits_path=hits_path,
                    output_name=args.output_name,
                    host_step=f"host-{args.host_reboot_mode}",
                    exc=exc,
                ),
                indent=2,
            )
        )
        return 1
    time.sleep(args.reboot_settle_seconds)

    try:
        run(
            [
                sys.executable,
                str(repo_root / "scripts" / "vm-kvm" / "ensure-guest-admin-shell.py"),
                "--repo-root",
                str(repo_root),
                "--domain",
                args.domain,
                "--connect",
                args.connect,
                "--bridge-base-url",
                args.bridge_base_url,
                "--upload-dir",
                str(upload_dir),
                "--guest-scripts-root",
                guest_scripts_root,
                "--delay-ms",
                args.delay_ms,
                "--marker-name",
                f"{args.output_name}-bootlog-collect-ready",
            ],
            cwd=repo_root,
        )
    except subprocess.CalledProcessError as exc:
        print(
            json.dumps(
                emit_host_step_error(
                    summary_path=summary_path,
                    arm_summary_path=arm_summary_path,
                    collect_summary_path=collect_summary_path,
                    hits_path=hits_path,
                    output_name=args.output_name,
                    host_step="ensure-admin-shell-collect",
                    exc=exc,
                ),
                indent=2,
            )
        )
        return 1

    collect_name = f"guest-procmon-bootlog-collect-{args.output_name}.ps1"
    collect_path = generated_dir / collect_name
    state_file = rf"C:\RegProbe-Diag\procmon-bootlog\{args.output_name}\state.json"
    collect_lines = [
        "$ErrorActionPreference = 'Stop'",
        f"New-Item -ItemType Directory -Path {quote_ps(guest_scripts_root)} -Force | Out-Null",
        (
            f"Invoke-WebRequest -UseBasicParsing -Uri "
            f"{quote_ps(bridge + '/scripts/vm/guest-tools/run-procmon-bootlog-probe.ps1')} "
            f"-OutFile {quote_ps(guest_scripts_root + r'\\run-procmon-bootlog-probe.ps1')}"
        ),
        (
            f"& {quote_ps(guest_scripts_root + r'\\run-procmon-bootlog-probe.ps1')} "
            f"-Stage collect "
            f"-StateFile {quote_ps(state_file)} "
            f"-UploadBaseUrl {quote_ps(bridge)}"
        ),
    ]
    collect_path.write_text("\n".join(collect_lines) + "\n", encoding="utf-8")

    try:
        run(
            [
                sys.executable,
                str(repo_root / "scripts" / "vm-kvm" / "type-to-guest.py"),
                args.domain,
                "--connect",
                args.connect,
                "--delay-ms",
                args.delay_ms,
                "--wake-key",
                args.wake_key,
                "--enter",
                build_guest_launcher(guest_scripts_root, bridge, collect_name),
            ],
            cwd=repo_root,
        )
    except subprocess.CalledProcessError as exc:
        print(
            json.dumps(
                emit_host_step_error(
                    summary_path=summary_path,
                    arm_summary_path=arm_summary_path,
                    collect_summary_path=collect_summary_path,
                    hits_path=hits_path,
                    output_name=args.output_name,
                    host_step="launch-collect-script",
                    exc=exc,
                ),
                indent=2,
            )
        )
        return 1

    deadline = time.time() + args.timeout_seconds
    first_artifact_timeout_seconds = max(1, min(args.first_artifact_timeout_seconds, args.timeout_seconds))
    first_artifact_deadline = time.time() + first_artifact_timeout_seconds
    last_stage_payload: dict[str, object] | None = None
    while time.time() < deadline:
        if summary_path.exists():
            summary, parse_failed = load_summary_or_error(
                summary_path,
                arm_summary_path=arm_summary_path,
                collect_summary_path=collect_summary_path,
                output_name=args.output_name,
            )
            if parse_failed:
                print(json.dumps(summary, indent=2))
                return 1
            payload = {
                "summary_arm_path": str(arm_summary_path),
                "summary_collect_path": str(collect_summary_path),
                "summary_path": str(summary_path),
                "stage_path": str(stage_path),
                "hits_path": str(hits_path),
                "output_name": args.output_name,
                "status": summary.get("status"),
                "reboot_observed": summary.get("reboot_observed"),
                "csv_exists": summary.get("csv_exists"),
                "match_count": summary.get("match_count"),
                "csv_row_count": summary.get("csv_row_count"),
                "normalized_bundle_exists": summary.get("normalized_bundle_exists"),
                "normalization_status": summary.get("normalization_status"),
                "normalizer_name": summary.get("normalizer_name"),
                "error_kind": summary.get("error_kind"),
                "recovery_action": summary.get("recovery_action"),
                "transport_blocker": summary.get("transport_blocker"),
                "guest_health": summary.get("guest_health"),
                "error": summary.get("error"),
            }
            print(json.dumps(payload, indent=2))
            if summary.get("status") == "error" or summary.get("normalization_status") not in {None, "ok"}:
                return 1
            return 0

        if stage_path.exists():
            stage, stage_parse_error = load_stage_or_error(
                stage_path,
                summary_path=summary_path,
                arm_summary_path=arm_summary_path,
                collect_summary_path=collect_summary_path,
                hits_path=hits_path,
                output_name=args.output_name,
            )
            if stage_parse_error is not None:
                print(json.dumps(stage_parse_error, indent=2))
                return 1
            assert stage is not None
            last_stage_payload = stage
            stage_status = str(stage.get("status", "")).lower()
            if stage_status == "error":
                summary = write_summary_contract(
                    summary_path,
                    {
                        "summary_arm_path": str(arm_summary_path),
                        "summary_collect_path": str(collect_summary_path),
                        "summary_path": str(summary_path),
                        "stage_path": str(stage_path),
                        "hits_path": str(hits_path),
                        "output_name": args.output_name,
                        "status": "error",
                        "stage": stage,
                        "error": stage.get("error"),
                        "summary_source": "stage-error",
                    },
                    default_error_kind="guest-stage-error",
                    default_recovery_action="inspect-procmon-stage",
                    default_transport_blocker="stage-error",
                    default_guest_health="degraded",
                )
                print(json.dumps(summary, indent=2))
                return 1
            if stage_status == "starting":
                started_at = stage_started_timestamp(stage, stage_path)
                if started_at is not None and (time.time() - started_at) >= first_artifact_timeout_seconds:
                    summary = emit_stage_stall_timeout(
                        summary_path=summary_path,
                        arm_summary_path=arm_summary_path,
                        collect_summary_path=collect_summary_path,
                        stage_path=stage_path,
                        hits_path=hits_path,
                        output_name=args.output_name,
                        stage_payload=stage,
                        stall_seconds=first_artifact_timeout_seconds,
                    )
                    print(json.dumps(summary, indent=2))
                    return 2
        elif last_stage_payload is None and time.time() >= first_artifact_deadline:
            summary = emit_first_artifact_timeout(
                summary_path=summary_path,
                arm_summary_path=arm_summary_path,
                collect_summary_path=collect_summary_path,
                stage_path=stage_path,
                hits_path=hits_path,
                output_name=args.output_name,
                wait_seconds=first_artifact_timeout_seconds,
            )
            print(json.dumps(summary, indent=2))
            return 2

        time.sleep(2)

    timeout_summary = write_summary_contract(
        summary_path,
        {
            "summary_arm_path": str(arm_summary_path),
            "summary_collect_path": str(collect_summary_path),
            "summary_path": str(summary_path),
            "stage_path": str(stage_path),
            "output_name": args.output_name,
            "status": "timeout",
            "stage": last_stage_payload,
        },
        default_error_kind="runner-timeout",
        default_recovery_action="rerun-procmon-bootlog",
        default_transport_blocker="timeout",
        default_guest_health="unknown",
    )
    print(json.dumps(timeout_summary, indent=2))
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
