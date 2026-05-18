#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
import tempfile
import time
from pathlib import Path
from typing import Any


REQUIRED_CARD_FIELDS = [
    "TweakId",
    "Name",
    "Category",
    "EvidenceClass",
    "ResearchStatus",
    "RollbackSnapshotState",
    "HasClaimBoundary",
    "WhatWeKnowSummary",
    "WhatWeDoNotClaimSummary",
    "ProofLanes",
]
REQUIRED_PROOF_LANES = ["docs", "runtime", "source", "rollback"]
VALID_ABSENT_PROBE_STATUSES = {"absent-key", "absent-value"}


def parse_report_text(report_text: str) -> tuple[dict[str, object], str | None]:
    if not report_text.strip():
        return {}, "empty-report"
    try:
        report = json.loads(report_text)
    except json.JSONDecodeError as exc:
        return {"parse_error": True, "raw": report_text}, str(exc)
    if not isinstance(report, dict):
        return {"parse_error": True, "raw": report_text}, "report root is not an object"
    return report, None


def download_guest_report(
    repo_root: Path,
    guest_output: str,
    *,
    attempts: int = 10,
    delay_seconds: float = 1.0,
) -> tuple[dict[str, object], dict[str, object]]:
    downloader = repo_root / "scripts" / "vm-kvm" / "qga-get-file.py"
    last_fetch: dict[str, object] = {
        "status": "not-attempted",
        "guest_output": guest_output,
    }
    for attempt in range(1, attempts + 1):
        with tempfile.TemporaryDirectory(prefix="regprobe-qa-report-") as temp_dir:
            destination = Path(temp_dir) / "qa-report.json"
            proc = subprocess.run(
                [
                    sys.executable,
                    str(downloader),
                    "--source",
                    guest_output,
                    "--destination",
                    str(destination),
                ],
                cwd=repo_root,
                capture_output=True,
                text=True,
            )

            fetch: dict[str, object] = {
                "status": "ok" if proc.returncode == 0 else "failed",
                "attempt": attempt,
                "attempts": attempts,
                "returncode": proc.returncode,
                "stdout": proc.stdout,
                "stderr": proc.stderr,
                "guest_output": guest_output,
                "host_output": str(destination),
            }
            last_fetch = fetch
            if proc.returncode == 0 and destination.exists():
                report_text = destination.read_text(encoding="utf-8")
                report, parse_error = parse_report_text(report_text)
                fetch["parse_error"] = parse_error
                fetch["bytes"] = destination.stat().st_size
                return report, fetch

        if attempt < attempts:
            time.sleep(delay_seconds)

    return {}, last_fetch


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


def has_required_value(value: Any) -> bool:
    if value is None:
        return False
    if isinstance(value, str):
        return bool(value.strip())
    if isinstance(value, list):
        return bool(value)
    return True


def summarize_card_snapshot(report: dict[str, object]) -> dict[str, object]:
    card = report.get("Card")
    if not isinstance(card, dict):
        return {
            "status": "missing-card",
            "present": False,
            "has_claim_boundary": False,
            "present_fields": [],
            "missing_fields": REQUIRED_CARD_FIELDS,
            "proof_lanes": [],
            "missing_proof_lanes": REQUIRED_PROOF_LANES,
            "failures": ["missing-card-snapshot"],
        }

    present_fields = [field for field in REQUIRED_CARD_FIELDS if has_required_value(card.get(field))]
    missing_fields = [field for field in REQUIRED_CARD_FIELDS if field not in present_fields]

    proof_lanes: list[str] = []
    for lane in card.get("ProofLanes") or []:
        if not isinstance(lane, dict):
            continue
        key = str(lane.get("Key") or "").strip().lower()
        if key:
            proof_lanes.append(key)

    proof_lane_set = set(proof_lanes)
    missing_proof_lanes = [lane for lane in REQUIRED_PROOF_LANES if lane not in proof_lane_set]
    has_claim_boundary = bool(card.get("HasClaimBoundary"))

    failures: list[str] = []
    if missing_fields:
        failures.append("missing-card-fields:" + ",".join(missing_fields))
    if not has_claim_boundary:
        failures.append("missing-claim-boundary")
    if missing_proof_lanes:
        failures.append("missing-proof-lanes:" + ",".join(missing_proof_lanes))

    return {
        "status": "ok" if not failures else "invalid-card",
        "present": True,
        "has_claim_boundary": has_claim_boundary,
        "present_fields": present_fields,
        "missing_fields": missing_fields,
        "proof_lanes": proof_lanes,
        "missing_proof_lanes": missing_proof_lanes,
        "failures": failures,
    }


def summarize_contract_failures(failures: list[str]) -> str:
    if not failures:
        return "QA card snapshot contract passed."
    return "QA card snapshot contract failed: " + "; ".join(failures)


def split_registry_value_path(registry_path: str | None) -> dict[str, str] | None:
    if not registry_path or not registry_path.strip():
        return None

    normalized = registry_path.strip().replace("/", "\\").rstrip("\\")
    parts = [part for part in normalized.split("\\") if part]
    if len(parts) < 3:
        return None

    root = parts[0].upper()
    if root not in {"HKLM", "HKEY_LOCAL_MACHINE", "HKCU", "HKEY_CURRENT_USER", "HKCR", "HKEY_CLASSES_ROOT", "HKU", "HKEY_USERS"}:
        return None

    key_path = "\\".join(parts[:-1])
    value_name = parts[-1]
    ps_root = {
        "HKEY_LOCAL_MACHINE": "HKLM",
        "HKEY_CURRENT_USER": "HKCU",
        "HKEY_CLASSES_ROOT": "HKCR",
        "HKEY_USERS": "HKU",
    }.get(root, root)

    return {
        "registry_path": normalized,
        "key_path": key_path,
        "value_name": value_name,
        "powershell_key_path": ps_root + ":\\" + "\\".join(parts[1:-1]),
    }


def powershell_single_quote(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


def build_registry_probe_command(powershell_key_path: str, value_name: str) -> str:
    key = powershell_single_quote(powershell_key_path)
    name = powershell_single_quote(value_name)
    return (
        "$ErrorActionPreference = 'Stop';"
        f"$key = {key};"
        f"$name = {name};"
        "if (-not (Test-Path -LiteralPath $key)) {"
        "  [pscustomobject]@{ status='absent-key'; exists=$false; key_path=$key; value_name=$name; value=$null; value_text='' } | ConvertTo-Json -Compress; exit 0"
        "};"
        "$item = Get-ItemProperty -LiteralPath $key -Name $name -ErrorAction SilentlyContinue;"
        "$prop = if ($null -eq $item) { $null } else { $item.PSObject.Properties[$name] };"
        "if ($null -eq $prop) {"
        "  [pscustomobject]@{ status='absent-value'; exists=$false; key_path=$key; value_name=$name; value=$null; value_text='' } | ConvertTo-Json -Compress; exit 0"
        "};"
        "$value = $prop.Value;"
        "[pscustomobject]@{ status='ok'; exists=$true; key_path=$key; value_name=$name; value=$value; value_text=([string]$value); value_type=($value.GetType().FullName) } | ConvertTo-Json -Compress"
    )


def parse_registry_probe_stdout(stdout: str) -> dict[str, object]:
    for line in reversed((stdout or "").splitlines()):
        text = line.strip()
        if not text:
            continue
        try:
            payload = json.loads(text)
        except json.JSONDecodeError:
            continue
        if isinstance(payload, dict):
            return payload
    return {
        "status": "parse-error",
        "exists": None,
        "stdout": stdout,
    }


def probe_guest_registry_value(repo_root: Path, registry_path: str | None, wait_timeout: int) -> dict[str, object]:
    split = split_registry_value_path(registry_path)
    if not split:
        return {
            "status": "skipped",
            "reason": "registry-path-not-parseable",
            "registry_path": registry_path or "",
        }

    command = build_registry_probe_command(split["powershell_key_path"], split["value_name"])
    proc = subprocess.run(
        [
            sys.executable,
            str(repo_root / "scripts" / "vm-kvm" / "qga-exec.py"),
            "--path",
            "powershell.exe",
            "--arg=-NoProfile",
            "--arg=-ExecutionPolicy",
            "--arg=Bypass",
            "--arg=-Command",
            f"--arg={command}",
            "--wait-timeout",
            str(max(10, min(wait_timeout, 60))),
        ],
        cwd=repo_root,
        capture_output=True,
        text=True,
    )

    try:
        outer = json.loads(proc.stdout)
    except json.JSONDecodeError:
        outer = {
            "status": "parse-error",
            "stdout": proc.stdout,
            "stderr": proc.stderr,
        }

    execution_stdout = outer.get("stdout") if isinstance(outer, dict) else ""
    probe = parse_registry_probe_stdout(execution_stdout if isinstance(execution_stdout, str) else "")
    probe.update(
        {
            "registry_path": split["registry_path"],
            "key_path": split["key_path"],
            "powershell_key_path": split["powershell_key_path"],
            "value_name": split["value_name"],
            "qga_returncode": proc.returncode,
            "qga_status": outer.get("status") if isinstance(outer, dict) else None,
            "qga_exitcode": outer.get("exitcode") if isinstance(outer, dict) else None,
            "qga_stderr": proc.stderr,
        }
    )
    if proc.returncode != 0 or (isinstance(outer, dict) and outer.get("exitcode") not in (0, None)):
        probe["status"] = "probe-error"
    return probe


def find_stage_current_value(report: dict[str, object], stage_name: str) -> str:
    for stage in report.get("Stages") or []:
        if not isinstance(stage, dict):
            continue
        if str(stage.get("Stage") or "").strip().lower() == stage_name.lower():
            return str(stage.get("CurrentValue") or "").strip()
    return ""


def normalize_reported_value(value: str) -> str:
    text = (value or "").strip()
    if "(" in text:
        text = text.split("(", 1)[0].strip()
    return text.lower()


def evaluate_post_run_registry_probe(report: dict[str, object], probe: dict[str, object]) -> str:
    status = str(probe.get("status") or "")
    if status == "skipped":
        return "skipped"
    if status == "probe-error" or status == "parse-error":
        return "probe-error"

    expected = normalize_reported_value(find_stage_current_value(report, "detect-after"))
    if not expected:
        return "unchecked"

    exists = bool(probe.get("exists"))
    if expected == "not set":
        return "verified" if not exists or status in VALID_ABSENT_PROBE_STATUSES else "mismatch"

    actual = normalize_reported_value(str(probe.get("value_text") or probe.get("value") or ""))
    return "verified" if exists and actual == expected else "mismatch"


def main() -> int:
    parser = argparse.ArgumentParser(description="Run guest-side RegProbe tweak QA batch through the interactive app harness.")
    parser.add_argument("--id", action="append", default=[], help="Tweak id to validate. Repeat for multiple ids.")
    parser.add_argument("--id-file", help="Optional file with one tweak id per line.")
    parser.add_argument("--guest-dir", default=r"C:\Tools\ValidationController\smoke")
    parser.add_argument("--guest-script", default="")
    parser.add_argument("--wait-timeout", type=int, default=600)
    parser.add_argument(
        "--allow-gated-mutation",
        action="store_true",
        help="Pass the explicit QA-only gated mutation override to the app. Intended for VM evidence acquisition only.",
    )
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
        if args.allow_gated_mutation:
            cmd.append("--ps-arg=-AllowGatedMutation")
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
        report_source = "missing"
        report_parse_error = None
        report_fetch: dict[str, object] | None = None
        if isinstance(execution, dict):
            report_text = execution.get("stdout")
            if isinstance(report_text, str) and report_text.strip():
                report, report_parse_error = parse_report_text(report_text)
                report_source = "stdout"

        if not report or report.get("parse_error"):
            fallback_report, report_fetch = download_guest_report(repo_root, guest_output)
            if fallback_report and not fallback_report.get("parse_error"):
                report = fallback_report
                report_source = "guest-file"
                report_parse_error = None
            elif report_fetch:
                report_parse_error = str(report_fetch.get("parse_error") or report_parse_error or report_fetch.get("stderr") or "guest report unavailable")

        app_success = bool(report.get("Success")) if report else False
        card_snapshot = summarize_card_snapshot(report) if report else {}
        contract_failures = list(card_snapshot.get("failures") or []) if isinstance(card_snapshot, dict) else []
        card = report.get("Card") if isinstance(report.get("Card"), dict) else {}
        registry_probe = probe_guest_registry_value(
            repo_root,
            str(card.get("RegistryPath") or "") if isinstance(card, dict) else "",
            args.wait_timeout,
        ) if report else {"status": "skipped", "reason": "missing-report"}
        registry_probe_verification = evaluate_post_run_registry_probe(report, registry_probe) if report else "skipped"
        if registry_probe_verification in {"probe-error", "mismatch"}:
            contract_failures.append("post-run-registry-" + registry_probe_verification)
        success = app_success and not contract_failures
        overall_success = overall_success and success
        results.append(
            {
                "tweak_id": tweak_id,
                "host_exit": proc.returncode,
                "execution_exit": execution.get("exitcode") if isinstance(execution, dict) else None,
                "payload_status": payload.get("status") if isinstance(payload, dict) else None,
                "report_source": report_source,
                "report_parse_error": report_parse_error,
                "report_fetch_status": report_fetch.get("status") if isinstance(report_fetch, dict) else None,
                "report_app_success": report.get("Success") if report else None,
                "report_success": success if report else None,
                "report_status": report.get("Status") if report else None,
                "report_summary": report.get("Summary") if report else None,
                "report_contract_status": card_snapshot.get("status") if isinstance(card_snapshot, dict) else None,
                "report_contract_summary": summarize_contract_failures(contract_failures) if report else None,
                "report_contract_failures": contract_failures,
                "report_card_present": card_snapshot.get("present") if isinstance(card_snapshot, dict) else None,
                "report_card_has_claim_boundary": card_snapshot.get("has_claim_boundary") if isinstance(card_snapshot, dict) else None,
                "report_card_present_fields": card_snapshot.get("present_fields") if isinstance(card_snapshot, dict) else [],
                "report_card_missing_fields": card_snapshot.get("missing_fields") if isinstance(card_snapshot, dict) else [],
                "report_card_proof_lanes": card_snapshot.get("proof_lanes") if isinstance(card_snapshot, dict) else [],
                "report_card_missing_proof_lanes": card_snapshot.get("missing_proof_lanes") if isinstance(card_snapshot, dict) else [],
                "post_run_registry_probe": registry_probe,
                "post_run_registry_verification": registry_probe_verification,
                "report": report,
            }
        )

    print(json.dumps(results, indent=2))
    return 0 if overall_success else 2


if __name__ == "__main__":
    raise SystemExit(main())
