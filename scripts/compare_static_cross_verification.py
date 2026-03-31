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


def summarize_status(ghidra: dict[str, Any], ida: dict[str, Any]) -> dict[str, Any]:
    g = first_match(ghidra)
    i = first_match(ida)

    if not g or not i:
        return {
            "executed": bool(ghidra) and bool(ida),
            "functions_match": None,
            "branches_match": None,
            "status": "insufficient",
            "confidence": "low",
            "cross_conflict": False,
            "notes": "At least one tool did not return a comparable bounded branch match.",
        }

    functions_match = (g.get("function_name") or "").lower() == (i.get("function_name") or "").lower()
    ghidra_branch = "\n".join(g.get("branch_snippet") or [])
    ida_branch = "\n".join(i.get("branch_snippet") or [])
    branches_match = bool(ghidra_branch and ida_branch and ghidra_branch.lower() == ida_branch.lower())

    if functions_match and branches_match:
        return {
            "executed": True,
            "functions_match": True,
            "branches_match": True,
            "status": "match",
            "confidence": "high",
            "cross_conflict": False,
            "notes": "Ghidra and IDA reported the same first function and bounded branch snippet.",
        }

    if functions_match is False or branches_match is False:
        return {
            "executed": True,
            "functions_match": functions_match,
            "branches_match": branches_match,
            "status": "conflict",
            "confidence": "low",
            "cross_conflict": True,
            "notes": "The tools disagreed on either the function identity or the bounded branch snippet.",
        }

    return {
        "executed": True,
        "functions_match": functions_match,
        "branches_match": branches_match,
        "status": "insufficient",
        "confidence": "low",
        "cross_conflict": False,
        "notes": "The tools ran, but one or both outputs were still unclear.",
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
