#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"
if str(SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_ROOT))

from research_v36_lib import (
    CURRENT_SCHEMA_VERSION,
    ETL_CORPUS_INVENTORY_PATH,
    ETL_REGISTRY_DISCOVERY_PATH,
    append_discovery_candidates,
    build_etl_corpus_inventory,
    default_execution_context,
    discover_etl_artifacts,
    etl_touch_candidates,
    load_etl_parser_config,
    normalize_repo_relative_path,
    now_utc,
    parse_etl_registry_touches,
    validate_discovery_candidate,
    write_json,
)


def _resolve_requested_repo_ref(raw_input: str) -> str:
    path = Path(raw_input)
    resolved = path if path.is_absolute() else (REPO_ROOT / path)
    return normalize_repo_relative_path(str(resolved.resolve().relative_to(REPO_ROOT)))


def main() -> int:
    parser = argparse.ArgumentParser(description="Parse ETL files into registry-touch discovery metadata.")
    parser.add_argument("--input", help="Specific ETL path to parse.")
    parser.add_argument("--output", help="Discovery JSON output path.")
    parser.add_argument("--inventory-output", help="Inventory JSON output path.")
    parser.add_argument("--parser", default=None, help="Parser name. Defaults to config value.")
    args = parser.parse_args()

    config = load_etl_parser_config()
    parser_name = args.parser or config.get("default_parser") or "tracerpt"
    provider_guid = config.get("provider_guid")
    artifacts = discover_etl_artifacts()
    if args.input:
        requested_ref = _resolve_requested_repo_ref(args.input)
        artifacts = [
            artifact
            for artifact in artifacts
            if str(artifact.get("path") or "") == requested_ref
            or str(artifact.get("actual_etl_path") or "") == requested_ref
        ]

    etl_paths = [REPO_ROOT / str(artifact["actual_etl_path"]) for artifact in artifacts if artifact.get("actual_etl_path")]
    results = [parse_etl_registry_touches(path, parser=parser_name, provider_guid=provider_guid) for path in etl_paths]
    inventory = build_etl_corpus_inventory(artifacts, results, parser_name=parser_name, provider_guid=provider_guid)

    discovery_candidates = []
    discarded_candidates = []
    for result in results:
        for candidate in etl_touch_candidates(result):
            errors = validate_discovery_candidate(candidate)
            if errors:
                rejected = dict(candidate)
                rejected["discard_reason"] = errors
                discarded_candidates.append(rejected)
                continue
            discovery_candidates.append(candidate)

    appended_event_count = append_discovery_candidates(discovery_candidates)
    payload = {
        "schema_version": CURRENT_SCHEMA_VERSION,
        "generated_utc": now_utc(),
        "parser": parser_name,
        "provider_guid": provider_guid,
        "execution_context": default_execution_context(),
        "etl_count": len(results),
        "inventory_summary": inventory.get("summary") or {},
        "candidate_count": len(discovery_candidates),
        "discarded_candidate_count": len(discarded_candidates),
        "appended_event_count": appended_event_count,
        "discovery_candidates": discovery_candidates,
        "discarded_candidates": discarded_candidates,
        "results": results,
    }

    inventory_output = Path(args.inventory_output) if args.inventory_output else ETL_CORPUS_INVENTORY_PATH
    discovery_output = Path(args.output) if args.output else ETL_REGISTRY_DISCOVERY_PATH
    write_json(inventory_output, inventory)
    write_json(discovery_output, payload)
    print(json.dumps({"inventory": str(inventory_output), "discovery": str(discovery_output), "candidate_count": len(discovery_candidates)}, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
