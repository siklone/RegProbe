from __future__ import annotations

import json
import unittest
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
APP_FACING_RECEIPTS = [
    REPO_ROOT / "registry-research-framework" / "audit" / "promoted-app-qa-batch-latest.json",
    REPO_ROOT / "registry-research-framework" / "audit" / "single-tweak-app-qa-systemresponsiveness-live-latest.json",
    REPO_ROOT / "registry-research-framework" / "audit" / "single-tweak-app-qa-disable-uac-gate-live-latest.json",
]

FORBIDDEN_PUBLIC_SOURCE_FRAGMENTS = [
    "github.com/nohuto/decompiled-pseudocode",
    "github.com/nohuto/win-config",
    "github.com/nohuto/win-registry",
    "decompiled-pseudocode /",
    "No upstream nohuto source link",
    "Upstream dump / pseudocode links are attached",
]


def iter_strings(value: Any) -> list[str]:
    if isinstance(value, str):
        return [value]
    if isinstance(value, dict):
        strings: list[str] = []
        for child in value.values():
            strings.extend(iter_strings(child))
        return strings
    if isinstance(value, list):
        strings: list[str] = []
        for child in value:
            strings.extend(iter_strings(child))
        return strings
    return []


class PublicEvidenceLinkReceiptTests(unittest.TestCase):
    def test_latest_app_facing_receipts_hide_external_pseudocode_links(self) -> None:
        leaked: list[str] = []

        for receipt_path in APP_FACING_RECEIPTS:
            payload = json.loads(receipt_path.read_text(encoding="utf-8"))
            for text in iter_strings(payload):
                for fragment in FORBIDDEN_PUBLIC_SOURCE_FRAGMENTS:
                    if fragment.lower() in text.lower():
                        leaked.append(f"{receipt_path.relative_to(REPO_ROOT)}: {fragment}: {text[:220]}")

        self.assertEqual(leaked, [])

    def test_catalog_only_public_copy_names_the_actual_proof_lanes(self) -> None:
        payload = json.loads(APP_FACING_RECEIPTS[0].read_text(encoding="utf-8"))
        catalog_only_summaries = [
            text
            for text in iter_strings(payload)
            if "Catalog-only source context" in text
        ]

        self.assertGreater(len(catalog_only_summaries), 0)
        for summary in catalog_only_summaries:
            self.assertIn("not a value-semantics proof", summary)
            self.assertIn("Docs, Runtime, and Rollback", summary)


if __name__ == "__main__":
    unittest.main()
