#!/usr/bin/env python3
from __future__ import annotations

import csv
import json
import re
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
CATALOG_CSV = REPO_ROOT / "Docs" / "tweaks" / "tweak-catalog.csv"

DESCRIPTION_RULES: tuple[tuple[re.Pattern[str], str], ...] = (
    (re.compile(r"\buse this to fix\b", re.IGNORECASE), "Description uses a prescriptive fix claim."),
    (re.compile(r"\buseful for\b", re.IGNORECASE), "Description uses a generic usefulness claim."),
    (re.compile(r"\bonly recommended\b", re.IGNORECASE), "Description uses a recommendation claim."),
    (re.compile(r"\brecommended for\b", re.IGNORECASE), "Description uses a scenario recommendation claim."),
    (re.compile(r"\bpreferred by\b", re.IGNORECASE), "Description uses a preference claim."),
    (re.compile(r"\bprovides cleaner\b", re.IGNORECASE), "Description claims a subjective output improvement."),
    (re.compile(r"\bensuring fresh\b", re.IGNORECASE), "Description claims a freshness outcome."),
    (re.compile(r"\bimproves?\b", re.IGNORECASE), "Description claims an improvement outcome."),
    (re.compile(r"\bimproving\b", re.IGNORECASE), "Description claims an improvement outcome."),
    (re.compile(r"\bresponsiveness\b", re.IGNORECASE), "Description claims responsiveness improvement."),
    (re.compile(r"\bminimum latency\b", re.IGNORECASE), "Description claims a latency outcome."),
    (re.compile(r"\breduces latency\b", re.IGNORECASE), "Description claims a latency outcome."),
    (re.compile(r"\bmore aggressive\b", re.IGNORECASE), "Description uses comparative posture language."),
    (re.compile(r"\bbetter code completion performance\b", re.IGNORECASE), "Description claims a performance outcome."),
)

NAME_RULES: tuple[tuple[re.Pattern[str], str], ...] = (
    (re.compile(r"\boptimize\b", re.IGNORECASE), "Name uses optimize-style outcome language."),
    (re.compile(r"\bspeed up\b", re.IGNORECASE), "Name uses speed-up outcome language."),
)

TEMPLATE_RULES: tuple[tuple[re.Pattern[str], str], ...] = (
    (re.compile(r"\{[^}]+\}"), "Catalog contains an unresolved template placeholder."),
)


def build_tweak_catalog_truth_report(repo_root: Path | None = None) -> dict[str, object]:
    root = repo_root or REPO_ROOT
    catalog_csv = root / "Docs" / "tweaks" / "tweak-catalog.csv"
    report: dict[str, object] = {
        "catalog_csv": str(catalog_csv.relative_to(root)) if catalog_csv.exists() else str(catalog_csv),
        "check_status": "PASS",
        "errors": [],
        "name_violations": [],
        "description_violations": [],
        "template_violations": [],
        "entry_count": 0,
    }

    if not catalog_csv.exists():
        report["check_status"] = "FAIL"
        report["errors"] = ["Docs/tweaks/tweak-catalog.csv is missing."]
        return report

    description_violations: list[dict[str, str]] = []
    name_violations: list[dict[str, str]] = []
    template_violations: list[dict[str, str]] = []

    with catalog_csv.open("r", encoding="utf-8", newline="") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            tweak_id = (row.get("id") or "").strip()
            name = (row.get("name") or "").strip()
            description = (row.get("description") or "").strip()
            if not tweak_id:
                continue

            report["entry_count"] = int(report["entry_count"]) + 1

            for pattern, reason in TEMPLATE_RULES:
                for field_name, value in (("id", tweak_id), ("name", name), ("description", description)):
                    if pattern.search(value):
                        template_violations.append(
                            {
                                "id": tweak_id,
                                "field": field_name,
                                "pattern": pattern.pattern,
                                "reason": reason,
                                "value": value,
                            }
                        )

            for pattern, reason in DESCRIPTION_RULES:
                if pattern.search(description):
                    description_violations.append(
                        {
                            "id": tweak_id,
                            "pattern": pattern.pattern,
                            "reason": reason,
                            "description": description,
                        }
                    )

            for pattern, reason in NAME_RULES:
                if pattern.search(name):
                    name_violations.append(
                        {
                            "id": tweak_id,
                            "pattern": pattern.pattern,
                            "reason": reason,
                            "name": name,
                        }
                    )

    errors: list[str] = []
    if name_violations:
        errors.append(
            f"Tweak catalog contains {len(name_violations)} user-facing tweak name(s) that still use optimize or speed-up outcome language."
        )
    if description_violations:
        errors.append(
            f"Tweak catalog contains {len(description_violations)} user-facing description claim(s) that should be rewritten in a more factual, evidence-first style."
        )
    if template_violations:
        errors.append(
            f"Tweak catalog contains {len(template_violations)} unresolved template placeholder field(s)."
        )

    if errors:
        report["check_status"] = "FAIL"

    report["errors"] = errors
    report["name_violations"] = name_violations
    report["description_violations"] = description_violations
    report["template_violations"] = template_violations
    return report


def main() -> int:
    report = build_tweak_catalog_truth_report()
    print(json.dumps(report, indent=2))
    return 0 if report["check_status"] == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
