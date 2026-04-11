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
    CONTRACTS_ROOT,
    CURRENT_SCHEMA_VERSION,
    default_execution_context,
    load_backend_capabilities,
    validate_backend_capabilities,
    validate_canonical_bundle,
    validate_discovery_candidate,
    validate_execution_context,
    validate_gate_result,
    validate_queue_entry,
)


def load_json(path: Path):
    with path.open("r", encoding="utf-8-sig") as handle:
        return json.load(handle)


def validate_contract_examples() -> dict[str, list[str]]:
    backend = load_backend_capabilities()
    execution_context = default_execution_context()
    discovery_candidate = {
        "schema_version": CURRENT_SCHEMA_VERSION,
        "candidate_id": "example.discovery",
        "discovery_source": "imported_record",
        "discovery_reason": "existing_research",
        "feature_area": "Example",
        "key_path": "HKLM\\Software\\Example",
        "value_name": "Enabled",
        "registry_clue": "Example clue",
        "initial_confidence": "medium",
        "seed_reference": "research/records/example.json",
        "required_followup": "triage",
        "execution_context": execution_context,
    }
    queue_entry = {
        **discovery_candidate,
        "state": "triaged",
        "blockers": [],
        "required_capabilities": ["registry_read"],
        "next_lane": "runtime",
        "updated_utc": "2026-04-09T00:00:00Z",
    }
    gate_result = {
        "schema_version": CURRENT_SCHEMA_VERSION,
        "evaluator_version": "3.6.0",
        "supported_schema_versions": [CURRENT_SCHEMA_VERSION],
        "schema_compatibility_mode": "native",
        "candidate_id": "example.discovery",
        "record_id": "example.discovery",
        "tweak_id": "example.discovery",
        "tweak_origin": "research-derived",
        "promotion_state": "blocked",
        "promotion_blockers": ["runtime-trace"],
        "record_promotion_allowed": False,
        "tweak_ingest_allowed": False,
        "apply_allowed": False,
        "app_mapping_status": "not-mapped",
        "next_missing_layer": "runtime-trace",
        "debug_override_allowed": True,
        "last_evaluated_at": "2026-04-09T00:00:00Z",
    }
    canonical_bundle = {
        "schema_version": CURRENT_SCHEMA_VERSION,
        "candidate_id": "example.discovery",
        "feature_area": "Example",
        "discovery_source": "imported_record",
        "discovery_reason": "existing_research",
        "key_path": "HKLM\\Software\\Example",
        "value_name": "Enabled",
        "value_type": "REG_DWORD",
        "observed_default": {"value": 0},
        "recommended_value": {"value": 1},
        "rollback_value": {"rollback_strategy": "restore_default"},
        "execution_context": execution_context,
        "promotion_state": "blocked",
        "promotion_blockers": ["runtime-trace"],
        "record_promotion_allowed": False,
        "tweak_ingest_allowed": False,
        "tweak_origin": "research-derived",
        "gate_result": gate_result,
        "evidence_freshness": {"os_build": "26100"},
        "last_verified_at": "2026-04-09T00:00:00Z",
        "verification_environment": {"backend_id": "rai-linux-vm"},
        "negative_evidence": {},
        "before_after": {},
        "source_enrichment": [],
        "bench_results": {},
        "documentation_status": {},
        "evidence_status": {},
        "rollback_status": {},
        "verification_context": {},
    }
    return {
        "execution_context": validate_execution_context(execution_context),
        "backend_capabilities": validate_backend_capabilities(backend),
        "discovery_candidate": validate_discovery_candidate(discovery_candidate),
        "research_queue_entry": validate_queue_entry(queue_entry),
        "gate_result": validate_gate_result(gate_result),
        "canonical_bundle": validate_canonical_bundle(canonical_bundle),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate RegProbe 3.6 contract examples and schemas.")
    parser.add_argument("--emit-json", action="store_true", help="Emit JSON instead of text.")
    args = parser.parse_args()

    schemas = sorted(path.name for path in CONTRACTS_ROOT.glob("*.schema.json"))
    results = validate_contract_examples()
    payload = {
        "schema_version": CURRENT_SCHEMA_VERSION,
        "schemas": schemas,
        "results": results,
        "ok": all(not errors for errors in results.values()),
    }

    if args.emit_json:
        print(json.dumps(payload, ensure_ascii=False, indent=2))
    else:
        print(f"Schemas: {', '.join(schemas)}")
        for name, errors in results.items():
            print(f"{name}: {'ok' if not errors else ', '.join(errors)}")
    return 0 if payload["ok"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
