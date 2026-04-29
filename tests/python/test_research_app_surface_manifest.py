import json
import subprocess
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
RECORDS_ROOT = REPO_ROOT / "research" / "records"
MANIFEST_PATH = REPO_ROOT / "Docs" / "research" / "app-surface" / "validated-registry-values.json"
RESEARCH_PROVIDER_SOURCE = "app/Services/TweakProviders/ResearchAppSurfaceTweakProvider.cs"


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def manifest_entry_ids() -> set[str]:
    payload = load_json(MANIFEST_PATH)
    ids: set[str] = set()
    for category in (payload.get("categories") or {}).values():
        for entry in category.get("entries") or []:
            entry_id = str(entry.get("id") or "").strip()
            if entry_id:
                ids.add(entry_id)
    return ids


def state_values_for_record(record: dict) -> list[object]:
    setting = record.get("setting") or {}
    targets = setting.get("targets") or []
    if len(targets) != 1:
        return []

    target = targets[0]
    target_id = str(target.get("target_id") or "").strip()
    values: list[object] = []

    for windows_default in record.get("windows_defaults") or []:
        for state in windows_default.get("states") or []:
            if str(state.get("target_id") or "").strip() == target_id and state.get("value") is not None:
                values.append(state.get("value"))

    for state in target.get("allowed_values") or []:
        if state.get("value") is not None:
            values.append(state.get("value"))

    return values


def is_surfaceable_record(record: dict) -> bool:
    setting = record.get("setting") or {}
    targets = setting.get("targets") or []
    if len(targets) != 1:
        return False

    target = targets[0]
    if str(target.get("location_kind") or "").strip().lower() != "registry":
        return False

    value_type = str(target.get("value_type") or "").strip().lower()
    if "subtree" in value_type:
        return False

    value_name = str(target.get("value_name") or "").strip()
    if not value_name:
        return False

    values = state_values_for_record(record)
    if not values:
        return False

    if "pair" in value_type:
        return any(isinstance(value, str) and "=" in value and ";" in value for value in values)

    return "/" not in value_name


def expected_surface_record_ids() -> set[str]:
    expected: set[str] = set()
    for path in sorted(RECORDS_ROOT.glob("*.json")):
        record = load_json(path)
        if str(record.get("record_status") or "").strip() != "validated":
            continue
        if "25H2" not in (record.get("version_stable") or []):
            continue
        if not is_surfaceable_record(record):
            continue
        implementation = record.get("app_current_implementation") or {}
        provider_source = str(implementation.get("provider_source") or "").strip()
        if str(implementation.get("status") or "").strip() == "matches-research" and provider_source != RESEARCH_PROVIDER_SOURCE:
            continue
        expected.add(str(record.get("record_id") or path.stem))
    return expected


class ResearchAppSurfaceManifestTests(unittest.TestCase):
    def test_generator_reproduces_checked_in_manifest(self) -> None:
        result = subprocess.run(
            ["python3", "scripts/research/generate_app_surface_manifest.py", "--check"],
            cwd=REPO_ROOT,
            capture_output=True,
            text=True,
            check=False,
        )
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)

    def test_manifest_covers_all_surfaceable_validated_25h2_records(self) -> None:
        self.assertEqual(expected_surface_record_ids(), manifest_entry_ids())

    def test_manifest_backed_records_are_marked_matches_research(self) -> None:
        for entry_id in manifest_entry_ids():
            record = load_json(RECORDS_ROOT / f"{entry_id}.json")
            implementation = record.get("app_current_implementation") or {}
            self.assertEqual("matches-research", str(implementation.get("status") or "").strip(), entry_id)
            self.assertEqual(RESEARCH_PROVIDER_SOURCE, str(implementation.get("provider_source") or "").strip(), entry_id)

    def test_manifest_preserves_record_ids_for_runtime_binding(self) -> None:
        for entry_id in manifest_entry_ids():
            self.assertFalse(entry_id.startswith("json."))


if __name__ == "__main__":
    unittest.main()
