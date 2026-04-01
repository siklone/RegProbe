from __future__ import annotations

import argparse
import json
import re
from collections import OrderedDict
from pathlib import Path
from typing import Any


HEADER_RE = {
    "program": re.compile(r"(?m)^- Program: `([^`]+)`"),
    "probe": re.compile(r"(?m)^- Probe: `([^`]+)`"),
    "timestamp": re.compile(r"(?m)^- Timestamp: `([^`]+)`"),
    "pdb_source": re.compile(r"(?m)^- PDB source: `([^`]+)`"),
}
PATTERN_RE = re.compile(r"(?m)^## `([^`]+)`\s*$")


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")


def parse_markdown_metadata(markdown_path: Path) -> dict[str, Any]:
    text = markdown_path.read_text(encoding="utf-8-sig") if markdown_path.exists() else ""
    metadata: dict[str, Any] = {"program": None, "probe": None, "timestamp": None, "pdb_source": None, "patterns": []}
    for key, pattern in HEADER_RE.items():
        match = pattern.search(text)
        if match:
            metadata[key] = match.group(1)
    metadata["patterns"] = [match.group(1) for match in PATTERN_RE.finditer(text)]
    return metadata


def confidence_rank(match: dict[str, Any]) -> int:
    if match.get("function_confidence") == "symbolized_branch":
        return 3
    if match.get("function_source") == "pdb-symbol":
        return 2
    if match.get("function_name") and match.get("function_name") != "<no function>":
        return 1
    return 0


def sort_key(match: dict[str, Any]) -> tuple[Any, ...]:
    heuristic = int(match.get("heuristic_score") or 0)
    function_name = str(match.get("function_name") or "")
    address = str(match.get("address") or "")
    return (-confidence_rank(match), -heuristic, function_name.lower(), address.lower())


def compact_matches(
    raw_matches: list[dict[str, Any]],
    pattern_order: list[str],
    max_symbolized_per_pattern: int,
    max_review_per_pattern: int,
) -> tuple[list[dict[str, Any]], list[dict[str, Any]], dict[str, list[dict[str, Any]]]]:
    grouped: dict[str, list[dict[str, Any]]] = OrderedDict()
    for pattern in pattern_order:
        grouped[pattern] = []
    for match in raw_matches:
        pattern = str(match.get("pattern") or "")
        grouped.setdefault(pattern, []).append(match)

    committed: list[dict[str, Any]] = []
    summaries: list[dict[str, Any]] = []
    committed_by_pattern: dict[str, list[dict[str, Any]]] = OrderedDict()

    for pattern, matches in grouped.items():
        ordered = sorted(matches, key=sort_key)
        symbolized = [item for item in ordered if item.get("function_confidence") == "symbolized_branch"]
        named_review = [
            item
            for item in ordered
            if item.get("function_confidence") != "symbolized_branch"
            and item.get("function_name")
            and item.get("function_name") != "<no function>"
        ]
        unresolved = [
            item
            for item in ordered
            if not item.get("function_name") or item.get("function_name") == "<no function>"
        ]

        if symbolized:
            kept = symbolized[:max_symbolized_per_pattern]
        elif named_review:
            kept = named_review[:max_review_per_pattern]
        else:
            kept = []

        committed_by_pattern[pattern] = kept
        committed.extend(kept)

        summary = {
            "pattern": pattern,
            "raw_match_count": len(matches),
            "committed_match_count": len(kept),
            "symbolized_kept_count": len([item for item in kept if item.get("function_confidence") == "symbolized_branch"]),
            "review_only_kept_count": len([item for item in kept if item.get("function_confidence") != "symbolized_branch"]),
            "omitted_symbolized_count": len(
                [item for item in matches if item not in kept and item.get("function_confidence") == "symbolized_branch"]
            ),
            "omitted_review_only_count": len(
                [
                    item
                    for item in matches
                    if item not in kept
                    and item.get("function_confidence") != "symbolized_branch"
                    and item.get("function_name")
                    and item.get("function_name") != "<no function>"
                ]
            ),
            "omitted_unresolved_count": len([item for item in matches if item not in kept and (not item.get("function_name") or item.get("function_name") == "<no function>")]),
            "status": "kept" if kept else ("review-only-omitted" if matches else "no-match"),
        }
        if not kept and matches:
            summary["reason"] = "Raw review-only hits existed, but no committed PDB-backed or named review block survived compaction."
        elif kept and len(matches) > len(kept):
            summary["reason"] = "Lower-confidence or unresolved review-only hits were omitted from the committed surface."
        else:
            summary["reason"] = "Committed output already satisfied the bounded branch-review format."
        summaries.append(summary)

    return committed, summaries, committed_by_pattern


def inline_code(value: Any) -> str:
    text = "" if value is None else str(value)
    return text.replace("`", "\\`")


def render_match_block(match: dict[str, Any]) -> list[str]:
    lines = [
        f"### Branch @ `{inline_code(match.get('address'))}`",
        "",
        f"- Function: `{inline_code(match.get('function_name'))}`",
        f"- Function source: `{inline_code(match.get('function_source'))}`",
        f"- Function confidence: `{inline_code(match.get('function_confidence'))}`",
        f"- Register focus: `{inline_code(', '.join(match.get('register_focus') or []))}`",
        f"- Flag focus: `{inline_code(', '.join(match.get('flag_focus') or []))}`",
        f"- Compare: `{inline_code(match.get('compare_condition'))}`",
        f"- Jump: `{inline_code(match.get('jump_condition'))}`",
        f"- Value mapping: `{inline_code(match.get('value_map'))}`",
        f"- Branch effect: `{inline_code(match.get('branch_effect'))}`",
        f"- Stack note: `{inline_code(match.get('stack_summary'))}`",
        f"- Exception gate: `{inline_code(match.get('exception_reason'))}`",
        f"- Heuristic score: `{inline_code(match.get('heuristic_score'))}`",
        f"- Heuristic reasons: `{inline_code(' | '.join(match.get('heuristic_reasons') or []))}`",
        f"- Effect: {inline_code(match.get('effect_summary'))}",
        f"- Unclear: `{inline_code(match.get('unclear'))}`",
        "",
        "```asm",
        "; context_before",
        *(match.get("context_before") or []),
        "; branch_snippet",
        *(match.get("branch_snippet") or []),
        "; context_after",
        *(match.get("context_after") or []),
        "```",
        "",
    ]
    return lines


def render_markdown(
    markdown_path: Path,
    evidence_path: Path,
    payload: dict[str, Any],
    pattern_order: list[str],
    committed_by_pattern: dict[str, list[dict[str, Any]]],
    summaries: list[dict[str, Any]],
) -> str:
    metadata = parse_markdown_metadata(markdown_path)
    program = metadata.get("program") or payload.get("binary") or evidence_path.parent.name
    probe = metadata.get("probe") or payload.get("probe") or evidence_path.parent.name
    timestamp = metadata.get("timestamp") or payload.get("timestamp") or ""
    pdb_source = metadata.get("pdb_source") or payload.get("pdb_source") or ""
    summary_by_pattern = {entry["pattern"]: entry for entry in summaries}

    lines = [
        "# Ghidra Branch Review",
        "",
        f"- Program: `{inline_code(program)}`",
        f"- Probe: `{inline_code(probe)}`",
        f"- Timestamp: `{inline_code(timestamp)}`",
        f"- PDB source: `{inline_code(pdb_source)}`",
        f"- Patterns: `{inline_code(', '.join(pattern_order))}`" if pattern_order else "- Patterns: ``",
        f"- Raw matches: `{payload.get('raw_match_count', 0)}`",
        f"- Committed matches: `{payload.get('committed_match_count', 0)}`",
        f"- Omitted additional symbolized branch hits: `{payload.get('omitted_symbolized_count', 0)}`",
        f"- Omitted unresolved review hits: `{payload.get('omitted_unresolved_count', 0)}`",
        f"- Omitted lower-confidence review hits: `{payload.get('omitted_review_only_count', 0)}`",
        "",
    ]

    for pattern in pattern_order:
        lines.append(f"## `{inline_code(pattern)}`")
        lines.append("")
        kept = committed_by_pattern.get(pattern) or []
        summary = summary_by_pattern.get(pattern) or {}

        if kept:
            for match in kept:
                lines.extend(render_match_block(match))
            omitted_symbolized = int(summary.get("omitted_symbolized_count") or 0)
            omitted_review_only = int(summary.get("omitted_review_only_count") or 0)
            omitted_unresolved = int(summary.get("omitted_unresolved_count") or 0)
            if omitted_symbolized or omitted_review_only or omitted_unresolved:
                lines.append(
                    f"_Omitted {omitted_symbolized} additional symbolized branch hit(s), {omitted_review_only} lower-confidence review hit(s), and {omitted_unresolved} unresolved hit(s) from the committed surface._"
                )
                lines.append("")
            continue

        raw_match_count = int(summary.get("raw_match_count") or 0)
        if raw_match_count:
            lines.append(
                "_Review-only string hits existed for this pattern, but no committed branch block survived compaction. Raw unresolved output stays out of the committed surface._"
            )
            lines.append("")
        else:
            lines.append("_No matching strings found._")
            lines.append("")

    return "\n".join(lines).rstrip() + "\n"


def compact_artifact(
    evidence_path: Path,
    markdown_path: Path,
    max_symbolized_per_pattern: int,
    max_review_per_pattern: int,
    force: bool,
) -> dict[str, Any]:
    payload = load_json(evidence_path)
    if payload.get("compacted_for_commit") and not force:
        return {
            "evidence": str(evidence_path),
            "markdown": str(markdown_path),
            "raw_match_count": int(payload.get("raw_match_count") or len(payload.get("matches") or [])),
            "committed_match_count": int(payload.get("committed_match_count") or len(payload.get("matches") or [])),
            "omitted_review_only_count": int(payload.get("omitted_review_only_count") or 0),
            "omitted_unresolved_count": int(payload.get("omitted_unresolved_count") or 0),
            "skipped": True,
        }

    raw_matches = [item for item in payload.get("matches") or [] if isinstance(item, dict)]
    metadata = parse_markdown_metadata(markdown_path)
    pattern_order = list(metadata.get("patterns") or [])
    seen_patterns = {pattern for pattern in pattern_order}
    for match in raw_matches:
        pattern = str(match.get("pattern") or "")
        if pattern and pattern not in seen_patterns:
            pattern_order.append(pattern)
            seen_patterns.add(pattern)

    committed_matches, summaries, committed_by_pattern = compact_matches(
        raw_matches,
        pattern_order,
        max_symbolized_per_pattern=max_symbolized_per_pattern,
        max_review_per_pattern=max_review_per_pattern,
    )

    payload["raw_match_count"] = len(raw_matches)
    payload["committed_match_count"] = len(committed_matches)
    payload["omitted_symbolized_count"] = sum(int(item.get("omitted_symbolized_count") or 0) for item in summaries)
    payload["omitted_review_only_count"] = sum(int(item.get("omitted_review_only_count") or 0) for item in summaries)
    payload["omitted_unresolved_count"] = sum(int(item.get("omitted_unresolved_count") or 0) for item in summaries)
    payload["compacted_for_commit"] = True
    payload["match_compaction"] = {
        "version": 1,
        "max_symbolized_per_pattern": max_symbolized_per_pattern,
        "max_review_only_per_pattern": max_review_per_pattern,
        "patterns": summaries,
    }
    payload["matches"] = committed_matches

    write_json(evidence_path, payload)
    markdown = render_markdown(markdown_path, evidence_path, payload, pattern_order, committed_by_pattern, summaries)
    markdown_path.write_text(markdown, encoding="utf-8", newline="\n")

    return {
        "evidence": str(evidence_path),
        "markdown": str(markdown_path),
        "raw_match_count": len(raw_matches),
        "committed_match_count": len(committed_matches),
        "omitted_review_only_count": payload["omitted_review_only_count"],
        "omitted_unresolved_count": payload["omitted_unresolved_count"],
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Compact Ghidra branch-review output for committed artifacts.")
    parser.add_argument("--evidence", required=True, type=Path)
    parser.add_argument("--markdown", required=True, type=Path)
    parser.add_argument("--max-symbolized-per-pattern", type=int, default=2)
    parser.add_argument("--max-review-per-pattern", type=int, default=1)
    parser.add_argument("--force", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    result = compact_artifact(
        evidence_path=args.evidence,
        markdown_path=args.markdown,
        max_symbolized_per_pattern=args.max_symbolized_per_pattern,
        max_review_per_pattern=args.max_review_per_pattern,
        force=args.force,
    )
    print(json.dumps(result, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
