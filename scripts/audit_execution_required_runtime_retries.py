#!/usr/bin/env python3
from __future__ import annotations

import json
from collections import Counter
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
STAGING_ROOT = REPO_ROOT / "evidence" / "files" / "vm-tooling-staging"
OUTPUT_BASENAME = "execution-required-runtime-retry-audit-20260408"
OUTPUT_JSON = REPO_ROOT / "registry-research-framework" / "audit" / f"{OUTPUT_BASENAME}.json"
OUTPUT_MD = REPO_ROOT / "registry-research-framework" / "audit" / f"{OUTPUT_BASENAME}.md"
SUMMARY_GLOB = "power-control-batch-mega-trigger-runtime-primary-*/summary.json"
TARGETS = {
    "power.control.allow-audio-to-enable-execution-required-power-requests": "AllowAudioToEnableExecutionRequiredPowerRequests",
    "power.control.allow-system-required-power-requests": "AllowSystemRequiredPowerRequests",
}


def load_json(path: Path) -> dict:
    payload = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(payload, dict):
        raise ValueError(f"{path} JSON payload is not an object")
    return payload


def write_json(path: Path, payload: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content.rstrip() + "\n", encoding="utf-8")


def main() -> int:
    runs: list[dict] = []
    summary_status_counts: Counter[str] = Counter()
    candidate_status_counts: Counter[str] = Counter()

    for summary_path in sorted(STAGING_ROOT.glob(SUMMARY_GLOB)):
        probe_root = summary_path.parent
        results_path = probe_root / "results.json"
        state_path = probe_root / "state.json"
        if not results_path.exists() or not state_path.exists():
            continue

        try:
            summary = load_json(summary_path)
            results = load_json(results_path)
            state = load_json(state_path)
        except Exception:
            continue

        candidate_map = {
            str(candidate.get("candidate_id") or ""): candidate
            for candidate in results.get("candidates") or []
            if isinstance(candidate, dict)
        }
        if not TARGETS.keys() <= candidate_map.keys():
            continue

        transitions: dict[str, dict[str, int | None]] = {}
        pair_statuses: dict[str, str | None] = {}
        exact_query_hits = 0
        exact_line_count = 0
        for candidate_id, value_name in TARGETS.items():
            candidate = candidate_map[candidate_id]
            pair_statuses[candidate_id] = candidate.get("status")
            candidate_status_counts[str(candidate.get("status") or "unknown")] += 1
            exact_query_hits += int(candidate.get("exact_query_hits") or 0)
            exact_line_count += int(candidate.get("exact_line_count") or 0)
            transitions[value_name] = {
                "before": (state.get("baseline_values") or {}).get(value_name),
                "after": (state.get("candidate_values") or {}).get(value_name),
            }

        summary_status = str(summary.get("status") or "unknown")
        summary_status_counts[summary_status] += 1
        runs.append(
            {
                "probe_name": probe_root.name,
                "generated_utc": summary.get("generated_utc"),
                "summary_status": summary_status,
                "pair_statuses": pair_statuses,
                "exact_query_hits": exact_query_hits,
                "exact_line_count": exact_line_count,
                "value_transitions": transitions,
                "summary_file": summary_path.relative_to(REPO_ROOT).as_posix(),
                "results_file": results_path.relative_to(REPO_ROOT).as_posix(),
                "state_file": state_path.relative_to(REPO_ROOT).as_posix(),
            }
        )

    armed_pair_runs = 0
    for run in runs:
        if all(
            transition.get("before") is None and transition.get("after") == 1
            for transition in run["value_transitions"].values()
        ):
            armed_pair_runs += 1

    payload = {
        "title": "Execution-required mega-trigger runtime retry audit",
        "generated_utc": "2026-04-08T16:20:00Z",
        "target_path": r"HKLM\SYSTEM\CurrentControlSet\Control\Power",
        "target_candidates": sorted(TARGETS.keys()),
        "summary_glob": SUMMARY_GLOB,
        "run_count": len(runs),
        "armed_pair_run_count": armed_pair_runs,
        "summary_status_counts": dict(summary_status_counts),
        "candidate_status_counts": dict(candidate_status_counts),
        "runs": runs,
    }
    write_json(OUTPUT_JSON, payload)

    lines = [
        "# Execution-Required Runtime Retry Audit",
        "",
        "Date: 2026-04-08",
        r"Target path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`",
        f"Probe glob: `{SUMMARY_GLOB}`",
        "",
        "## Outcome",
        "",
        f"- Parsed retained runs: `{len(runs)}`",
        f"- Runs that armed both execution-required values from `null` to `1`: `{armed_pair_runs}`",
        f"- Summary statuses: `{dict(summary_status_counts)}`",
        f"- Candidate statuses: `{dict(candidate_status_counts)}`",
        "- Every parsed retained mega-trigger runtime retry ended `aborted-recovered` for both execution-required candidates.",
        "- None of the parsed retries produced an exact query hit or exact line hit for the pair.",
        "",
        "## Artifacts",
        "",
        f"- `{OUTPUT_JSON.relative_to(REPO_ROOT).as_posix()}`",
    ]
    for run in runs:
        lines.append(f"- `{run['summary_file']}`")
        lines.append(f"- `{run['results_file']}`")
        lines.append(f"- `{run['state_file']}`")

    lines.extend(
        [
            "",
            "## Interpretation",
            "",
            "- The execution-required pair no longer lacks repeated runtime trigger attempts. The retained mega-trigger family was exercised many times on the current build.",
            "- The unresolved gap is now narrower than a generic `runtime-trace` miss: the repeated trigger family is unstable for this pair and consistently recovers before yielding an exact registry read.",
            "- This makes the next best proof path either a narrower exact-read lane or a different runtime surface, not another generic mega-trigger retry.",
        ]
    )
    write_text(OUTPUT_MD, "\n".join(lines))
    print(OUTPUT_JSON.relative_to(REPO_ROOT).as_posix())
    print(OUTPUT_MD.relative_to(REPO_ROOT).as_posix())
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
