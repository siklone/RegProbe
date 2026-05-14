#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
from collections import Counter
from datetime import UTC, datetime
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
AUDIT_DIR = REPO_ROOT / "registry-research-framework" / "audit"
DEFAULT_OUTPUT = AUDIT_DIR / "operator96-low-noise-rerun-aggregate-20260510.json"
DEFAULT_MARKDOWN_OUTPUT = AUDIT_DIR / "operator96-low-noise-rerun-aggregate-20260510.md"
DEFAULT_GLOB = "operator96-low-noise-rerun-tranche*.json"

OLD_TRANCHE_RE = re.compile(r"operator96-low-noise-rerun-tranche(?:-(?P<number>\d+))?-(?P<date>\d{8})\.json$")
NEW_TRANCHE_RE = re.compile(r"operator96-low-noise-rerun-tranche-(?P<date>\d{8})-(?P<number>\d+)\.json$")


def now_utc() -> str:
    return datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def relative(path: Path) -> str:
    try:
        return str(path.resolve().relative_to(REPO_ROOT))
    except ValueError:
        return str(path)


def load_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def campaign_sort_key(path: Path) -> tuple[str, int, str]:
    old_match = OLD_TRANCHE_RE.match(path.name)
    if old_match:
        number = int(old_match.group("number") or "1")
        return (old_match.group("date"), number, path.name)
    new_match = NEW_TRANCHE_RE.match(path.name)
    if new_match:
        return (new_match.group("date"), int(new_match.group("number")), path.name)
    return ("99999999", 9999, path.name)


def find_campaign_paths(audit_dir: Path = AUDIT_DIR, pattern: str = DEFAULT_GLOB) -> list[Path]:
    return sorted(audit_dir.glob(pattern), key=campaign_sort_key)


def item_key(item: dict[str, Any]) -> tuple[Any, ...]:
    experiment_id = item.get("experiment_id")
    if experiment_id:
        return ("experiment_id", str(experiment_id))
    return (
        "record",
        item.get("index"),
        item.get("registry_path"),
        item.get("value_name"),
        item.get("value_data"),
    )


def merge_unique(items: list[dict[str, Any]]) -> tuple[list[dict[str, Any]], int]:
    merged: list[dict[str, Any]] = []
    seen: set[tuple[Any, ...]] = set()
    duplicates = 0
    for item in items:
        key = item_key(item)
        if key in seen:
            duplicates += 1
            continue
        seen.add(key)
        merged.append(item)
    return merged, duplicates


def observation_counter(results: list[dict[str, Any]], field: str) -> Counter[str]:
    counter: Counter[str] = Counter()
    for result in results:
        observations = result.get("observations") if isinstance(result.get("observations"), dict) else {}
        counter[str(observations.get(field) or "missing")] += 1
    return counter


def noisy_results(results: list[dict[str, Any]]) -> list[dict[str, Any]]:
    noisy: list[dict[str, Any]] = []
    for result in results:
        observations = result.get("observations") if isinstance(result.get("observations"), dict) else {}
        if observations.get("verdict") != "noisy" and observations.get("host_noise") != "noisy":
            continue
        noisy.append(
            {
                "experiment_id": result.get("experiment_id"),
                "value_name": result.get("value_name"),
                "verdict": observations.get("verdict"),
                "host_noise": observations.get("host_noise"),
                "artifact_json": result.get("artifact_json"),
            }
        )
    return noisy


def hard_smoke_passed(result: dict[str, Any]) -> bool:
    observations = result.get("observations") if isinstance(result.get("observations"), dict) else {}
    hard = observations.get("smoke_hard_success") if isinstance(observations.get("smoke_hard_success"), dict) else {}
    return all(bool(value) for value in hard.values()) if hard else False


def build_aggregate(paths: list[Path]) -> dict[str, Any]:
    source_campaigns: list[dict[str, Any]] = []
    all_plan: list[dict[str, Any]] = []
    all_results: list[dict[str, Any]] = []

    for path in paths:
        payload = load_json(path)
        plan = [item for item in payload.get("plan") or [] if isinstance(item, dict)]
        results = [item for item in payload.get("results") or [] if isinstance(item, dict)]
        non_ok = [result.get("experiment_id") for result in results if result.get("status") != "ok"]
        source_campaigns.append(
            {
                "path": relative(path),
                "status": payload.get("status"),
                "plan_count": len(plan),
                "result_count": len(results),
                "non_ok_count": len(non_ok),
                "non_ok": non_ok,
            }
        )
        all_plan.extend(plan)
        all_results.extend(results)

    plan, duplicate_plan_count = merge_unique(all_plan)
    results, duplicate_result_count = merge_unique(all_results)
    non_ok = [result.get("experiment_id") for result in results if result.get("status") != "ok"]
    source_failures = [
        source["path"]
        for source in source_campaigns
        if source.get("status") != "ok" or int(source.get("non_ok_count") or 0) > 0
    ]
    noisy = noisy_results(results)

    status = "ok" if not source_failures and not non_ok else "review"
    return {
        "schema_version": "1.0",
        "generated_utc": now_utc(),
        "campaign_id": "operator96-low-noise-rerun-aggregate-20260510",
        "status": status,
        "source_campaigns": source_campaigns,
        "summary": {
            "source_campaign_count": len(source_campaigns),
            "plan_count": len(plan),
            "result_count": len(results),
            "duplicate_plan_count": duplicate_plan_count,
            "duplicate_result_count": duplicate_result_count,
            "non_ok_count": len(non_ok),
            "non_ok": non_ok,
            "source_failures": source_failures,
            "verdict_counts": dict(sorted(observation_counter(results, "verdict").items())),
            "host_noise_counts": dict(sorted(observation_counter(results, "host_noise").items())),
            "confidence_counts": dict(sorted(observation_counter(results, "confidence").items())),
            "hard_smoke_all": all(hard_smoke_passed(result) for result in results),
            "noisy_result_count": len(noisy),
            "noisy_results": noisy,
        },
        "plan": plan,
        "results": results,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    summary = payload.get("summary") or {}
    lines = [
        "# Custom Registry Value Low-Noise Rerun Aggregate",
        "",
        f"- Generated UTC: `{payload.get('generated_utc')}`",
        f"- Status: `{payload.get('status')}`",
        f"- Source campaigns: `{summary.get('source_campaign_count')}`",
        f"- Plan entries: `{summary.get('plan_count')}`",
        f"- Results: `{summary.get('result_count')}`",
        f"- Non-ok: `{summary.get('non_ok_count')}`",
        f"- Hard smoke all: `{summary.get('hard_smoke_all')}`",
        f"- Noisy results: `{summary.get('noisy_result_count')}`",
        "",
        "## Counts",
        "",
        f"- Verdicts: `{summary.get('verdict_counts')}`",
        f"- Host noise: `{summary.get('host_noise_counts')}`",
        f"- Confidence: `{summary.get('confidence_counts')}`",
        "",
        "## Source Campaigns",
        "",
        "| Campaign | Status | Plan | Results | Non-ok |",
        "|---|---|---:|---:|---:|",
    ]
    for source in payload.get("source_campaigns") or []:
        lines.append(
            f"| `{source.get('path')}` | `{source.get('status')}` | "
            f"{source.get('plan_count')} | {source.get('result_count')} | {source.get('non_ok_count')} |"
        )
    noisy = summary.get("noisy_results") if isinstance(summary.get("noisy_results"), list) else []
    if noisy:
        lines.extend(
            [
                "",
                "## Noisy Results",
                "",
                "| Experiment | Value | Verdict | Host noise | Artifact |",
                "|---|---|---|---|---|",
            ]
        )
        for item in noisy:
            lines.append(
                f"| `{item.get('experiment_id')}` | `{item.get('value_name')}` | "
                f"`{item.get('verdict')}` | `{item.get('host_noise')}` | `{item.get('artifact_json')}` |"
            )
    return "\n".join(lines) + "\n"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Aggregate custom registry value low-noise rerun tranche campaign outputs."
    )
    parser.add_argument("--audit-dir", default=str(AUDIT_DIR))
    parser.add_argument("--pattern", default=DEFAULT_GLOB)
    parser.add_argument("--output", default=str(DEFAULT_OUTPUT))
    parser.add_argument("--markdown-output", default=str(DEFAULT_MARKDOWN_OUTPUT))
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    paths = find_campaign_paths(Path(args.audit_dir).resolve(), args.pattern)
    payload = build_aggregate(paths)
    output = Path(args.output).resolve()
    markdown_output = Path(args.markdown_output).resolve()
    write_json(output, payload)
    markdown_output.parent.mkdir(parents=True, exist_ok=True)
    markdown_output.write_text(render_markdown(payload), encoding="utf-8")
    print(
        json.dumps(
            {
                "status": payload["status"],
                "json": relative(output),
                "markdown": relative(markdown_output),
                "source_campaigns": payload["summary"]["source_campaign_count"],
                "results": payload["summary"]["result_count"],
                "non_ok": payload["summary"]["non_ok_count"],
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
