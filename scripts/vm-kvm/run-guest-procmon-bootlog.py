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


def wait_for_file(path: Path, timeout_seconds: int) -> bool:
    deadline = time.time() + timeout_seconds
    while time.time() < deadline:
        if path.exists():
            return True
        time.sleep(2)
    return path.exists()


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
    parser.add_argument("--domain", default="regprobe-win11-25h2-session")
    parser.add_argument("--connect", default="qemu:///session")
    parser.add_argument("--bridge-base-url", default="http://10.0.2.2:8766")
    parser.add_argument("--upload-dir", default="/tmp/regprobe-bridge")
    parser.add_argument("--guest-scripts-root", default=r"C:\RegProbe-Diag\bootstrap")
    parser.add_argument("--delay-ms", default="18")
    parser.add_argument("--wake-key", default="KEY_ENTER")
    parser.add_argument("--prepare-timeout-seconds", type=int, default=180)
    parser.add_argument("--timeout-seconds", type=int, default=420)
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
    hits_path = upload_dir / f"{args.output_name}.hits.csv"
    for path in (arm_summary_path, collect_summary_path, summary_path, hits_path):
        if path.exists():
            path.unlink()

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
            f"{args.output_name}-bootlog-arm-ready",
        ],
        cwd=repo_root,
    )

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

    if not wait_for_file(arm_summary_path, args.prepare_timeout_seconds):
        print(
            json.dumps(
                {
                    "summary_arm_path": str(arm_summary_path),
                    "output_name": args.output_name,
                    "status": "prepare-timeout",
                },
                indent=2,
            )
        )
        return 2

    run(["virsh", "-c", args.connect, args.host_reboot_mode, args.domain], cwd=repo_root)
    time.sleep(args.reboot_settle_seconds)

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

    if wait_for_file(summary_path, args.timeout_seconds):
        summary = json.loads(summary_path.read_text(encoding="utf-8-sig"))
        payload = {
            "summary_arm_path": str(arm_summary_path),
            "summary_collect_path": str(collect_summary_path),
            "summary_path": str(summary_path),
            "hits_path": str(hits_path),
            "output_name": args.output_name,
            "reboot_observed": summary.get("reboot_observed"),
            "csv_exists": summary.get("csv_exists"),
            "match_count": summary.get("match_count"),
            "csv_row_count": summary.get("csv_row_count"),
        }
        print(json.dumps(payload, indent=2))
        return 0

    print(
        json.dumps(
            {
                "summary_arm_path": str(arm_summary_path),
                "summary_collect_path": str(collect_summary_path),
                "summary_path": str(summary_path),
                "output_name": args.output_name,
                "status": "timeout",
            },
            indent=2,
        )
    )
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
