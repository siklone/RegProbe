#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
from pathlib import Path

from guest_bridge import ensure_guest_bridge
from summary_contract_lib import apply_summary_contract, read_json_object, write_summary_contract


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
    output_name: str,
    binary_path: str,
    exc: subprocess.CalledProcessError,
) -> dict[str, object]:
    return write_summary_contract(
        summary_path,
        {
            "output_name": output_name,
            "binary_path": binary_path,
            "status": "error",
            "host_step": getattr(exc, "stage", None),
            "exit_code": exc.returncode,
            "command": [str(part) for part in exc.cmd] if isinstance(exc.cmd, list) else str(exc.cmd),
            "error": format_process_error(exc),
            "summary_source": "host-launch-failure",
        },
        default_error_kind="ghidra-symbolized-launch-error",
        default_recovery_action="rerun-ghidra-symbolized-probe",
        default_transport_blocker="launch-failed",
        default_guest_health="unknown",
    )


def load_summary_or_error(summary_path: Path, output_name: str, binary_path: str) -> tuple[dict[str, object], bool]:
    try:
        return read_json_object(summary_path, context="ghidra symbolized summary"), False
    except (OSError, json.JSONDecodeError, ValueError) as exc:
        return (
            write_summary_contract(
                summary_path,
                {
                    "output_name": output_name,
                    "binary_path": binary_path,
                    "status": "error",
                    "summary_parse_error": str(exc),
                },
                default_error_kind="ghidra-symbolized-summary-parse-error",
                default_recovery_action="rerun-ghidra-symbolized-probe",
                default_transport_blocker="summary-parse-error",
                default_guest_health="unknown",
            ),
            True,
        )


def load_stage_or_error(stage_path: Path, output_name: str, binary_path: str) -> tuple[dict[str, object] | None, dict[str, object] | None]:
    try:
        return read_json_object(stage_path, context="ghidra symbolized stage"), None
    except (OSError, json.JSONDecodeError, ValueError) as exc:
        return None, write_summary_contract(
            stage_path.with_name(f"{output_name}-summary.json"),
            {
                "output_name": output_name,
                "binary_path": binary_path,
                "status": "error",
                "summary_source": "launcher-stage-parse-error",
                "summary_parse_error": str(exc),
            },
            default_error_kind="ghidra-symbolized-stage-parse-error",
            default_recovery_action="rerun-ghidra-symbolized-probe",
            default_transport_blocker="summary-parse-error",
            default_guest_health="unknown",
        )


def main() -> int:
    parser = argparse.ArgumentParser(description="Stage and run run-ghidra-symbolized-probe.ps1 inside the KVM guest.")
    parser.add_argument("--repo-root", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--domain", default="regprobe-win11-25h2-session")
    parser.add_argument("--connect", default="qemu:///session")
    parser.add_argument("--bridge-base-url", default="http://10.0.2.2:8766")
    parser.add_argument("--upload-dir", default="/tmp/regprobe-bridge")
    parser.add_argument("--guest-scripts-root", default=r"C:\RegProbe-Diag\bootstrap")
    parser.add_argument("--delay-ms", default="18")
    parser.add_argument("--wake-key", default="KEY_ENTER")
    parser.add_argument("--timeout-seconds", type=int, default=900)
    parser.add_argument("--binary-path", required=True)
    parser.add_argument("--output-name", required=True)
    parser.add_argument("--pattern", action="append", default=[])
    parser.add_argument("--module-offset", action="append", default=[])
    parser.add_argument("--no-analysis", action="store_true")
    parser.add_argument("--skip-symchk", action="store_true")
    args = parser.parse_args()

    if not args.pattern:
        parser.error("At least one --pattern is required")

    repo_root = Path(args.repo_root).resolve()
    upload_dir = Path(args.upload_dir).resolve()
    summary_path = upload_dir / f"{args.output_name}-summary.json"
    stage_path = upload_dir / f"{args.output_name}-launcher-stage.json"
    generated_dir = repo_root / "dist" / "kvm-generated"
    generated_dir.mkdir(parents=True, exist_ok=True)

    for stale_path in (summary_path, stage_path):
        try:
            stale_path.unlink()
        except FileNotFoundError:
            pass

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
                f"{args.output_name}-admin-shell-ready",
            ],
            cwd=repo_root,
        )
    except subprocess.CalledProcessError as exc:
        print(json.dumps(emit_host_step_error(summary_path=summary_path, output_name=args.output_name, binary_path=args.binary_path, exc=annotate_process_error(exc, stage="ensure-admin-shell")), indent=2))
        return 1

    bridge = args.bridge_base_url.rstrip("/")
    generated_name = f"guest-ghidra-symbolized-{args.output_name}.ps1"
    generated_path = generated_dir / generated_name

    guest_output_root = rf"C:\RegProbe-Diag\ghidra\{args.output_name}"
    guest_summary_path = guest_output_root + r"\run-summary.json"
    host_summary_name = f"{args.output_name}-summary.json"
    host_stage_name = f"{args.output_name}-launcher-stage.json"

    command_lines = [
        "$ErrorActionPreference = 'Stop'",
        f"$bridgeBase = {quote_ps(bridge)}",
        f"$hostSummaryUri = {quote_ps(bridge + '/' + host_summary_name)}",
        f"$hostStageUri = {quote_ps(bridge + '/' + host_stage_name)}",
        f"$guestScriptsRoot = {quote_ps(guest_scripts_root)}",
        f"$guestOutputRoot = {quote_ps(guest_output_root)}",
        f"$guestSummaryPath = {quote_ps(guest_summary_path)}",
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
        f"    binary_path = {quote_ps(args.binary_path)}",
        f"    module_offsets = @({quote_ps_array(args.module_offset)})",
        "    status = 'starting'",
        "    stage = 'bootstrap'",
        "    wrapper_summary_exists = $false",
        "    error = $null",
        "}",
        "try {",
        "    New-Item -ItemType Directory -Path $guestScriptsRoot -Force | Out-Null",
        "    New-Item -ItemType Directory -Path (Join-Path $guestScriptsRoot 'ghidra') -Force | Out-Null",
        "    New-Item -ItemType Directory -Path $guestOutputRoot -Force | Out-Null",
        "    Publish-LauncherStage -Stage 'download-scripts' -Status 'starting'",
        (
            f"    Invoke-WebRequest -UseBasicParsing -Uri {quote_ps(bridge + '/scripts/vm/guest-tools/run-ghidra-symbolized-probe.ps1')} "
            f"-OutFile {quote_ps(guest_scripts_root + r'\\run-ghidra-symbolized-probe.ps1')}"
        ),
        (
            f"    Invoke-WebRequest -UseBasicParsing -Uri {quote_ps(bridge + '/scripts/vm/guest-tools/ghidra-headless.cmd')} "
            f"-OutFile {quote_ps(guest_scripts_root + r'\\ghidra-headless.cmd')}"
        ),
        (
            f"    Invoke-WebRequest -UseBasicParsing -Uri {quote_ps(bridge + '/scripts/vm/ghidra/ExportBranchAnalysis.java')} "
            f"-OutFile {quote_ps(guest_scripts_root + r'\\ghidra\\ExportBranchAnalysis.java')}"
        ),
        (
            f"    Invoke-WebRequest -UseBasicParsing -Uri {quote_ps(bridge + '/scripts/vm/ghidra/SetPdbSymbolRepository.java')} "
            f"-OutFile {quote_ps(guest_scripts_root + r'\\ghidra\\SetPdbSymbolRepository.java')}"
        ),
    ]

    probe_command = [
        "&",
        quote_ps(guest_scripts_root + r"\run-ghidra-symbolized-probe.ps1"),
        "-BinaryPath",
        quote_ps(args.binary_path),
        "-OutputName",
        quote_ps(args.output_name),
        "-ScriptsRoot",
        quote_ps(guest_scripts_root),
        "-UploadBaseUrl",
        quote_ps(bridge),
        "-Patterns",
        f"@({quote_ps_array(args.pattern)})",
    ]
    if args.module_offset:
        probe_command.extend(["-ModuleOffsets", f"@({quote_ps_array(args.module_offset)})"])
    if args.no_analysis:
        probe_command.append("-NoAnalysis")
    if args.skip_symchk:
        probe_command.append("-SkipSymchk")

    command_lines.extend(
        [
            "    Publish-LauncherStage -Stage 'invoke-wrapper' -Status 'starting'",
            "    " + " ".join(probe_command),
            "    $launcherSummary.status = 'ok'",
            "    $launcherSummary.stage = 'wrapper-returned'",
            "    Publish-LauncherStage -Stage 'wrapper-returned' -Status 'ok'",
            "}",
            "catch {",
            "    $launcherSummary.status = 'error'",
            "    $launcherSummary.stage = 'launcher-exception'",
            "    $launcherSummary.error = $_.Exception.Message",
            "    Publish-LauncherStage -Stage 'launcher-exception' -Status 'error' -ErrorMessage $_.Exception.Message",
            "}",
            "finally {",
            "    if (Test-Path $guestSummaryPath) {",
            "        $launcherSummary.wrapper_summary_exists = $true",
            "        try {",
            "            Invoke-WebRequest -Method Put -Uri $hostSummaryUri -InFile $guestSummaryPath -UseBasicParsing | Out-Null",
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
        print(json.dumps(emit_host_step_error(summary_path=summary_path, output_name=args.output_name, binary_path=args.binary_path, exc=annotate_process_error(exc, stage="type-to-guest")), indent=2))
        return 1

    deadline = time.time() + args.timeout_seconds
    while time.time() < deadline:
        if stage_path.exists() and not summary_path.exists():
            stage_payload, stage_parse_error = load_stage_or_error(stage_path, args.output_name, args.binary_path)
            if stage_parse_error is not None:
                summary_path.write_text(json.dumps(stage_parse_error, indent=2) + "\n", encoding="utf-8")
                print(json.dumps(stage_parse_error, indent=2))
                return 1
            assert stage_payload is not None
            if str(stage_payload.get("status", "")).lower() == "error":
                error_summary = write_summary_contract(
                    summary_path,
                    {
                        "output_name": args.output_name,
                        "binary_path": args.binary_path,
                        "status": "error",
                        "error_kind": "guest-launcher-error",
                        "recovery_action": "inspect-launcher-stage",
                        "transport_blocker": "launcher-error",
                        "guest_health": "degraded",
                        "launcher_stage": stage_payload,
                    },
                )
                print(json.dumps(error_summary, indent=2))
                return 1

        if summary_path.exists():
            summary, parse_failed = load_summary_or_error(summary_path, args.output_name, args.binary_path)
            if parse_failed:
                print(json.dumps(summary, indent=2))
                return 1
            if "status" not in summary:
                ghidra_exit = summary.get("ghidra_exit_code")
                summary["status"] = "ok" if ghidra_exit == 0 else "error"

            summary = apply_summary_contract(
                summary,
                default_error_kind="ghidra-symbolized-probe-error",
                default_recovery_action="inspect-ghidra-symbolized-run",
                default_transport_blocker="ghidra",
                default_guest_health="stable" if summary.get("status") == "ok" else "degraded",
            )
            summary_path.write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
            print(json.dumps(summary, indent=2))
            return 0 if summary.get("status") == "ok" else 1

        time.sleep(2)

    timeout_summary = write_summary_contract(
        summary_path,
        {
            "output_name": args.output_name,
            "binary_path": args.binary_path,
            "status": "timeout",
        },
        default_error_kind="runner-timeout",
        default_recovery_action="rerun-ghidra-symbolized-probe",
        default_transport_blocker="timeout",
        default_guest_health="unknown",
    )
    print(json.dumps(timeout_summary, indent=2))
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
