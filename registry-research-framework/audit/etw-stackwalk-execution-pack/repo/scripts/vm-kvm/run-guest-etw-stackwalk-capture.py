#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import sys
import tempfile
import time
from datetime import datetime
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[2]
CURRENT_DIR = Path(__file__).resolve().parent
FRAMEWORK_SCRIPTS = REPO_ROOT / "registry-research-framework" / "scripts"
DEFAULT_PROFILE_CONFIG = REPO_ROOT / "registry-research-framework" / "config" / "etw-stackwalk-profiles.json"
DEFAULT_RUNNER_CONFIG = REPO_ROOT / "registry-research-framework" / "config" / "tweak-vm-runners.json"
DEFAULT_RUN_ID = "wave4-registry-stackwalk"
DEFAULT_DURATION_SECONDS = 60
DEFAULT_REGISTRY_PATH = r"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel"
DEFAULT_VALUE_NAME = "TimerCheckFlags"
DEFAULT_GUEST_OUTPUT_ROOT = r"C:\RegProbe-Diag\etw-stackwalk"
DEFAULT_KERNEL_FLAGS = ["PROC_THREAD", "LOADER", "REGISTRY"]
DEFAULT_STACKWALK_EVENTS = [
    "RegCreateKey",
    "RegOpenKey",
    "RegQueryKey",
    "RegSetValue",
    "RegQueryValue",
    "RegDeleteValue",
    "RegCloseKey",
]
DEFAULT_BUFFER_SIZE_KB = 1024
DEFAULT_MIN_BUFFERS = 64
DEFAULT_MAX_BUFFERS = 256

if str(CURRENT_DIR) not in sys.path:
    sys.path.insert(0, str(CURRENT_DIR))
if str(FRAMEWORK_SCRIPTS) not in sys.path:
    sys.path.insert(0, str(FRAMEWORK_SCRIPTS))

from summary_contract_lib import apply_summary_contract, read_json_object, write_summary_contract

from guest_bridge import ensure_guest_bridge
from generate_etw_stackwalk_capture_plan import load_config as load_profile_config  # noqa: E402
from generate_etw_stackwalk_capture_plan import load_runner_config  # noqa: E402
from generate_etw_stackwalk_capture_plan import profile_id_for_candidate  # noqa: E402
from generate_etw_stackwalk_capture_plan import profile_by_id  # noqa: E402
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


def emit_launch_error(
    *,
    summary_path: Path,
    run_id: str,
    profile_id: str | None,
    requested_launch_transport: str,
    exc: subprocess.CalledProcessError,
) -> dict[str, object]:
    return write_summary_contract(
        summary_path,
        {
            "status": "error",
            "summary_path": str(summary_path),
            "run_id": run_id,
            "profile_id": profile_id,
            "launch_transport": requested_launch_transport,
            "host_step": getattr(exc, "stage", None),
            "exit_code": exc.returncode,
            "command": [str(part) for part in exc.cmd] if isinstance(exc.cmd, list) else str(exc.cmd),
            "error": format_process_error(exc),
            "summary_source": "host-launch-failure",
        },
        default_error_kind="etw-stackwalk-launch-error",
        default_recovery_action="rerun-etw-stackwalk-capture",
        default_transport_blocker="launch-failed",
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
    run_id: str,
    profile_id: str,
    launch_transport: str,
) -> tuple[dict[str, object], bool]:
    try:
        return apply_summary_contract(read_json_object(summary_path, context="etw stackwalk summary")), False
    except (OSError, json.JSONDecodeError, ValueError) as exc:
        return (
            write_summary_contract(
                summary_path,
                {
                    "status": "error",
                    "summary_path": str(summary_path),
                    "run_id": run_id,
                    "profile_id": profile_id,
                    "launch_transport": launch_transport,
                    "summary_parse_error": str(exc),
                },
                default_error_kind="etw-stackwalk-summary-parse-error",
                default_recovery_action="rerun-etw-stackwalk-capture",
                default_transport_blocker="summary-parse-error",
                default_guest_health="unknown",
            ),
            True,
        )


def load_stage_or_error(
    stage_path: Path,
    *,
    summary_path: Path,
    run_id: str,
    profile_id: str | None,
    launch_transport: str,
) -> tuple[dict[str, object] | None, dict[str, object] | None]:
    try:
        return read_json_object(stage_path, context="etw stackwalk stage"), None
    except (OSError, json.JSONDecodeError, ValueError) as exc:
        return None, write_summary_contract(
            summary_path,
            {
                "status": "error",
                "summary_path": str(summary_path),
                "run_id": run_id,
                "profile_id": profile_id,
                "launch_transport": launch_transport,
                "summary_source": "stage-parse-error",
                "summary_parse_error": str(exc),
            },
            default_error_kind="etw-stackwalk-stage-parse-error",
            default_recovery_action="rerun-etw-stackwalk-capture",
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
    run_id: str,
    profile_id: str | None,
    launch_transport: str,
    stage_payload: dict[str, object],
    stall_seconds: int,
) -> dict[str, object]:
    return write_summary_contract(
        summary_path,
        {
            "status": "timeout",
            "summary_path": str(summary_path),
            "run_id": run_id,
            "profile_id": profile_id,
            "launch_transport": launch_transport,
            "stage": stage_payload,
            "stall_seconds": stall_seconds,
            "summary_source": "stage-timeout",
        },
        default_error_kind="guest-stage-stall",
        default_recovery_action="inspect-stage-upload",
        default_transport_blocker="stage-stall",
        default_guest_health="degraded",
    )


def emit_first_artifact_timeout(
    *,
    summary_path: Path,
    run_id: str,
    profile_id: str | None,
    launch_transport: str,
    wait_seconds: int,
) -> dict[str, object]:
    return write_summary_contract(
        summary_path,
        {
            "status": "timeout",
            "summary_path": str(summary_path),
            "run_id": run_id,
            "profile_id": profile_id,
            "launch_transport": launch_transport,
            "first_artifact_timeout_seconds": wait_seconds,
            "summary_source": "first-artifact-timeout",
        },
        default_error_kind="bridge-artifact-timeout",
        default_recovery_action="inspect-bridge-upload",
        default_transport_blocker="bridge-artifact-timeout",
        default_guest_health="unknown",
    )


def read_json_object_if_exists(path: Path) -> dict[str, Any] | None:
    if not path.exists():
        return None
    try:
        return read_json_object(path, context=f"JSON object payload at {path}")
    except (OSError, json.JSONDecodeError, ValueError):
        return None


def bundle_error_kind(bundle_path: Path) -> str | None:
    payload = read_json_object_if_exists(bundle_path)
    if not payload:
        return None
    error_kind = str(payload.get("error_kind") or "").strip()
    return error_kind or None


def run_bundle_generation(
    *,
    repo_root: Path,
    target_etl: Path,
    target_bundle: Path,
    run_id: str,
) -> subprocess.CompletedProcess[str]:
    bundle_cmd = [
        sys.executable,
        str(repo_root / "registry-research-framework" / "scripts" / "generate_etw_stackwalk_bundle.py"),
        "--input",
        str(target_etl),
        "--output",
        str(target_bundle),
        "--run-id",
        run_id,
    ]
    return subprocess.run(bundle_cmd, cwd=str(repo_root), capture_output=True, text=True)


def try_guest_xml_backfill(
    *,
    repo_root: Path,
    run_id: str,
    target_summary: Path,
    target_xml: Path,
    upload_dir: Path,
    guest_launch_context: dict[str, object] | None,
) -> dict[str, object]:
    if target_xml.exists():
        return {
            "status": "skipped",
            "reason": "xml-already-present",
            "target_xml": str(target_xml),
        }
    if not guest_launch_context:
        return {
            "status": "skipped",
            "reason": "guest-launch-context-missing",
        }

    summary_payload = read_json_object_if_exists(target_summary)
    if not summary_payload:
        return {
            "status": "error",
            "reason": "summary-unavailable",
            "summary_path": str(target_summary),
        }

    tracerpt_exists = bool(summary_payload.get("tracerpt_exists"))
    guest_etl_path = str(summary_payload.get("etl_path") or "").strip()
    guest_xml_path = str(summary_payload.get("xml_path") or "").strip()
    bridge_base_url = str(guest_launch_context.get("bridge_base_url") or summary_payload.get("upload_base_url") or "").strip()
    if not tracerpt_exists:
        return {
            "status": "skipped",
            "reason": "guest-tracerpt-missing",
        }
    if not guest_etl_path or not guest_xml_path:
        return {
            "status": "skipped",
            "reason": "guest-artifact-paths-missing",
        }
    if not bridge_base_url:
        return {
            "status": "skipped",
            "reason": "bridge-base-url-missing",
        }

    upload_xml = upload_dir / f"{run_id}.xml"
    upload_xml.unlink(missing_ok=True)
    script_body = "\n".join(
        [
            "$ErrorActionPreference = 'Stop'",
            f"$tracerpt = {quote_ps(r'C:\\Windows\\System32\\tracerpt.exe')}",
            f"$etlPath = {quote_ps(guest_etl_path)}",
            f"$xmlPath = {quote_ps(guest_xml_path)}",
            f"$uploadUri = {quote_ps(bridge_base_url.rstrip('/') + '/' + run_id + '.xml')}",
            "if (-not (Test-Path -LiteralPath $tracerpt)) { throw 'tracerpt.exe not found.' }",
            "if (-not (Test-Path -LiteralPath $etlPath)) { throw ('ETL path not found: ' + $etlPath) }",
            "if (Test-Path -LiteralPath $xmlPath) { Remove-Item -LiteralPath $xmlPath -Force }",
            "& $tracerpt $etlPath -o $xmlPath -of XML -lr",
            "if ($LASTEXITCODE -ne 0) { throw ('tracerpt.exe failed with exit code ' + $LASTEXITCODE) }",
            "Invoke-WebRequest -Method Put -Uri $uploadUri -InFile $xmlPath -UseBasicParsing | Out-Null",
        ]
    ) + "\n"

    with tempfile.NamedTemporaryFile("w", encoding="utf-8", suffix=".ps1", delete=False) as handle:
        handle.write(script_body)
        script_path = Path(handle.name)

    command = [
        sys.executable,
        str(repo_root / "scripts" / "vm-kvm" / "qga-run-powershell.py"),
        "--domain",
        str(guest_launch_context.get("domain") or vm_domain("regprobe-win11-25h2-session")),
        "--connect",
        str(guest_launch_context.get("connect") or vm_connect("qemu:///session")),
        "--script",
        str(script_path),
        "--guest-dir",
        str(guest_launch_context.get("guest_scripts_root") or r"C:\RegProbe-Diag\bootstrap"),
        "--wait-timeout",
        str(int(guest_launch_context.get("qga_wait_timeout") or 600)),
    ]
    try:
        result = subprocess.run(command, cwd=str(repo_root), capture_output=True, text=True)
    finally:
        script_path.unlink(missing_ok=True)

    payload: dict[str, object] = {
        "status": "error" if result.returncode != 0 else "ok",
        "command": command,
        "returncode": result.returncode,
        "stdout": (result.stdout or "").strip(),
        "stderr": (result.stderr or "").strip(),
    }
    if result.returncode != 0:
        payload["reason"] = "guest-xml-export-failed"
        return payload
    if not wait_for_file(upload_xml, timeout_seconds=120):
        payload["status"] = "error"
        payload["reason"] = "uploaded-xml-missing"
        payload["uploaded_xml"] = str(upload_xml)
        return payload

    shutil.copy2(upload_xml, target_xml)
    payload["uploaded_xml"] = str(upload_xml)
    payload["target_xml"] = str(target_xml)
    return payload


def build_ingest_payload(
    *,
    target_root: Path,
    target_etl: Path,
    target_xml: Path,
    target_summary: Path,
    target_bundle: Path,
    bundle_result: subprocess.CompletedProcess[str],
) -> dict[str, object]:
    return {
        "status": "ok" if bundle_result.returncode == 0 else "error",
        "target_root": str(target_root),
        "etl_path": str(target_etl),
        "xml_path": str(target_xml) if target_xml.exists() else None,
        "summary_path": str(target_summary),
        "bundle_path": str(target_bundle) if target_bundle.exists() else None,
        "bundle_returncode": bundle_result.returncode,
        "bundle_stdout": (bundle_result.stdout or "").strip(),
        "bundle_stderr": (bundle_result.stderr or "").strip(),
        "bundle_error_kind": bundle_error_kind(target_bundle),
    }


def ingest_capture_artifacts(
    *,
    repo_root: Path,
    run_id: str,
    summary_path: Path,
    xml_path: Path | None,
    etl_path: Path | None,
    ingest_root: Path,
    refresh_ghidra: bool,
    guest_launch_context: dict[str, object] | None = None,
) -> dict[str, object]:
    if etl_path is None or not etl_path.exists():
        return apply_summary_contract(
            {
                "status": "error",
                "summary_source": "ingest-preflight",
                "error": "ETL upload is required for ingest.",
            },
            default_error_kind="ingest-missing-etl",
            default_recovery_action="rerun-etw-stackwalk-capture",
            default_transport_blocker="missing-etl",
            default_guest_health="degraded",
        )

    target_root = (ingest_root / run_id).resolve()
    target_root.mkdir(parents=True, exist_ok=True)
    target_etl = target_root / f"{run_id}.etl"
    target_xml = target_root / f"{run_id}.xml"
    target_summary = target_root / f"{run_id}-summary.json"
    target_bundle = target_root / "normalized-registry-bundle.json"

    shutil.copy2(etl_path, target_etl)
    shutil.copy2(summary_path, target_summary)
    if xml_path and xml_path.exists():
        shutil.copy2(xml_path, target_xml)

    bundle_result = run_bundle_generation(
        repo_root=repo_root,
        target_etl=target_etl,
        target_bundle=target_bundle,
        run_id=run_id,
    )
    payload = build_ingest_payload(
        target_root=target_root,
        target_etl=target_etl,
        target_xml=target_xml,
        target_summary=target_summary,
        target_bundle=target_bundle,
        bundle_result=bundle_result,
    )
    if bundle_result.returncode != 0:
        if payload.get("bundle_error_kind") == "parser-unavailable" and not target_xml.exists():
            xml_backfill = try_guest_xml_backfill(
                repo_root=repo_root,
                run_id=run_id,
                target_summary=target_summary,
                target_xml=target_xml,
                upload_dir=Path(str((guest_launch_context or {}).get("upload_dir") or "")),
                guest_launch_context=guest_launch_context,
            )
            payload["xml_backfill"] = xml_backfill
            if xml_backfill.get("status") == "ok":
                bundle_result = run_bundle_generation(
                    repo_root=repo_root,
                    target_etl=target_etl,
                    target_bundle=target_bundle,
                    run_id=run_id,
                )
                payload = build_ingest_payload(
                    target_root=target_root,
                    target_etl=target_etl,
                    target_xml=target_xml,
                    target_summary=target_summary,
                    target_bundle=target_bundle,
                    bundle_result=bundle_result,
                )
                payload["xml_backfill"] = xml_backfill
        if bundle_result.returncode != 0:
            return payload

    if refresh_ghidra and target_bundle.exists():
        refresh_cmd = [
            sys.executable,
            str(repo_root / "registry-research-framework" / "scripts" / "refresh_ghidra_autotrigger_pipeline.py"),
            "--bundle",
            str(target_bundle),
        ]
        refresh_result = subprocess.run(refresh_cmd, cwd=str(repo_root), capture_output=True, text=True)
        payload["ghidra_refresh_returncode"] = refresh_result.returncode
        payload["ghidra_refresh_stdout"] = (refresh_result.stdout or "").strip()
        payload["ghidra_refresh_stderr"] = (refresh_result.stderr or "").strip()
        if refresh_result.returncode != 0:
            payload["status"] = "error"
    return payload


def build_guest_launcher(
    *,
    bridge: str,
    guest_scripts_root: str,
    generated_name: str,
) -> str:
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
        sys.stderr.write("[run-guest-etw-stackwalk-capture] qga launch failed, falling back to send-key transport.\n")
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


def build_generated_script(*, args: argparse.Namespace, bridge: str, guest_scripts_root: str) -> str:
    guest_helper = guest_scripts_root + r"\run-etw-registry-stackwalk-capture.ps1"
    command = [
        "&",
        quote_ps(guest_helper),
        "-RunId",
        quote_ps(args.run_id),
        "-DurationSeconds",
        str(args.duration_seconds),
        "-OutputRoot",
        quote_ps(args.guest_output_root),
        "-RegistryPath",
        quote_ps(args.registry_path),
        "-ValueName",
        quote_ps(args.value_name),
        "-KernelFlags",
        f"@({quote_ps_array(args.kernel_flags)})",
        "-StackwalkEvents",
        f"@({quote_ps_array(args.stackwalk_events)})",
        "-BufferSizeKb",
        str(args.buffer_size_kb),
        "-MinBuffers",
        str(args.min_buffers),
        "-MaxBuffers",
        str(args.max_buffers),
        "-UploadBaseUrl",
        quote_ps(bridge),
    ]
    if args.upload_etl:
        command.append("-UploadEtl")
    if args.skip_tracerpt:
        command.append("-SkipTracerpt")

    return "\n".join(
        [
            "$ErrorActionPreference = 'Stop'",
            f"New-Item -ItemType Directory -Path {quote_ps(guest_scripts_root)} -Force | Out-Null",
            (
                f"Invoke-WebRequest -UseBasicParsing -Uri "
                f"{quote_ps(bridge + '/scripts/vm/guest-tools/run-etw-registry-stackwalk-capture.ps1')} "
                f"-OutFile {quote_ps(guest_helper)}"
            ),
            " ".join(command),
        ]
    ) + "\n"


def unique_strings(values: list[Any] | None) -> list[str]:
    result: list[str] = []
    seen: set[str] = set()
    for value in values or []:
        text = str(value or "").strip()
        if not text or text.lower() in seen:
            continue
        seen.add(text.lower())
        result.append(text)
    return result


def list_profiles_payload(config: dict[str, Any]) -> dict[str, Any]:
    default_profile = str(config.get("default_profile") or "").strip()
    profiles = []
    for profile in config.get("profiles") or []:
        target_defaults = profile.get("target_defaults") or {}
        profiles.append(
            {
                "profile_id": profile.get("profile_id"),
                "description": profile.get("description"),
                "default_run_id": profile.get("default_run_id"),
                "registry_path": target_defaults.get("registry_path"),
                "value_name": target_defaults.get("value_name"),
                "is_default": str(profile.get("profile_id") or "") == default_profile,
            }
        )
    return {
        "default_profile": default_profile,
        "profiles": profiles,
    }


def effective_config_payload(
    *,
    candidate_id: str | None,
    profile_config_path: Path,
    runner_config_path: Path,
    effective: dict[str, Any],
) -> dict[str, Any]:
    return {
        "candidate_id": candidate_id,
        "profile_config": str(profile_config_path),
        "runner_config": str(runner_config_path),
        "effective": effective,
    }


def resolve_effective_capture_settings(
    *,
    config: dict[str, Any] | None,
    profile_id: str | None,
    run_id: str | None,
    duration_seconds: int | None,
    registry_path: str | None,
    value_name: str | None,
    guest_output_root: str | None,
    kernel_flags: list[str] | None,
    stackwalk_events: list[str] | None,
    buffer_size_kb: int | None,
    min_buffers: int | None,
    max_buffers: int | None,
) -> dict[str, Any]:
    profile: dict[str, Any] = {}
    if config:
        profile = profile_by_id(config, profile_id)
    target_defaults = profile.get("target_defaults") or {}
    buffer = profile.get("buffer") or {}

    effective_kernel_flags = unique_strings(kernel_flags) or unique_strings(profile.get("kernel_flags")) or list(DEFAULT_KERNEL_FLAGS)
    effective_stackwalk_events = unique_strings(stackwalk_events) or unique_strings(profile.get("stackwalk_events")) or list(DEFAULT_STACKWALK_EVENTS)

    return {
        "profile_id": profile.get("profile_id"),
        "profile_description": profile.get("description"),
        "run_id": str(run_id or profile.get("default_run_id") or DEFAULT_RUN_ID),
        "duration_seconds": int(duration_seconds or profile.get("default_duration_seconds") or DEFAULT_DURATION_SECONDS),
        "registry_path": str(registry_path or target_defaults.get("registry_path") or DEFAULT_REGISTRY_PATH),
        "value_name": str(value_name if value_name is not None else (target_defaults.get("value_name") or DEFAULT_VALUE_NAME)),
        "guest_output_root": str(guest_output_root or profile.get("default_output_root") or DEFAULT_GUEST_OUTPUT_ROOT),
        "kernel_flags": effective_kernel_flags,
        "stackwalk_events": effective_stackwalk_events,
        "buffer_size_kb": int(buffer_size_kb or buffer.get("size_kb") or DEFAULT_BUFFER_SIZE_KB),
        "min_buffers": int(min_buffers or buffer.get("min_buffers") or DEFAULT_MIN_BUFFERS),
        "max_buffers": int(max_buffers or buffer.get("max_buffers") or DEFAULT_MAX_BUFFERS),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Run the ETW registry stackwalk capture helper inside the KVM guest.")
    parser.add_argument("--repo-root", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--domain", default=vm_domain("regprobe-win11-25h2-session"))
    parser.add_argument("--connect", default=vm_connect("qemu:///session"))
    parser.add_argument("--bridge-base-url", default=bridge_base_url("http://10.0.2.2:8766"))
    parser.add_argument("--upload-dir", default=default_upload_dir("/tmp/regprobe-bridge"))
    parser.add_argument("--guest-scripts-root", default=r"C:\RegProbe-Diag\bootstrap")
    parser.add_argument("--delay-ms", default="18")
    parser.add_argument("--wake-key", default="KEY_ENTER")
    parser.add_argument("--timeout-seconds", type=int, default=180)
    parser.add_argument("--first-artifact-timeout-seconds", type=int, default=120)
    parser.add_argument("--qga-retry-seconds", type=int, default=30)
    parser.add_argument("--qga-retry-interval-seconds", type=int, default=5)
    parser.add_argument("--launch-transport", choices=["auto", "qga", "send-key"], default="auto")
    parser.add_argument("--profile-config", default=str(DEFAULT_PROFILE_CONFIG))
    parser.add_argument("--runner-config", default=str(DEFAULT_RUNNER_CONFIG))
    parser.add_argument("--candidate-id", default=None, help="Resolve the ETW stackwalk profile from tweak-vm-runners.json.")
    parser.add_argument("--profile-id", default=None)
    parser.add_argument("--list-profiles", action="store_true", help="List available ETW stackwalk profiles and exit.")
    parser.add_argument("--print-effective-config", action="store_true", help="Print the resolved capture settings and exit without launching the guest.")
    parser.add_argument("--run-id", default=None)
    parser.add_argument("--duration-seconds", type=int, default=None)
    parser.add_argument("--registry-path", default=None)
    parser.add_argument("--value-name", default=None)
    parser.add_argument("--guest-output-root", default=None)
    parser.add_argument("--kernel-flag", action="append", default=[], help="Override xperf kernel flags. Repeat for multiple values.")
    parser.add_argument("--stackwalk-event", action="append", default=[], help="Override xperf stackwalk events. Repeat for multiple values.")
    parser.add_argument("--buffer-size-kb", type=int, default=None)
    parser.add_argument("--min-buffers", type=int, default=None)
    parser.add_argument("--max-buffers", type=int, default=None)
    parser.add_argument("--upload-etl", action="store_true")
    parser.add_argument("--skip-tracerpt", action="store_true")
    parser.add_argument("--ingest-to-repo", action="store_true", help="Copy uploaded ETL/XML into evidence/raw/etw-stackwalk/<run-id> and build a normalized bundle.")
    parser.add_argument("--refresh-ghidra", action="store_true", help="After ingest, refresh the Ghidra autotrigger pipeline from the new normalized bundle.")
    parser.add_argument("--ingest-root", default="evidence/raw/etw-stackwalk")
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    profile_config_path = Path(args.profile_config)
    if not profile_config_path.is_absolute():
        profile_config_path = (repo_root / profile_config_path).resolve()
    runner_config_path = Path(args.runner_config)
    if not runner_config_path.is_absolute():
        runner_config_path = (repo_root / runner_config_path).resolve()
    config = load_profile_config(profile_config_path)
    if args.list_profiles:
        print(json.dumps(list_profiles_payload(config), indent=2))
        return 0
    if args.candidate_id and not args.profile_id:
        runner_config = load_runner_config(runner_config_path)
        resolved_profile_id = profile_id_for_candidate(args.candidate_id, runner_config)
        if not resolved_profile_id:
            parser.error(f"--candidate-id has no ETW stackwalk profile mapping: {args.candidate_id}")
        args.profile_id = resolved_profile_id

    effective = resolve_effective_capture_settings(
        config=config,
        profile_id=args.profile_id,
        run_id=args.run_id,
        duration_seconds=args.duration_seconds,
        registry_path=args.registry_path,
        value_name=args.value_name,
        guest_output_root=args.guest_output_root,
        kernel_flags=args.kernel_flag,
        stackwalk_events=args.stackwalk_event,
        buffer_size_kb=args.buffer_size_kb,
        min_buffers=args.min_buffers,
        max_buffers=args.max_buffers,
    )
    args.run_id = effective["run_id"]
    args.duration_seconds = effective["duration_seconds"]
    args.registry_path = effective["registry_path"]
    args.value_name = effective["value_name"]
    args.guest_output_root = effective["guest_output_root"]
    args.kernel_flags = effective["kernel_flags"]
    args.stackwalk_events = effective["stackwalk_events"]
    args.buffer_size_kb = effective["buffer_size_kb"]
    args.min_buffers = effective["min_buffers"]
    args.max_buffers = effective["max_buffers"]
    args.profile_id = effective["profile_id"]
    if args.print_effective_config:
        print(
            json.dumps(
                effective_config_payload(
                    candidate_id=args.candidate_id,
                    profile_config_path=profile_config_path,
                    runner_config_path=runner_config_path,
                    effective=effective,
                ),
                indent=2,
            )
        )
        return 0

    upload_dir = Path(args.upload_dir).resolve()
    upload_dir.mkdir(parents=True, exist_ok=True)
    safe_run_id = "".join(ch if ch.isalnum() or ch in "._-" else "-" for ch in args.run_id).strip("-") or "registry-stackwalk"
    summary_path = upload_dir / f"{safe_run_id}-summary.json"
    stage_path = upload_dir / f"{safe_run_id}-stage.json"
    xml_path = upload_dir / f"{safe_run_id}.xml"
    etl_path = upload_dir / f"{safe_run_id}.etl"
    for path in (summary_path, stage_path, xml_path, etl_path):
        path.unlink(missing_ok=True)

    if args.refresh_ghidra and not args.ingest_to_repo:
        parser.error("--refresh-ghidra requires --ingest-to-repo.")
    if args.ingest_to_repo:
        args.upload_etl = True

    ensure_guest_bridge(repo_root=repo_root, bridge_base_url=args.bridge_base_url, upload_root=upload_dir)
    bridge = args.bridge_base_url.rstrip("/")
    guest_scripts_root = args.guest_scripts_root
    generated_dir = repo_root / "dist" / "kvm-generated"
    generated_dir.mkdir(parents=True, exist_ok=True)
    generated_name = f"guest-etw-stackwalk-{safe_run_id}.ps1"
    generated_path = generated_dir / generated_name
    generated_path.write_text(
        build_generated_script(args=args, bridge=bridge, guest_scripts_root=guest_scripts_root),
        encoding="utf-8",
    )

    try:
        launch_transport = launch_generated_script(
            repo_root=repo_root,
            generated_path=generated_path,
            guest_launcher=build_guest_launcher(
                bridge=bridge,
                guest_scripts_root=guest_scripts_root,
                generated_name=generated_name,
            ),
            guest_scripts_root=guest_scripts_root,
            marker_name=f"{safe_run_id}-etw-stackwalk-ready",
            args=args,
        )
    except subprocess.CalledProcessError as exc:
        print(
            json.dumps(
                emit_launch_error(
                    summary_path=summary_path,
                    run_id=safe_run_id,
                    profile_id=args.profile_id,
                    requested_launch_transport=args.launch_transport,
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
        if stage_path.exists() and not summary_path.exists():
            stage_payload, stage_parse_error = load_stage_or_error(
                stage_path,
                summary_path=summary_path,
                run_id=safe_run_id,
                profile_id=args.profile_id,
                launch_transport=launch_transport,
            )
            if stage_parse_error is not None:
                print(json.dumps(stage_parse_error, indent=2))
                return 1
            assert stage_payload is not None
            last_stage_payload = stage_payload
            stage_status = str(stage_payload.get("status", "")).lower()
            if stage_status == "error":
                error_summary = write_summary_contract(
                    summary_path,
                    {
                        "status": "error",
                        "summary_path": str(summary_path),
                        "run_id": safe_run_id,
                        "profile_id": args.profile_id,
                        "launch_transport": launch_transport,
                        "stage": stage_payload,
                        "error": stage_payload.get("error"),
                        "summary_source": "stage-error",
                    },
                    default_error_kind="guest-stage-error",
                    default_recovery_action="inspect-stage-upload",
                    default_transport_blocker="stage-error",
                    default_guest_health="degraded",
                )
                print(json.dumps(error_summary, indent=2))
                return 1
            if stage_status == "starting":
                started_at = stage_started_timestamp(stage_payload, stage_path)
                if started_at is not None and (time.time() - started_at) >= first_artifact_timeout_seconds:
                    stall_summary = emit_stage_stall_timeout(
                        summary_path=summary_path,
                        run_id=safe_run_id,
                        profile_id=args.profile_id,
                        launch_transport=launch_transport,
                        stage_payload=stage_payload,
                        stall_seconds=first_artifact_timeout_seconds,
                    )
                    print(json.dumps(stall_summary, indent=2))
                    return 2

        if summary_path.exists():
            break

        if last_stage_payload is None and time.time() >= first_artifact_deadline:
            first_artifact_timeout = emit_first_artifact_timeout(
                summary_path=summary_path,
                run_id=safe_run_id,
                profile_id=args.profile_id,
                launch_transport=launch_transport,
                wait_seconds=first_artifact_timeout_seconds,
            )
            print(json.dumps(first_artifact_timeout, indent=2))
            return 2

        time.sleep(2)

    if not summary_path.exists():
        timeout_summary = write_summary_contract(
            summary_path,
            {
                "status": "timeout",
                "summary_path": str(summary_path),
                "run_id": safe_run_id,
                "profile_id": args.profile_id,
                "launch_transport": launch_transport,
                "xml_exists": False,
                "etl_exists": False,
                "stage": last_stage_payload,
                "summary_source": "host-timeout",
            },
            default_error_kind="runner-timeout",
            default_recovery_action="rerun-etw-stackwalk-capture",
            default_transport_blocker="timeout",
            default_guest_health="unknown",
        )
        print(json.dumps(timeout_summary, indent=2))
        return 2

    summary, parse_failed = load_summary_or_error(
        summary_path,
        run_id=safe_run_id,
        profile_id=args.profile_id,
        launch_transport=launch_transport,
    )
    if parse_failed:
        print(json.dumps(summary, indent=2))
        return 1
    payload = {
        "status": summary.get("status", "unknown"),
        "error_kind": summary.get("error_kind"),
        "error": summary.get("error"),
        "profile_id": args.profile_id,
        "run_id": safe_run_id,
        "launch_transport": launch_transport,
        "summary_path": str(summary_path),
        "xml_path": str(xml_path) if xml_path.exists() else None,
        "etl_path": str(etl_path) if etl_path.exists() else None,
        "stack_field_hit_count": summary.get("stack_field_hit_count"),
        "etl_exists": summary.get("etl_exists"),
        "xml_exists": summary.get("xml_exists"),
    }
    if args.ingest_to_repo and payload["status"] == "ok":
        payload["ingest"] = ingest_capture_artifacts(
            repo_root=repo_root,
            run_id=safe_run_id,
            summary_path=summary_path,
            xml_path=xml_path if xml_path.exists() else None,
            etl_path=etl_path if etl_path.exists() else None,
            ingest_root=(Path(args.ingest_root) if Path(args.ingest_root).is_absolute() else (repo_root / args.ingest_root)).resolve(),
            refresh_ghidra=args.refresh_ghidra,
            guest_launch_context={
                "domain": args.domain,
                "connect": args.connect,
                "bridge_base_url": args.bridge_base_url,
                "guest_scripts_root": args.guest_scripts_root,
                "upload_dir": str(upload_dir),
                "qga_wait_timeout": max(args.timeout_seconds, args.duration_seconds + 180),
            },
        )
        if str((payload.get("ingest") or {}).get("status") or "") != "ok":
            payload["status"] = "error"
            payload["error_kind"] = "ingest-failed"
            payload["error"] = str((payload.get("ingest") or {}).get("error") or "ETW capture ingest failed.")
    print(json.dumps(payload, indent=2))
    return 0 if payload["status"] == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
