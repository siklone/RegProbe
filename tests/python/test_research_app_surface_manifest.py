import json
import re
import subprocess
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
RECORDS_ROOT = REPO_ROOT / "research" / "records"
MANIFEST_PATH = REPO_ROOT / "Docs" / "research" / "app-surface" / "validated-registry-values.json"
INTENTIONAL_NOT_MAPPED_PATH = REPO_ROOT / "Docs" / "research" / "app-surface" / "intentional-not-mapped-records.json"
APP_ONLY_CATALOG_PATH = REPO_ROOT / "Docs" / "research" / "app-surface" / "app-only-catalog-tweaks.json"
TWEAK_PROVIDER_ROOT = REPO_ROOT / "app" / "Services" / "TweakProviders"
RESEARCH_PROVIDER_SOURCE = "app/Services/TweakProviders/ResearchAppSurfaceTweakProvider.cs"
APP_ONLY_PROVIDER_SOURCE_MARKERS = {
    "SevenZipSettingsTweak.CreateOptimize7ZipSettingsTweak": "misc.optimize-7zip-settings",
    "MouseTweaks.CreateDisableMouseThrottleTweak": "peripheral.mouse-disable-throttle",
    "MouseTweaks.CreateDisableMouseAccelerationTweak": "peripheral.mouse-disable-acceleration",
    "KeyboardTweaks.CreateOptimizeKeyboardRepeatTweak": "peripheral.keyboard-optimize-repeat",
    "KeyboardTweaks.CreateDisableLanguageSwitchHotkeyTweak": "peripheral.keyboard-disable-language-hotkey",
    "AudioTweaks.CreateDisableAudioDuckingTweak": "peripheral.audio-disable-ducking",
    "AudioTweaks.CreateDisableAudioEnhancementsTweak": "peripheral.audio-disable-enhancements",
    "DisableOneDriveTweaks.CreateDisableOneDriveTweak": "misc.disable-onedrive",
    "DisableEdgeFeaturesTweaks.CreateDisableEdgeFeaturesTweak": "misc.disable-edge-features",
    "DisableVisualStudioTelemetryTweak.CreateDisableVisualStudioTelemetryTweak": "misc.disable-visual-studio-telemetry",
    "DisableOfficeTelemetryTweak.CreateDisableOfficeTelemetryTweak": "misc.disable-office-telemetry",
    "new DisableVSCodeTelemetryTweak": "misc.disable-vscode-telemetry",
    "new DisableHibernationTweak": "power.disable-hibernation",
    "new DisableUsbSelectiveSuspendTweak": "power.disable-usb-selective-suspend",
    "new DisableCpuCoreParkingTweak": "power.disable-cpu-parking",
    "new FlushDnsCacheTweak": "network.flush-dns-cache",
    "new ResetNetworkStackTweak": "network.reset-winsock",
    "new CleanupComponentStoreTweak": "cleanup.component-store",
    "new ClearRecycleBinTweak": "cleanup.recycle-bin",
    "new ClearShadowCopiesTweak": "cleanup.shadow-copies",
    "new ClearTemporaryFilesTweak": "cleanup.temp-files",
    "new ClearDirectXShaderCacheTweak": "cleanup.directx-shader-cache",
    "new ClearThumbnailCacheTweak": "cleanup.thumbnail-cache",
    "new ClearWindowsUpdateCacheTweak": "cleanup.windows-update-cache",
    "new ClearWERFilesTweak": "cleanup.wer-files",
    "new ClearPrefetchFilesTweak": "cleanup.prefetch-files",
    "new ClearFontCacheTweak": "cleanup.font-cache",
    "new ClearWindowsOldTweak": "cleanup.windows-old",
    "new ClearMemoryDumpFilesTweak": "cleanup.memory-dumps",
    "new RemoveProductKeyTweak": "cleanup.product-key",
    "new DisableSuperfetchTweak": "power.disable-superfetch",
    "new DisableUacFullTweak": "security.disable-uac",
    "new ClearEventLogsTweak": "cleanup.eventlog-system",
}
APP_ONLY_PROVIDER_ID_PATTERNS = [
    r'Create(?:CommandBacked)?RegistryTweak\(\s*context,\s*"([^"]+)"',
    r'CreateRegistryValue(?:Set|Batch|PresetBatch)Tweak\(\s*context,\s*"([^"]+)"',
    r'CreateServiceStartModeBatchTweak\(\s*context,\s*"([^"]+)"',
    r'CreateScheduledTaskBatchTweak\(\s*context,\s*"([^"]+)"',
    r'CreateFileRenameTweak\(\s*context,\s*"([^"]+)"',
    r'CreateCommandBackedRegistryValueBatchTweak\(\s*context,\s*"([^"]+)"',
    r'CreateCompositeTweak\(\s*"([^"]+)"',
]


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def record_path_for_id(record_id: str) -> Path:
    for path in sorted(RECORDS_ROOT.glob("*.json")):
        record = load_json(path)
        if str(record.get("record_id") or path.stem) == record_id:
            return path
    raise FileNotFoundError(record_id)


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


def intentional_not_mapped_entries() -> list[dict]:
    payload = load_json(INTENTIONAL_NOT_MAPPED_PATH)
    return payload.get("records") or []


def app_only_catalog_entries() -> list[dict]:
    payload = load_json(APP_ONLY_CATALOG_PATH)
    return payload.get("tweaks") or []


def live_provider_tweak_ids() -> set[str]:
    ids: set[str] = set()

    for path in sorted(TWEAK_PROVIDER_ROOT.glob("*TweakProvider.cs")):
        if path.name in {"BaseTweakProvider.cs", "ResearchAppSurfaceTweakProvider.cs"}:
            continue

        text = path.read_text(encoding="utf-8-sig")
        for pattern in APP_ONLY_PROVIDER_ID_PATTERNS:
            ids.update(re.findall(pattern, text, re.S))
        for marker, tweak_id in APP_ONLY_PROVIDER_SOURCE_MARKERS.items():
            if marker in text:
                ids.add(tweak_id)

    return ids


def has_defined_value_name(payload: dict) -> bool:
    return payload.get("value_name") is not None


def normalized_value_name(payload: dict) -> str:
    return str(payload.get("value_name") or "").strip()


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

    command_type = supported_command_type(record)
    if command_type:
        command_targets = [
            target
            for target in targets
            if str(target.get("target_id") or "").strip() in write_target_ids
        ]
        if command_targets:
            return command_targets[0]

    return None


def coordinated_registry_targets(record: dict) -> list[dict]:
    setting = record.get("setting") or {}
    targets = setting.get("targets") or []
    if len(targets) < 2:
        return []

    implementation = record.get("app_current_implementation") or {}
    writes = implementation.get("writes") or []
    writes_by_target_id = {
        str(write.get("target_id") or "").strip(): write
        for write in writes
        if str(write.get("target_id") or "").strip()
        and str(write.get("path") or "").strip()
        and has_defined_value_name(write)
        and "value" in write
    }
    if not writes_by_target_id:
        return []

    matched: list[dict] = []
    for target in targets:
        target_id = str(target.get("target_id") or "").strip()
        if not target_id or target_id not in writes_by_target_id:
            return []
        if str(target.get("location_kind") or "").strip().lower() not in {"registry", "group-policy"}:
            return []
        value_type = str(target.get("value_type") or "").strip().lower()
        value_name = normalized_value_name(target)
        if not has_defined_value_name(target):
            return []
        if "subtree" in value_type or "pair" in value_type or " set" in value_type:
            return []
        matched.append(target)

    return matched


def coordinated_task_targets(record: dict) -> list[dict]:
    setting = record.get("setting") or {}
    targets = setting.get("targets") or []
    if len(targets) < 2:
        return []

    implementation = record.get("app_current_implementation") or {}
    writes = implementation.get("writes") or []
    writes_by_target_id = {
        str(write.get("target_id") or "").strip(): write
        for write in writes
        if str(write.get("target_id") or "").strip()
        and str(write.get("path") or "").strip()
        and has_defined_value_name(write)
        and "value" in write
    }
    if not writes_by_target_id:
        return []

    matched: list[dict] = []
    for target in targets:
        target_id = str(target.get("target_id") or "").strip()
        if not target_id or target_id not in writes_by_target_id:
            return []
        if str(target.get("location_kind") or "").strip().lower() != "scheduled-task":
            return []
        if str(target.get("value_type") or "").strip().lower() != "taskenabledstate":
            return []
        write = writes_by_target_id[target_id]
        if str(write.get("value") or "").strip().lower() != "disabled":
            return []
        matched.append(target)

    return matched


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


def current_app_writes(record: dict, target: dict) -> list[dict]:
    implementation = record.get("app_current_implementation") or {}
    writes = implementation.get("writes") or []
    target_id = str(target.get("target_id") or "").strip()
    return [
        write
        for write in writes
        if str(write.get("target_id") or "").strip() == target_id
        and str(write.get("path") or "").strip()
        and has_defined_value_name(write)
        and "value" in write
    ]


def current_app_value(record: dict, target: dict) -> object | None:
    implementation = record.get("app_current_implementation") or {}
    writes = implementation.get("writes") or []
    target_id = str(target.get("target_id") or "").strip()
    for write in writes:
        if str(write.get("target_id") or "").strip() == target_id and "value" in write:
            return write.get("value")
    return None


def writes_share_single_slot(writes: list[dict]) -> bool:
    slots = {
        (
            str(write.get("path") or "").strip(),
            normalized_value_name(write),
            str(write.get("value_type") or "").strip(),
        )
        for write in writes
        if str(write.get("path") or "").strip() and has_defined_value_name(write)
    }
    return len(slots) == 1


def baseline_value_for_target(record: dict, target: dict) -> object | None:
    target_id = str(target.get("target_id") or "").strip()
    for windows_default in record.get("windows_defaults") or []:
        for state in windows_default.get("states") or []:
            if str(state.get("target_id") or "").strip() == target_id and state.get("value") is not None:
                return state.get("value")
    return None


def is_supported_file_target(target: dict) -> bool:
    path = str(target.get("path") or "").strip().lower()
    value_name = str(target.get("value_name") or "").strip().lower()
    value_type = str(target.get("value_type") or "").strip().lower()

    return (
        value_type == "json boolean"
        and "settings-store.json" in path
        and value_name == "linuxvm.wslengineenabled.value"
    ) or (
        value_type == "wsl setting"
        and path.endswith(".wslconfig")
        and value_name == "[wsl2].memory"
    )


def supported_command_type(record: dict) -> str | None:
    record_id = str(record.get("record_id") or "").strip()
    return {
        "cleanup.disable-reserved-storage": "COMMAND_RESERVED_STORAGE",
        "network.disable-netbios": "COMMAND_DISABLE_NETBIOS",
        "network.smb-disable-leasing": "COMMAND_SMB_DISABLE_LEASING",
        "network.smb-enable-multichannel": "COMMAND_SMB_ENABLE_MULTICHANNEL",
        "power.optimize-cpu-boost": "COMMAND_POWER_PERFBOOSTMODE",
        "security.disable-system-mitigations": "COMMAND_DISABLE_SYSTEM_MITIGATIONS",
    }.get(record_id)


def is_surfaceable_record(record: dict) -> bool:
    if coordinated_registry_targets(record):
        return True
    if coordinated_task_targets(record):
        return True

    target = surface_target(record)
    if target is None:
        return False
    location_kind = str(target.get("location_kind") or "").strip().lower()
    if location_kind == "service":
        return current_app_value(record, target) is not None
    if location_kind == "scheduled-task":
        return str(current_app_value(record, target) or "").strip().lower() == "disabled"
    if location_kind == "file":
        return is_supported_file_target(target) and current_app_value(record, target) is not None
    if supported_command_type(record):
        return current_app_value(record, target) is not None
    if location_kind not in {"registry", "group-policy"}:
        return False

    value_type = str(target.get("value_type") or "").strip().lower()
    if "subtree" in value_type:
        return True

    value_name = normalized_value_name(target)
    if not has_defined_value_name(target):
        return False

    values = state_values_for_record(record)
    if not values:
        return False

    if len(current_app_writes(record, target)) > 1:
        return True

    if " set" in value_type:
        return len(current_app_writes(record, target)) > 1

    if "pair" in value_type:
        return any(isinstance(value, str) and "=" in value and ";" in value for value in values)

    return "/" not in value_name


def expected_surface_record_ids() -> set[str]:
    expected: set[str] = set()
    for path in sorted(RECORDS_ROOT.glob("*.json")):
        record = load_json(path)
        record_status = str(record.get("record_status") or "").strip()
        if record_status not in {"validated", "draft", "deprecated"}:
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
        if record_status not in {"validated", "draft", "deprecated"}:
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
        if record_status not in {"validated", "draft", "deprecated"}:
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
            target_writes = [
                write
                for write in writes
                if str(write.get("target_id") or "").strip() == target_id and "value" in write
            ]
            if len(target_writes) > 1 and writes_share_single_slot(target_writes):
                continue

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

    def test_manifest_uses_presets_for_same_slot_alternative_writes(self) -> None:
        for entry in manifest_entries():
            record = load_json(REPO_ROOT / str(entry["documentation"]))
            target = surface_target(record)
            if target is None:
                continue

            writes = current_app_writes(record, target)
            if len(writes) <= 1 or not writes_share_single_slot(writes):
                continue

            presets = entry.get("presets") or []
            self.assertTrue(presets, entry["id"])

            baseline_value = baseline_value_for_target(record, target)
            if baseline_value is None:
                continue

            self.assertEqual("observed-baseline", str(entry.get("default_preset_key") or "").strip(), entry["id"])

    def test_ssh_agent_autostart_runtime_value_is_real_executable_path(self) -> None:
        entry = next(
            item
            for item in manifest_entries()
            if str(item.get("id") or "").strip() == "developer.ssh-agent-autostart"
        )
        self.assertEqual(
            r"C:\Windows\System32\OpenSSH\ssh-agent.exe",
            entry.get("recommended_value"),
        )

        record = load_json(record_path_for_id("developer.ssh-agent-autostart"))
        target = surface_target(record)
        self.assertIsNotNone(target)
        self.assertEqual(
            r"C:\Windows\System32\OpenSSH\ssh-agent.exe",
            current_app_value(record, target),
        )

    def test_non_manifest_surfaceable_records_are_intentional_legacy_backlog(self) -> None:
        for record_id in sorted(all_surfaceable_record_ids() - manifest_entry_ids()):
            record = load_json(record_path_for_id(record_id))
            implementation = record.get("app_current_implementation") or {}
            provider_source = str(implementation.get("provider_source") or "").strip()
            self.assertNotEqual(RESEARCH_PROVIDER_SOURCE, provider_source, record_id)

    def test_intentional_not_mapped_ledger_matches_checked_in_record_metadata(self) -> None:
        expected = {
            str(entry.get("record_id") or "").strip(): entry
            for entry in intentional_not_mapped_entries()
        }
        actual: dict[str, dict] = {}

        for path in sorted(RECORDS_ROOT.glob("*.json")):
            record = load_json(path)
            implementation = record.get("app_current_implementation") or {}
            if str(implementation.get("status") or "").strip() != "not-mapped":
                continue

            record_id = str(record.get("record_id") or path.stem)
            actual[record_id] = {
                "provider_source": str(implementation.get("provider_source") or "").strip(),
                "notes": str(implementation.get("notes") or "").strip(),
            }

        self.assertEqual(set(expected), set(actual))

        for record_id, entry in expected.items():
            self.assertEqual(
                str(entry.get("provider_source") or "").strip(),
                actual[record_id]["provider_source"],
                record_id,
            )
            self.assertEqual(
                str(entry.get("notes") or "").strip(),
                actual[record_id]["notes"],
                record_id,
            )

    def test_app_only_catalog_ledger_matches_live_provider_minus_record_corpus(self) -> None:
        record_ids = {
            str(load_json(path).get("record_id") or path.stem)
            for path in sorted(RECORDS_ROOT.glob("*.json"))
        }
        actual = live_provider_tweak_ids() - record_ids
        expected = {
            str(entry.get("tweak_id") or "").strip()
            for entry in app_only_catalog_entries()
        }

        self.assertEqual(expected, actual)

    def test_app_only_catalog_ledger_entries_are_fully_annotated(self) -> None:
        for entry in app_only_catalog_entries():
            tweak_id = str(entry.get("tweak_id") or "").strip()
            self.assertTrue(tweak_id)
            self.assertTrue(str(entry.get("reason") or "").strip(), tweak_id)
            self.assertTrue(str(entry.get("provider_source") or "").strip(), tweak_id)
            self.assertTrue(str(entry.get("notes") or "").strip(), tweak_id)


if __name__ == "__main__":
    unittest.main()
