import importlib.util
import sys
import unittest
from pathlib import Path


SCRIPT_PATH = (
    Path(__file__).resolve().parents[2]
    / "registry-research-framework"
    / "scripts"
    / "generate_operator96_enriched_value_matrix.py"
)


def load_module():
    spec = importlib.util.spec_from_file_location("generate_operator96_enriched_value_matrix_for_tests", SCRIPT_PATH)
    assert spec and spec.loader
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def row(**overrides):
    payload = {
        "index": 1,
        "registry_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
        "value_name": "EnableThing",
        "requested_data": "1",
        "default_kind": "observed-absent",
        "default_value": None,
        "vm_status": "value-missing",
        "record_class": "value",
        "source_quality": "vm-observed",
    }
    payload.update(overrides)
    return payload


class Operator96EnrichedValueMatrixTests(unittest.TestCase):
    def setUp(self):
        self.module = load_module()

    def values_for(self, candidates):
        return [candidate["value"] for candidate in candidates]

    def test_boolean_enable_disable_names_get_zero_one(self):
        candidates = self.module.build_candidates(row(value_name="DisableWidget", requested_data="0"))

        self.assertEqual(self.values_for(candidates), [0, 1])
        self.assertTrue(any(source["source_label"] == "boolean-toggle" for source in candidates[0]["sources"]))

    def test_percent_names_get_percent_range(self):
        candidates = self.module.build_candidates(row(value_name="HiberFileSizePercent", requested_data="0"))

        self.assertEqual(self.values_for(candidates), [0, 1, 50, 100])

    def test_timeout_names_include_requested_default_and_boundaries(self):
        candidates = self.module.build_candidates(
            row(
                value_name="PowerWatchdogRequestQueueTimeoutMsec",
                requested_data="30000",
                default_kind="observed-present",
                default_value=5000,
            )
        )

        self.assertEqual(self.values_for(candidates), [5000, 30000, 0, 1])

    def test_duplicate_values_merge_sources_without_duplicate_candidates(self):
        candidates = self.module.build_candidates(
            row(value_name="EnableThing", requested_data="1"),
            {
                1: {
                    "experiment_id": "operator96-001-enablething-1",
                    "status": "ok",
                    "verdict": "low_confidence",
                    "confidence": "low",
                    "restore_action": "removed-created-value",
                }
            },
        )
        value_one = [candidate for candidate in candidates if candidate["value"] == 1]

        self.assertEqual(len(value_one), 1)
        self.assertTrue(value_one[0]["already_tested"])
        self.assertTrue(value_one[0]["vm_validated"])
        self.assertGreaterEqual(len(value_one[0]["sources"]), 2)

    def test_community_hint_is_tagged_and_never_counts_as_proof(self):
        candidates = self.module.build_candidates(
            row(value_name="SystemResponsiveness", requested_data="10"),
            {},
            [
                {
                    "value_name": "SystemResponsiveness",
                    "value": "30000",
                    "source_kind": "community-hint",
                    "source_label": "forum-post",
                }
            ],
        )
        community = [candidate for candidate in candidates if candidate["value"] == 30000][0]

        self.assertTrue(community["community_only"])
        self.assertTrue(community["requires_vm_validation"])
        self.assertFalse(community["vm_validated"])

    def test_source_backed_extra_values_are_added_only_from_hints(self):
        without_hint = self.module.build_candidates(row(value_name="CustomThreshold", requested_data="5"))
        with_hint = self.module.build_candidates(
            row(value_name="CustomThreshold", requested_data="5"),
            {},
            [{"value_name": "CustomThreshold", "value": "64", "source_kind": "source-backed", "source_label": "static-xref"}],
        )

        self.assertNotIn(64, self.values_for(without_hint))
        self.assertIn(64, self.values_for(with_hint))

    def test_app_gate_requires_default_and_rollback_proof(self):
        test_row = row(value_name="EnableThing", default_kind="observed-present", default_value=0)
        tested = {
            1: {
                "experiment_id": "operator96-001-enablething-1",
                "status": "ok",
                "verdict": "low_confidence",
                "confidence": "low",
                "restore_action": "restored-original-value",
            }
        }
        gate = self.module.app_surface_gate(test_row, self.module.build_candidates(test_row, tested), tested)

        self.assertTrue(gate["eligible_for_app_card"])
        self.assertEqual(gate["blockers"], [])

    def test_key_missing_no_authoritative_evidence_is_classified(self):
        review = self.module.evidence_review(
            row(
                record_class="key-missing",
                source_quality="no-authoritative-evidence-for-25h2",
            )
        )

        self.assertEqual(review["outcome"], "no-evidence-found-on-win11-25h2")

    def test_security_mitigation_overrides_are_not_app_card_eligible(self):
        test_row = row(value_name="DisableExceptionChainValidation", default_kind="observed-present", default_value=0)
        tested = {
            1: {
                "experiment_id": "operator96-021-disableexceptionchainvalidation-1",
                "status": "ok",
                "verdict": "low_confidence",
                "confidence": "low",
                "restore_action": "restored-original-value",
            }
        }
        gate = self.module.app_surface_gate(test_row, self.module.build_candidates(test_row, tested), tested)

        self.assertFalse(gate["eligible_for_app_card"])
        self.assertIn("security-mitigation-override", gate["blockers"])


if __name__ == "__main__":
    unittest.main()
