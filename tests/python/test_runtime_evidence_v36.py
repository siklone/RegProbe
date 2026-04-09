from __future__ import annotations

import importlib.util
import json
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


runtime_evidence_v36_lib = load_module("runtime_evidence_v36_lib", SCRIPTS_ROOT / "runtime_evidence_v36_lib.py")
research_v36_lib = load_module("research_v36_lib", SCRIPTS_ROOT / "research_v36_lib.py")
build_research_queue = load_module("build_research_queue", FRAMEWORK_SCRIPTS / "build_research_queue.py")


class RuntimeNormalizerTests(unittest.TestCase):
    def test_discovery_mode_collapses_bursts_filters_window_and_emits_candidates(self) -> None:
        events = [
            {
                "timestamp_ms": 1000,
                "process_name": "svchost.exe",
                "process_id": 10,
                "parent_process_id": 4,
                "key_path": "HKLM\\System\\CurrentControlSet\\Control\\Power",
                "value_name": "AllowSystemRequiredPowerRequests",
                "operation": "RegQueryValue",
                "data": "1",
            },
            {
                "timestamp_ms": 1100,
                "process_name": "svchost.exe",
                "process_id": 10,
                "parent_process_id": 4,
                "key_path": "HKLM\\System\\CurrentControlSet\\Control\\Power",
                "value_name": "AllowSystemRequiredPowerRequests",
                "operation": "RegQueryValue",
                "data": "1",
            },
            {
                "timestamp_ms": 2200,
                "process_name": "services.exe",
                "process_id": 11,
                "parent_process_id": 4,
                "key_path": "HKLM\\System\\CurrentControlSet\\Control\\Power\\PowerRequestOverride",
                "value_name": "Process",
                "operation": "RegOpenKey",
            },
            {
                "timestamp_ms": 9000,
                "process_name": "ignored.exe",
                "process_id": 99,
                "parent_process_id": 4,
                "key_path": "HKLM\\Software\\Ignored",
                "value_name": "AfterWindow",
                "operation": "RegSetValue",
            },
        ]

        payload = runtime_evidence_v36_lib.normalize_runtime_registry_events(
            events,
            mode="discovery",
            trace_source="procmon",
            seed_reference="tests/runtime",
            trigger_start_ms=900,
            trigger_end_ms=3000,
            append_discovery=False,
        )

        self.assertEqual(payload["summary"]["raw_event_count"], 4)
        self.assertEqual(payload["summary"]["windowed_event_count"], 3)
        self.assertEqual(payload["summary"]["collapsed_event_count"], 2)
        self.assertEqual(payload["summary"]["operation_family_counts"]["read"], 2)
        self.assertEqual(payload["summary"]["value_extraction_confidence_counts"]["high"], 1)
        self.assertEqual(len(payload["aggregated_paths"]), 2)
        self.assertEqual(len(payload["discovery_candidates"]), 2)
        self.assertEqual(payload["discovery_candidates"][0]["required_followup"], "triage")

    def test_proof_mode_packages_runtime_evidence(self) -> None:
        events = [
            {
                "timestamp_ms": 1000,
                "process_name": "svchost.exe",
                "process_id": 10,
                "key_path": "HKLM\\System\\CurrentControlSet\\Control\\Power",
                "value_name": "AllowAudioToEnableExecutionRequiredPowerRequests",
                "operation": "RegSetValue",
                "data": "1",
            }
        ]

        payload = runtime_evidence_v36_lib.normalize_runtime_registry_events(
            events,
            mode="proof",
            trace_source="etw",
            seed_reference="tests/runtime",
            append_discovery=False,
        )

        self.assertIn("runtime_evidence", payload)
        self.assertEqual(payload["runtime_evidence"]["format"], "normalized-runtime-registry")
        self.assertEqual(payload["runtime_evidence"]["operation_family_counts"]["write"], 1)


class StructuredDiffTests(unittest.TestCase):
    def test_structured_state_diff_emits_add_delete_and_change(self) -> None:
        baseline = {
            "HKLM\\Software\\Example": {
                "RemovedOnly": 1,
                "Changed": 10,
            }
        }
        candidate = {
            "HKLM\\Software\\Example": {
                "Changed": 11,
                "AddedOnly": 2,
            }
        }

        payload = runtime_evidence_v36_lib.build_structured_state_diff_from_snapshots(baseline, candidate)

        self.assertEqual(payload["summary_counts"]["value_added"], 1)
        self.assertEqual(payload["summary_counts"]["value_deleted"], 1)
        self.assertEqual(payload["summary_counts"]["value_changed"], 1)

    def test_structured_state_diff_from_registry_files_uses_semantic_engine(self) -> None:
        before = "\n".join(
            [
                "Windows Registry Editor Version 5.00",
                "",
                "[HKEY_LOCAL_MACHINE\\Software\\Example]",
                "\"Changed\"=dword:00000001",
                "\"Deleted\"=dword:00000002",
            ]
        )
        after = "\n".join(
            [
                "Windows Registry Editor Version 5.00",
                "",
                "[HKEY_LOCAL_MACHINE\\Software\\Example]",
                "\"Changed\"=dword:00000003",
                "\"Added\"=dword:00000004",
            ]
        )
        with tempfile.TemporaryDirectory() as temp_dir:
            before_path = Path(temp_dir) / "before.reg"
            after_path = Path(temp_dir) / "after.reg"
            before_path.write_text(before + "\n", encoding="utf-8")
            after_path.write_text(after + "\n", encoding="utf-8")

            payload = runtime_evidence_v36_lib.build_structured_state_diff_from_registry_files(before_path, after_path)

        self.assertEqual(payload["summary_counts"]["value_added"], 1)
        self.assertEqual(payload["summary_counts"]["value_deleted"], 1)
        self.assertEqual(payload["summary_counts"]["value_changed"], 1)


class RollbackVerificationTests(unittest.TestCase):
    def test_verified_true_when_restore_matches_baseline(self) -> None:
        payload = runtime_evidence_v36_lib.evaluate_rollback_verification(
            baseline_state={"Example": {"Value": 0}},
            candidate_state={"Example": {"Value": 1}},
            restored_state={"Example": {"Value": 0}},
            rollback_declared=True,
            rollback_executed=True,
        )

        self.assertTrue(payload["state_changed"])
        self.assertTrue(payload["rollback_verified"])
        self.assertIsNone(payload["rollback_failure_reason"])

    def test_verified_false_when_restore_mismatches(self) -> None:
        payload = runtime_evidence_v36_lib.evaluate_rollback_verification(
            baseline_state={"Example": {"Value": 0}},
            candidate_state={"Example": {"Value": 1}},
            restored_state={"Example": {"Value": 2}},
            rollback_declared=True,
            rollback_executed=True,
        )

        self.assertFalse(payload["rollback_verified"])
        self.assertEqual(payload["rollback_failure_reason"], "rollback-state-mismatch")

    def test_executed_false_result_is_distinct(self) -> None:
        payload = runtime_evidence_v36_lib.evaluate_rollback_verification(
            baseline_state={"Example": {"Value": 0}},
            candidate_state={"Example": {"Value": 1}},
            restored_state=None,
            rollback_declared=True,
            rollback_executed=False,
        )

        self.assertFalse(payload["rollback_verified"])
        self.assertEqual(payload["rollback_failure_reason"], "rollback-not-executed")


class DiscoveryQueueAdapterTests(unittest.TestCase):
    def test_runtime_discovery_candidates_flow_into_queue_entries(self) -> None:
        candidates = [
            {
                "schema_version": research_v36_lib.CURRENT_SCHEMA_VERSION,
                "candidate_id": "runtime::one",
                "discovery_source": "procmon-registry-touch",
                "discovery_reason": "runtime_registry_touch",
                "feature_area": "Power",
                "key_path": "HKLM\\System\\CurrentControlSet\\Control\\Power",
                "value_name": "AllowSystemRequiredPowerRequests",
                "registry_clue": "read via svchost.exe",
                "initial_confidence": "medium",
                "seed_reference": "tests/runtime",
                "required_followup": "triage",
                "execution_context": research_v36_lib.default_execution_context(),
            },
            {
                "schema_version": research_v36_lib.CURRENT_SCHEMA_VERSION,
                "candidate_id": "runtime::bad",
                "discovery_source": "procmon-registry-touch",
                "discovery_reason": "runtime_registry_touch",
                "feature_area": "Power",
                "key_path": "C:\\Bad\\Path",
                "value_name": "Bad",
                "registry_clue": "invalid",
                "initial_confidence": "low",
                "seed_reference": "tests/runtime",
                "required_followup": "triage",
                "execution_context": research_v36_lib.default_execution_context(),
            },
        ]

        entries = build_research_queue.queue_entries_from_runtime_discovery(candidates)

        self.assertEqual(entries[0]["state"], "triaged")
        self.assertEqual(entries[0]["next_lane"], "scoring")
        self.assertEqual(entries[1]["state"], "discarded")
        self.assertIn("invalid:key_path", entries[1]["discard_reason"])


if __name__ == "__main__":
    unittest.main()
