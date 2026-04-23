#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

from command_json_lib import parse_command_json
from guest_bridge import ensure_guest_bridge
from summary_contract_lib import apply_summary_contract, read_json_object, write_summary_contract
from vm_env import bridge_base_url, upload_dir as default_upload_dir, vm_connect, vm_domain


def quote_ps(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


def quote_ps_array(values: list[str]) -> str:
    return ", ".join(quote_ps(value) for value in values)


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
    summary_arm_path: Path,
    stage_path: Path,
    hits_path: Path,
    output_name: str,
    arm_launch_transport: str,
    collect_launch_transport: str,
    exc: subprocess.CalledProcessError,
) -> dict[str, object]:
    return write_summary_contract(
        summary_path,
        {
            "summary_arm_path": str(summary_arm_path),
            "summary_path": str(summary_path),
            "stage_path": str(stage_path),
            "hits_path": str(hits_path),
            "output_name": output_name,
            "arm_launch_transport": arm_launch_transport,
            "collect_launch_transport": collect_launch_transport,
            "status": "error",
            "host_step": getattr(exc, "stage", None),
            "exit_code": exc.returncode,
            "command": [str(part) for part in exc.cmd] if isinstance(exc.cmd, list) else str(exc.cmd),
            "error": format_process_error(exc),
            "summary_source": "host-step-failure",
        },
        default_error_kind="wpr-host-step-error",
        default_recovery_action="rerun-wpr-boot-registry",
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
            f"[run-guest-wpr-boot-registry] qga launch failed, falling back to send-key transport for {args.output_name}.\n"
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
    active_summary_path: Path,
    *,
    summary_path: Path,
    summary_arm_path: Path,
    stage_path: Path,
    hits_path: Path,
    output_name: str,
    arm_launch_transport: str,
    collect_launch_transport: str,
) -> tuple[dict[str, object], bool]:
    try:
        return apply_summary_contract(read_json_object(active_summary_path, context="wpr summary")), False
    except (OSError, json.JSONDecodeError, ValueError) as exc:
        return (
            write_summary_contract(
                summary_path,
                {
                    "summary_arm_path": str(summary_arm_path),
                    "summary_path": str(active_summary_path),
                    "stage_path": str(stage_path),
                    "hits_path": str(hits_path),
                    "output_name": output_name,
                    "arm_launch_transport": arm_launch_transport,
                    "collect_launch_transport": collect_launch_transport,
                    "status": "error",
                    "summary_parse_error": str(exc),
                },
                default_error_kind="wpr-summary-parse-error",
                default_recovery_action="rerun-wpr-boot-registry",
                default_transport_blocker="summary-parse-error",
                default_guest_health="unknown",
            ),
            True,
        )


def load_arm_summary_or_error(
    summary_arm_path: Path,
    *,
    output_name: str,
    arm_launch_transport: str,
) -> tuple[dict[str, object] | None, dict[str, object] | None]:
    try:
        return read_json_object(summary_arm_path, context="wpr arm summary"), None
    except (OSError, json.JSONDecodeError, ValueError) as exc:
        return None, apply_summary_contract(
            {
                "summary_arm_path": str(summary_arm_path),
                "output_name": output_name,
                "arm_launch_transport": arm_launch_transport,
                "status": "error",
                "stage": "arm",
                "summary_source": "arm-summary-parse",
                "summary_parse_error": str(exc),
            },
            default_error_kind="wpr-arm-summary-parse-error",
            default_recovery_action="rerun-wpr-boot-registry",
            default_transport_blocker="summary-parse-error",
            default_guest_health="unknown",
        )


def describe_downloaded_file(path: Path) -> dict[str, object]:
    exists = path.exists()
    size_bytes = path.stat().st_size if exists else 0
    return {
        "exists": exists,
        "size_bytes": size_bytes,
        "is_zero_byte": exists and size_bytes == 0,
    }


def try_qga_download(
    *,
    repo_root: Path,
    args: argparse.Namespace,
    guest_path: str,
    host_path: Path,
) -> dict[str, object]:
    cmd = [
        sys.executable,
        str(repo_root / "scripts" / "vm-kvm" / "qga-get-file.py"),
        "--domain",
        args.domain,
        "--connect",
        args.connect,
        "--source",
        guest_path,
        "--destination",
        str(host_path),
        "--timeout",
        str(args.salvage_qga_timeout_seconds),
    ]
    result = subprocess.run(cmd, cwd=str(repo_root), capture_output=True, text=True)
    payload: dict[str, object] = {
        "guest_path": guest_path,
        "host_path": str(host_path),
        "returncode": result.returncode,
    }
    payload.update(describe_downloaded_file(host_path))
    if result.stdout:
        parsed = parse_command_json(result.stdout, stderr=result.stderr)
        if parsed.get("status") == "error" and parsed.get("stdout_parse_error"):
            payload["stdout"] = str(parsed.get("stdout") or result.stdout)
            payload["stdout_parse_error"] = parsed["stdout_parse_error"]
        else:
            payload["result"] = parsed
    if result.stderr:
        payload["stderr"] = result.stderr.strip()
    return payload


def inspect_hits_csv(path: Path, value_name: str) -> dict[str, object]:
    if not path.exists():
        return {
            "exists": False,
            "line_count": 0,
            "hit_line_count": 0,
            "contains_value_name": False,
        }

    text = path.read_text(encoding="utf-8-sig", errors="replace")
    lines = [line for line in text.splitlines() if line.strip()]
    data_lines = lines[1:] if lines else []
    return {
        "exists": True,
        "size_bytes": path.stat().st_size,
        "line_count": len(lines),
        "hit_line_count": len(data_lines),
        "contains_value_name": value_name.lower() in text.lower(),
    }


def split_registry_path(registry_path: str, value_name: str) -> dict[str, object]:
    normalized = registry_path.replace("/", "\\").replace("HKLM:\\", "HKLM\\").replace("HKCU:\\", "HKCU\\")
    for hive in ("HKLM", "HKCU", "HKCR", "HKU", "HKCC"):
        prefix = hive + "\\"
        if normalized.lower().startswith(prefix.lower()):
            return {
                "hive": hive,
                "key_path": normalized[len(prefix) :],
                "value_name": value_name,
            }
    return {
        "hive": None,
        "key_path": normalized or None,
        "value_name": value_name,
    }


def synthesize_empty_normalized_bundle(
    *,
    hits_csv_path: Path,
    bundle_path: Path,
    args: argparse.Namespace,
) -> dict[str, object]:
    path_parts = split_registry_path(args.registry_path, args.value_name)
    bundle = {
        "$schema": "registry-research-framework/schemas/normalized-registry-bundle.schema.json",
        "run_id": args.output_name,
        "source_tool": "wpr",
        "capture_phase": "boot",
        "generated_utc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "normalizer_name": "HostTimeoutSalvageNormalizer",
        "input_path": str(hits_csv_path),
        "status": "ok",
        "error_kind": None,
        "errors": [],
        "event_count": 0,
        "filtered_event_count": 0,
        "evidence_refs": [str(hits_csv_path), str(bundle_path)],
        "target": path_parts,
        "stack_capture": {
            "parser_supported": True,
            "captured_event_count": 0,
            "source_fields": ["Stack", "CallStack", "Call Stack", "StackTrace", "Stack Trace", "UserStack", "User Stack"],
        },
        "events": [],
        "salvage_note": "QGA timeout salvage retained a header-only hits CSV; no target registry events were present.",
    }
    bundle_path.write_text(json.dumps(bundle, indent=2) + "\n", encoding="utf-8")
    return {
        "created": True,
        "path": str(bundle_path),
        "event_count": 0,
        "normalizer_name": bundle["normalizer_name"],
    }


def salvage_timeout_artifacts(
    *,
    repo_root: Path,
    args: argparse.Namespace,
    upload_dir: Path,
    guest_output_root: str,
) -> dict[str, object]:
    if not args.salvage_timeout_artifacts or args.launch_transport == "send-key":
        return {
            "enabled": bool(args.salvage_timeout_artifacts),
            "attempted": False,
            "reason": "disabled-or-non-qga-transport",
        }

    salvage_targets = {
        "guest_summary": ("summary.json", upload_dir / f"{args.output_name}-summary-guest.json"),
        "guest_summary_arm": ("summary-arm.json", upload_dir / f"{args.output_name}-summary-arm-guest.json"),
        "guest_stage": ("stage.json", upload_dir / f"{args.output_name}-stage-guest.json"),
        "guest_hits_csv": (f"{args.output_name}.hits.csv", upload_dir / f"{args.output_name}.hits.csv"),
        "guest_normalized": (f"{args.output_name}.normalized.json", upload_dir / f"{args.output_name}.normalized.json"),
    }
    downloads = {}
    for name, (guest_name, host_path) in salvage_targets.items():
        guest_path = guest_output_root.rstrip("\\") + "\\" + guest_name
        downloads[name] = try_qga_download(repo_root=repo_root, args=args, guest_path=guest_path, host_path=host_path)

    artifact_health = {
        name: describe_downloaded_file(host_path)
        for name, (_, host_path) in salvage_targets.items()
    }
    hits_csv_path = salvage_targets["guest_hits_csv"][1]
    normalized_path = salvage_targets["guest_normalized"][1]
    hits_csv = inspect_hits_csv(hits_csv_path, args.value_name)
    normalized_salvage: dict[str, object] = {
        "created": False,
        "reason": "not-needed",
    }
    if (
        hits_csv.get("exists")
        and int(hits_csv.get("hit_line_count") or 0) == 0
        and (not normalized_path.exists() or normalized_path.stat().st_size == 0)
    ):
        normalized_salvage = synthesize_empty_normalized_bundle(
            hits_csv_path=hits_csv_path,
            bundle_path=normalized_path,
            args=args,
        )

    return {
        "enabled": True,
        "attempted": True,
        "guest_output_root": guest_output_root,
        "downloads": downloads,
        "artifact_health": artifact_health,
        "hits_csv": hits_csv,
        "normalized_salvage": normalized_salvage,
    }


def summarize_timeout_salvage(timeout_salvage: dict[str, object]) -> dict[str, object]:
    artifact_health = timeout_salvage.get("artifact_health") or {}
    if not isinstance(artifact_health, dict):
        artifact_health = {}
    hits_csv = timeout_salvage.get("hits_csv") or {}
    if not isinstance(hits_csv, dict):
        hits_csv = {}
    normalized_salvage = timeout_salvage.get("normalized_salvage") or {}
    if not isinstance(normalized_salvage, dict):
        normalized_salvage = {}

    guest_summary = artifact_health.get("guest_summary") or {}
    guest_normalized = artifact_health.get("guest_normalized") or {}
    guest_hits_csv = artifact_health.get("guest_hits_csv") or {}
    if not isinstance(guest_summary, dict):
        guest_summary = {}
    if not isinstance(guest_normalized, dict):
        guest_normalized = {}
    if not isinstance(guest_hits_csv, dict):
        guest_hits_csv = {}

    normalized_created = bool(normalized_salvage.get("created"))
    hit_line_count = int(hits_csv.get("hit_line_count") or 0)

    if normalized_created and hit_line_count == 0:
        salvage_classification = "header-only-no-hit"
        normalization_status = "ok"
        normalizer_name = normalized_salvage.get("normalizer_name")
    elif normalized_created:
        salvage_classification = "normalized-salvage-created"
        normalization_status = "ok"
        normalizer_name = normalized_salvage.get("normalizer_name")
    else:
        salvage_classification = str(normalized_salvage.get("reason") or "not-needed")
        normalization_status = "timeout"
        normalizer_name = None

    return {
        "summary_source": "timeout-salvage",
        "hit_line_count": hit_line_count,
        "hits_csv_exists": bool(guest_hits_csv.get("exists")),
        "guest_summary_zero_byte": bool(guest_summary.get("is_zero_byte")),
        "guest_normalized_zero_byte": bool(guest_normalized.get("is_zero_byte")),
        "normalized_bundle_exists": bool(guest_normalized.get("exists")) or normalized_created,
        "normalization_status": normalization_status,
        "normalizer_name": normalizer_name,
        "salvage_classification": salvage_classification,
    }


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
    parser = argparse.ArgumentParser(description="Stage and run a WPR boot-registry capture inside the KVM guest.")
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
    parser.add_argument("--reboot-settle-seconds", type=int, default=45)
    parser.add_argument("--host-reboot-mode", choices=["reboot", "reset"], default="reboot")
    parser.add_argument("--qga-retry-seconds", type=int, default=90)
    parser.add_argument("--qga-retry-interval-seconds", type=int, default=5)
    parser.add_argument("--launch-transport", choices=["auto", "qga", "send-key"], default="auto")
    parser.add_argument("--wpr-timeout-seconds", type=int, default=180)
    parser.add_argument("--tracerpt-timeout-seconds", type=int, default=180)
    parser.add_argument("--salvage-timeout-artifacts", action=argparse.BooleanOptionalAction, default=True)
    parser.add_argument("--salvage-qga-timeout-seconds", type=int, default=30)
    parser.add_argument("--expect-caller-stack", action="store_true", help="Fail the run if the normalized bundle has no caller_stack frames.")
    parser.add_argument("--registry-path", required=True)
    parser.add_argument("--value-name", required=True)
    parser.add_argument("--output-name", required=True)
    parser.add_argument("--match-fragment", action="append", default=[])
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    upload_dir = Path(args.upload_dir).resolve()
    upload_dir.mkdir(parents=True, exist_ok=True)
    summary_arm_path = upload_dir / f"{args.output_name}-summary-arm.json"
    summary_path = upload_dir / f"{args.output_name}-summary.json"
    stage_path = upload_dir / f"{args.output_name}-stage.json"
    legacy_summary_path = upload_dir / "wpr-boot-registry-summary.json"
    hits_path = upload_dir / f"{args.output_name}.hits.txt"
    for path in (summary_arm_path, summary_path, stage_path, legacy_summary_path, hits_path):
        if path.exists():
            path.unlink()

    generated_dir = repo_root / "dist" / "kvm-generated"
    generated_dir.mkdir(parents=True, exist_ok=True)

    ensure_guest_bridge(repo_root=repo_root, bridge_base_url=args.bridge_base_url, upload_root=upload_dir)
    guest_scripts_root = args.guest_scripts_root

    bridge = args.bridge_base_url.rstrip("/")
    arm_name = f"guest-wpr-boot-registry-arm-{args.output_name}.ps1"
    arm_path = generated_dir / arm_name
    arm_lines = [
        "$ErrorActionPreference = 'Stop'",
        f"New-Item -ItemType Directory -Path {quote_ps(guest_scripts_root)} -Force | Out-Null",
        (
            f"Invoke-WebRequest -UseBasicParsing -Uri "
            f"{quote_ps(bridge + '/scripts/vm/guest-tools/run-wpr-boot-registry-probe.ps1')} "
            f"-OutFile {quote_ps(guest_scripts_root + r'\\run-wpr-boot-registry-probe.ps1')}"
        ),
    ]
    arm_command = [
        "&",
        quote_ps(guest_scripts_root + r"\run-wpr-boot-registry-probe.ps1"),
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
        "-WprTimeoutSeconds",
        str(args.wpr_timeout_seconds),
        "-TracerptTimeoutSeconds",
        str(args.tracerpt_timeout_seconds),
    ]
    if args.match_fragment:
        arm_command.extend(["-MatchFragments", quote_ps_array(args.match_fragment)])
    arm_lines.append(" ".join(arm_command))
    arm_path.write_text("\n".join(arm_lines) + "\n", encoding="utf-8")

    arm_launch_transport = "not-started"
    collect_launch_transport = "not-started"
    try:
        arm_launch_transport = launch_generated_script(
            repo_root=repo_root,
            generated_path=arm_path,
            guest_launcher=build_guest_launcher(guest_scripts_root, bridge, arm_name),
            guest_scripts_root=guest_scripts_root,
            marker_name=f"{args.output_name}-wpr-arm-ready",
            args=args,
        )
    except subprocess.CalledProcessError as exc:
        print(
            json.dumps(
                emit_host_step_error(
                    summary_path=summary_path,
                    summary_arm_path=summary_arm_path,
                    stage_path=stage_path,
                    hits_path=hits_path,
                    output_name=args.output_name,
                    arm_launch_transport=arm_launch_transport,
                    collect_launch_transport=collect_launch_transport,
                    exc=exc,
                ),
                indent=2,
            )
        )
        return 1

    if not wait_for_file(summary_arm_path, args.prepare_timeout_seconds):
        print(
            json.dumps(
                apply_summary_contract(
                    {
                        "summary_arm_path": str(summary_arm_path),
                        "output_name": args.output_name,
                        "arm_launch_transport": arm_launch_transport,
                        "status": "prepare-timeout",
                        "summary_source": "wpr-prepare-timeout",
                        "error_kind": "runner-timeout",
                        "recovery_action": "rerun-wpr-boot-registry",
                        "transport_blocker": "timeout",
                        "guest_health": "unknown",
                    }
                ),
                indent=2,
            )
        )
        return 2

    summary_arm, arm_parse_error = load_arm_summary_or_error(
        summary_arm_path,
        output_name=args.output_name,
        arm_launch_transport=arm_launch_transport,
    )
    if arm_parse_error is not None:
        print(json.dumps(arm_parse_error, indent=2))
        return 1
    assert summary_arm is not None
    if summary_arm.get("status") == "error":
        payload = apply_summary_contract(
            {
                "summary_arm_path": str(summary_arm_path),
                "output_name": args.output_name,
                "arm_launch_transport": arm_launch_transport,
                "status": "error",
                "error_kind": summary_arm.get("error_kind") or "wpr-arm-error",
                "error": summary_arm.get("error"),
                "stage": "arm",
                "summary_source": "arm-summary",
            },
            default_error_kind="wpr-arm-error",
            default_recovery_action="inspect-wpr-arm",
            default_transport_blocker="arm-stage-error",
            default_guest_health="degraded",
        )
        print(json.dumps(payload, indent=2))
        return 1

    try:
        run(["virsh", "-c", args.connect, args.host_reboot_mode, args.domain], cwd=repo_root)
    except subprocess.CalledProcessError as exc:
        print(
            json.dumps(
                emit_host_step_error(
                    summary_path=summary_path,
                    summary_arm_path=summary_arm_path,
                    stage_path=stage_path,
                    hits_path=hits_path,
                    output_name=args.output_name,
                    arm_launch_transport=arm_launch_transport,
                    collect_launch_transport=collect_launch_transport,
                    exc=annotate_process_error(exc, stage=f"host-{args.host_reboot_mode}"),
                ),
                indent=2,
            )
        )
        return 1
    time.sleep(args.reboot_settle_seconds)

    state_file = rf"C:\RegProbe-Diag\wpr-boot-registry\{args.output_name}\state.json"
    collect_name = f"guest-wpr-boot-registry-collect-{args.output_name}.ps1"
    collect_path = generated_dir / collect_name
    collect_lines = [
        "$ErrorActionPreference = 'Stop'",
        f"New-Item -ItemType Directory -Path {quote_ps(guest_scripts_root)} -Force | Out-Null",
        (
            f"Invoke-WebRequest -UseBasicParsing -Uri "
            f"{quote_ps(bridge + '/scripts/vm/guest-tools/run-wpr-boot-registry-probe.ps1')} "
            f"-OutFile {quote_ps(guest_scripts_root + r'\\run-wpr-boot-registry-probe.ps1')}"
        ),
        (
            f"& {quote_ps(guest_scripts_root + r'\\run-wpr-boot-registry-probe.ps1')} "
            f"-Stage collect "
            f"-StateFile {quote_ps(state_file)} "
            f"-WprTimeoutSeconds {args.wpr_timeout_seconds} "
            f"-TracerptTimeoutSeconds {args.tracerpt_timeout_seconds}"
        ),
    ]
    collect_path.write_text("\n".join(collect_lines) + "\n", encoding="utf-8")

    try:
        collect_launch_transport = launch_generated_script(
            repo_root=repo_root,
            generated_path=collect_path,
            guest_launcher=build_guest_launcher(guest_scripts_root, bridge, collect_name),
            guest_scripts_root=guest_scripts_root,
            marker_name=f"{args.output_name}-wpr-collect-ready",
            args=args,
        )
    except subprocess.CalledProcessError as exc:
        print(
            json.dumps(
                emit_host_step_error(
                    summary_path=summary_path,
                    summary_arm_path=summary_arm_path,
                    stage_path=stage_path,
                    hits_path=hits_path,
                    output_name=args.output_name,
                    arm_launch_transport=arm_launch_transport,
                    collect_launch_transport=collect_launch_transport,
                    exc=exc,
                ),
                indent=2,
            )
        )
        return 1

    deadline = time.time() + args.timeout_seconds
    while time.time() < deadline:
        active_summary_path = None
        if summary_path.exists():
            active_summary_path = summary_path
        elif legacy_summary_path.exists():
            try:
                legacy_summary = read_json_object(legacy_summary_path, context="wpr legacy summary")
            except (OSError, json.JSONDecodeError, ValueError) as exc:
                summary = write_summary_contract(
                    summary_path,
                    {
                        "summary_arm_path": str(summary_arm_path),
                        "summary_path": str(legacy_summary_path),
                        "stage_path": str(stage_path),
                        "hits_path": str(hits_path),
                        "output_name": args.output_name,
                        "arm_launch_transport": arm_launch_transport,
                        "collect_launch_transport": collect_launch_transport,
                        "status": "error",
                        "summary_source": "legacy-summary-parse-error",
                        "summary_parse_error": str(exc),
                    },
                    default_error_kind="wpr-legacy-summary-parse-error",
                    default_recovery_action="rerun-wpr-boot-registry",
                    default_transport_blocker="summary-parse-error",
                    default_guest_health="unknown",
                )
                print(json.dumps(summary, indent=2))
                return 1
            if legacy_summary.get("output_name") == args.output_name:
                summary_path.write_text(json.dumps(legacy_summary, indent=2) + "\n", encoding="utf-8")
                active_summary_path = summary_path

        if active_summary_path is not None:
            summary, parse_failed = load_summary_or_error(
                active_summary_path,
                summary_path=summary_path,
                summary_arm_path=summary_arm_path,
                stage_path=stage_path,
                hits_path=hits_path,
                output_name=args.output_name,
                arm_launch_transport=arm_launch_transport,
                collect_launch_transport=collect_launch_transport,
            )
            if parse_failed:
                print(json.dumps(summary, indent=2))
                return 1
            payload = {
                "summary_arm_path": str(summary_arm_path),
                "summary_path": str(active_summary_path),
                "stage_path": str(stage_path),
                "hits_path": str(hits_path),
                "output_name": args.output_name,
                "arm_launch_transport": arm_launch_transport,
                "collect_launch_transport": collect_launch_transport,
                "status": summary.get("status", "unknown"),
                "error_kind": summary.get("error_kind"),
                "error": summary.get("error"),
                "reboot_observed": summary.get("reboot_observed"),
                "etl_exists": summary.get("etl_exists"),
                "csv_exists": summary.get("csv_exists"),
                "hit_line_count": summary.get("hit_line_count"),
                "normalized_bundle_exists": summary.get("normalized_bundle_exists"),
                "normalization_status": summary.get("normalization_status"),
                "normalizer_name": summary.get("normalizer_name"),
                "caller_stack_event_count": summary.get("caller_stack_event_count"),
                "recovery_action": summary.get("recovery_action"),
                "transport_blocker": summary.get("transport_blocker"),
                "guest_health": summary.get("guest_health"),
            }
            caller_stack_event_count = int(summary.get("caller_stack_event_count") or 0)
            if args.expect_caller_stack and caller_stack_event_count == 0 and summary.get("status") != "error":
                payload = apply_summary_contract(
                    {
                        **payload,
                        "status": "error",
                        "error_kind": "caller-stack-missing",
                        "error": "Caller stack frames were requested but the normalized bundle contains none.",
                        "summary_source": "caller-stack-check",
                        "recovery_action": "rerun-wpr-with-caller-stack",
                        "transport_blocker": "caller-stack-missing",
                        "guest_health": "degraded",
                    },
                    default_error_kind="caller-stack-missing",
                    default_recovery_action="rerun-wpr-with-caller-stack",
                    default_transport_blocker="caller-stack-missing",
                    default_guest_health="degraded",
                )
                print(json.dumps(payload, indent=2))
                return 1
            print(json.dumps(payload, indent=2))
            if summary.get("status") == "error" or summary.get("normalization_status") not in {None, "ok"}:
                return 1
            return 0

        if stage_path.exists():
            try:
                stage = read_json_object(stage_path, context="wpr stage")
            except (OSError, json.JSONDecodeError, ValueError) as exc:
                summary = write_summary_contract(
                    summary_path,
                    {
                        "summary_arm_path": str(summary_arm_path),
                        "summary_path": str(summary_path),
                        "stage_path": str(stage_path),
                        "hits_path": str(hits_path),
                        "output_name": args.output_name,
                        "arm_launch_transport": arm_launch_transport,
                        "collect_launch_transport": collect_launch_transport,
                        "status": "error",
                        "summary_source": "stage-parse-error",
                        "summary_parse_error": str(exc),
                    },
                    default_error_kind="wpr-stage-parse-error",
                    default_recovery_action="rerun-wpr-boot-registry",
                    default_transport_blocker="summary-parse-error",
                    default_guest_health="unknown",
                )
                print(json.dumps(summary, indent=2))
                return 1
            if stage.get("status") == "error":
                summary = write_summary_contract(
                    summary_path,
                    {
                    "generated_utc": stage.get("generated_utc"),
                    "stage": "collect",
                    "registry_path": args.registry_path,
                    "value_name": args.value_name,
                    "output_name": args.output_name,
                    "status": "error",
                    "error_kind": "wpr-stage-error",
                    "error": stage.get("message"),
                    "summary_source": "stage-fallback",
                    "stage_name": stage.get("stage"),
                    "stage_status": stage.get("status"),
                    "reboot_observed": None,
                    "etl_exists": None,
                    "csv_exists": False,
                    "hit_line_count": 0,
                    "normalized_bundle_exists": False,
                    "normalization_status": "error",
                    "normalizer_name": None,
                    },
                    default_error_kind="wpr-stage-error",
                    default_recovery_action="inspect-wpr-stage",
                    default_transport_blocker="stage-error",
                    default_guest_health="degraded",
                )
                payload = {
                    "summary_arm_path": str(summary_arm_path),
                    "summary_path": str(summary_path),
                    "stage_path": str(stage_path),
                    "hits_path": str(hits_path),
                    "output_name": args.output_name,
                    "arm_launch_transport": arm_launch_transport,
                    "collect_launch_transport": collect_launch_transport,
                    "status": "error",
                    "error_kind": "wpr-stage-error",
                    "recovery_action": summary.get("recovery_action"),
                    "transport_blocker": summary.get("transport_blocker"),
                    "guest_health": summary.get("guest_health"),
                    "error": stage.get("message"),
                    "stage_name": stage.get("stage"),
                    "summary_source": "stage-fallback",
                }
                payload = apply_summary_contract(
                    payload,
                    default_error_kind="wpr-stage-error",
                    default_recovery_action="inspect-wpr-stage",
                    default_transport_blocker="stage-error",
                    default_guest_health="degraded",
                )
                print(json.dumps(payload, indent=2))
                return 1
        time.sleep(2)

    timeout_salvage = salvage_timeout_artifacts(
        repo_root=repo_root,
        args=args,
        upload_dir=upload_dir,
        guest_output_root=rf"C:\RegProbe-Diag\wpr-boot-registry\{args.output_name}",
    )
    timeout_summary = write_summary_contract(
        summary_path,
        {
            "summary_arm_path": str(summary_arm_path),
            "summary_path": str(summary_path),
            "output_name": args.output_name,
            "arm_launch_transport": arm_launch_transport,
            "collect_launch_transport": collect_launch_transport,
            "status": "timeout",
            "timeout_salvage": timeout_salvage,
            **summarize_timeout_salvage(timeout_salvage),
        },
        default_error_kind="runner-timeout",
        default_recovery_action="rerun-wpr-boot-registry",
        default_transport_blocker="timeout",
        default_guest_health="unknown",
    )
    print(json.dumps(timeout_summary, indent=2))
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
