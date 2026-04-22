#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
from pathlib import Path

from guest_bridge import ensure_guest_bridge
from summary_contract_lib import apply_summary_contract, write_summary_contract


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
    output_name: str,
    registry_path: str,
    value_name: str,
    trigger_profile: str,
    requested_launch_transport: str,
    exc: subprocess.CalledProcessError,
) -> dict[str, object]:
    return write_summary_contract(
        summary_path,
        {
            "summary_path": str(summary_path),
            "output_name": output_name,
            "registry_path": registry_path,
            "value_name": value_name,
            "trigger_profile": trigger_profile,
            "launch_transport": requested_launch_transport,
            "status": "error",
            "host_step": getattr(exc, "stage", None),
            "exit_code": exc.returncode,
            "command": [str(part) for part in exc.cmd] if isinstance(exc.cmd, list) else str(exc.cmd),
            "error": format_process_error(exc),
            "summary_source": "host-launch-failure",
        },
        default_error_kind="registry-policy-launch-error",
        default_recovery_action="rerun-registry-policy-probe",
        default_transport_blocker="launch-failed",
        default_guest_health="unknown",
    )


def launch_generated_script(
    *,
    repo_root: Path,
    generated_path: Path,
    guest_launcher: str,
    guest_scripts_root: str,
    output_name: str,
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
        qga_result = subprocess.run(qga_cmd, cwd=str(repo_root), capture_output=True, text=True)
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
            f"[run-guest-registry-policy-probe] qga launch failed, falling back to send-key transport for {output_name}.\n"
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
                f"{output_name}-admin-shell-ready",
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


def try_probe_stage_fallback(
    *,
    summary_path: Path,
    probe_stage_path: Path,
    result_path: Path,
    args: argparse.Namespace,
) -> tuple[dict[str, object], dict[str, object]] | None:
    if not probe_stage_path.exists():
        return None

    try:
        probe_stage = json.loads(probe_stage_path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        summary = write_summary_contract(
            summary_path,
            {
                "registry_path": args.registry_path,
                "value_name": args.value_name,
                "output_name": args.output_name,
                "trigger_profile": args.trigger_profile,
                "status": "error",
                "probe_stage_exists": True,
                "summary_source": "probe-stage-parse-error",
                "summary_parse_error": str(exc),
            },
            default_error_kind="probe-stage-parse-error",
            default_recovery_action="rerun-registry-policy-probe",
            default_transport_blocker="summary-parse-error",
            default_guest_health="unknown",
        )
        payload = {
            "summary_path": str(summary_path),
            "output_name": args.output_name,
            "trigger_profile": args.trigger_profile,
            "status": "error",
            "error_kind": summary.get("error_kind"),
            "recovery_action": summary.get("recovery_action"),
            "transport_blocker": summary.get("transport_blocker"),
            "guest_health": summary.get("guest_health"),
            "summary_source": summary.get("summary_source"),
            "summary_parse_error": summary.get("summary_parse_error"),
        }
        return summary, payload

    if probe_stage.get("status") != "error":
        return None

    result_error = None
    if result_path.exists():
        for line in result_path.read_text(encoding="utf-8-sig", errors="replace").splitlines():
            if line.startswith("ERROR="):
                result_error = line[len("ERROR=") :]
                break

    summary = write_summary_contract(
        summary_path,
        {
            "generated_utc": probe_stage.get("generated_utc"),
            "registry_path": args.registry_path,
            "value_name": args.value_name,
            "output_name": args.output_name,
            "trigger_profile": args.trigger_profile,
            "output_root": rf"C:\RegProbe-Diag\procmon\{args.output_name}",
            "status": "error",
            "result_exists": result_path.exists(),
            "csv_exists": False,
            "hits_csv_exists": False,
            "normalized_bundle_exists": False,
            "normalization_status": "error",
            "normalizer_name": None,
            "probe_stage_exists": True,
            "probe_stage": probe_stage.get("stage"),
            "probe_stage_status": probe_stage.get("status"),
            "probe_stage_message": probe_stage.get("message"),
            "result_error_line": f"ERROR={result_error}" if result_error else None,
            "error_kind": "probe-stage-error",
            "error": result_error or probe_stage.get("message"),
            "error_position": None,
            "summary_source": "probe-stage-fallback",
        },
        default_error_kind="probe-stage-error",
        default_recovery_action="inspect-probe-stage",
        default_transport_blocker="probe-stage-error",
        default_guest_health="degraded",
    )
    payload = {
        "summary_path": str(summary_path),
        "output_name": args.output_name,
        "trigger_profile": args.trigger_profile,
        "status": "error",
        "csv_exists": False,
        "hits_csv_exists": False,
        "normalized_bundle_exists": False,
        "normalization_status": "error",
        "normalizer_name": None,
        "probe_stage": probe_stage.get("stage"),
        "probe_stage_status": probe_stage.get("status"),
        "error_kind": "probe-stage-error",
        "recovery_action": summary.get("recovery_action"),
        "transport_blocker": summary.get("transport_blocker"),
        "guest_health": summary.get("guest_health"),
        "error": result_error or probe_stage.get("message"),
        "summary_source": "probe-stage-fallback",
    }
    return summary, payload


def load_summary_or_error(
    summary_path: Path,
    *,
    output_name: str,
    trigger_profile: str,
    timeout_seconds: int,
    effective_timeout_seconds: int,
    saveas_timeout_seconds: int,
    launch_transport: str,
) -> tuple[dict[str, object], bool]:
    try:
        return apply_summary_contract(json.loads(summary_path.read_text(encoding="utf-8-sig"))), False
    except (OSError, json.JSONDecodeError) as exc:
        return (
            write_summary_contract(
                summary_path,
                {
                    "summary_path": str(summary_path),
                    "output_name": output_name,
                    "trigger_profile": trigger_profile,
                    "timeout_seconds": timeout_seconds,
                    "effective_timeout_seconds": effective_timeout_seconds,
                    "saveas_timeout_seconds": saveas_timeout_seconds,
                    "launch_transport": launch_transport,
                    "status": "error",
                    "summary_parse_error": str(exc),
                },
                default_error_kind="registry-policy-summary-parse-error",
                default_recovery_action="rerun-registry-policy-probe",
                default_transport_blocker="summary-parse-error",
                default_guest_health="unknown",
            ),
            True,
        )


def main() -> int:
    parser = argparse.ArgumentParser(description="Stage and run run-registry-policy-probe.ps1 inside the KVM guest.")
    parser.add_argument("--repo-root", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--domain", default="regprobe-win11-25h2-session")
    parser.add_argument("--connect", default="qemu:///session")
    parser.add_argument("--bridge-base-url", default="http://10.0.2.2:8766")
    parser.add_argument("--upload-dir", default="/tmp/regprobe-bridge")
    parser.add_argument("--guest-scripts-root", default=r"C:\RegProbe-Diag\bootstrap")
    parser.add_argument("--delay-ms", default="18")
    parser.add_argument("--wake-key", default="KEY_ENTER")
    parser.add_argument("--timeout-seconds", type=int, default=180)
    parser.add_argument("--registry-path", required=True)
    parser.add_argument("--value-name", required=True)
    parser.add_argument("--output-name", required=True)
    parser.add_argument("--trigger-profile", required=True)
    parser.add_argument("--saveas-timeout-seconds", type=int, default=60)
    parser.add_argument("--powershell-command", default="")
    parser.add_argument("--match-fragment", action="append", default=[])
    parser.add_argument("--process-name", action="append", default=[])
    parser.add_argument("--launch-transport", choices=["auto", "qga", "send-key"], default="auto")
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    upload_dir = Path(args.upload_dir).resolve()
    summary_path = upload_dir / f"{args.output_name}-summary.json"
    probe_stage_path = upload_dir / f"{args.output_name}-probe-stage.json"
    result_path = upload_dir / f"{args.output_name}.txt"
    generated_dir = repo_root / "dist" / "kvm-generated"
    generated_dir.mkdir(parents=True, exist_ok=True)
    ensure_guest_bridge(repo_root=repo_root, bridge_base_url=args.bridge_base_url, upload_root=upload_dir)
    guest_scripts_root = args.guest_scripts_root

    bridge = args.bridge_base_url.rstrip("/")
    generated_name = f"guest-registry-probe-{args.output_name}.ps1"
    generated_path = generated_dir / generated_name

    guest_output_root = rf"C:\RegProbe-Diag\procmon\{args.output_name}"
    guest_wrapper_summary = guest_output_root + r"\run-summary.json"
    host_summary_name = f"{args.output_name}-summary.json"
    host_stage_name = f"{args.output_name}-launcher-stage.json"

    command_lines = [
        "$ErrorActionPreference = 'Stop'",
        f"$bridgeBase = {quote_ps(bridge)}",
        f"$hostSummaryUri = {quote_ps(bridge + '/' + host_summary_name)}",
        f"$hostStageUri = {quote_ps(bridge + '/' + host_stage_name)}",
        f"$guestScriptsRoot = {quote_ps(guest_scripts_root)}",
        f"$guestOutputRoot = {quote_ps(guest_output_root)}",
        f"$guestWrapperSummary = {quote_ps(guest_wrapper_summary)}",
        "function Publish-LauncherStage {",
        "    param([string]$Stage, [string]$Status, [string]$ErrorMessage = '')",
        "    try {",
        "        $payloadPath = Join-Path $guestOutputRoot 'launcher-stage.json'",
        "        [ordered]@{",
        "            generated_utc = [DateTime]::UtcNow.ToString('o')",
        f"            output_name = {quote_ps(args.output_name)}",
        "            stage = $Stage",
        "            status = $Status",
        "            error = $ErrorMessage",
        "        } | ConvertTo-Json -Depth 6 | Set-Content -Path $payloadPath -Encoding UTF8",
        "        Invoke-WebRequest -Method Put -Uri $hostStageUri -InFile $payloadPath -UseBasicParsing | Out-Null",
        "    }",
        "    catch {",
        "    }",
        "}",
        "$launcherSummary = [ordered]@{",
        "    generated_utc = [DateTime]::UtcNow.ToString('o')",
        f"    output_name = {quote_ps(args.output_name)}",
        f"    trigger_profile = {quote_ps(args.trigger_profile)}",
        "    status = 'starting'",
        "    stage = 'bootstrap'",
        "    error = $null",
        "    error_position = $null",
        "    wrapper_summary_exists = $false",
        "}",
        "try {",
        "    New-Item -ItemType Directory -Path $guestScriptsRoot -Force | Out-Null",
        "    New-Item -ItemType Directory -Path $guestOutputRoot -Force | Out-Null",
        "    Publish-LauncherStage -Stage 'bootstrap' -Status 'starting'",
        "    $launcherSummary.stage = 'download-registry-policy-probe'",
        "    Publish-LauncherStage -Stage 'download-registry-policy-probe' -Status 'starting'",
        (
            f"    Invoke-WebRequest -UseBasicParsing -Uri {quote_ps(bridge + '/scripts/vm/registry-policy-probe.ps1')} "
            f"-OutFile {quote_ps(guest_scripts_root + r'\\registry-policy-probe.ps1')}"
        ),
        "    $launcherSummary.stage = 'download-runner'",
        "    Publish-LauncherStage -Stage 'download-runner' -Status 'starting'",
        (
            f"    Invoke-WebRequest -UseBasicParsing -Uri {quote_ps(bridge + '/scripts/vm/guest-tools/run-registry-policy-probe.ps1')} "
            f"-OutFile {quote_ps(guest_scripts_root + r'\\run-registry-policy-probe.ps1')}"
        ),
    ]

    probe_command = [
        "&",
        quote_ps(guest_scripts_root + r"\run-registry-policy-probe.ps1"),
        "-RegistryPath",
        quote_ps(args.registry_path),
        "-ValueName",
        quote_ps(args.value_name),
        "-OutputName",
        quote_ps(args.output_name),
        "-TriggerProfile",
        quote_ps(args.trigger_profile),
        "-SaveAsTimeoutSeconds",
        str(args.saveas_timeout_seconds),
        "-ScriptsRoot",
        quote_ps(guest_scripts_root),
        "-UploadBaseUrl",
        quote_ps(bridge),
    ]

    if args.powershell_command:
        probe_command.extend(["-PowerShellCommand", quote_ps(args.powershell_command)])

    if args.match_fragment:
        probe_command.extend(["-MatchFragments", quote_ps_array(args.match_fragment)])

    if args.process_name:
        probe_command.extend(["-ProcessNames", quote_ps_array(args.process_name)])

    command_lines.extend(
        [
            "    $launcherSummary.stage = 'invoke-wrapper'",
            "    Publish-LauncherStage -Stage 'invoke-wrapper' -Status 'starting'",
            "    " + " ".join(probe_command),
            "    $launcherSummary.stage = 'wrapper-returned'",
            "    $launcherSummary.status = 'ok'",
            "    Publish-LauncherStage -Stage 'wrapper-returned' -Status 'ok'",
            "}",
            "catch {",
            "    $launcherSummary.status = 'error'",
            "    $launcherSummary.stage = 'launcher-exception'",
            "    $launcherSummary.error = $_.Exception.Message",
            "    if ($_.InvocationInfo) {",
            "        $launcherSummary.error_position = $_.InvocationInfo.PositionMessage",
            "    }",
            "    Publish-LauncherStage -Stage 'launcher-exception' -Status 'error' -ErrorMessage $_.Exception.Message",
            "}",
            "finally {",
            "    if (Test-Path $guestWrapperSummary) {",
            "        $launcherSummary.wrapper_summary_exists = $true",
            "        try {",
            "            Invoke-WebRequest -Method Put -Uri $hostSummaryUri -InFile $guestWrapperSummary -UseBasicParsing | Out-Null",
            "        }",
            "        catch {",
            "            $launcherSummary.status = 'wrapper-summary-upload-error'",
            "            $launcherSummary.error = $_.Exception.Message",
            "        }",
            "    }",
            "    else {",
            "        $launcherSummaryPath = Join-Path $guestOutputRoot 'launcher-summary.json'",
            "        $launcherSummary | ConvertTo-Json -Depth 8 | Set-Content -Path $launcherSummaryPath -Encoding UTF8",
            "        try {",
            "            Invoke-WebRequest -Method Put -Uri $hostSummaryUri -InFile $launcherSummaryPath -UseBasicParsing | Out-Null",
            "        }",
            "        catch {",
            "        }",
            "    }",
            "}",
        ]
    )
    guest_script = "\n".join(command_lines) + "\n"
    generated_path.write_text(guest_script, encoding="utf-8")

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
        launcher_transport = launch_generated_script(
            repo_root=repo_root,
            generated_path=generated_path,
            guest_launcher=guest_launcher,
            guest_scripts_root=guest_scripts_root,
            output_name=args.output_name,
            args=args,
        )
    except subprocess.CalledProcessError as exc:
        print(
            json.dumps(
                emit_launch_error(
                    summary_path=summary_path,
                    output_name=args.output_name,
                    registry_path=args.registry_path,
                    value_name=args.value_name,
                    trigger_profile=args.trigger_profile,
                    requested_launch_transport=args.launch_transport,
                    exc=exc,
                ),
                indent=2,
            )
        )
        return 1

    effective_timeout_seconds = max(args.timeout_seconds, args.saveas_timeout_seconds + 120)
    deadline = time.time() + effective_timeout_seconds
    while time.time() < deadline:
        if summary_path.exists():
            summary, parse_failed = load_summary_or_error(
                summary_path,
                output_name=args.output_name,
                trigger_profile=args.trigger_profile,
                timeout_seconds=args.timeout_seconds,
                effective_timeout_seconds=effective_timeout_seconds,
                saveas_timeout_seconds=args.saveas_timeout_seconds,
                launch_transport=launcher_transport,
            )
            if parse_failed:
                print(json.dumps(summary, indent=2))
                return 1
            payload = {
                "summary_path": str(summary_path),
                "output_name": args.output_name,
                "trigger_profile": args.trigger_profile,
                "timeout_seconds": args.timeout_seconds,
                "effective_timeout_seconds": effective_timeout_seconds,
                "saveas_timeout_seconds": args.saveas_timeout_seconds,
                "launch_transport": launcher_transport,
                "status": summary.get("status", "unknown"),
                "csv_exists": summary.get("csv_exists"),
                "hits_csv_exists": summary.get("hits_csv_exists"),
                "normalized_bundle_exists": summary.get("normalized_bundle_exists"),
                "normalization_status": summary.get("normalization_status"),
                "normalizer_name": summary.get("normalizer_name"),
                "probe_stage": summary.get("probe_stage"),
                "probe_stage_status": summary.get("probe_stage_status"),
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

        fallback = try_probe_stage_fallback(
            summary_path=summary_path,
            probe_stage_path=probe_stage_path,
            result_path=result_path,
            args=args,
        )
        if fallback is not None:
            _summary, payload = fallback
            print(json.dumps(payload, indent=2))
            return 1
        time.sleep(2)

    for _ in range(5):
        fallback = try_probe_stage_fallback(
            summary_path=summary_path,
            probe_stage_path=probe_stage_path,
            result_path=result_path,
            args=args,
        )
        if fallback is not None:
            _summary, payload = fallback
            print(json.dumps(payload, indent=2))
            return 1
        time.sleep(2)

    timeout_summary = write_summary_contract(
        summary_path,
        {
            "summary_path": str(summary_path),
            "output_name": args.output_name,
            "trigger_profile": args.trigger_profile,
            "timeout_seconds": args.timeout_seconds,
            "effective_timeout_seconds": effective_timeout_seconds,
            "saveas_timeout_seconds": args.saveas_timeout_seconds,
            "launch_transport": launcher_transport,
            "status": "timeout",
        },
        default_error_kind="runner-timeout",
        default_recovery_action="rerun-registry-policy-probe",
        default_transport_blocker="timeout",
        default_guest_health="unknown",
    )
    print(json.dumps(timeout_summary, indent=2))
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
