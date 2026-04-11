from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"
FRAMEWORK_SCRIPTS = REPO_ROOT / "registry-research-framework" / "scripts"
if str(SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_ROOT))


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


research_v36_lib = load_module("research_v36_lib", SCRIPTS_ROOT / "research_v36_lib.py")
generate_evidence_audit = load_module("generate_evidence_audit", SCRIPTS_ROOT / "generate_evidence_audit.py")
build_research_queue = load_module("build_research_queue", FRAMEWORK_SCRIPTS / "build_research_queue.py")


class EnrichmentCacheTests(unittest.TestCase):
    def test_enrichment_cache_roundtrip(self) -> None:
        record = {
            "record_id": "example.enrichment",
            "tweak_id": "example.enrichment",
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:10-12",
                "notes": "Documents the API semantics.",
            },
            "evidence": [
                {
                    "evidence_id": "ghidra-proof",
                    "kind": "ghidra-headless",
                    "title": "Ghidra proof",
                    "location": "evidence/files/ghidra/example",
                    "supports": ["caller_chain", "path"],
                    "summary": "Recovered the caller chain for the registry write.",
                    "strength": "high",
                }
            ],
            "static_analysis": {
                "ghidra": {
                    "executed": True,
                    "function_name": "ExampleWrapper",
                    "function_confidence": "symbolized_branch",
                    "effect_summary": "Recovered API semantics and a bounded caller chain.",
                    "branch_analysis": [
                        {
                            "condition": "Example branch",
                            "compare_condition": "cmp eax, 1",
                            "jump_condition": "jnz target",
                            "effect_summary": "caller chain survived bounded review",
                        }
                    ],
                }
            },
        }

        entries = research_v36_lib.build_enrichment_cache_entries(record)
        self.assertTrue(entries)
        self.assertIn(entries[0]["clue_type"], research_v36_lib.ENRICHMENT_CLUE_TYPES)

        with tempfile.TemporaryDirectory() as temp_dir:
            cache_path = Path(temp_dir) / "enrichment-cache.jsonl"
            research_v36_lib.write_enrichment_cache(entries, cache_path)
            loaded = research_v36_lib.load_enrichment_cache(cache_path)

        self.assertEqual(len(loaded), len(entries))
        self.assertEqual(loaded[0]["record_id"], "example.enrichment")


class EvidenceAuditSurfaceTests(unittest.TestCase):
    def test_audit_surface_projection_exposes_freshness_and_rollback(self) -> None:
        promotion_gate = {
            "freshness_status": {
                "stale_reason": "build-drift-threshold",
                "revalidation_needed": True,
                "last_known_good_build": "26097",
                "last_known_good_verification_context": {
                    "backend_id": "rai-linux-vm",
                    "tested_build": "26097",
                },
            },
            "rollback_status": {
                "rollback_verified": False,
                "rollback_failure_reason": "rollback-state-mismatch",
            },
            "bench_status": {
                "executed": False,
            },
            "negative_evidence_status": {
                "signals": ["functional-no-effect"],
                "conflict_reason": "state-change-expected-but-diff-empty",
            },
            "verification_context": {
                "tested_build": "26097",
            },
        }
        full_evidence = {
            "build_sku_awareness": {
                "os_build": "26100",
                "os_edition": "unknown",
                "architecture": "unknown",
                "elevation_context": "elevated",
                "machine_user_scope": "machine",
            }
        }

        payload = generate_evidence_audit.audit_surface_from_gate({"setting": {"targets": [{"path": "HKLM\\Software\\Example"}]}}, promotion_gate, full_evidence)

        self.assertEqual(payload["stale_reason"], "build-drift-threshold")
        self.assertEqual(payload["revalidation_need"], "required")
        self.assertEqual(payload["rollback_verification_status"], "failed")
        self.assertEqual(payload["conflict_reason"], "state-change-expected-but-diff-empty")
        self.assertEqual(payload["os_build"], "26100")


class SiblingQueueAdapterTests(unittest.TestCase):
    def test_sibling_candidates_flow_into_queue(self) -> None:
        records = [
            {
                "record_id": "example.promoted",
                "tweak_id": "example.promoted",
                "record_status": "validated",
                "setting": {
                    "area": "Example",
                    "targets": [
                        {
                            "path": "HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer",
                            "value_name": "Enabled",
                            "value_type": "REG_DWORD",
                        }
                    ],
                },
                "decision": {
                    "apply_allowed": True,
                    "confidence": "high",
                    "restore_default_supported": True,
                },
                "app_current_implementation": {"status": "matches-research"},
                "validation_proof": {"exact_quote_or_path": "Docs/example.md:1"},
            }
        ]
        gate_map = {
            "example.promoted": {
                "promotion_state": "promoted",
            }
        }

        entries = build_research_queue.queue_entries_from_sibling_discovery(records, {}, gate_map)

        self.assertTrue(entries)
        self.assertEqual(entries[0]["discovery_source"], "sibling_expansion")
        self.assertEqual(entries[0]["state"], "triaged")


if __name__ == "__main__":
    unittest.main()
