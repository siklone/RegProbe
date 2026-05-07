from __future__ import annotations

import importlib.util
import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"
if str(SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_ROOT))


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


refresh_script = load_module("refresh_research_publish_surfaces", SCRIPTS_ROOT / "refresh_research_publish_surfaces.py")


class RefreshResearchPublishSurfacesTests(unittest.TestCase):
    def test_build_refresh_steps_uses_dependency_order(self) -> None:
        steps = refresh_script.build_refresh_steps(REPO_ROOT)
        self.assertEqual(
            [step["name"] for step in steps],
            [
                "app-surface-manifest",
                "evidence-audit",
                "promotion-gates",
                "imported-candidate-backlog",
                "evidence-classes",
                "evidence-index",
                "evidence-manifest",
                "evidence-atlas",
            ],
        )
        self.assertEqual(
            [step["script"] for step in steps],
            [
                "scripts/research/generate_app_surface_manifest.py",
                "scripts/generate_evidence_audit.py",
                "scripts/generate_promotion_gates.py",
                "scripts/generate_imported_candidate_backlog.py",
                "scripts/generate_evidence_classes.py",
                "scripts/generate_evidence_index.py",
                "scripts/generate_evidence_manifest.py",
                "scripts/generate_evidence_atlas.py",
            ],
        )


if __name__ == "__main__":
    unittest.main()
