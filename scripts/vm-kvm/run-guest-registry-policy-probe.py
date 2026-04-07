#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
from pathlib import Path

from guest_bridge import ensure_guest_bridge


def quote_ps(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


def quote_ps_array(values: list[str]) -> str:
    return ", ".join(quote_ps(value) for value in values)


def run(cmd: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(cmd, cwd=str(cwd), check=True, capture_output=True, text=True)


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
    parser.add_argument("--powershell-command", default="")
    parser.add_argument("--match-fragment", action="append", default=[])
    parser.add_argument("--process-name", action="append", default=[])
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    upload_dir = Path(args.upload_dir).resolve()
    summary_path = upload_dir / f"{args.output_name}-summary.json"
    probe_stage_path = upload_dir / f"{args.output_name}-probe-stage.json"
    result_path = upload_dir / f"{args.output_name}.txt"
    generated_dir = repo_root / "dist" / "kvm-generated"
    generated_dir.mkdir(parents=True, exist_ok=True)
    ensure_guest_bridge(repo_root=repo_root, bridge_base_url=args.bridge_base_url, upload_root=upload_dir)
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
            guest_scripts_root := args.guest_scripts_root,
            "--delay-ms",
            args.delay_ms,
            "--marker-name",
            f"{args.output_name}-admin-shell-ready",
        ],
        cwd=repo_root,
    )

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

    deadline = time.time() + args.timeout_seconds
    while time.time() < deadline:
        if summary_path.exists():
            summary = json.loads(summary_path.read_text(encoding="utf-8-sig"))
            payload = {
                "summary_path": str(summary_path),
                "output_name": args.output_name,
                "trigger_profile": args.trigger_profile,
                "status": summary.get("status", "unknown"),
                "csv_exists": summary.get("csv_exists"),
                "hits_csv_exists": summary.get("hits_csv_exists"),
                "normalized_bundle_exists": summary.get("normalized_bundle_exists"),
                "normalization_status": summary.get("normalization_status"),
                "normalizer_name": summary.get("normalizer_name"),
                "probe_stage": summary.get("probe_stage"),
                "probe_stage_status": summary.get("probe_stage_status"),
                "error_kind": summary.get("error_kind"),
                "error": summary.get("error"),
            }
            print(json.dumps(payload, indent=2))
            if summary.get("status") == "error" or summary.get("normalization_status") not in {None, "ok"}:
                return 1
            return 0

        if probe_stage_path.exists() and result_path.exists():
            probe_stage = json.loads(probe_stage_path.read_text(encoding="utf-8-sig"))
            if probe_stage.get("status") == "error":
                result_error = None
                for line in result_path.read_text(encoding="utf-8-sig", errors="replace").splitlines():
                    if line.startswith("ERROR="):
                        result_error = line[len("ERROR=") :]
                        break

                summary = {
                    "generated_utc": probe_stage.get("generated_utc"),
                    "registry_path": args.registry_path,
                    "value_name": args.value_name,
                    "output_name": args.output_name,
                    "trigger_profile": args.trigger_profile,
                    "output_root": rf"C:\RegProbe-Diag\procmon\{args.output_name}",
                    "status": "error",
                    "result_exists": True,
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
                }
                summary_path.write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
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
                    "error": result_error or probe_stage.get("message"),
                    "summary_source": "probe-stage-fallback",
                }
                print(json.dumps(payload, indent=2))
                return 1
        time.sleep(2)

    print(
        json.dumps(
            {
                "summary_path": str(summary_path),
                "output_name": args.output_name,
                "trigger_profile": args.trigger_profile,
                "status": "timeout",
            },
            indent=2,
        )
    )
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
