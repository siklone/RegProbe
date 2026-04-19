#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
RUNNER_PATH = REPO_ROOT / "scripts" / "vm-kvm" / "run-power-request-override-reader-binding-reacquire.py"
LEDGER_GENERATOR_PATH = (
    REPO_ROOT / "registry-research-framework" / "scripts" / "generate_power_request_override_result_ledger.py"
)


def artifact_paths(upload_dir: Path, output_name: str) -> dict[str, Path]:
    return {
        "stdout": upload_dir / f"{output_name}.stdout.txt",
        "summary": upload_dir / f"{output_name}-summary.json",
    }


def portable_path(path: Path, repo_root: Path) -> str:
    try:
        return str(path.resolve().relative_to(repo_root)).replace("\\", "/")
    except ValueError:
        return str(path.resolve()).replace("\\", "/")


def build_runner_command(args: argparse.Namespace, repo_root: Path, upload_dir: Path) -> list[str]:
    return [
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
        str(upload_dir),
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
        "--response-output-name",
        args.response_output_name,
        "--umpo-output-name",
        args.umpo_output_name,
    ]


def build_generator_command(args: argparse.Namespace, repo_root: Path, upload_dir: Path) -> list[str]:
    response_paths = artifact_paths(upload_dir, args.response_output_name)
    umpo_paths = artifact_paths(upload_dir, args.umpo_output_name)
    return [
        sys.executable,
        str(LEDGER_GENERATOR_PATH),
        "--run-id",
        args.run_id,
        "--response-stdout",
        str(response_paths["stdout"]),
        "--response-summary",
        str(response_paths["summary"]),
        "--umpo-stdout",
        str(umpo_paths["stdout"]),
        "--umpo-summary",
        str(umpo_paths["summary"]),
        "--output-json",
        str(Path(args.output_json).resolve()),
        "--output-md",
        str(Path(args.output_md).resolve()),
    ]


def build_plan_payload(args: argparse.Namespace, repo_root: Path, upload_dir: Path) -> dict[str, object]:
    response_paths = artifact_paths(upload_dir, args.response_output_name)
    umpo_paths = artifact_paths(upload_dir, args.umpo_output_name)
    return {
        "mode": "dry-run" if args.dry_run else "execute",
        "runner": portable_path(RUNNER_PATH, repo_root),
        "ledger_generator": portable_path(LEDGER_GENERATOR_PATH, repo_root),
        "runner_command": build_runner_command(args, repo_root, upload_dir),
        "generator_command": build_generator_command(args, repo_root, upload_dir),
        "expected_artifacts": {
            "response": {key: str(value) for key, value in response_paths.items()},
            "umpo": {key: str(value) for key, value in umpo_paths.items()},
        },
        "ledger_outputs": {
            "json": str(Path(args.output_json).resolve()),
            "markdown": str(Path(args.output_md).resolve()),
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Run the PowerRequestOverride reacquire wrapper and then generate a prefilled result ledger."
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
    parser.add_argument("--run-id", default="power-request-override-reader-binding-reacquire")
    parser.add_argument(
        "--output-json",
        default=str(REPO_ROOT / "registry-research-framework" / "audit" / "power-request-override-reader-binding-result-ledger-autofill.json"),
    )
    parser.add_argument(
        "--output-md",
        default=str(REPO_ROOT / "registry-research-framework" / "audit" / "power-request-override-reader-binding-result-ledger-autofill.md"),
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Print the planned reacquire and ledger-generation commands without touching the VM.",
    )
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    upload_dir = Path(args.upload_dir).resolve()

    if args.dry_run:
        print(json.dumps(build_plan_payload(args, repo_root, upload_dir), indent=2))
        return 0

    runner_cmd = build_runner_command(args, repo_root, upload_dir)
    runner_proc = subprocess.run(runner_cmd, cwd=str(repo_root), capture_output=True, text=True)
    if runner_proc.returncode != 0:
        if runner_proc.stdout:
            print(runner_proc.stdout)
        if runner_proc.stderr:
            print(runner_proc.stderr, file=sys.stderr)
        return runner_proc.returncode

    generator_cmd = build_generator_command(args, repo_root, upload_dir)
    generator_proc = subprocess.run(generator_cmd, cwd=str(repo_root), capture_output=True, text=True)
    if generator_proc.returncode != 0:
        if generator_proc.stdout:
            print(generator_proc.stdout)
        if generator_proc.stderr:
            print(generator_proc.stderr, file=sys.stderr)
        return generator_proc.returncode

    payload = {
        "runner": str(RUNNER_PATH.relative_to(repo_root)).replace("\\", "/"),
        "ledger_generator": str(LEDGER_GENERATOR_PATH.relative_to(repo_root)).replace("\\", "/"),
        "runner_output": json.loads(runner_proc.stdout),
        "ledger_output": json.loads(generator_proc.stdout),
    }
    print(json.dumps(payload, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
