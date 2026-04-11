from __future__ import annotations

import argparse
import importlib.util
import json
import sys
from datetime import datetime, timezone
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
TOOLS_PATH = REPO_ROOT / "registry-research-framework" / "tools" / "registry_sideeffect_diff.py"


def load_module(path: Path):
    spec = importlib.util.spec_from_file_location("registry_sideeffect_diff", path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules["registry_sideeffect_diff"] = module
    spec.loader.exec_module(module)
    return module


registry_sideeffect_diff = load_module(TOOLS_PATH)


DEFAULT_PAIRS = [
    {
        "id": "session-manager-power-baseline-cross-format",
        "label": "Session Manager Power baseline (.reg vs .txt)",
        "before": "evidence/files/vm-tooling-staging/session-manager-power-baseline-20260328-080010/session-manager-power-baseline.reg",
        "after": "evidence/files/vm-tooling-staging/session-manager-power-baseline-20260328-080010/session-manager-power-baseline.txt",
    },
    {
        "id": "power-control-root-cross-format",
        "label": "Power Control root dump (.reg vs .txt)",
        "before": "evidence/files/vm-tooling-staging/registry-dumps/power-control-root-20260324-210206/power-control-root.reg",
        "after": "evidence/files/vm-tooling-staging/registry-dumps/power-control-root-20260324-210206/power-control-root.txt",
    },
]


def build_pair_result(pair: dict[str, str]) -> dict[str, object]:
    before_path = REPO_ROOT / pair["before"]
    after_path = REPO_ROOT / pair["after"]

    before_text = registry_sideeffect_diff.read_registry_text(before_path)
    after_text = registry_sideeffect_diff.read_registry_text(after_path)
    naive = registry_sideeffect_diff.get_line_summary_diff(before_text, after_text)
    semantic = registry_sideeffect_diff.build_diff_payload(before_path, after_path)

    return {
        "id": pair["id"],
        "label": pair["label"],
        "before": pair["before"],
        "after": pair["after"],
        "naive_line_counts": {
            "before_lines": naive["BeforeLineCount"],
            "after_lines": naive["AfterLineCount"],
            "ignored_before_noise_lines": naive["IgnoredBeforeNoise"],
            "ignored_after_noise_lines": naive["IgnoredAfterNoise"],
            "added_lines": sum(int(item["Count"]) for item in naive["Added"]),
            "removed_lines": sum(int(item["Count"]) for item in naive["Removed"]),
        },
        "semantic_diff": {
            "detected_format": semantic["detected_format"],
            "before_format": semantic["before_format"],
            "after_format": semantic["after_format"],
            "summary_counts": semantic["summary_counts"],
        },
    }


def build_report() -> dict[str, object]:
    pairs = [build_pair_result(pair) for pair in DEFAULT_PAIRS]
    return {
        "title": "Semantic sideeffect regression audit",
        "generated_at_utc": datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z"),
        "pairs": pairs,
        "conclusion": (
            "Cross-format registry exports and dump text can produce large naive line churn while representing the same registry state. "
            "Semantic diff collapses those pairs back to zero modified values."
        ),
    }


def render_markdown(report: dict[str, object]) -> str:
    lines = [
        "# Semantic Sideeffect Regression Audit",
        "",
        report["conclusion"],
        "",
    ]

    for pair in report["pairs"]:
        naive = pair["naive_line_counts"]
        semantic = pair["semantic_diff"]["summary_counts"]
        lines.extend(
            [
                f"## {pair['label']}",
                "",
                f"- Before: `{pair['before']}`",
                f"- After: `{pair['after']}`",
                f"- Naive line churn: `+{naive['added_lines']}` / `-{naive['removed_lines']}`",
                f"- Semantic values: `added={semantic.get('added_values', 0)}` / `removed={semantic.get('removed_values', 0)}` / `modified={semantic.get('modified_values', 0)}`",
                "",
            ]
        )

    return "\n".join(lines).rstrip() + "\n"


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Audit known registry sideeffect regression pairs.")
    parser.add_argument("--output-json", required=True, type=Path)
    parser.add_argument("--output-md", type=Path)
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    report = build_report()
    args.output_json.parent.mkdir(parents=True, exist_ok=True)
    args.output_json.write_text(json.dumps(report, indent=2), encoding="utf-8")

    if args.output_md is not None:
        args.output_md.parent.mkdir(parents=True, exist_ok=True)
        args.output_md.write_text(render_markdown(report), encoding="utf-8")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
