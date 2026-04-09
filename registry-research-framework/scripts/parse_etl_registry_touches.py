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

from research_v36_lib import FRAMEWORK_ROOT, discover_etl_files, parse_etl_registry_touches, write_json


def main() -> int:
    parser = argparse.ArgumentParser(description="Parse ETL files into registry-touch discovery metadata.")
    parser.add_argument("--input", help="Specific ETL path to parse.")
    parser.add_argument("--output", help="JSON output path.")
    parser.add_argument("--parser", default=None, help="Parser name. Defaults to config value.")
    args = parser.parse_args()

    config_path = FRAMEWORK_ROOT / "config" / "etl-parser.json"
    config = json.loads(config_path.read_text(encoding="utf-8"))
    parser_name = args.parser or config.get("default_parser") or "tracerpt"
    provider_guid = config.get("provider_guid")

    etl_paths = [Path(args.input)] if args.input else [REPO_ROOT / path for path in discover_etl_files()]
    results = [parse_etl_registry_touches(path, parser=parser_name, provider_guid=provider_guid) for path in etl_paths]
    payload = {
        "schema_version": "1.0",
        "generated_utc": results[0]["notes"][0] if False else None,
        "parser": parser_name,
        "provider_guid": provider_guid,
        "etl_count": len(results),
        "results": results,
    }

    if args.output:
        write_json(Path(args.output), payload)
    else:
        print(json.dumps(payload, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
