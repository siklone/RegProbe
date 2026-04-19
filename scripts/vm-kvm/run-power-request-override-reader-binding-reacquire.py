#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
AUDIT_ROOT = REPO_ROOT / "registry-research-framework" / "audit"
RUNNER_PATH = REPO_ROOT / "scripts" / "vm-kvm" / "run-guest-local-kd-smoke.py"

RESPONSE_COMMAND_FILE = AUDIT_ROOT / "power-request-override-response-reacquire-local-kd-20260419.txt"
UMPO_COMMAND_FILE = AUDIT_ROOT / "power-request-override-umpo-message-reacquire-local-kd-20260419.txt"


def load_kd_commands(path: Path) -> list[str]:
    commands: list[str] = []
    for raw_line in path.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line:
            continue
        lowered = line.lower()
        if lowered in {".echo regprobe_localkd_begin", ".echo regprobe_localkd_end", "q"}:
            continue
        commands.append(line)
    return commands


def run_pass(*, repo_root: Path, output_name: str, command_file: Path, args: argparse.Namespace) -> dict[str, object]:
    kd_commands = load_kd_commands(command_file)
    cmd = [
        sys.executable,
        str(RUNNER_PATH),
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
        args.guest_scripts_root,
        "--delay-ms",
        args.delay_ms,
        "--wake-key",
        args.wake_key,
        "--timeout-seconds",
        str(args.timeout_seconds),
        "--smoke-timeout-seconds",
        str(args.smoke_timeout_seconds),
        "--output-name",
        output_name,
    ]
    for kd_command in kd_commands:
        cmd.extend(["--kd-command", kd_command])

    proc = subprocess.run(cmd, cwd=str(repo_root), capture_output=True, text=True)
    stdout = proc.stdout.strip()
    payload = json.loads(stdout) if stdout else {}
    return {
        "output_name": output_name,
        "command_file": str(command_file.relative_to(repo_root)).replace("\\", "/"),
        "kd_command_count": len(kd_commands),
        "returncode": proc.returncode,
        "runner_payload": payload,
        "stderr": proc.stderr.strip(),
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Run the PowerRequestOverride reader-binding response and UMPO local-KD reacquire passes."
    )
    parser.add_argument("--repo-root", default=str(REPO_ROOT))
    parser.add_argument("--domain", default="regprobe-win11-25h2-session")
    parser.add_argument("--connect", default="qemu:///session")
    parser.add_argument("--bridge-base-url", default="http://10.0.2.2:8766")
    parser.add_argument("--upload-dir", default="/tmp/regprobe-bridge")
    parser.add_argument("--guest-scripts-root", default=r"C:\RegProbe-Diag\bootstrap")
    parser.add_argument("--delay-ms", default="18")
    parser.add_argument("--wake-key", default="KEY_ENTER")
    parser.add_argument("--timeout-seconds", type=int, default=240)
    parser.add_argument("--smoke-timeout-seconds", type=int, default=180)
    parser.add_argument("--response-output-name", default="local-kd-powerrequest-response-reacquire-20260419a")
    parser.add_argument("--umpo-output-name", default="local-kd-powerrequest-umpo-message-reacquire-20260419a")
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    response = run_pass(
        repo_root=repo_root,
        output_name=args.response_output_name,
        command_file=RESPONSE_COMMAND_FILE,
        args=args,
    )
    umpo = run_pass(
        repo_root=repo_root,
        output_name=args.umpo_output_name,
        command_file=UMPO_COMMAND_FILE,
        args=args,
    )

    payload = {
        "runner": str(Path(__file__).resolve().relative_to(repo_root)).replace("\\", "/"),
        "record_id": "power.control.power-request-override-subtree",
        "passes": [response, umpo],
    }
    print(json.dumps(payload, indent=2))
    return 0 if response["returncode"] == 0 and umpo["returncode"] == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())
