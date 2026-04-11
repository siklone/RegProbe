from __future__ import annotations

import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
TOOL_PATH = REPO_ROOT / "registry-research-framework" / "tools" / "import-external-evidence.py"


class ImportExternalEvidenceToolTests(unittest.TestCase):
    def test_cli_supports_custom_backlog_output(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            temp_root_path = Path(temp_root)
            input_path = temp_root_path / "osquery-registry.json"
            input_path.write_text(
                json.dumps(
                    [
                        {
                            "path": r"HKLM\SOFTWARE\Policies\Microsoft\Windows\Explorer",
                            "name": "HideRecommendedSection",
                            "type": "REG_DWORD",
                            "data": "1",
                        }
                    ]
                )
                + "\n",
                encoding="utf-8",
            )

            output_root = temp_root_path / "imported"
            evidence_root = temp_root_path / "evidence"
            backlog_output = temp_root_path / "research" / "imported-candidate-backlog.json"

            result = subprocess.run(
                [
                    sys.executable,
                    str(TOOL_PATH),
                    "--input",
                    str(input_path),
                    "--source-tool",
                    "osquery",
                    "--run-id",
                    "external-cli-test",
                    "--output-root",
                    str(output_root),
                    "--evidence-root",
                    str(evidence_root),
                    "--backlog-output",
                    str(backlog_output),
                ],
                cwd=str(REPO_ROOT),
                check=True,
                capture_output=True,
                text=True,
            )

            payload = json.loads(result.stdout)
            self.assertEqual(payload["bundle"]["run_id"], "external-cli-test")
            self.assertEqual(payload["imported_candidate_backlog"], backlog_output.as_posix())
            self.assertTrue(backlog_output.exists())
            self.assertTrue((output_root / "external-cli-test" / "candidate-queue.csv").exists())
            self.assertTrue((evidence_root / "external-cli-test" / "normalized-registry-bundle.json").exists())
            self.assertTrue(
                payload["outputs"]["candidate_queue"].endswith("/imported/external-cli-test/candidate-queue.csv")
            )


if __name__ == "__main__":
    unittest.main()
