#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path


def load_ids(id_args: list[str], id_file: str | None) -> list[str]:
    ids = [value.strip() for value in id_args if value.strip()]
    if id_file:
        for line in Path(id_file).read_text(encoding="utf-8").splitlines():
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            ids.append(line)
    deduped: list[str] = []
    seen: set[str] = set()
    for tweak_id in ids:
        if tweak_id not in seen:
            seen.add(tweak_id)
            deduped.append(tweak_id)
    return deduped


def main() -> int:
    parser = argparse.ArgumentParser(description="Run guest-side RegProbe tweak QA batch through the interactive app harness.")
    parser.add_argument("--id", action="append", default=[], help="Tweak id to validate. Repeat for multiple ids.")
    parser.add_argument("--id-file", help="Optional file with one tweak id per line.")
    parser.add_argument("--guest-dir", default=r"C:\Tools\ValidationController\smoke")
    parser.add_argument("--guest-script", default="")
    parser.add_argument("--wait-timeout", type=int, default=600)
    args = parser.parse_args()

    tweak_ids = load_ids(args.id, args.id_file)
    if not tweak_ids:
        parser.error("Provide at least one --id or --id-file entry.")

    repo_root = Path(__file__).resolve().parents[2]
    runner = repo_root / "scripts" / "vm-kvm" / "qga-run-powershell.py"
    guest_script = Path(args.guest_script).resolve() if args.guest_script else repo_root / "scripts" / "vm" / "guest-app-tweak-qa.ps1"

    results: list[dict[str, object]] = []
    overall_success = True

    for tweak_id in tweak_ids:
        guest_output = rf"{args.guest_dir}\{tweak_id}.qa.json"
        cmd = [
            sys.executable,
            str(runner),
            "--script",
            str(guest_script),
            "--guest-dir",
            args.guest_dir,
            "--ps-arg",
            tweak_id,
            "--ps-arg",
            guest_output,
            "--wait-timeout",
            str(args.wait_timeout),
        ]
        proc = subprocess.run(cmd, cwd=repo_root, capture_output=True, text=True)

        try:
            payload = json.loads(proc.stdout)
        except json.JSONDecodeError:
            payload = {
                "status": "parse-error",
                "stdout": proc.stdout,
                "stderr": proc.stderr,
            }

        execution = payload.get("execution", {}) if isinstance(payload, dict) else {}
        report: dict[str, object] = {}
        if isinstance(execution, dict):
            report_text = execution.get("stdout")
            if isinstance(report_text, str) and report_text.strip():
                try:
                    report = json.loads(report_text)
                except json.JSONDecodeError:
                    report = {"parse_error": True, "raw": report_text}

        success = bool(report.get("Success")) if report else False
        overall_success = overall_success and success
        results.append(
            {
                "tweak_id": tweak_id,
                "host_exit": proc.returncode,
                "execution_exit": execution.get("exitcode") if isinstance(execution, dict) else None,
                "payload_status": payload.get("status") if isinstance(payload, dict) else None,
                "report_success": report.get("Success") if report else None,
                "report_status": report.get("Status") if report else None,
                "report_summary": report.get("Summary") if report else None,
                "report": report,
            }
        )

    print(json.dumps(results, indent=2))
    return 0 if overall_success else 2


if __name__ == "__main__":
    raise SystemExit(main())
