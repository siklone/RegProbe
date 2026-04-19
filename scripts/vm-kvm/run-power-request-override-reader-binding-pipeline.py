#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]


def runner_path(repo_root: Path) -> Path:
    return repo_root / "scripts" / "vm-kvm" / "run-power-request-override-reader-binding-reacquire.py"


def ledger_generator_path(repo_root: Path) -> Path:
    return repo_root / "registry-research-framework" / "scripts" / "generate_power_request_override_result_ledger.py"


def ledger_promoter_path(repo_root: Path) -> Path:
    return repo_root / "registry-research-framework" / "scripts" / "promote_power_request_override_result_ledger.py"


def bundle_verifier_path(repo_root: Path) -> Path:
    return repo_root / "registry-research-framework" / "scripts" / "verify_power_request_override_handoff_bundle.py"


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


def slugify_fragment(value: str) -> str:
    return re.sub(r"[^A-Za-z0-9._-]+", "-", value.strip()).strip("-._").lower()


def parse_json_object(stdout: str) -> dict[str, object]:
    text = stdout.strip()
    if not text:
        return {}
    try:
        payload = json.loads(text)
    except json.JSONDecodeError:
        decoder = json.JSONDecoder()
        for index, character in enumerate(text):
            if character != "{":
                continue
            try:
                payload, _ = decoder.raw_decode(text[index:])
            except json.JSONDecodeError:
                continue
            if isinstance(payload, dict):
                return payload
        preview = text[:500].replace("\n", "\\n")
        raise ValueError(f"stdout did not contain a JSON object: {preview}") from None
    if not isinstance(payload, dict):
        raise ValueError("stdout JSON payload is not an object")
    return payload


def try_parse_json_object(stdout: str) -> tuple[dict[str, object], str | None]:
    try:
        return parse_json_object(stdout), None
    except ValueError as exc:
        return {}, str(exc)


def build_runner_command(args: argparse.Namespace, repo_root: Path, upload_dir: Path) -> list[str]:
    return [
        sys.executable,
        str(runner_path(repo_root)),
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
        str(ledger_generator_path(repo_root)),
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
    resolved_runner = runner_path(repo_root)
    resolved_ledger_generator = ledger_generator_path(repo_root)
    resolved_ledger_promoter = ledger_promoter_path(repo_root)
    resolved_bundle_verifier = bundle_verifier_path(repo_root)
    return {
        "mode": "dry-run" if args.dry_run else "execute",
        "runner": portable_path(resolved_runner, repo_root),
        "ledger_generator": portable_path(resolved_ledger_generator, repo_root),
        "ledger_promoter": portable_path(resolved_ledger_promoter, repo_root),
        "bundle_verifier": portable_path(resolved_bundle_verifier, repo_root),
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
        "scratch_policy": "The default ledger outputs are local-only gitignored autofill drafts; review them first, then promote them into dated audit files.",
        "verify_bundle_first": {
            "script": portable_path(resolved_bundle_verifier, repo_root),
            "example": f"python3 {portable_path(resolved_bundle_verifier, repo_root)}",
            "markdown_example": f"python3 {portable_path(resolved_bundle_verifier, repo_root)} --markdown",
        },
        "promote_after_review": build_promotion_payload(args, repo_root),
    }


def build_promotion_payload(args: argparse.Namespace, repo_root: Path) -> dict[str, str]:
    resolved_ledger_promoter = ledger_promoter_path(repo_root)
    source_json = Path(args.output_json).resolve()
    source_md = Path(args.output_md).resolve()
    suffix = slugify_fragment(args.run_id) or "power-request-override-reader-binding"
    target_stem = f"power-request-override-reader-binding-result-ledger-{suffix}"
    promoter_rel = portable_path(resolved_ledger_promoter, repo_root)
    return {
        "scratch_policy": "The default ledger outputs are local-only gitignored autofill drafts; review them first, then promote them into dated audit files.",
        "script": promoter_rel,
        "source_json": portable_path(source_json, repo_root),
        "source_md": portable_path(source_md, repo_root),
        "target_json": portable_path(source_json.parent / f"{target_stem}.json", repo_root),
        "target_md": portable_path(source_md.parent / f"{target_stem}.md", repo_root),
        "example": f"python3 {promoter_rel} --run-id <dated-run-id>",
        "dry_run_example": f"python3 {promoter_rel} --run-id <dated-run-id> --dry-run",
        "current_run_id": args.run_id,
        "current_run_example": f"python3 {promoter_rel} --run-id {args.run_id}",
        "current_run_dry_run_example": f"python3 {promoter_rel} --run-id {args.run_id} --dry-run",
        "overwrite_policy": "The promote step refuses to overwrite an existing dated ledger unless --force is passed intentionally.",
    }


def build_bundle_verifier_payload(repo_root: Path) -> dict[str, str]:
    resolved_bundle_verifier = bundle_verifier_path(repo_root)
    verifier_rel = portable_path(resolved_bundle_verifier, repo_root)
    return {
        "script": verifier_rel,
        "example": f"python3 {verifier_rel}",
        "markdown_example": f"python3 {verifier_rel} --markdown",
    }


def execute_pipeline(args: argparse.Namespace, repo_root: Path, upload_dir: Path) -> tuple[dict[str, object], int]:
    runner_cmd = build_runner_command(args, repo_root, upload_dir)
    runner_proc = subprocess.run(runner_cmd, cwd=str(repo_root), capture_output=True, text=True)
    runner_output, runner_parse_error = try_parse_json_object(runner_proc.stdout)
    runner_exit_code = runner_proc.returncode if runner_proc.returncode != 0 else (1 if runner_parse_error else 0)

    generator_cmd = build_generator_command(args, repo_root, upload_dir)
    generator_proc = subprocess.run(generator_cmd, cwd=str(repo_root), capture_output=True, text=True)
    generator_output, generator_parse_error = try_parse_json_object(generator_proc.stdout)
    if generator_proc.returncode != 0:
        payload = {
            "runner": portable_path(runner_path(repo_root), repo_root),
            "ledger_generator": portable_path(ledger_generator_path(repo_root), repo_root),
            "bundle_verifier": build_bundle_verifier_payload(repo_root),
            "promote_after_review": build_promotion_payload(args, repo_root),
            "runner_returncode": runner_proc.returncode,
            "runner_output": runner_output,
            "runner_stdout_parse_error": runner_parse_error,
            "runner_stderr": runner_proc.stderr.strip(),
            "ledger_generator_returncode": generator_proc.returncode,
            "ledger_generator_stdout": generator_proc.stdout.strip(),
            "ledger_generator_stderr": generator_proc.stderr.strip(),
        }
        return payload, generator_proc.returncode
    if generator_parse_error:
        payload = {
            "runner": portable_path(runner_path(repo_root), repo_root),
            "ledger_generator": portable_path(ledger_generator_path(repo_root), repo_root),
            "bundle_verifier": build_bundle_verifier_payload(repo_root),
            "promote_after_review": build_promotion_payload(args, repo_root),
            "runner_returncode": runner_proc.returncode,
            "runner_output": runner_output,
            "runner_stdout_parse_error": runner_parse_error,
            "runner_stderr": runner_proc.stderr.strip(),
            "ledger_generator_returncode": generator_proc.returncode,
            "ledger_generator_stdout_parse_error": generator_parse_error,
            "ledger_generator_stdout": generator_proc.stdout.strip(),
            "ledger_generator_stderr": generator_proc.stderr.strip(),
        }
        return payload, 1

    payload = {
        "runner": portable_path(runner_path(repo_root), repo_root),
        "ledger_generator": portable_path(ledger_generator_path(repo_root), repo_root),
        "bundle_verifier": build_bundle_verifier_payload(repo_root),
        "promote_after_review": build_promotion_payload(args, repo_root),
        "runner_returncode": runner_proc.returncode,
        "runner_output": runner_output,
        "runner_stdout_parse_error": runner_parse_error,
        "runner_stderr": runner_proc.stderr.strip(),
        "ledger_output": generator_output,
    }
    return payload, runner_exit_code


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

    payload, exit_code = execute_pipeline(args, repo_root, upload_dir)
    print(json.dumps(payload, indent=2))
    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
