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


def run(cmd: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(cmd, cwd=str(cwd), check=True, capture_output=True, text=True)


def annotate_process_error(exc: subprocess.CalledProcessError, *, stage: str) -> subprocess.CalledProcessError:
    setattr(exc, "stage", stage)
    return exc


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
    output_name: str,
    registry_path: str,
    value_name: str,
    prepare_launch_transport: str,
    post_reboot_launch_transport: str,
    exc: subprocess.CalledProcessError,
) -> dict[str, object]:
    return write_summary_contract(
        summary_path,
        {
            "summary_path": str(summary_path),
            "output_name": output_name,
            "registry_path": registry_path,
            "value_name": value_name,
            "prepare_launch_transport": prepare_launch_transport,
            "post_reboot_launch_transport": post_reboot_launch_transport,
            "status": "error",
            "host_step": getattr(exc, "stage", None),
            "exit_code": exc.returncode,
            "command": [str(part) for part in exc.cmd] if isinstance(exc.cmd, list) else str(exc.cmd),
            "error": format_process_error(exc),
            "summary_source": "host-step-failure",
        },
        default_error_kind="reboot-observation-host-step-error",
        default_recovery_action="rerun-reboot-observation",
        default_transport_blocker="host-step-error",
        default_guest_health="unknown",
    )


def launch_generated_script(
    *,
    repo_root: Path,
    generated_path: Path,
    guest_launcher: str,
    guest_scripts_root: str,
    marker_name: str,
    args: argparse.Namespace,
) -> str:
    if args.launch_transport in {"auto", "qga"}:
        qga_cmd = [
            sys.executable,
            str(repo_root / "scripts" / "vm-kvm" / "qga-run-powershell.py"),
            "--domain",
            args.domain,
            "--connect",
            args.connect,
            "--script",
            str(generated_path),
            "--guest-dir",
            guest_scripts_root,
            "--no-wait",
        ]
        qga_result = None
        deadline = time.time() + max(args.qga_retry_seconds, 0)
        while True:
            qga_result = subprocess.run(qga_cmd, cwd=str(repo_root), capture_output=True, text=True)
            if qga_result.returncode == 0:
                break
            if time.time() >= deadline:
                break
            time.sleep(max(args.qga_retry_interval_seconds, 1))
        if qga_result.returncode == 0:
            return "qga"
        if args.launch_transport == "qga":
            raise annotate_process_error(
                subprocess.CalledProcessError(
                    qga_result.returncode,
                    qga_cmd,
                    output=qga_result.stdout,
                    stderr=qga_result.stderr,
                ),
                stage="qga-launch",
            )
        sys.stderr.write(
            f"[run-guest-reboot-observation] qga launch failed, falling back to send-key transport for {args.output_name}.\n"
        )
        if qga_result.stdout:
            sys.stderr.write(qga_result.stdout)
        if qga_result.stderr:
            sys.stderr.write(qga_result.stderr)

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
                str(Path(args.upload_dir).resolve()),
                "--guest-scripts-root",
                guest_scripts_root,
                "--delay-ms",
                args.delay_ms,
                "--marker-name",
                marker_name,
            ],
            cwd=repo_root,
        )
    except subprocess.CalledProcessError as exc:
        raise annotate_process_error(exc, stage="ensure-admin-shell") from exc
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
                guest_launcher,
            ],
            cwd=repo_root,
        )
    except subprocess.CalledProcessError as exc:
        raise annotate_process_error(exc, stage="type-to-guest") from exc
    return "send-key"


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
    output_name: str,
    registry_path: str,
    value_name: str,
    prepare_launch_transport: str,
    post_reboot_launch_transport: str,
) -> tuple[dict[str, object], bool]:
    try:
        return apply_summary_contract(read_json_object(summary_path, context="reboot observation summary")), False
    except (OSError, json.JSONDecodeError, ValueError) as exc:
        return (
            write_summary_contract(
                summary_path,
                {
                    "summary_path": str(summary_path),
                    "output_name": output_name,
                    "registry_path": registry_path,
                    "value_name": value_name,
                    "prepare_launch_transport": prepare_launch_transport,
                    "post_reboot_launch_transport": post_reboot_launch_transport,
                    "status": "error",
                    "summary_parse_error": str(exc),
                },
                default_error_kind="reboot-observation-summary-parse-error",
                default_recovery_action="rerun-reboot-observation",
                default_transport_blocker="summary-parse-error",
                default_guest_health="unknown",
            ),
            True,
        )


def load_stage_or_error(
    stage_path: Path,
    *,
    summary_path: Path,
    output_name: str,
    registry_path: str,
    value_name: str,
    prepare_launch_transport: str,
    post_reboot_launch_transport: str,
) -> tuple[dict[str, object] | None, dict[str, object] | None]:
    try:
        return read_json_object(stage_path, context="reboot observation stage"), None
    except (OSError, json.JSONDecodeError, ValueError) as exc:
        return None, write_summary_contract(
            summary_path,
            {
                "summary_path": str(summary_path),
                "stage_path": str(stage_path),
                "output_name": output_name,
                "registry_path": registry_path,
                "value_name": value_name,
                "prepare_launch_transport": prepare_launch_transport,
                "post_reboot_launch_transport": post_reboot_launch_transport,
                "status": "error",
                "summary_source": "stage-parse-error",
                "summary_parse_error": str(exc),
            },
            default_error_kind="reboot-observation-stage-parse-error",
            default_recovery_action="rerun-reboot-observation",
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
    stage_path: Path,
    output_name: str,
    registry_path: str,
    value_name: str,
    prepare_launch_transport: str,
    post_reboot_launch_transport: str,
    stage_payload: dict[str, object] | None,
    stall_seconds: int,
) -> dict[str, object]:
    return write_summary_contract(
        summary_path,
        {
            "summary_path": str(summary_path),
            "stage_path": str(stage_path),
            "output_name": output_name,
            "registry_path": registry_path,
            "value_name": value_name,
            "prepare_launch_transport": prepare_launch_transport,
            "post_reboot_launch_transport": post_reboot_launch_transport,
            "status": "timeout",
            "stage": stage_payload,
            "stall_seconds": stall_seconds,
            "summary_source": "stage-timeout",
        },
        default_error_kind="guest-stage-stall",
        default_recovery_action="inspect-reboot-observation-stage",
        default_transport_blocker="stage-stall",
        default_guest_health="degraded",
    )


def emit_first_artifact_timeout(
    *,
    summary_path: Path,
    stage_path: Path,
    output_name: str,
    registry_path: str,
    value_name: str,
    prepare_launch_transport: str,
    post_reboot_launch_transport: str,
    wait_seconds: int,
) -> dict[str, object]:
    return write_summary_contract(
        summary_path,
        {
            "summary_path": str(summary_path),
            "stage_path": str(stage_path),
            "output_name": output_name,
            "registry_path": registry_path,
            "value_name": value_name,
            "prepare_launch_transport": prepare_launch_transport,
            "post_reboot_launch_transport": post_reboot_launch_transport,
            "status": "timeout",
            "first_artifact_timeout_seconds": wait_seconds,
            "summary_source": "first-artifact-timeout",
        },
        default_error_kind="bridge-artifact-timeout",
        default_recovery_action="inspect-bridge-upload",
        default_transport_blocker="bridge-artifact-timeout",
        default_guest_health="unknown",
    )


def main() -> int:
    parser = argparse.ArgumentParser(description="Stage and run a reboot-backed registry observation inside the KVM guest.")
    parser.add_argument("--repo-root", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--domain", default=vm_domain("regprobe-win11-25h2-session"))
    parser.add_argument("--connect", default=vm_connect("qemu:///session"))
    parser.add_argument("--bridge-base-url", default=bridge_base_url("http://10.0.2.2:8766"))
    parser.add_argument("--upload-dir", default=default_upload_dir("/tmp/regprobe-bridge"))
    parser.add_argument("--guest-scripts-root", default=r"C:\RegProbe-Diag\bootstrap")
    parser.add_argument("--delay-ms", default="18")
    parser.add_argument("--wake-key", default="KEY_ENTER")
    parser.add_argument("--timeout-seconds", type=int, default=420)
    parser.add_argument("--prepare-timeout-seconds", type=int, default=180)
    parser.add_argument("--first-artifact-timeout-seconds", type=int, default=120)
    parser.add_argument("--post-reboot-delay-seconds", type=int, default=20)
    parser.add_argument("--reboot-settle-seconds", type=int, default=45)
    parser.add_argument("--host-reboot-mode", choices=["reboot", "reset"], default="reboot")
    parser.add_argument("--qga-retry-seconds", type=int, default=90)
    parser.add_argument("--qga-retry-interval-seconds", type=int, default=5)
    parser.add_argument("--registry-path", required=True)
    parser.add_argument("--value-name", required=True)
    parser.add_argument("--output-name", required=True)
    parser.add_argument("--launch-transport", choices=["auto", "qga", "send-key"], default="auto")
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    upload_dir = Path(args.upload_dir).resolve()
    upload_dir.mkdir(parents=True, exist_ok=True)
    before_path = upload_dir / f"{args.output_name}-before.json"
    before_powercfg_path = upload_dir / f"{args.output_name}-powercfg-a-before.txt"
    summary_path = upload_dir / f"{args.output_name}-summary.json"
    stage_path = upload_dir / f"{args.output_name}-stage.json"
    after_path = upload_dir / f"{args.output_name}-after.json"
    after_powercfg_path = upload_dir / f"{args.output_name}-powercfg-a-after.txt"
    for path in (before_path, before_powercfg_path, summary_path, stage_path, after_path, after_powercfg_path):
        if path.exists():
            path.unlink()

    if summary_path.exists():
        summary_path.unlink()

    generated_dir = repo_root / "dist" / "kvm-generated"
    generated_dir.mkdir(parents=True, exist_ok=True)

    ensure_guest_bridge(repo_root=repo_root, bridge_base_url=args.bridge_base_url, upload_root=upload_dir)
    guest_scripts_root = args.guest_scripts_root

    bridge = args.bridge_base_url.rstrip("/")
    generated_name = f"guest-reboot-observation-{args.output_name}.ps1"
    generated_path = generated_dir / generated_name

    command_lines = [
        "$ErrorActionPreference = 'Stop'",
        f"New-Item -ItemType Directory -Path {quote_ps(guest_scripts_root)} -Force | Out-Null",
        (
            f"Invoke-WebRequest -UseBasicParsing -Uri "
            f"{quote_ps(bridge + '/scripts/vm/guest-tools/run-reboot-observation.ps1')} "
            f"-OutFile {quote_ps(guest_scripts_root + r'\\run-reboot-observation.ps1')}"
        ),
        (
            f"& {quote_ps(guest_scripts_root + r'\\run-reboot-observation.ps1')} "
            f"-Stage prepare-reboot "
            f"-RegistryPath {quote_ps(args.registry_path)} "
            f"-ValueName {quote_ps(args.value_name)} "
            f"-OutputName {quote_ps(args.output_name)} "
            f"-UploadBaseUrl {quote_ps(bridge)} "
            f"-PostRebootDelaySeconds {args.post_reboot_delay_seconds} "
            f"-SkipTaskRegistration "
            f"-SkipGuestRestart"
        ),
    ]
    generated_path.write_text("\n".join(command_lines) + "\n", encoding="utf-8")

    guest_launcher = "\n".join(
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

    prepare_launch_transport = "not-started"
    post_reboot_launch_transport = "not-started"
    try:
        prepare_launch_transport = launch_generated_script(
            repo_root=repo_root,
            generated_path=generated_path,
            guest_launcher=guest_launcher,
            guest_scripts_root=guest_scripts_root,
            marker_name=f"{args.output_name}-admin-shell-ready",
            args=args,
        )
    except subprocess.CalledProcessError as exc:
        print(
            json.dumps(
                emit_host_step_error(
                    summary_path=summary_path,
                    output_name=args.output_name,
                    registry_path=args.registry_path,
                    value_name=args.value_name,
                    prepare_launch_transport=prepare_launch_transport,
                    post_reboot_launch_transport=post_reboot_launch_transport,
                    exc=exc,
                ),
                indent=2,
            )
        )
        return 1

    if not wait_for_file(before_path, args.prepare_timeout_seconds):
        print(
            json.dumps(
                apply_summary_contract(
                    {
                        "before_path": str(before_path),
                        "stage_path": str(stage_path),
                        "output_name": args.output_name,
                        "registry_path": args.registry_path,
                        "value_name": args.value_name,
                        "prepare_launch_transport": prepare_launch_transport,
                        "status": "prepare-timeout",
                        "summary_source": "reboot-observation-prepare-timeout",
                        "error_kind": "runner-timeout",
                        "recovery_action": "rerun-reboot-observation",
                        "transport_blocker": "timeout",
                        "guest_health": "unknown",
                    }
                ),
                indent=2,
            )
        )
        return 2

    stage_path.unlink(missing_ok=True)

    try:
        run(["virsh", "-c", args.connect, args.host_reboot_mode, args.domain], cwd=repo_root)
    except subprocess.CalledProcessError as exc:
        print(
            json.dumps(
                emit_host_step_error(
                    summary_path=summary_path,
                    output_name=args.output_name,
                    registry_path=args.registry_path,
                    value_name=args.value_name,
                    prepare_launch_transport=prepare_launch_transport,
                    post_reboot_launch_transport=post_reboot_launch_transport,
                    exc=annotate_process_error(exc, stage=f"host-{args.host_reboot_mode}"),
                ),
                indent=2,
            )
        )
        return 1
    time.sleep(args.reboot_settle_seconds)

    state_file = rf"C:\RegProbe-Diag\reboot-observation\{args.output_name}\state.json"
    generated_name = f"guest-reboot-observation-post-{args.output_name}.ps1"
    generated_path = generated_dir / generated_name
    command_lines = [
        "$ErrorActionPreference = 'Stop'",
        f"New-Item -ItemType Directory -Path {quote_ps(guest_scripts_root)} -Force | Out-Null",
        (
            f"Invoke-WebRequest -UseBasicParsing -Uri "
            f"{quote_ps(bridge + '/scripts/vm/guest-tools/run-reboot-observation.ps1')} "
            f"-OutFile {quote_ps(guest_scripts_root + r'\\run-reboot-observation.ps1')}"
        ),
        (
            f"& {quote_ps(guest_scripts_root + r'\\run-reboot-observation.ps1')} "
            f"-Stage post-reboot "
            f"-StateFile {quote_ps(state_file)}"
        ),
    ]
    generated_path.write_text("\n".join(command_lines) + "\n", encoding="utf-8")

    guest_launcher = "\n".join(
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

    try:
        post_reboot_launch_transport = launch_generated_script(
            repo_root=repo_root,
            generated_path=generated_path,
            guest_launcher=guest_launcher,
            guest_scripts_root=guest_scripts_root,
            marker_name=f"{args.output_name}-post-reboot-admin-shell-ready",
            args=args,
        )
    except subprocess.CalledProcessError as exc:
        print(
            json.dumps(
                emit_host_step_error(
                    summary_path=summary_path,
                    output_name=args.output_name,
                    registry_path=args.registry_path,
                    value_name=args.value_name,
                    prepare_launch_transport=prepare_launch_transport,
                    post_reboot_launch_transport=post_reboot_launch_transport,
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
                output_name=args.output_name,
                registry_path=args.registry_path,
                value_name=args.value_name,
                prepare_launch_transport=prepare_launch_transport,
                post_reboot_launch_transport=post_reboot_launch_transport,
            )
            if parse_failed:
                print(json.dumps(summary, indent=2))
                return 1
            payload = {
                "summary_path": str(summary_path),
                "stage_path": str(stage_path),
                "output_name": args.output_name,
                "registry_path": args.registry_path,
                "value_name": args.value_name,
                "prepare_launch_transport": prepare_launch_transport,
                "post_reboot_launch_transport": post_reboot_launch_transport,
                "status": summary.get("status", "unknown"),
                "reboot_observed": summary.get("reboot_observed"),
                "error_kind": summary.get("error_kind"),
                "recovery_action": summary.get("recovery_action"),
                "transport_blocker": summary.get("transport_blocker"),
                "guest_health": summary.get("guest_health"),
            }
            print(json.dumps(payload, indent=2))
            if summary.get("status") == "error":
                return 1
            return 0 if summary.get("reboot_observed") else 3

        if stage_path.exists():
            stage_payload, stage_parse_error = load_stage_or_error(
                stage_path,
                summary_path=summary_path,
                output_name=args.output_name,
                registry_path=args.registry_path,
                value_name=args.value_name,
                prepare_launch_transport=prepare_launch_transport,
                post_reboot_launch_transport=post_reboot_launch_transport,
            )
            if stage_parse_error is not None:
                print(json.dumps(stage_parse_error, indent=2))
                return 1
            assert stage_payload is not None
            last_stage_payload = stage_payload
            stage_status = str(stage_payload.get("status", "")).lower()
            if stage_status == "error":
                summary = write_summary_contract(
                    summary_path,
                    {
                        "summary_path": str(summary_path),
                        "stage_path": str(stage_path),
                        "output_name": args.output_name,
                        "registry_path": args.registry_path,
                        "value_name": args.value_name,
                        "prepare_launch_transport": prepare_launch_transport,
                        "post_reboot_launch_transport": post_reboot_launch_transport,
                        "status": "error",
                        "stage": stage_payload,
                        "error": stage_payload.get("error"),
                        "summary_source": "stage-error",
                    },
                    default_error_kind="guest-stage-error",
                    default_recovery_action="inspect-reboot-observation-stage",
                    default_transport_blocker="stage-error",
                    default_guest_health="degraded",
                )
                print(json.dumps(summary, indent=2))
                return 1
            if stage_status == "starting":
                started_at = stage_started_timestamp(stage_payload, stage_path)
                if started_at is not None and (time.time() - started_at) >= first_artifact_timeout_seconds:
                    summary = emit_stage_stall_timeout(
                        summary_path=summary_path,
                        stage_path=stage_path,
                        output_name=args.output_name,
                        registry_path=args.registry_path,
                        value_name=args.value_name,
                        prepare_launch_transport=prepare_launch_transport,
                        post_reboot_launch_transport=post_reboot_launch_transport,
                        stage_payload=stage_payload,
                        stall_seconds=first_artifact_timeout_seconds,
                    )
                    print(json.dumps(summary, indent=2))
                    return 2
        elif last_stage_payload is None and time.time() >= first_artifact_deadline:
            summary = emit_first_artifact_timeout(
                summary_path=summary_path,
                stage_path=stage_path,
                output_name=args.output_name,
                registry_path=args.registry_path,
                value_name=args.value_name,
                prepare_launch_transport=prepare_launch_transport,
                post_reboot_launch_transport=post_reboot_launch_transport,
                wait_seconds=first_artifact_timeout_seconds,
            )
            print(json.dumps(summary, indent=2))
            return 2

        time.sleep(2)

    timeout_summary = write_summary_contract(
        summary_path,
        {
            "summary_path": str(summary_path),
            "stage_path": str(stage_path),
            "output_name": args.output_name,
            "registry_path": args.registry_path,
            "value_name": args.value_name,
            "prepare_launch_transport": prepare_launch_transport,
            "post_reboot_launch_transport": post_reboot_launch_transport,
            "status": "timeout",
            "stage": last_stage_payload,
        },
        default_error_kind="runner-timeout",
        default_recovery_action="rerun-reboot-observation",
        default_transport_blocker="timeout",
        default_guest_health="unknown",
    )
    print(json.dumps(timeout_summary, indent=2))
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
