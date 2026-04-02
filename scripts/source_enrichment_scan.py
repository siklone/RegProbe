#!/usr/bin/env python3
import argparse
import fnmatch
import json
import os
from pathlib import Path
from typing import Dict, List


def load_json(path: Path):
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def normalize_candidate(candidate: Dict) -> Dict:
    return {
        "candidate_id": candidate.get("candidate_id"),
        "family": candidate.get("family"),
        "registry_path": candidate.get("registry_path"),
        "value_name": candidate.get("value_name"),
    }


def load_candidates(path: Path, family: str, candidate_ids: List[str]) -> List[Dict]:
    data = load_json(path)
    candidates = [normalize_candidate(item) for item in data.get("candidates", [])]
    if family:
        candidates = [item for item in candidates if item.get("family") == family]
    if candidate_ids:
        wanted = set(candidate_ids)
        candidates = [item for item in candidates if item.get("candidate_id") in wanted]
    return [item for item in candidates if item.get("candidate_id") and item.get("value_name")]


def load_sources(path: Path, source_ids: List[str]) -> List[Dict]:
    data = load_json(path)
    sources = data.get("sources", [])
    if source_ids:
        wanted = set(source_ids)
        sources = [item for item in sources if item.get("id") in wanted]
    else:
        sources = [item for item in sources if item.get("enabled_by_default", False)]
    return sources


def matches_patterns(name: str, patterns: List[str]) -> bool:
    return any(fnmatch.fnmatch(name, pattern) for pattern in patterns)


def scan_source(source: Dict, candidates: List[Dict], max_hits_per_key: int = 8) -> Dict:
    root = Path(os.path.expandvars(source["root"]))
    patterns = source.get("patterns", [])
    result = {
        "id": source["id"],
        "label": source.get("label", source["id"]),
        "kind": source.get("kind", "unknown"),
        "root": str(root),
        "git_url": source.get("git_url"),
        "exists": root.exists(),
        "files_scanned": 0,
        "hit_count": 0,
        "hits_by_candidate": {},
    }
    if not root.exists():
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

    return result


def build_master(candidates: List[Dict], source_results: List[Dict]) -> Dict:
    per_key = {}
    for candidate in candidates:
        candidate_id = candidate["candidate_id"]
        source_hits = {}
        found_count = 0
        for source in source_results:
            hits = source["hits_by_candidate"].get(candidate_id, [])
            source_hits[source["id"]] = {
                "found": len(hits) > 0,
                "hit_count": len(hits),
                "hits": hits,
                "root": source["root"],
                "kind": source["kind"],
            }
            if hits:
                found_count += 1
        per_key[candidate_id] = {
            "candidate_id": candidate_id,
            "family": candidate.get("family"),
            "registry_path": candidate.get("registry_path"),
            "value_name": candidate.get("value_name"),
            "source_hits": source_hits,
            "enrichment_score": found_count,
        }

    summary = {
        "generated_utc": __import__("datetime").datetime.utcnow().isoformat() + "Z",
        "total_candidates": len(candidates),
        "total_sources": len(source_results),
        "sources": [
            {
                "id": source["id"],
                "label": source["label"],
                "kind": source["kind"],
                "root": source["root"],
                "exists": source["exists"],
                "files_scanned": source["files_scanned"],
                "hit_count": source["hit_count"],
            }
            for source in source_results
        ],
        "candidates": list(per_key.values()),
    }
    return summary


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--candidate-manifest", required=True)
    parser.add_argument("--source-config", required=True)
    parser.add_argument("--output-root", required=True)
    parser.add_argument("--family", default="")
    parser.add_argument("--candidate-id", action="append", default=[])
    parser.add_argument("--source-id", action="append", default=[])
    args = parser.parse_args()

    output_root = Path(args.output_root)
    output_root.mkdir(parents=True, exist_ok=True)
    per_key_root = output_root / "per-key"
    per_key_root.mkdir(parents=True, exist_ok=True)

    candidates = load_candidates(Path(args.candidate_manifest), args.family, args.candidate_id)
    sources = load_sources(Path(args.source_config), args.source_id)
    source_results = [scan_source(source, candidates) for source in sources]
    master = build_master(candidates, source_results)

    for candidate in master["candidates"]:
        candidate_path = per_key_root / f"{candidate['candidate_id']}.json"
        with candidate_path.open("w", encoding="utf-8") as handle:
            json.dump(candidate, handle, indent=2)

    master_path = output_root / "master-enrichment.json"
    with master_path.open("w", encoding="utf-8") as handle:
        json.dump(master, handle, indent=2)

    print(str(master_path))


if __name__ == "__main__":
    main()
