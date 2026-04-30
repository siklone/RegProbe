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


def manifest_entries() -> list[dict]:
    payload = load_json(MANIFEST_PATH)
    entries: list[dict] = []
    for category in (payload.get("categories") or {}).values():
        entries.extend(category.get("entries") or [])
    return entries


def surface_target(record: dict) -> dict | None:
    setting = record.get("setting") or {}
    targets = setting.get("targets") or []
    if len(targets) == 1:
        return targets[0]

    implementation = record.get("app_current_implementation") or {}
    write_target_ids = {
        str(write.get("target_id") or "").strip()
        for write in (implementation.get("writes") or [])
        if str(write.get("target_id") or "").strip() and "value" in write
    }
    matching_targets = [
        target
        for target in targets
        if str(target.get("target_id") or "").strip() in write_target_ids
        and str(target.get("location_kind") or "").strip().lower() in {"registry", "group-policy"}
    ]
    if len(matching_targets) == 1:
        return matching_targets[0]

    return None


def state_values_for_record(record: dict) -> list[object]:
    target = surface_target(record)
    if target is None:
        return []

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
    target = surface_target(record)
    if target is None:
        return False
    if str(target.get("location_kind") or "").strip().lower() not in {"registry", "group-policy"}:
        return False

    value_type = str(target.get("value_type") or "").strip().lower()
    if "subtree" in value_type:
        return True

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
        record_status = str(record.get("record_status") or "").strip()
        if record_status not in {"validated", "draft"}:
            continue
        if record_status == "draft" and "25H2" not in (record.get("version_stable") or []):
            continue
        if not is_surfaceable_record(record):
            continue
        implementation = record.get("app_current_implementation") or {}
        if str(implementation.get("status") or "").strip() != "matches-research":
            continue
        provider_source = str(implementation.get("provider_source") or "").strip()
        if provider_source != RESEARCH_PROVIDER_SOURCE:
            continue
        expected.add(str(record.get("record_id") or path.stem))
    return expected


def all_surfaceable_record_ids() -> set[str]:
    surfaceable: set[str] = set()
    for path in sorted(RECORDS_ROOT.glob("*.json")):
        record = load_json(path)
        record_status = str(record.get("record_status") or "").strip()
        if record_status not in {"validated", "draft"}:
            continue
        if record_status == "draft" and "25H2" not in (record.get("version_stable") or []):
            continue
        if is_surfaceable_record(record):
            surfaceable.add(str(record.get("record_id") or path.stem))
    return surfaceable


def legacy_provider_surface_record_ids() -> set[str]:
    surfaced: set[str] = set()
    for path in sorted(RECORDS_ROOT.glob("*.json")):
        record = load_json(path)
        record_status = str(record.get("record_status") or "").strip()
        if record_status not in {"validated", "draft"}:
            continue
        if record_status == "draft" and "25H2" not in (record.get("version_stable") or []):
            continue
        if not is_surfaceable_record(record):
            continue
        implementation = record.get("app_current_implementation") or {}
        if str(implementation.get("status") or "").strip() != "matches-research":
            continue
        provider_source = str(implementation.get("provider_source") or "").strip()
        if provider_source and provider_source != RESEARCH_PROVIDER_SOURCE:
            surfaced.add(str(record.get("record_id") or path.stem))
    return surfaced


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

    def test_manifest_covers_all_surfaceable_stable_25h2_records(self) -> None:
        self.assertEqual(expected_surface_record_ids(), manifest_entry_ids())

    def test_manifest_backed_records_are_marked_matches_research(self) -> None:
        for entry in manifest_entries():
            entry_id = str(entry.get("id") or "").strip()
            record = load_json(REPO_ROOT / str(entry["documentation"]))
            implementation = record.get("app_current_implementation") or {}
            self.assertEqual("matches-research", str(implementation.get("status") or "").strip(), entry_id)
            self.assertEqual(RESEARCH_PROVIDER_SOURCE, str(implementation.get("provider_source") or "").strip(), entry_id)

    def test_manifest_preserves_record_ids_for_runtime_binding(self) -> None:
        for entry_id in manifest_entry_ids():
            self.assertFalse(entry_id.startswith("json."))

    def test_manifest_documentation_paths_exist(self) -> None:
        for entry in manifest_entries():
            documentation = str(entry.get("documentation") or "").strip()
            self.assertTrue(documentation, entry.get("id"))
            self.assertTrue((REPO_ROOT / documentation).exists(), documentation)

    def test_manifest_preserves_current_app_choice_for_multi_value_records(self) -> None:
        for entry in manifest_entries():
            presets = entry.get("presets") or []
            if not presets:
                continue

            record = load_json(REPO_ROOT / str(entry["documentation"]))
            target = ((record.get("setting") or {}).get("targets") or [None])[0]
            if not target:
                continue

            target_id = str(target.get("target_id") or "").strip()
            implementation = record.get("app_current_implementation") or {}
            writes = implementation.get("writes") or []
            preferred_value = next(
                (
                    write.get("value")
                    for write in writes
                    if str(write.get("target_id") or "").strip() == target_id and "value" in write
                ),
                None,
            )
            if preferred_value is None:
                continue

            default_key = str(entry.get("default_preset_key") or "").strip()
            matching_key = next(
                (
                    str(preset.get("key") or "")
                    for preset in presets
                    if (preset.get("entries") or [{}])[0].get("target_value") == preferred_value
                ),
                "",
            )
            self.assertEqual(matching_key, default_key, entry["id"])

    def test_non_manifest_surfaceable_records_are_intentional_legacy_backlog(self) -> None:
        for record_id in sorted(all_surfaceable_record_ids() - manifest_entry_ids()):
            record = load_json(next(RECORDS_ROOT.glob(f"{record_id}*.json")))
            implementation = record.get("app_current_implementation") or {}
            provider_source = str(implementation.get("provider_source") or "").strip()
            self.assertNotEqual(RESEARCH_PROVIDER_SOURCE, provider_source, record_id)


if __name__ == "__main__":
    unittest.main()
