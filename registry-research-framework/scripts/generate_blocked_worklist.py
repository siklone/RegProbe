#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import sys
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
AUDIT_ROOT = FRAMEWORK_ROOT / "audit"
JSON_OUTPUT = AUDIT_ROOT / "blocked-worklist.json"
MARKDOWN_OUTPUT = AUDIT_ROOT / "blocked-worklist.md"

if str(SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_ROOT))

from research_v36_lib import load_promotion_gate_map, load_records, primary_target  # noqa: E402


LANE_PRIORITY = {
    "restore-story": 40,
    "ghidra": 35,
    "runtime-trace": 30,
    "intentional-hold": 10,
}

GENERIC_SLUG_WORDS = {
    "control",
    "kernel",
    "policy",
    "power",
    "session",
    "system",
}


def blocker_hint(blockers: list[str], lane: str) -> str:
    lowered = " | ".join(str(item).lower() for item in blockers)
    if "restore-story" in lowered or "rollback" in lowered:
        return "Prove restore or rollback behavior for the exact subtree or value."
    if "init-walker" in lowered or "specific-caller" in lowered or "string-or-symbol-hit" in lowered or "adjacent-not-leaf-specific" in lowered or "conditional-initialization" in lowered:
        return "Continue static RE or Ghidra work until the exact reader or initializer is named."
    if "wpr-boot-no-hit" in lowered or "etw-no-hit" in lowered or "runtime-read-unresolved" in lowered or "procmon-saveas-timeout" in lowered:
        return "Retry runtime capture with a narrower trigger or a more reliable trace lane."
    if "no-primary-current-build-doc" in lowered:
        return "Find a primary current-build Microsoft source or explicitly accept research-only status."
    if "boot-unsafe" in lowered or "trigger-not-available-on-current-vm" in lowered or "research-only-raw" in lowered:
        return "Treat as environment-limited or intentional hold unless a safer lane becomes available."
    if lane == "ghidra":
        return "Continue static reverse-engineering for exact leaf-level proof."
    if lane == "runtime-trace":
        return "Collect a stronger runtime trace for the exact key or value."
    if lane == "intentional-hold":
        return "Wait for a safer environment or a clearer product surface before probing."
    return "Review blockers manually and choose the next evidence lane."


def actionability_for_lane(lane: str) -> str:
    return "active" if lane in {"restore-story", "ghidra", "runtime-trace"} else "hold"


def priority_score_for(lane: str, blocker_count: int) -> int:
    return int(LANE_PRIORITY.get(lane, 0)) - int(blocker_count)


def candidate_slug_tokens(candidate_id: str) -> list[str]:
    parts = [segment for segment in candidate_id.lower().split(".") if segment]
    tokens: list[str] = []
    if not parts:
        return tokens

    joined = "-".join(parts)
    tail = parts[-1]
    tail_pair = "-".join(parts[-2:]) if len(parts) >= 2 else ""
    for token in (joined, tail_pair, tail):
        if token and token not in tokens:
            tokens.append(token)
    return tokens


def candidate_match_words(candidate_id: str) -> list[str]:
    words = [
        word
        for word in candidate_id.lower().replace(".", "-").split("-")
        if word and len(word) >= 3 and word not in GENERIC_SLUG_WORDS
    ]
    return sorted(set(words), key=len, reverse=True)


def normalize_match_word(word: str) -> str:
    lowered = word.lower()
    if len(lowered) > 4 and lowered.endswith("s") and not lowered.endswith("ss"):
        return lowered[:-1]
    return lowered


def artifact_match_words(artifact_name: str) -> set[str]:
    stem = Path(artifact_name).stem.lower()
    return {
        normalize_match_word(word)
        for word in re.split(r"[^a-z0-9]+", stem)
        if word
    }


def audit_artifact_match_score(candidate_id: str, artifact_name: str) -> int:
    lowered = artifact_name.lower()
    score = 0
    for index, token in enumerate(candidate_slug_tokens(candidate_id)):
        if token and token in lowered:
            score += 10 - index
    artifact_words = artifact_match_words(artifact_name)
    word_hits = sum(
        1
        for word in candidate_match_words(candidate_id)
        if normalize_match_word(word) in artifact_words
    )
    if word_hits >= 2:
        score += 5 + word_hits
    return score


def recent_audit_artifacts_for(candidate_id: str, limit: int = 3) -> list[str]:
    if not AUDIT_ROOT.exists():
        return []

    matches: list[tuple[int, str, Path]] = []
    for path in AUDIT_ROOT.iterdir():
        if not path.is_file():
            continue
        score = audit_artifact_match_score(candidate_id, path.name)
        if score <= 0:
            continue
        matches.append((score, path.name.lower(), path))

    matches.sort(key=lambda item: (item[0], item[1]), reverse=True)
    return [
        str(path.relative_to(REPO_ROOT)).replace("\\", "/")
        for _, _, path in matches[:limit]
    ]


def build_worklist() -> dict[str, Any]:
    gate_map = load_promotion_gate_map()
    record_map = {
        str(record.get("record_id") or record.get("tweak_id") or ""): record
        for record in load_records()
    }
    blocked_entries = [
        entry for entry in gate_map.values()
        if str(entry.get("promotion_state") or "") == "blocked"
    ]

    items: list[dict[str, Any]] = []
    lane_counts: Counter[str] = Counter()
    for entry in blocked_entries:
        candidate_id = str(entry.get("candidate_id") or entry.get("tweak_id") or "")
        lane = str(entry.get("next_missing_layer") or "unknown")
        blockers = [str(item) for item in (entry.get("promotion_blockers") or [])]
        blocker_count = len(blockers)
        record = record_map.get(candidate_id, {})
        target = primary_target(record) if record else {}
        items.append(
            {
                "candidate_id": candidate_id,
                "feature_area": (
                    record.get("setting", {}).get("area")
                    or record.get("feature_area")
                    or entry.get("feature_area")
                    or "Unknown"
                ),
                "next_missing_layer": lane,
                "actionability": actionability_for_lane(lane),
                "priority_score": priority_score_for(lane, blocker_count),
                "blocker_count": blocker_count,
                "promotion_blockers": blockers,
                "key_path": str(target.get("path") or entry.get("key_path") or ""),
                "value_name": str(target.get("value_name") or entry.get("value_name") or ""),
                "recent_audit_artifacts": recent_audit_artifacts_for(candidate_id),
                "next_action_hint": blocker_hint(blockers, lane),
            }
        )
        lane_counts[lane] += 1

    items.sort(
        key=lambda item: (
            -int(item.get("priority_score") or 0),
            int(item.get("blocker_count") or 0),
            str(item.get("candidate_id") or ""),
        )
    )

    top_actionable_candidates = [
        str(item.get("candidate_id") or "")
        for item in items
        if str(item.get("actionability") or "") == "active"
    ][:5]

    return {
        "generated_at": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "blocked_count": len(items),
        "lane_counts": dict(sorted(lane_counts.items())),
        "top_actionable_candidates": top_actionable_candidates,
        "items": items,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    lines = [
        "# Blocked Worklist",
        "",
        f"Generated: `{payload.get('generated_at')}`",
        "",
        f"Blocked candidates: `{payload.get('blocked_count')}`",
        "",
        "## Lane Summary",
        "",
    ]
    for lane, count in (payload.get("lane_counts") or {}).items():
        lines.append(f"- `{lane}`: {count}")

    actionable = [item for item in (payload.get("items") or []) if item.get("actionability") == "active"][:5]
    if actionable:
        lines.extend(["", "## Top Actionable Candidates", ""])
        for item in actionable:
            lines.append(
                f"- `{item['candidate_id']}` (`{item['next_missing_layer']}`, score={item['priority_score']}, blockers={item['blocker_count']})"
            )

    lines.extend(["", "## Candidates", ""])
    for item in payload.get("items") or []:
        lines.append(f"### `{item['candidate_id']}`")
        lines.append("")
        lines.append(f"- Lane: `{item['next_missing_layer']}`")
        lines.append(f"- Actionability: `{item['actionability']}`")
        lines.append(f"- Priority score: `{item['priority_score']}`")
        lines.append(f"- Feature area: `{item['feature_area']}`")
        if item.get("key_path"):
            lines.append(f"- Key path: `{item['key_path']}`")
        if item.get("value_name"):
            lines.append(f"- Value name: `{item['value_name']}`")
        lines.append(f"- Blockers: {', '.join(f'`{value}`' for value in item.get('promotion_blockers') or [])}")
        artifacts = item.get("recent_audit_artifacts") or []
        if artifacts:
            lines.append(f"- Recent audit artifacts: {', '.join(f'`{value}`' for value in artifacts)}")
        lines.append(f"- Next action hint: {item['next_action_hint']}")
        lines.append("")
    return "\n".join(lines).rstrip() + "\n"


def write_outputs(payload: dict[str, Any] | None = None) -> dict[str, Any]:
    AUDIT_ROOT.mkdir(parents=True, exist_ok=True)
    payload = payload or build_worklist()
    JSON_OUTPUT.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    MARKDOWN_OUTPUT.write_text(render_markdown(payload), encoding="utf-8")
    return payload


def main() -> int:
    payload = write_outputs()
    print(
        json.dumps(
            {
                "json": str(JSON_OUTPUT.relative_to(REPO_ROOT)),
                "markdown": str(MARKDOWN_OUTPUT.relative_to(REPO_ROOT)),
                "blocked_count": payload["blocked_count"],
                "lane_counts": payload["lane_counts"],
            },
            ensure_ascii=False,
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
