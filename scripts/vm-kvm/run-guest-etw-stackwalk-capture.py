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


def run(cmd: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(cmd, cwd=str(cwd), check=True, capture_output=True, text=True)


def wait_for_file(path: Path, timeout_seconds: int) -> bool:
    deadline = time.time() + timeout_seconds
    while time.time() < deadline:
        if path.exists():
            return True
        time.sleep(2)
    return path.exists()


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
            raise subprocess.CalledProcessError(
                qga_result.returncode,
                qga_cmd,
                output=qga_result.stdout,
                stderr=qga_result.stderr,
            )
        sys.stderr.write("[run-guest-etw-stackwalk-capture] qga launch failed, falling back to send-key transport.\n")
        if qga_result.stdout:
            sys.stderr.write(qga_result.stdout)
        if qga_result.stderr:
            sys.stderr.write(qga_result.stderr)

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
        "-RegistryPath",
        quote_ps(args.registry_path),
        "-ValueName",
        quote_ps(args.value_name),
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


def main() -> int:
    parser = argparse.ArgumentParser(description="Run the ETW registry stackwalk capture helper inside the KVM guest.")
    parser.add_argument("--repo-root", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--domain", default="regprobe-win11-25h2-session")
    parser.add_argument("--connect", default="qemu:///session")
    parser.add_argument("--bridge-base-url", default="http://10.0.2.2:8766")
    parser.add_argument("--upload-dir", default="/tmp/regprobe-bridge")
    parser.add_argument("--guest-scripts-root", default=r"C:\RegProbe-Diag\bootstrap")
    parser.add_argument("--delay-ms", default="18")
    parser.add_argument("--wake-key", default="KEY_ENTER")
    parser.add_argument("--timeout-seconds", type=int, default=180)
    parser.add_argument("--qga-retry-seconds", type=int, default=30)
    parser.add_argument("--qga-retry-interval-seconds", type=int, default=5)
    parser.add_argument("--launch-transport", choices=["auto", "qga", "send-key"], default="auto")
    parser.add_argument("--run-id", default="wave4-registry-stackwalk")
    parser.add_argument("--duration-seconds", type=int, default=60)
    parser.add_argument("--registry-path", default=r"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel")
    parser.add_argument("--value-name", default="TimerCheckFlags")
    parser.add_argument("--upload-etl", action="store_true")
    parser.add_argument("--skip-tracerpt", action="store_true")
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    upload_dir = Path(args.upload_dir).resolve()
    upload_dir.mkdir(parents=True, exist_ok=True)
    safe_run_id = "".join(ch if ch.isalnum() or ch in "._-" else "-" for ch in args.run_id).strip("-") or "registry-stackwalk"
    summary_path = upload_dir / f"{safe_run_id}-summary.json"
    xml_path = upload_dir / f"{safe_run_id}.xml"
    etl_path = upload_dir / f"{safe_run_id}.etl"
    for path in (summary_path, xml_path, etl_path):
        path.unlink(missing_ok=True)

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

    if not wait_for_file(summary_path, args.timeout_seconds):
        print(
            json.dumps(
                {
                    "status": "timeout",
                    "summary_path": str(summary_path),
                    "run_id": safe_run_id,
                    "launch_transport": launch_transport,
                },
                indent=2,
            )
        )
        return 2

    summary = json.loads(summary_path.read_text(encoding="utf-8-sig"))
    payload = {
        "status": summary.get("status", "unknown"),
        "error_kind": summary.get("error_kind"),
        "error": summary.get("error"),
        "run_id": safe_run_id,
        "launch_transport": launch_transport,
        "summary_path": str(summary_path),
        "xml_path": str(xml_path) if xml_path.exists() else None,
        "etl_path": str(etl_path) if etl_path.exists() else None,
        "stack_field_hit_count": summary.get("stack_field_hit_count"),
        "etl_exists": summary.get("etl_exists"),
        "xml_exists": summary.get("xml_exists"),
    }
    print(json.dumps(payload, indent=2))
    return 0 if payload["status"] == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
