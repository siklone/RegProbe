from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
REPO_METRICS = REPO_ROOT / "tools" / "RepoMetrics" / "repo_metrics.py"


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


repo_metrics = load_module("repo_metrics", REPO_METRICS)


class RepoMetricsTests(unittest.TestCase):
    def test_parse_cobertura_xml_deduplicates_files(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            xml_path = Path(temp_root) / "coverage.cobertura.xml"
            xml_path.write_text(
                """
                <coverage line-rate="0.5" branch-rate="0.25">
                  <packages>
                    <package name="A">
                      <classes>
                        <class filename="app\\Foo.cs" line-rate="0.0" branch-rate="0.0">
                          <lines>
                            <line number="10" hits="0" />
                            <line number="11" hits="1" branch="true" condition-coverage="50% (1/2)" />
                          </lines>
                        </class>
                        <class filename="app\\Foo.cs" line-rate="0.0" branch-rate="0.0">
                          <lines>
                            <line number="10" hits="1" />
                          </lines>
                        </class>
                        <class filename="app\\Bar.cs" line-rate="0.0" branch-rate="0.0">
                          <lines>
                            <line number="1" hits="0" />
                          </lines>
                        </class>
                      </classes>
                    </package>
                  </packages>
                </coverage>
                """.strip(),
                encoding="utf-8",
            )

            report = repo_metrics.parse_cobertura_xml(xml_path)

            self.assertEqual(report["totals"]["line_percent"], 50.0)
            self.assertEqual(report["totals"]["branch_percent"], 25.0)
            self.assertEqual(report["lowest_files"][0]["path"], "app/Bar.cs")
            self.assertEqual(report["lowest_files"][1]["path"], "app/Foo.cs")
            self.assertEqual(report["lowest_files"][1]["line_hits"], 2)

    def test_extract_method_metrics_counts_length_and_complexity(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            repo_root = Path(temp_root)
            sample_path = repo_root / "Sample.cs"
            sample_path.write_text(
                """
                namespace Sample;

                public sealed class Example
                {
                    public int One(int value)
                    {
                        if (value > 0 && value < 10)
                        {
                            return value;
                        }

                        return 0;
                    }

                    private static string Two()
                    {
                        switch (DateTime.UtcNow.DayOfWeek)
                        {
                            case DayOfWeek.Monday:
                                return "monday";
                            default:
                                return "other";
                        }
                    }
                }
                """.strip(),
                encoding="utf-8",
            )

            metrics = repo_metrics.extract_method_metrics(sample_path, repo_root)
            payload = [
                {
                    "member": item.member,
                    "line_count": item.line_count,
                    "cyclomatic_complexity": item.cyclomatic_complexity,
                }
                for item in metrics
            ]

            self.assertEqual([item["member"] for item in payload], ["One", "Two"])
            self.assertGreaterEqual(payload[0]["cyclomatic_complexity"], 3)
            self.assertGreaterEqual(payload[1]["cyclomatic_complexity"], 2)


if __name__ == "__main__":
    unittest.main()
