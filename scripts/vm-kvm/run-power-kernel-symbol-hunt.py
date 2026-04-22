#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
CURRENT_DIR = Path(__file__).resolve().parent
if str(CURRENT_DIR) not in sys.path:
    sys.path.insert(0, str(CURRENT_DIR))

from command_json_lib import extract_json_object  # noqa: E402


def audit_root(repo_root: Path) -> Path:
    return repo_root / "registry-research-framework" / "audit"


def local_kd_runner_path(repo_root: Path) -> Path:
    return repo_root / "scripts" / "vm-kvm" / "run-guest-local-kd-smoke.py"


def init_walker_command_file(repo_root: Path) -> Path:
    return audit_root(repo_root) / "execution-required-init-walker-reacquire-local-kd-20260422.txt"


def consumers_command_file(repo_root: Path) -> Path:
    return audit_root(repo_root) / "execution-required-consumers-reacquire-local-kd-20260422.txt"


def global_timer_command_file(repo_root: Path) -> Path:
    return audit_root(repo_root) / "global-timer-resolution-reader-reacquire-local-kd-20260422.txt"


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


def portable_path(path: Path, repo_root: Path) -> str:
    try:
        return str(path.resolve().relative_to(repo_root)).replace("\\", "/")
    except ValueError:
        return str(path.resolve()).replace("\\", "/")


def try_parse_json_object(stdout: str) -> tuple[dict[str, object], str | None]:
    try:
        return extract_json_object(stdout), None
    except ValueError as exc:
        return {}, str(exc)


def build_pass_command(*, repo_root: Path, output_name: str, command_file: Path, args: argparse.Namespace) -> list[str]:
    kd_commands = load_kd_commands(command_file)
    cmd = [
        sys.executable,
        str(local_kd_runner_path(repo_root)),
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
    return cmd


def run_pass(*, repo_root: Path, output_name: str, command_file: Path, args: argparse.Namespace) -> dict[str, object]:
    kd_commands = load_kd_commands(command_file)
    cmd = build_pass_command(repo_root=repo_root, output_name=output_name, command_file=command_file, args=args)
    proc = subprocess.run(cmd, cwd=str(repo_root), capture_output=True, text=True)
    stdout = proc.stdout.strip()
    payload, parse_error = try_parse_json_object(stdout)
    return {
        "output_name": output_name,
        "command_file": portable_path(command_file, repo_root),
        "kd_command_count": len(kd_commands),
        "returncode": proc.returncode,
        "runner_payload": payload,
        "runner_stdout_parse_error": parse_error,
        "stdout": stdout,
        "stderr": proc.stderr.strip(),
    }


def build_plan_payload(args: argparse.Namespace, repo_root: Path) -> dict[str, object]:
    passes = [
        {
            "name": "execution-required-init-walker",
            "output_name": args.init_walker_output_name,
            "command_file": init_walker_command_file(repo_root),
        },
        {
            "name": "execution-required-consumers",
            "output_name": args.consumers_output_name,
            "command_file": consumers_command_file(repo_root),
        },
        {
            "name": "global-timer-resolution-reader",
            "output_name": args.global_timer_output_name,
            "command_file": global_timer_command_file(repo_root),
        },
    ]
    return {
        "mode": "dry-run",
        "runner": portable_path(Path(__file__), repo_root),
        "record_ids": [
            "power.control.allow-system-required-power-requests",
            "power.control.allow-audio-to-enable-execution-required-power-requests",
            "system.kernel.global-timer-resolution-requests",
        ],
        "passes": [
            {
                "name": item["name"],
                "output_name": item["output_name"],
                "command_file": portable_path(item["command_file"], repo_root),
                "kd_command_count": len(load_kd_commands(item["command_file"])) if item["command_file"].exists() else 0,
                "command": build_pass_command(
                    repo_root=repo_root,
                    output_name=item["output_name"],
                    command_file=item["command_file"],
                    args=args,
                )
                if item["command_file"].exists()
                else [],
            }
            for item in passes
        ],
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Run narrow local-KD symbol hunts for the execution-required pair and GlobalTimerResolutionRequests."
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
    parser.add_argument("--init-walker-output-name", default="local-kd-execution-required-init-walker-20260422a")
    parser.add_argument("--consumers-output-name", default="local-kd-execution-required-consumers-20260422a")
    parser.add_argument("--global-timer-output-name", default="local-kd-global-timer-resolution-reader-20260422a")
    parser.add_argument("--dry-run", action="store_true", help="Print the planned local-KD smoke commands without touching the VM.")
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    if args.dry_run:
        print(json.dumps(build_plan_payload(args, repo_root), indent=2))
        return 0

    passes = [
        (
            "execution-required-init-walker",
            args.init_walker_output_name,
            init_walker_command_file(repo_root),
        ),
        (
            "execution-required-consumers",
            args.consumers_output_name,
            consumers_command_file(repo_root),
        ),
        (
            "global-timer-resolution-reader",
            args.global_timer_output_name,
            global_timer_command_file(repo_root),
        ),
    ]
    results = []
    overall_ok = True
    for name, output_name, command_file in passes:
        result = run_pass(repo_root=repo_root, output_name=output_name, command_file=command_file, args=args)
        result["name"] = name
        results.append(result)
        if result["returncode"] != 0 or result.get("runner_stdout_parse_error"):
            overall_ok = False

    print(
        json.dumps(
            {
                "runner": portable_path(Path(__file__), repo_root),
                "record_ids": [
                    "power.control.allow-system-required-power-requests",
                    "power.control.allow-audio-to-enable-execution-required-power-requests",
                    "system.kernel.global-timer-resolution-requests",
                ],
                "passes": results,
            },
            indent=2,
        )
    )
    return 0 if overall_ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
