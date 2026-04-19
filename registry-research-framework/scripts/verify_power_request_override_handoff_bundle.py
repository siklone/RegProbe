#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
AUDIT_ROOT = REPO_ROOT / "registry-research-framework" / "audit"
MANIFEST_JSON = AUDIT_ROOT / "power-request-override-reader-binding-execution-manifest-20260419.json"
HANDOFF_JSON = AUDIT_ROOT / "power-request-override-handoff-index-20260419.json"
REACQUIRE_PLAN_JSON = AUDIT_ROOT / "power-request-override-reader-binding-reacquire-plan-20260419.json"


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def portable_path(path: Path, *, repo_root: Path = REPO_ROOT) -> str:
    try:
        return str(path.resolve().relative_to(repo_root)).replace("\\", "/")
    except ValueError:
        return str(path.resolve()).replace("\\", "/")


def normalized_promotion_block(payload: dict[str, Any]) -> dict[str, Any]:
    return {
        "promote_script": payload.get("promote_script"),
        "promote_example": payload.get("promote_example"),
        "promote_dry_run_example": payload.get("promote_dry_run_example"),
        "current_run_id": payload.get("current_run_id"),
        "current_run_example": payload.get("current_run_example"),
        "current_run_dry_run_example": payload.get("current_run_dry_run_example"),
        "preview_targets": payload.get("preview_targets") or {},
        "overwrite_policy": payload.get("overwrite_policy"),
    }


def summarize_checks(payload: dict[str, Any]) -> dict[str, Any]:
    checks = payload.get("checks") or {}
    return {
        "status": payload.get("status"),
        "promotion_blocks_match": checks.get("promotion_blocks_match"),
        "missing_read_order_count": len(checks.get("missing_read_order_paths") or []),
        "missing_command_file_count": len(checks.get("missing_command_files") or []),
        "missing_review_input_count": len(checks.get("missing_review_inputs") or []),
        "missing_reacquire_command_count": len(checks.get("missing_reacquire_commands") or []),
        "missing_promote_script": bool(checks.get("missing_promote_script")),
    }


def build_blockers(summary: dict[str, Any]) -> list[str]:
    blockers: list[str] = []
    if summary.get("promotion_blocks_match") is False:
        blockers.append("promotion_blocks_mismatch")
    if summary.get("missing_read_order_count"):
        blockers.append("missing_read_order_paths")
    if summary.get("missing_command_file_count"):
        blockers.append("missing_command_files")
    if summary.get("missing_review_input_count"):
        blockers.append("missing_review_inputs")
    if summary.get("missing_reacquire_command_count"):
        blockers.append("missing_reacquire_commands")
    if summary.get("missing_promote_script"):
        blockers.append("missing_promote_script")
    return blockers


def build_next_steps(payload: dict[str, Any]) -> dict[str, str]:
    if payload.get("status") == "ok":
        recommended_example = "python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py"
        recommended_reason = "Bundle verifier passed; the normal pipeline execute path is ready."
    else:
        recommended_example = "python3 registry-research-framework/scripts/verify_power_request_override_handoff_bundle.py --markdown"
        recommended_reason = "Bundle verifier reported blockers; inspect the markdown summary before executing the VM lane."
    return {
        "recommended_example": recommended_example,
        "recommended_reason": recommended_reason,
        "dry_run_example": "python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py --dry-run",
        "verify_only_example": "python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py --verify-only",
        "markdown_summary_example": "python3 registry-research-framework/scripts/verify_power_request_override_handoff_bundle.py --markdown",
        "skip_bundle_verifier_example": "python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py --skip-bundle-verifier",
    }


def verify_bundle(*, repo_root: Path = REPO_ROOT) -> dict[str, Any]:
    manifest = load_json(MANIFEST_JSON)
    handoff = load_json(HANDOFF_JSON)
    reacquire_plan = load_json(REACQUIRE_PLAN_JSON)

    read_order = handoff.get("read_order") or []
    missing_read_order = [entry["path"] for entry in read_order if not (repo_root / entry["path"]).exists()]
    command_files = [entry["command_file"] for entry in (manifest.get("entries") or [])]
    missing_command_files = [rel for rel in command_files if not (repo_root / rel).exists()]
    review_inputs = manifest.get("review_inputs") or []
    missing_review_inputs = [rel for rel in review_inputs if not (repo_root / rel).exists()]
    reacquire_artifacts = reacquire_plan.get("required_reacquire_artifacts") or []
    missing_reacquire_commands = [
        artifact["command_file"]
        for artifact in reacquire_artifacts
        if not (repo_root / artifact["command_file"]).exists()
    ]

    promotion_manifest = manifest.get("promotion") or {}
    promotion_handoff = handoff.get("promotion") or {}
    normalized_manifest = normalized_promotion_block(promotion_manifest)
    normalized_handoff = normalized_promotion_block(promotion_handoff)
    promotion_consistent = normalized_manifest == normalized_handoff
    missing_promote_script = not (repo_root / promotion_manifest.get("promote_script", "")).exists()

    payload = {
        "record_id": manifest.get("record_id"),
        "status": "ok"
        if not (
            missing_read_order
            or missing_command_files
            or missing_review_inputs
            or missing_reacquire_commands
            or missing_promote_script
            or not promotion_consistent
        )
        else "error",
        "manifest": portable_path(MANIFEST_JSON),
        "handoff": portable_path(HANDOFF_JSON),
        "reacquire_plan": portable_path(REACQUIRE_PLAN_JSON),
        "read_order_count": len(read_order),
        "command_file_count": len(command_files),
        "review_input_count": len(review_inputs),
        "promotion": {
            "current_run_id": normalized_manifest.get("current_run_id"),
            "current_run_example": normalized_manifest.get("current_run_example"),
            "current_run_dry_run_example": normalized_manifest.get("current_run_dry_run_example"),
            "preview_targets": normalized_manifest.get("preview_targets") or {},
        },
        "checks": {
            "missing_read_order_paths": missing_read_order,
            "missing_command_files": missing_command_files,
            "missing_review_inputs": missing_review_inputs,
            "missing_reacquire_commands": missing_reacquire_commands,
            "missing_promote_script": missing_promote_script,
            "promotion_blocks_match": promotion_consistent,
            "normalized_promotion_manifest": normalized_manifest,
            "normalized_promotion_handoff": normalized_handoff,
        },
    }
    payload["summary"] = summarize_checks(payload)
    payload["blockers"] = build_blockers(payload["summary"])
    payload["ready_for_execute"] = payload["status"] == "ok"
    payload["next_steps"] = build_next_steps(payload)
    return payload


def render_markdown(payload: dict[str, Any]) -> str:
    promotion = payload["promotion"]
    preview_targets = promotion["preview_targets"]
    checks = payload["checks"]
    summary = payload.get("summary") or {}
    next_steps = payload.get("next_steps") or {}
    blockers = payload.get("blockers") or []
    lines = [
        "# PowerRequestOverride Handoff Bundle Verification",
        "",
        f"- Status: `{payload['status']}`",
        f"- Ready for execute: `{payload.get('ready_for_execute')}`",
        f"- Record: `{payload['record_id']}`",
        f"- Read-order entries: `{payload['read_order_count']}`",
        f"- Command files: `{payload['command_file_count']}`",
        f"- Review inputs: `{payload['review_input_count']}`",
        "",
        "## Current Run",
        f"- Run id: `{promotion['current_run_id']}`",
        f"- Promote command: `{promotion['current_run_example']}`",
        f"- Promote dry-run: `{promotion['current_run_dry_run_example']}`",
        "",
        "## Preview Targets",
        f"- Source JSON: `{preview_targets.get('source_json', '')}`",
        f"- Source MD: `{preview_targets.get('source_md', '')}`",
        f"- Target JSON: `{preview_targets.get('target_json', '')}`",
        f"- Target MD: `{preview_targets.get('target_md', '')}`",
        "",
        "## Checks",
        f"- Promotion blocks match: `{checks['promotion_blocks_match']}`",
        f"- Missing read-order paths: `{checks['missing_read_order_paths']}`",
        f"- Missing command files: `{checks['missing_command_files']}`",
        f"- Missing review inputs: `{checks['missing_review_inputs']}`",
        f"- Missing reacquire commands: `{checks['missing_reacquire_commands']}`",
        f"- Missing promote script: `{checks['missing_promote_script']}`",
        "",
        "## Summary",
        f"- Missing read-order count: `{summary.get('missing_read_order_count', '')}`",
        f"- Missing command-file count: `{summary.get('missing_command_file_count', '')}`",
        f"- Missing review-input count: `{summary.get('missing_review_input_count', '')}`",
        f"- Missing reacquire-command count: `{summary.get('missing_reacquire_command_count', '')}`",
        f"- Blockers: `{blockers}`",
        "",
        "## Next Steps",
        f"- Recommended command: `{next_steps.get('recommended_example', '')}`",
        f"- Reason: `{next_steps.get('recommended_reason', '')}`",
        f"- Dry-run: `{next_steps.get('dry_run_example', '')}`",
        f"- Verify-only: `{next_steps.get('verify_only_example', '')}`",
    ]
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Verify that the PowerRequestOverride handoff bundle is structurally intact.")
    parser.add_argument("--markdown", action="store_true", help="Render a markdown summary instead of JSON.")
    args = parser.parse_args()

    payload = verify_bundle()
    if args.markdown:
        print(render_markdown(payload))
    else:
        print(json.dumps(payload, indent=2))
    return 0 if payload["status"] == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
