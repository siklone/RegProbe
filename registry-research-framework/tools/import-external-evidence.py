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

from external_evidence_import_lib import (  # noqa: E402
    import_external_evidence,
    materialize_external_research_artifacts,
    write_imported_candidate_backlog,
)


def main() -> int:
    parser = argparse.ArgumentParser(description="Import external registry evidence into candidate/backlog artifacts.")
    parser.add_argument("--input", required=True, help="Path to external evidence export.")
    parser.add_argument("--source-tool", help="Force a specific importer source tool.")
    parser.add_argument("--run-id", help="Override generated run id.")
    parser.add_argument(
        "--output-root",
        default=str(REPO_ROOT / "registry-research-framework" / "imported"),
        help="Output root for candidate queue, note stubs, and record seeds.",
    )
    parser.add_argument(
        "--evidence-root",
        default=str(REPO_ROOT / "evidence" / "files" / "external-imports"),
        help="Output root for canonical external/imported evidence bundles.",
    )
    parser.add_argument(
        "--backlog-output",
        default=str(REPO_ROOT / "research" / "imported-candidate-backlog.json"),
        help="Canonical output path for the aggregated imported candidate backlog.",
    )
    args = parser.parse_args()

    input_path = Path(args.input).resolve()
    output_root = Path(args.output_root).resolve()
    evidence_root = Path(args.evidence_root).resolve()
    backlog_output = Path(args.backlog_output).resolve()
    bundle = import_external_evidence(input_path, source_tool=args.source_tool, run_id=args.run_id)
    run_output_root = output_root / bundle["run_id"]
    run_evidence_root = evidence_root / bundle["run_id"]
    outputs = materialize_external_research_artifacts(bundle, run_output_root, bundle_root=run_evidence_root)
    backlog_path = write_imported_candidate_backlog(
        output_root,
        backlog_output,
    )
    payload = {
        "bundle": bundle,
        "outputs": outputs,
        "imported_candidate_backlog": backlog_path.as_posix(),
    }
    print(json.dumps(payload, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
