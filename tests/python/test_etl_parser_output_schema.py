from __future__ import annotations

import json
import subprocess
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT = REPO_ROOT / "registry-research-framework" / "scripts" / "extract_etl_registry_events.py"
ETL_PATH = (
    REPO_ROOT
    / "evidence"
    / "raw"
    / "etw-stackwalk"
    / "power-request-override-subtree-etw-stackwalk-20260418"
    / "power-request-override-subtree-etw-stackwalk-20260418.etl"
)

REQUIRED_FIELDS = {
    "timestamp",
    "process_name",
    "pid",
    "operation",
    "key_path",
    "value_name",
    "result",
    "detail",
}


class EtlParserOutputSchemaTests(unittest.TestCase):
    def test_extractor_writes_expected_registry_event_schema(self) -> None:
        self.assertTrue(SCRIPT.exists(), SCRIPT)
        self.assertTrue(ETL_PATH.exists(), ETL_PATH)

        with tempfile.TemporaryDirectory() as temp_dir:
            output_path = Path(temp_dir) / "power-request-override-events.json"
            completed = subprocess.run(
                [
                    "python3",
                    str(SCRIPT),
                    "--input",
                    str(ETL_PATH),
                    "--output",
                    str(output_path),
                    "--filter",
                    "PowerRequestOverride",
                ],
                capture_output=True,
                text=True,
                check=False,
            )

            self.assertEqual(completed.returncode, 0, completed.stderr)
            self.assertTrue(output_path.exists(), output_path)

            payload = json.loads(output_path.read_text(encoding="utf-8"))
            self.assertIsInstance(payload, list)
            self.assertGreaterEqual(len(payload), 1)

            for event in payload:
                self.assertIsInstance(event, dict)
                self.assertTrue(REQUIRED_FIELDS.issubset(event.keys()))
                for field in REQUIRED_FIELDS:
                    self.assertIn(field, event)

                detail = event["detail"]
                self.assertIsInstance(detail, dict)
                self.assertIn("artifacts", detail)
                artifacts = detail["artifacts"]
                self.assertIsInstance(artifacts, dict)
                self.assertIn("etl", artifacts)
                self.assertTrue(
                    "xml" in artifacts or "normalized_bundle" in artifacts,
                    artifacts,
                )

                etl_meta = artifacts["etl"]
                secondary_meta = artifacts.get("xml") or artifacts.get("normalized_bundle")
                self.assertEqual(len(str(etl_meta.get("sha256") or "")), 64)
                self.assertEqual(len(str(secondary_meta.get("sha256") or "")), 64)
                self.assertIn("collected_utc", etl_meta)
                self.assertIn("collected_utc", secondary_meta)


if __name__ == "__main__":
    unittest.main()
