from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any


def load_json(path: Path) -> dict[str, Any]:
    with path.open("r", encoding="utf-8-sig") as handle:
        return json.load(handle)


def first_match(payload: dict[str, Any]) -> dict[str, Any] | None:
    matches = payload.get("matches") or []
    if matches:
        return matches[0]
    return None


def norm_text(value: Any) -> str:
    return str(value or "").strip().lower()


def branch_signature(match: dict[str, Any]) -> dict[str, str]:
    return {
        "compare_condition": norm_text(match.get("compare_condition")),
        "jump_condition": norm_text(match.get("jump_condition")),
        "branch_effect": norm_text(match.get("branch_effect")),
        "value_map": norm_text(match.get("value_map")),
    }


def build_parity_summary(
    ghidra_function: str,
    ida_function: str,
    branch_status: str,
    ghidra_value_map: str,
    ida_value_map: str,
    verdict: str,
) -> str:
    return (
        f"Ghidra function={ghidra_function or '<none>'}; "
        f"IDA function={ida_function or '<none>'}; "
        f"branch={branch_status}; "
        f"value_map={ghidra_value_map or '<none>'} vs {ida_value_map or '<none>'}; "
        f"verdict={verdict}."
    )


def summarize_status(ghidra: dict[str, Any], ida: dict[str, Any]) -> dict[str, Any]:
    g = first_match(ghidra)
    i = first_match(ida)

    if not g or not i:
        verdict = "review-only"
        return {
            "executed": bool(ghidra) and bool(ida),
            "functions_match": None,
            "branches_match": None,
            "status": "insufficient",
            "confidence": "low",
            "cross_conflict": False,
            "ghidra_function": g.get("function_name") if g else None,
            "ida_function": i.get("function_name") if i else None,
            "ghidra_function_confidence": g.get("function_confidence") if g else None,
            "ida_function_confidence": i.get("function_confidence") if i else None,
            "ghidra_value_map": g.get("value_map") if g else None,
            "ida_value_map": i.get("value_map") if i else None,
            "verdict": verdict,
            "parity_summary": build_parity_summary(
                ghidra_function=str(g.get("function_name") or "") if g else "",
                ida_function=str(i.get("function_name") or "") if i else "",
                branch_status="insufficient",
                ghidra_value_map=str(g.get("value_map") or "") if g else "",
                ida_value_map=str(i.get("value_map") or "") if i else "",
                verdict=verdict,
            ),
            "notes": "At least one tool did not return a comparable bounded branch match.",
        }

    ghidra_function = str(g.get("function_name") or "")
    ida_function = str(i.get("function_name") or "")
    ghidra_confidence = str(g.get("function_confidence") or "")
    ida_confidence = str(i.get("function_confidence") or "")
    ghidra_value_map = str(g.get("value_map") or "")
    ida_value_map = str(i.get("value_map") or "")
    functions_match = norm_text(ghidra_function) == norm_text(ida_function)
    ghidra_branch = branch_signature(g)
    ida_branch = branch_signature(i)
    branches_match = ghidra_branch == ida_branch and bool(ghidra_branch["compare_condition"] or ghidra_branch["jump_condition"])
    symbolized_both = ghidra_confidence == "symbolized_branch" and ida_confidence == "symbolized_branch"
    either_unclear = bool(g.get("unclear")) or bool(i.get("unclear"))

    if symbolized_both and functions_match and branches_match and not either_unclear:
        verdict = "cross-verified"
        return {
            "executed": True,
            "functions_match": True,
            "branches_match": True,
            "status": "match",
            "confidence": "high",
            "cross_conflict": False,
            "ghidra_function": ghidra_function,
            "ida_function": ida_function,
            "ghidra_function_confidence": ghidra_confidence,
            "ida_function_confidence": ida_confidence,
            "ghidra_value_map": ghidra_value_map,
            "ida_value_map": ida_value_map,
            "verdict": verdict,
            "parity_summary": build_parity_summary(
                ghidra_function=ghidra_function,
                ida_function=ida_function,
                branch_status="match",
                ghidra_value_map=ghidra_value_map,
                ida_value_map=ida_value_map,
                verdict=verdict,
            ),
            "notes": "Ghidra and IDA reported the same function, bounded branch template, and value map.",
        }

    if symbolized_both and (functions_match is False or branches_match is False):
        verdict = "manual-review-required"
        return {
            "executed": True,
            "functions_match": functions_match,
            "branches_match": branches_match,
            "status": "conflict",
            "confidence": "low",
            "cross_conflict": True,
            "ghidra_function": ghidra_function,
            "ida_function": ida_function,
            "ghidra_function_confidence": ghidra_confidence,
            "ida_function_confidence": ida_confidence,
            "ghidra_value_map": ghidra_value_map,
            "ida_value_map": ida_value_map,
            "verdict": verdict,
            "parity_summary": build_parity_summary(
                ghidra_function=ghidra_function,
                ida_function=ida_function,
                branch_status="conflict",
                ghidra_value_map=ghidra_value_map,
                ida_value_map=ida_value_map,
                verdict=verdict,
            ),
            "notes": "The tools disagreed on function identity, branch template, or value mapping.",
        }

    verdict = "review-only"
    return {
        "executed": True,
        "functions_match": functions_match,
        "branches_match": branches_match,
        "status": "insufficient",
        "confidence": "low",
        "cross_conflict": False,
        "ghidra_function": ghidra_function,
        "ida_function": ida_function,
        "ghidra_function_confidence": ghidra_confidence,
        "ida_function_confidence": ida_confidence,
        "ghidra_value_map": ghidra_value_map,
        "ida_value_map": ida_value_map,
        "verdict": verdict,
        "parity_summary": build_parity_summary(
            ghidra_function=ghidra_function,
            ida_function=ida_function,
            branch_status="insufficient",
            ghidra_value_map=ghidra_value_map,
            ida_value_map=ida_value_map,
            verdict=verdict,
        ),
        "notes": "The tools ran, but one or both outputs are still review-only or lack a symbolized branch mapping.",
    }


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(payload, handle, ensure_ascii=False, indent=2)
        handle.write("\n")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Compare Ghidra and IDA bounded branch outputs.")
    parser.add_argument("--ghidra", type=Path, required=True)
    parser.add_argument("--ida", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    ghidra = load_json(args.ghidra)
    ida = load_json(args.ida)
    payload = summarize_status(ghidra, ida)
    payload["ghidra_path"] = str(args.ghidra)
    payload["ida_path"] = str(args.ida)
    write_json(args.output, payload)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
