#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]


def build_refresh_steps(repo_root: Path = REPO_ROOT) -> list[dict[str, object]]:
    scripts_root = repo_root / "scripts"
    framework_scripts_root = repo_root / "registry-research-framework" / "scripts"
    return [
        {
            "name": "app-surface-manifest",
            "script": str((scripts_root / "research" / "generate_app_surface_manifest.py").relative_to(repo_root)).replace("\\", "/"),
            "command": [sys.executable, str(scripts_root / "research" / "generate_app_surface_manifest.py"), "--write"],
        },
        {
            "name": "evidence-audit",
            "script": str((scripts_root / "generate_evidence_audit.py").relative_to(repo_root)).replace("\\", "/"),
            "command": [sys.executable, str(scripts_root / "generate_evidence_audit.py")],
        },
        {
            "name": "promotion-gates",
            "script": str((scripts_root / "generate_promotion_gates.py").relative_to(repo_root)).replace("\\", "/"),
            "command": [sys.executable, str(scripts_root / "generate_promotion_gates.py")],
        },
        {
            "name": "rejected-closure-ledger",
            "script": str((framework_scripts_root / "generate_rejected_closure_ledger.py").relative_to(repo_root)).replace("\\", "/"),
            "command": [sys.executable, str(framework_scripts_root / "generate_rejected_closure_ledger.py")],
        },
        {
            "name": "promotion-eligible-review-pack",
            "script": str((framework_scripts_root / "generate_promotion_eligible_review_pack.py").relative_to(repo_root)).replace("\\", "/"),
            "command": [sys.executable, str(framework_scripts_root / "generate_promotion_eligible_review_pack.py")],
        },
        {
            "name": "imported-candidate-backlog",
            "script": str((scripts_root / "generate_imported_candidate_backlog.py").relative_to(repo_root)).replace("\\", "/"),
            "command": [sys.executable, str(scripts_root / "generate_imported_candidate_backlog.py")],
        },
        {
            "name": "evidence-classes",
            "script": str((scripts_root / "generate_evidence_classes.py").relative_to(repo_root)).replace("\\", "/"),
            "command": [sys.executable, str(scripts_root / "generate_evidence_classes.py")],
        },
        {
            "name": "evidence-index",
            "script": str((scripts_root / "generate_evidence_index.py").relative_to(repo_root)).replace("\\", "/"),
            "command": [sys.executable, str(scripts_root / "generate_evidence_index.py")],
        },
        {
            "name": "evidence-manifest",
            "script": str((scripts_root / "generate_evidence_manifest.py").relative_to(repo_root)).replace("\\", "/"),
            "command": [sys.executable, str(scripts_root / "generate_evidence_manifest.py")],
        },
        {
            "name": "evidence-atlas",
            "script": str((scripts_root / "generate_evidence_atlas.py").relative_to(repo_root)).replace("\\", "/"),
            "command": [sys.executable, str(scripts_root / "generate_evidence_atlas.py")],
        },
    ]


def main() -> int:
    parser = argparse.ArgumentParser(description="Refresh canonical published research surfaces in dependency order.")
    parser.add_argument("--dry-run", action="store_true", help="Print the planned generator order without executing it.")
    args = parser.parse_args()

    steps = build_refresh_steps()
    if args.dry_run:
        print(
            json.dumps(
                {
                    "repo_root": REPO_ROOT.as_posix(),
                    "steps": [
                        {
                            "name": str(step["name"]),
                            "script": str(step["script"]),
                        }
                        for step in steps
                    ],
                },
                ensure_ascii=False,
                indent=2,
            )
        )
        return 0

    executed: list[dict[str, str]] = []
    for step in steps:
        subprocess.run(step["command"], cwd=str(REPO_ROOT), check=True)
        executed.append({"name": str(step["name"]), "script": str(step["script"])})

    print(
        json.dumps(
            {
                "repo_root": REPO_ROOT.as_posix(),
                "executed": executed,
            },
            ensure_ascii=False,
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
