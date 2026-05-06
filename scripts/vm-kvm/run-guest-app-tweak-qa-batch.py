#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
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

        app_success = bool(report.get("Success")) if report else False
        card_snapshot = summarize_card_snapshot(report) if report else {}
        contract_failures = list(card_snapshot.get("failures") or []) if isinstance(card_snapshot, dict) else []
        success = app_success and not contract_failures
        overall_success = overall_success and success
        results.append(
            {
                "tweak_id": tweak_id,
                "host_exit": proc.returncode,
                "execution_exit": execution.get("exitcode") if isinstance(execution, dict) else None,
                "payload_status": payload.get("status") if isinstance(payload, dict) else None,
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
                "report": report,
            }
        )

    print(json.dumps(results, indent=2))
    return 0 if overall_success else 2


if __name__ == "__main__":
    raise SystemExit(main())
