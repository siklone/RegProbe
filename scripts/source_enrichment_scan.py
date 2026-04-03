from __future__ import annotations

import argparse
import fnmatch
import json
import os
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Dict, Iterable, List


DEFAULT_WEIGHTS = {
    "reactos": 3,
    "wrk": 3,
    "systeminformer": 2,
    "sandboxie": 1,
    "wine": 1,
    "admx": 2,
    "wdk_headers": 2,
    "geoff_chappell": 2,
}


def load_json(path: Path):
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def normalize_candidate(candidate: Dict[str, Any]) -> Dict[str, Any]:
    return {
        "candidate_id": candidate.get("candidate_id"),
        "family": candidate.get("family"),
        "suspected_layer": candidate.get("suspected_layer"),
        "boot_phase_relevant": bool(candidate.get("boot_phase_relevant", False)),
        "frida_allowed": bool(candidate.get("frida_allowed", False)),
        "registry_path": candidate.get("registry_path"),
        "value_name": candidate.get("value_name"),
        "route_bucket": candidate.get("route_bucket"),
        "route_note": candidate.get("route_note"),
        "source_note": candidate.get("source_note"),
    }


def load_candidates(path: Path, family: str, candidate_ids: List[str]) -> List[Dict[str, Any]]:
    data = load_json(path)
    candidates = [normalize_candidate(item) for item in data.get("candidates", [])]
    if family:
        candidates = [item for item in candidates if item.get("family") == family]
    if candidate_ids:
        wanted = set(candidate_ids)
        candidates = [item for item in candidates if item.get("candidate_id") in wanted]
    return [item for item in candidates if item.get("candidate_id") and item.get("value_name")]


def load_sources(path: Path, source_ids: List[str]) -> List[Dict[str, Any]]:
    data = load_json(path)
    sources = data.get("sources", [])
    if source_ids:
        wanted = set(source_ids)
        sources = [item for item in sources if item.get("id") in wanted]
    else:
        sources = [item for item in sources if item.get("enabled_by_default", False)]
    return sources


def expand_root(value: str | None) -> Path | None:
    if not value:
        return None
    expanded = os.path.expandvars(value)
    if not expanded:
        return None
    return Path(expanded)


def matches_patterns(name: str, patterns: List[str]) -> bool:
    return any(fnmatch.fnmatch(name, pattern) for pattern in patterns)


def count_source_weight(source: Dict[str, Any]) -> int:
    weight = source.get("enrichment_weight")
    if isinstance(weight, int):
        return weight
    return DEFAULT_WEIGHTS.get(source.get("id"), 1)


def scan_source(source: Dict[str, Any], candidates: List[Dict[str, Any]], max_hits_per_key: int = 8) -> Dict[str, Any]:
    root = expand_root(source.get("root"))
    patterns = source.get("patterns", [])
    result: Dict[str, Any] = {
        "id": source["id"],
        "label": source.get("label", source["id"]),
        "surface_group": source.get("surface_group", "unknown"),
        "kind": source.get("kind", "unknown"),
        "root": str(root) if root else None,
        "git_url": source.get("git_url"),
        "weight": count_source_weight(source),
        "exists": bool(root and root.exists()),
        "files_scanned": 0,
        "hit_count": 0,
        "candidate_hit_count": 0,
        "hits_by_candidate": {},
        "missing_reason": None,
    }
    if not root or not root.exists():
        result["missing_reason"] = "root-missing"
        return result

    lowered = [(item["candidate_id"], item["value_name"], item["value_name"].lower()) for item in candidates]
    for file_path in root.rglob("*"):
        if not file_path.is_file():
            continue
        if patterns and not matches_patterns(file_path.name, patterns):
            continue

        result["files_scanned"] += 1
        try:
            with file_path.open("r", encoding="utf-8", errors="ignore") as handle:
                for line_number, line in enumerate(handle, start=1):
                    line_lower = line.lower()
                    for candidate_id, value_name, lowered_value in lowered:
                        if lowered_value not in line_lower:
                            continue
                        bucket = result["hits_by_candidate"].setdefault(candidate_id, [])
                        if len(bucket) >= max_hits_per_key:
                            continue
                        bucket.append(
                            {
                                "file": str(file_path),
                                "line_number": line_number,
                                "value_name": value_name,
                                "content": line.strip(),
                            }
                        )
                        result["hit_count"] += 1
        except OSError:
            continue

    result["candidate_hit_count"] = len(result["hits_by_candidate"])
    return result


def trigger_family_for_candidate(candidate: Dict[str, Any]) -> str:
    value_name = (candidate.get("value_name") or "").lower()
    family = (candidate.get("family") or "").lower()

    if "power" in family or "request" in value_name:
        return "power-request-simulation"
    if any(token in value_name for token in ("timer", "tick", "dpc")) or "kernel" in family:
        return "timer-dpc-stress"
    if any(token in value_name for token in ("worker", "thread", "mutant", "mutex")):
        return "worker-thread-contention"
    if any(token in value_name for token in ("hiber", "sleep", "standby", "resume")):
        return "power-state-transition"
    if any(token in value_name for token in ("display", "vsync")):
        return "display-state-toggle"
    if any(token in value_name for token in ("network", "smb")):
        return "network-activity"
    if any(token in value_name for token in ("telemetry", "wer")):
        return "telemetry-or-reporting"
    return "general-registry-watch"


def suggested_trigger(candidate: Dict[str, Any]) -> List[str]:
    family = trigger_family_for_candidate(candidate)
    if family == "power-request-simulation":
        return [
            "PowerCreateRequest(SystemRequired)",
            "PowerSetRequest(DisplayRequired)",
            "audio playback session",
        ]
    if family == "timer-dpc-stress":
        return [
            "high-resolution timer request",
            "multiple concurrent timers",
            "DPC-heavy workload",
        ]
    if family == "worker-thread-contention":
        return [
            "worker thread saturation",
            "mutex/mutant contention",
            "rapid process create/destroy",
        ]
    if family == "power-state-transition":
        return [
            "power plan cycling",
            "hibernate toggle",
            "sleep-study / resume cycle",
        ]
    if family == "display-state-toggle":
        return [
            "foreground/background switch",
            "display power-state toggle",
            "DWM flush",
        ]
    if family == "network-activity":
        return [
            "network adapter reset",
            "SMB multichannel activity",
            "localhost request burst",
        ]
    if family == "telemetry-or-reporting":
        return [
            "WER trigger",
            "power report generation",
            "telemetry query",
        ]
    return ["boot trace", "ETW mega-trigger", "bounded registry watch"]


def score_candidate(candidate: Dict[str, Any], source_results: List[Dict[str, Any]]) -> Dict[str, Any]:
    route_bucket = candidate.get("route_bucket")
    suspected_layer = candidate.get("suspected_layer")
    boot_phase_relevant = bool(candidate.get("boot_phase_relevant", False))

    source_hits: Dict[str, Any] = {}
    weighted_score = 0
    source_count = 0
    top_source_hits = []
    for source in source_results:
        hits = source["hits_by_candidate"].get(candidate["candidate_id"], [])
        supported = len(hits) > 0
        if supported:
            source_count += 1
            weighted_score += int(source["weight"])
            top_source_hits.append(
                {
                    "source_id": source["id"],
                    "label": source["label"],
                    "surface_group": source["surface_group"],
                    "kind": source["kind"],
                    "weight": source["weight"],
                    "hit_count": len(hits),
                    "hits": hits,
                }
            )
        source_hits[source["id"]] = {
            "found": supported,
            "hit_count": len(hits),
            "weight": source["weight"],
            "hits": hits,
            "root": source["root"],
            "kind": source["kind"],
            "surface_group": source["surface_group"],
            "missing_reason": source.get("missing_reason"),
        }

    trigger_family = trigger_family_for_candidate(candidate)
    trigger_suggestion = suggested_trigger(candidate)
    runtime_priority = "low"
    if boot_phase_relevant and weighted_score >= 4:
        runtime_priority = "high"
    elif boot_phase_relevant and weighted_score >= 2:
        runtime_priority = "medium"
    elif weighted_score > 0:
        runtime_priority = "medium"

    if route_bucket in {"net-new", "docs-first-new-candidate"} and weighted_score >= 2 and boot_phase_relevant:
        runtime_priority = "high"
    if route_bucket == "research-only" and weighted_score <= 1:
        runtime_priority = "hold"

    if suspected_layer in {"kernel", "boot", "driver"} and weighted_score <= 1:
        suggested_bucket = "windbg"
    elif trigger_family in {"power-request-simulation", "power-state-transition"}:
        suggested_bucket = "runtime"
    elif trigger_family in {"timer-dpc-stress", "worker-thread-contention"}:
        suggested_bucket = "windbg"
    else:
        suggested_bucket = "hold"

    return {
        "candidate_id": candidate["candidate_id"],
        "family": candidate.get("family"),
        "suspected_layer": suspected_layer,
        "boot_phase_relevant": boot_phase_relevant,
        "registry_path": candidate.get("registry_path"),
        "value_name": candidate.get("value_name"),
        "route_bucket": route_bucket,
        "route_note": candidate.get("route_note"),
        "source_note": candidate.get("source_note"),
        "source_hits": source_hits,
        "support_count": source_count,
        "enrichment_score": weighted_score,
        "trigger_family": trigger_family,
        "suggested_trigger_family": trigger_family,
        "suggested_trigger": trigger_suggestion,
        "suggested_runtime_priority": runtime_priority,
        "suggested_queue_bucket": suggested_bucket,
        "supporting_sources": top_source_hits,
    }


def build_source_index(source_results: List[Dict[str, Any]], candidates: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
    index: List[Dict[str, Any]] = []
    candidate_map = {item["candidate_id"]: item for item in candidates}
    for source in source_results:
        candidate_hits = []
        for candidate_id, hits in source["hits_by_candidate"].items():
            candidate = candidate_map.get(candidate_id, {})
            candidate_hits.append(
                {
                    "candidate_id": candidate_id,
                    "family": candidate.get("family"),
                    "registry_path": candidate.get("registry_path"),
                    "value_name": candidate.get("value_name"),
                    "hit_count": len(hits),
                    "sample_hits": hits[:3],
                }
            )

        index.append(
            {
                "id": source["id"],
                "label": source["label"],
                "surface_group": source["surface_group"],
                "kind": source["kind"],
                "root": source["root"],
                "exists": source["exists"],
                "missing_reason": source["missing_reason"],
                "weight": source["weight"],
                "files_scanned": source["files_scanned"],
                "hit_count": source["hit_count"],
                "candidate_hit_count": source["candidate_hit_count"],
                "candidate_hits": sorted(candidate_hits, key=lambda item: (-item["hit_count"], item["candidate_id"])),
            }
        )
    return index


def _priority_sort_key(candidate: Dict[str, Any]) -> tuple:
    return (
        -int(candidate.get("enrichment_score", 0)),
        -int(candidate.get("support_count", 0)),
        candidate.get("candidate_id") or "",
    )


def build_priority_queue(candidates: List[Dict[str, Any]]) -> Dict[str, List[str]]:
    high_priority_runtime: List[Dict[str, Any]] = []
    high_priority_windbg: List[Dict[str, Any]] = []
    low_priority_hold: List[Dict[str, Any]] = []

    for candidate in candidates:
        queue_bucket = candidate.get("suggested_queue_bucket")
        if queue_bucket == "runtime":
            high_priority_runtime.append(candidate)
        elif queue_bucket == "windbg":
            high_priority_windbg.append(candidate)
        else:
            low_priority_hold.append(candidate)

    return {
        "high_priority_runtime": [item["candidate_id"] for item in sorted(high_priority_runtime, key=_priority_sort_key)],
        "high_priority_windbg": [item["candidate_id"] for item in sorted(high_priority_windbg, key=_priority_sort_key)],
        "low_priority_hold": [item["candidate_id"] for item in sorted(low_priority_hold, key=_priority_sort_key)],
    }


def build_master(candidates: List[Dict[str, Any]], source_results: List[Dict[str, Any]], candidate_manifest: Path, source_config: Path, output_root: Path) -> Dict[str, Any]:
    per_key = [score_candidate(candidate, source_results) for candidate in candidates]
    source_index = build_source_index(source_results, candidates)
    queue = build_priority_queue(per_key)

    route_counts = Counter(item.get("route_bucket") or "unknown" for item in candidates)
    support_counts = Counter(item["suggested_trigger_family"] for item in per_key)

    summary = {
        "generated_utc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "candidate_manifest": str(candidate_manifest),
        "source_config": str(source_config),
        "output_root": str(output_root),
        "total_candidates": len(candidates),
        "total_sources": len(source_results),
        "route_counts": dict(route_counts),
        "trigger_family_counts": dict(support_counts),
        "sources": [
            {
                "id": source["id"],
                "label": source["label"],
                "surface_group": source["surface_group"],
                "kind": source["kind"],
                "root": source["root"],
                "exists": source["exists"],
                "missing_reason": source["missing_reason"],
                "weight": source["weight"],
                "files_scanned": source["files_scanned"],
                "hit_count": source["hit_count"],
                "candidate_hit_count": source["candidate_hit_count"],
            }
            for source in source_results
        ],
        "candidates": per_key,
        "priority_queue": queue,
        "source_index": source_index,
        "source_support_snapshot": {
            "available_sources": [item["id"] for item in source_results if item["exists"]],
            "missing_sources": [item["id"] for item in source_results if not item["exists"]],
            "high_priority_count": len(queue["high_priority_runtime"]),
            "windbg_priority_count": len(queue["high_priority_windbg"]),
        },
    }
    return summary


def write_markdown_summary(master: Dict[str, Any]) -> str:
    lines = [
        "# Source Enrichment Summary",
        "",
        f"- Generated: `{master['generated_utc']}`",
        f"- Candidates: `{master['total_candidates']}`",
        f"- Sources: `{master['total_sources']}`",
        "",
        "## Priority Queue",
        f"- Runtime: `{len(master['priority_queue']['high_priority_runtime'])}`",
        f"- WinDbg: `{len(master['priority_queue']['high_priority_windbg'])}`",
        f"- Hold: `{len(master['priority_queue']['low_priority_hold'])}`",
        "",
        "## Available Sources",
    ]
    for source in master["sources"]:
        state = "present" if source["exists"] else f"missing ({source['missing_reason']})"
        lines.append(f"- `{source['id']}`: {state}, hits `{source['hit_count']}`, scanned `{source['files_scanned']}`")
    return "\n".join(lines) + "\n"


def candidate_to_json(candidate: Dict[str, Any]) -> Dict[str, Any]:
    return candidate


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--candidate-manifest", required=True)
    parser.add_argument("--source-config", required=True)
    parser.add_argument("--output-root", required=True)
    parser.add_argument("--family", default="")
    parser.add_argument("--candidate-id", action="append", default=[])
    parser.add_argument("--source-id", action="append", default=[])
    args = parser.parse_args()

    candidate_manifest = Path(args.candidate_manifest)
    source_config = Path(args.source_config)
    output_root = Path(args.output_root)
    output_root.mkdir(parents=True, exist_ok=True)

    per_key_root = output_root / "per-key"
    per_source_root = output_root / "per-source"
    per_key_root.mkdir(parents=True, exist_ok=True)
    per_source_root.mkdir(parents=True, exist_ok=True)

    candidates = load_candidates(candidate_manifest, args.family, args.candidate_id)
    sources = load_sources(source_config, args.source_id)
    source_results = [scan_source(source, candidates) for source in sources]
    master = build_master(candidates, source_results, candidate_manifest, source_config, output_root)

    for candidate in master["candidates"]:
        candidate_path = per_key_root / f"{candidate['candidate_id']}.json"
        with candidate_path.open("w", encoding="utf-8") as handle:
            json.dump(candidate_to_json(candidate), handle, indent=2)

    for source in master["source_index"]:
        source_path = per_source_root / f"{source['id']}.json"
        with source_path.open("w", encoding="utf-8") as handle:
            json.dump(source, handle, indent=2)

    master_path = output_root / "master-enrichment.json"
    with master_path.open("w", encoding="utf-8") as handle:
        json.dump(master, handle, indent=2)

    source_index_path = output_root / "source-index.json"
    with source_index_path.open("w", encoding="utf-8") as handle:
        json.dump(master["source_index"], handle, indent=2)

    priority_queue_path = output_root / "priority-queue.json"
    with priority_queue_path.open("w", encoding="utf-8") as handle:
        json.dump(master["priority_queue"], handle, indent=2)

    summary_md_path = output_root / "master-enrichment.md"
    summary_md_path.write_text(write_markdown_summary(master), encoding="utf-8")

    print(str(master_path))


if __name__ == "__main__":
    main()
