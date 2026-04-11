from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"
if str(SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_ROOT))


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    spec.loader.exec_module(module)
    return module


source_enrichment_scan = load_module("source_enrichment_scan", SCRIPTS_ROOT / "source_enrichment_scan.py")


class SourceEnrichmentTests(unittest.TestCase):
    def test_expand_root_supports_windows_style_env_vars(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            windir = Path(temp_root) / "WindowsRoot"
            with patch.dict(source_enrichment_scan.os.environ, {"WINDIR": str(windir)}, clear=False):
                expanded = source_enrichment_scan.expand_root(r"%WINDIR%\PolicyDefinitions")

            self.assertEqual(expanded, windir / "PolicyDefinitions")

    def test_expand_root_supports_case_insensitive_windows_env_names(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            program_files = Path(temp_root) / "Program Files (x86)"
            with patch.dict(source_enrichment_scan.os.environ, {"programfiles(x86)": str(program_files)}, clear=False):
                expanded = source_enrichment_scan.expand_root(r"%ProgramFiles(x86)%\Windows Kits\10\Include")

            self.assertEqual(expanded, program_files / "Windows Kits" / "10" / "Include")

    def test_expand_root_prefers_source_specific_override(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            windir = Path(temp_root) / "WindowsRoot"
            override = Path(temp_root) / "override" / "admx"
            with patch.dict(
                source_enrichment_scan.os.environ,
                {
                    "WINDIR": str(windir),
                    "REGPROBE_SOURCE_ROOT_ADMX": str(override),
                },
                clear=False,
            ):
                expanded = source_enrichment_scan.expand_root(r"%WINDIR%\PolicyDefinitions", source_id="admx")

            self.assertEqual(expanded, override)

    def test_score_candidate_tracks_weighted_support_and_trigger_family(self) -> None:
        candidate = {
            "candidate_id": "power.control.allow-system-required-power-requests",
            "family": "power-control",
            "suspected_layer": "kernel",
            "boot_phase_relevant": True,
            "registry_path": r"HKLM\SYSTEM\CurrentControlSet\Control\Power",
            "value_name": "AllowSystemRequiredPowerRequests",
            "route_bucket": "docs-first-new-candidate",
        }
        source_results = [
            {
                "id": "admx",
                "label": "ADMX",
                "surface_group": "policy-templates",
                "kind": "local-source",
                "weight": 2,
                "hits_by_candidate": {
                    candidate["candidate_id"]: [
                        {
                            "file": "PolicyDefinitions/power.admx",
                            "line_number": 17,
                            "value_name": candidate["value_name"],
                            "content": candidate["value_name"],
                        }
                    ]
                },
                "root": "C:/Windows/PolicyDefinitions",
                "missing_reason": None,
            }
        ]

        scored = source_enrichment_scan.score_candidate(candidate, source_results)

        self.assertEqual(scored["support_count"], 1)
        self.assertEqual(scored["enrichment_score"], 2)
        self.assertEqual(scored["trigger_family"], "power-request-simulation")
        self.assertEqual(scored["suggested_queue_bucket"], "runtime")
        self.assertIn("PowerCreateRequest(SystemRequired)", scored["suggested_trigger"])

    def test_build_priority_queue_orders_by_score_and_bucket(self) -> None:
        candidates = [
            {
                "candidate_id": "alpha",
                "enrichment_score": 1,
                "support_count": 1,
                "suggested_runtime_priority": "medium",
                "suggested_queue_bucket": "runtime",
            },
            {
                "candidate_id": "beta",
                "enrichment_score": 4,
                "support_count": 2,
                "suggested_runtime_priority": "high",
                "suggested_queue_bucket": "runtime",
            },
            {
                "candidate_id": "gamma",
                "enrichment_score": 3,
                "support_count": 1,
                "suggested_runtime_priority": "low",
                "suggested_queue_bucket": "windbg",
            },
        ]

        queue = source_enrichment_scan.build_priority_queue(candidates)

        self.assertEqual(queue["high_priority_runtime"], ["beta", "alpha"])
        self.assertEqual(queue["high_priority_windbg"], ["gamma"])

    def test_scan_source_marks_missing_roots_honestly(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            missing_source = {
                "id": "ghost",
                "label": "Ghost",
                "surface_group": "reference-cache",
                "kind": "reference-cache",
                "root": str(Path(temp_root) / "does-not-exist"),
                "patterns": ["*.txt"],
                "enrichment_weight": 1,
            }
            result = source_enrichment_scan.scan_source(missing_source, [])

            self.assertFalse(result["exists"])
            self.assertEqual(result["missing_reason"], "root-missing")

    def test_scan_source_requires_registry_context_for_generic_value_names(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            root = Path(temp_root)
            (root / "AutoPlay.admx").write_text(
                '<policy name="Autorun" valueName="Policy" key="Software\\\\Microsoft\\\\Windows\\\\CurrentVersion\\\\Policies\\\\Explorer">',
                encoding="utf-8",
            )
            (root / "Distractor.admx").write_text(
                '<policy name="OtherPowerPolicy" valueName="Policy" key="System\\\\CurrentControlSet\\\\Control\\\\Power\\\\OtherSetting">',
                encoding="utf-8",
            )
            (root / "Power.admx").write_text(
                '<policy name="ForceHibernateDisabled" valueName="Policy" key="System\\\\CurrentControlSet\\\\Control\\\\Power\\\\ForceHibernateDisabled">',
                encoding="utf-8",
            )

            source = {
                "id": "admx",
                "label": "Windows ADMX",
                "surface_group": "policy-templates",
                "kind": "local-source",
                "root": str(root),
                "patterns": ["*.admx"],
                "enrichment_weight": 2,
            }
            candidates = [
                {
                    "candidate_id": "power.force-hibernate-disabled.policy",
                    "family": "power-force-hibernate-disabled",
                    "suspected_layer": "kernel",
                    "boot_phase_relevant": True,
                    "registry_path": r"HKLM\SYSTEM\CurrentControlSet\Control\Power\ForceHibernateDisabled",
                    "value_name": "Policy",
                    "route_bucket": "docs-first-new-candidate",
                }
            ]

            result = source_enrichment_scan.scan_source(source, candidates)
            hits = result["hits_by_candidate"]["power.force-hibernate-disabled.policy"]

            self.assertEqual(len(hits), 1)
            self.assertTrue(hits[0]["file"].endswith("Power.admx"))

    def test_scan_source_keeps_exact_hits_for_non_generic_value_names(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            root = Path(temp_root)
            (root / "Power.admx").write_text(
                '<policy name="PowerThrottlingTurnOff" valueName="PowerThrottlingOff" key="System\\\\CurrentControlSet\\\\Control\\\\Power\\\\PowerThrottling">',
                encoding="utf-8",
            )

            source = {
                "id": "admx",
                "label": "Windows ADMX",
                "surface_group": "policy-templates",
                "kind": "local-source",
                "root": str(root),
                "patterns": ["*.admx"],
                "enrichment_weight": 2,
            }
            candidates = [
                {
                    "candidate_id": "power.throttling.power-throttling-off",
                    "family": "power-throttling",
                    "suspected_layer": "kernel",
                    "boot_phase_relevant": True,
                    "registry_path": r"HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling",
                    "value_name": "PowerThrottlingOff",
                    "route_bucket": "existing-covered",
                }
            ]

            result = source_enrichment_scan.scan_source(source, candidates)

            self.assertEqual(result["candidate_hit_count"], 1)
            self.assertEqual(result["hit_count"], 1)

    def test_scan_source_does_not_match_longer_prefix_values(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            root = Path(temp_root)
            (root / "DeviceGuard.admx").write_text(
                '<policy name="VirtualizationBasedSecurity" valueName="EnableVirtualizationBasedSecurity" key="Software\\\\Microsoft\\\\Windows\\\\CurrentVersion\\\\Policies\\\\System">',
                encoding="utf-8",
            )

            source = {
                "id": "admx",
                "label": "Windows ADMX",
                "surface_group": "policy-templates",
                "kind": "local-source",
                "root": str(root),
                "patterns": ["*.admx"],
                "enrichment_weight": 2,
            }
            candidates = [
                {
                    "candidate_id": "policy.system.enable-virtualization",
                    "family": "policy-system",
                    "suspected_layer": "policy",
                    "boot_phase_relevant": False,
                    "registry_path": r"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM",
                    "value_name": "EnableVirtualization",
                    "route_bucket": "research-only",
                }
            ]

            result = source_enrichment_scan.scan_source(source, candidates)

            self.assertEqual(result["candidate_hit_count"], 0)
            self.assertEqual(result["hit_count"], 0)


if __name__ == "__main__":
    unittest.main()
