#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from collections import Counter
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_ROOT = Path(__file__).resolve().parent
DEFAULT_WORKLIST_PATH = REPO_ROOT / "registry-research-framework" / "audit" / "blocked-worklist.json"

if str(SCRIPT_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPT_ROOT))

from generate_blocked_worklist import ordered_lanes  # noqa: E402


def sorted_counter(items: list[dict[str, Any]], key: str) -> dict[str, int]:
    return dict(sorted(Counter(str(item.get(key) or "unknown") for item in items).items()))


def first_by_lane(items: list[dict[str, Any]]) -> dict[str, dict[str, str]]:
    focus: dict[str, dict[str, str]] = {}
    for item in items:
        lane = str(item.get("next_missing_layer") or "unknown")
        if lane in focus:
            continue
        focus[lane] = {
            "candidate_id": str(item.get("candidate_id") or ""),
            "suggested_command": str(item.get("suggested_command") or ""),
            "next_action_hint": str(item.get("next_action_hint") or ""),
        }
    return focus


def validate_payload(payload: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    items = list(payload.get("items") or [])
    blocked_count = int(payload.get("blocked_count") or 0)
    lane_counts = dict(payload.get("lane_counts") or {})
    actionability_counts = dict(payload.get("actionability_counts") or {})
    expected_lane_counts = sorted_counter(items, "next_missing_layer")
    expected_actionability_counts = sorted_counter(items, "actionability")

    if blocked_count != len(items):
        errors.append(f"blocked_count mismatch: {blocked_count} != {len(items)}")
    if lane_counts != expected_lane_counts:
        errors.append(f"lane_counts mismatch: {lane_counts} != {expected_lane_counts}")
    if actionability_counts != expected_actionability_counts:
        errors.append(f"actionability_counts mismatch: {actionability_counts} != {expected_actionability_counts}")

    unexpected_actionability = sorted(
        value
        for value in expected_actionability_counts
        if value not in {"active", "hold"}
    )
    if unexpected_actionability:
        errors.append(f"unexpected actionability values: {unexpected_actionability}")

    expected_ordered_lanes = ordered_lanes(list(expected_lane_counts))
    if list(payload.get("ordered_lanes") or []) != expected_ordered_lanes:
        errors.append(f"ordered_lanes mismatch: {payload.get('ordered_lanes')} != {expected_ordered_lanes}")

    expected_focus = first_by_lane(items)
    if dict(payload.get("lane_focus") or {}) != expected_focus:
        errors.append("lane_focus does not match first candidate per ordered worklist lane")

    expected_top_actionable = [
        str(item.get("candidate_id") or "")
        for item in items
        if str(item.get("actionability") or "") == "active"
    ][:5]
    if list(payload.get("top_actionable_candidates") or []) != expected_top_actionable:
        errors.append("top_actionable_candidates does not match first five active items")

    expected_top_holds = [
        str(item.get("candidate_id") or "")
        for item in items
        if str(item.get("actionability") or "") == "hold"
    ][:5]
    if list(payload.get("top_hold_candidates") or []) != expected_top_holds:
        errors.append("top_hold_candidates does not match first five hold items")

    for index, item in enumerate(items):
        candidate_id = str(item.get("candidate_id") or "")
        prefix = candidate_id or f"item[{index}]"
        if not candidate_id:
            errors.append(f"{prefix}: missing candidate_id")
        if not str(item.get("next_missing_layer") or ""):
            errors.append(f"{prefix}: missing next_missing_layer")
        if not str(item.get("next_action_hint") or ""):
            errors.append(f"{prefix}: missing next_action_hint")
        if not str(item.get("suggested_command") or ""):
            errors.append(f"{prefix}: missing suggested_command")
        if not isinstance(item.get("promotion_blockers"), list):
            errors.append(f"{prefix}: promotion_blockers is not a list")
        artifacts = item.get("recent_audit_artifacts")
        if not isinstance(artifacts, list):
            errors.append(f"{prefix}: recent_audit_artifacts is not a list")
        elif len(artifacts) > 3:
            errors.append(f"{prefix}: recent_audit_artifacts has more than 3 entries")

    return errors


def build_result(path: Path) -> dict[str, Any]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError(f"{path} JSON payload is not an object")
    errors = validate_payload(payload)
    return {
        "status": "PASS" if not errors else "FAIL",
        "path": str(path.relative_to(REPO_ROOT)).replace("\\", "/") if path.is_relative_to(REPO_ROOT) else str(path),
        "blocked_count": int(payload.get("blocked_count") or 0),
        "lane_counts": dict(payload.get("lane_counts") or {}),
        "actionability_counts": dict(payload.get("actionability_counts") or {}),
        "errors": errors,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate the generated blocked worklist surface.")
    parser.add_argument("--path", type=Path, default=DEFAULT_WORKLIST_PATH, help="Path to blocked-worklist.json.")
    parser.add_argument("--emit-json", action="store_true", help="Emit JSON payload.")
    args = parser.parse_args()

    result = build_result(args.path)
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0 if result["status"] == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
