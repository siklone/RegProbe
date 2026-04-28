import json
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
RECORDS_ROOT = REPO_ROOT / "research" / "records"
MANIFEST_PATH = REPO_ROOT / "Docs" / "research" / "app-surface" / "validated-registry-values.json"


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


def concrete_state_for_record(record: dict) -> object | None:
    setting = record.get("setting") or {}
    targets = setting.get("targets") or []
    if len(targets) != 1:
        return None

    target = targets[0]
    if str(target.get("location_kind") or "").strip().lower() != "registry":
        return None

    value_name = str(target.get("value_name") or "").strip()
    value_type = str(target.get("value_type") or "").strip()
    if not value_name or "/" in value_name or "pair" in value_type.lower() or "subtree" in value_type.lower():
        return None

    target_id = str(target.get("target_id") or "").strip()
    for windows_default in record.get("windows_defaults") or []:
        for state in windows_default.get("states") or []:
            if (
                str(state.get("target_id") or "").strip() == target_id
                and str(state.get("state_kind") or "").strip() == "value"
                and state.get("value") is not None
            ):
                return state.get("value")

    for state in target.get("allowed_values") or []:
        if str(state.get("state_kind") or "").strip() == "value" and state.get("value") is not None:
            return state.get("value")

    return None


def expected_surface_record_ids() -> set[str]:
    expected: set[str] = set()
    for path in sorted(RECORDS_ROOT.glob("*.json")):
        record = load_json(path)
        if str(record.get("record_status") or "").strip() != "validated":
            continue
        version_stable = record.get("version_stable") or []
        if "25H2" not in version_stable:
            continue
        app_status = str(((record.get("app_current_implementation") or {}).get("status")) or "").strip()
        if app_status == "matches-research":
            continue
        if concrete_state_for_record(record) is None:
            continue
        expected.add(str(record.get("record_id") or path.stem))
    return expected


class ResearchAppSurfaceManifestTests(unittest.TestCase):
    def test_manifest_covers_all_eligible_validated_25h2_records(self) -> None:
        self.assertEqual(expected_surface_record_ids(), manifest_entry_ids())

    def test_manifest_preserves_record_ids_for_runtime_binding(self) -> None:
        for entry_id in manifest_entry_ids():
            self.assertFalse(entry_id.startswith("json."))


if __name__ == "__main__":
    unittest.main()
