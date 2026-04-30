#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from collections import defaultdict
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
RECORDS_ROOT = REPO_ROOT / "research" / "records"
MANIFEST_PATH = REPO_ROOT / "Docs" / "research" / "app-surface" / "validated-registry-values.json"
RESEARCH_PROVIDER_SOURCE = "app/Services/TweakProviders/ResearchAppSurfaceTweakProvider.cs"

CATEGORY_META = {
    "policy": {
        "name": "Policy",
        "description": "Stable policy-backed registry research cards surfaced directly from research records.",
        "risk_level": "medium",
    },
    "power": {
        "name": "Power",
        "description": "Stable raw power-manager registry research cards surfaced directly from research records.",
        "risk_level": "medium",
    },
    "system": {
        "name": "System",
        "description": "Stable kernel-adjacent and Session Manager registry research cards surfaced directly from research records.",
        "risk_level": "high",
    },
}


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def normalize_category_key(record_id: str) -> str:
    return (record_id.split(".", 1)[0] or "research").strip().lower()


def category_metadata(category_key: str, category_name: str) -> dict:
    fallback = {
        "name": category_name or category_key.title(),
        "description": f"Validated {category_name or category_key} registry research cards surfaced directly from research records.",
        "risk_level": "medium",
    }
    return {**fallback, **CATEGORY_META.get(category_key, {})}


def state_metadata_by_value(target: dict) -> dict[str, dict]:
    metadata: dict[str, dict] = {}
    for state in target.get("allowed_values") or []:
        value = state.get("value")
        if value is None:
            continue
        metadata[json.dumps(value, sort_keys=True)] = state
    return metadata


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


def concrete_states(record: dict, target: dict) -> list[object]:
    values: list[object] = []
    target_id = str(target.get("target_id") or "").strip()

    for windows_default in record.get("windows_defaults") or []:
        for state in windows_default.get("states") or []:
            if str(state.get("target_id") or "").strip() == target_id and state.get("value") is not None:
                values.append(state.get("value"))

    for state in target.get("allowed_values") or []:
        if state.get("value") is not None:
            values.append(state.get("value"))

    deduped: list[object] = []
    seen: set[str] = set()
    for value in values:
        key = json.dumps(value, sort_keys=True)
        if key in seen:
            continue
        seen.add(key)
        deduped.append(value)
    return deduped


def current_app_value(record: dict, target: dict) -> object | None:
    implementation = record.get("app_current_implementation") or {}
    writes = implementation.get("writes") or []
    target_id = str(target.get("target_id") or "").strip()
    for write in writes:
        if str(write.get("target_id") or "").strip() == target_id and "value" in write:
            return write.get("value")
    return None


def current_app_writes(record: dict, target: dict) -> list[dict]:
    implementation = record.get("app_current_implementation") or {}
    writes = implementation.get("writes") or []
    target_id = str(target.get("target_id") or "").strip()
    return [
        write
        for write in writes
        if str(write.get("target_id") or "").strip() == target_id
        and str(write.get("path") or "").strip()
        and str(write.get("value_name") or "").strip()
        and "value" in write
    ]


def is_surfaceable_by_research_provider(record: dict) -> bool:
    record_status = str(record.get("record_status") or "").strip()
    if record_status not in {"validated", "draft"}:
        return False
    if record_status == "draft" and "25H2" not in (record.get("version_stable") or []):
        return False

    implementation = record.get("app_current_implementation") or {}
    if str(implementation.get("status") or "").strip() != "matches-research":
        return False
    provider_source = str(implementation.get("provider_source") or "").strip()
    if provider_source != RESEARCH_PROVIDER_SOURCE:
        return False

    target = surface_target(record)
    if target is None:
        return False
    if str(target.get("location_kind") or "").strip().lower() not in {"registry", "group-policy"}:
        return False

    value_type = str(target.get("value_type") or "").strip().lower()
    value_name = str(target.get("value_name") or "").strip()
    if "subtree" in value_type:
        return True

    if not value_name:
        return False

    values = concrete_states(record, target)
    if not values:
        return False

    if " set" in value_type:
        return len(current_app_writes(record, target)) > 1

    if "pair" in value_type:
        return any(isinstance(value, str) and "=" in value and ";" in value for value in values)

    return "/" not in value_name


def parse_pair_value(value: str) -> list[tuple[str, object]]:
    entries: list[tuple[str, object]] = []
    for chunk in value.split(";"):
        part = chunk.strip()
        if not part:
            continue
        name, raw = part.split("=", 1)
        parsed: object
        raw = raw.strip()
        if raw.isdigit() or (raw.startswith("-") and raw[1:].isdigit()):
            parsed = int(raw)
        else:
            parsed = raw
        entries.append((name.strip(), parsed))
    return entries


def build_entry(record: dict, source_path: Path) -> dict:
    setting = record["setting"]
    target = surface_target(record)
    if target is None:
        raise ValueError(f"Record {record['record_id']} is not surfaceable by the research provider.")
    values = concrete_states(record, target)
    category_key = normalize_category_key(str(record["record_id"]))
    value_metadata = state_metadata_by_value(target)

    base = {
        "id": record["record_id"],
        "name": setting.get("name") or record["record_id"],
        "description": setting.get("casual_explanation") or record.get("summary") or "",
        "documentation": source_path.relative_to(REPO_ROOT).as_posix(),
        "verified": str(record.get("record_status") or "").strip() == "validated",
    }

    value_type = str(target.get("value_type") or "")
    if "subtree" in value_type.lower():
        base.update(
            {
                "path": target["path"],
                "value_name": target.get("value_name") or "(subtree root)",
                "type": "REG_SUBTREE",
            }
        )
        return {"category_key": category_key, "entry": base}

    if "pair" in value_type.lower():
        pair_value = next(value for value in values if isinstance(value, str) and "=" in value and ";" in value)
        base["batch_entries"] = [
            {
                "path": target["path"],
                "value_name": name,
                "type": "REG_DWORD",
                "target_value": parsed_value,
            }
            for name, parsed_value in parse_pair_value(pair_value)
        ]
        return {"category_key": category_key, "entry": base}

    if " set" in value_type.lower():
        writes = current_app_writes(record, target)
        base["batch_entries"] = [
            {
                "path": str(write["path"]),
                "value_name": str(write["value_name"]),
                "type": str(write["value_type"]),
                "target_value": write["value"],
            }
            for write in writes
        ]
        return {"category_key": category_key, "entry": base}

    if len(values) > 1:
        presets = []
        baseline_value = None
        preferred_value = current_app_value(record, target)
        target_id = str(target.get("target_id") or "").strip()
        for windows_default in record.get("windows_defaults") or []:
            for state in windows_default.get("states") or []:
                if str(state.get("target_id") or "").strip() == target_id and state.get("value") is not None:
                    baseline_value = state.get("value")
                    break
            if baseline_value is not None:
                break

        for value in values:
            state = value_metadata.get(json.dumps(value, sort_keys=True), {})
            label = str(state.get("label") or "").strip()
            if not label:
                label = "Observed Baseline" if baseline_value == value else f"{setting.get('name')} {value}"
            key = "observed-baseline" if baseline_value == value else f"value-{value}".replace(" ", "-").replace(".", "_")
            presets.append(
                {
                    "key": key,
                    "label": label,
                    "description": str(state.get("meaning") or f"{setting.get('name')} = {value}"),
                    "entries": [
                        {
                            "path": target["path"],
                            "value_name": target["value_name"],
                            "type": target["value_type"],
                            "target_value": value,
                        }
                    ],
                }
            )

        default_key = presets[0]["key"]
        if preferred_value in values:
            default_key = next(
                preset["key"]
                for preset in presets
                if preset["entries"][0]["target_value"] == preferred_value
            )
        elif baseline_value in values:
            default_key = "observed-baseline"
        base["default_preset_key"] = default_key
        base["presets"] = presets
        return {"category_key": category_key, "entry": base}

    base.update(
        {
            "path": target["path"],
            "value_name": target["value_name"],
            "type": target["value_type"],
            "recommended_value": values[0],
        }
    )
    if any(
        str(state.get("target_id") or "").strip() == str(target.get("target_id") or "").strip()
        and state.get("value") is not None
        for windows_default in record.get("windows_defaults") or []
        for state in windows_default.get("states") or []
    ):
        baseline = next(
            state.get("value")
            for windows_default in record.get("windows_defaults") or []
            for state in windows_default.get("states") or []
            if str(state.get("target_id") or "").strip() == str(target.get("target_id") or "").strip()
            and state.get("value") is not None
        )
        base["default_value"] = baseline
    return {"category_key": category_key, "entry": base}


def build_manifest() -> dict:
    categories: dict[str, dict] = defaultdict(dict)
    grouped_entries: dict[str, list[dict]] = defaultdict(list)

    for path in sorted(RECORDS_ROOT.glob("*.json")):
        record = load_json(path)
        if not is_surfaceable_by_research_provider(record):
            continue
        built = build_entry(record, path)
        grouped_entries[built["category_key"]].append(built["entry"])

    for category_key, entries in sorted(grouped_entries.items()):
        entries.sort(key=lambda entry: entry["id"])
        meta = category_metadata(category_key, category_key.title())
        categories[category_key] = {
            "name": meta["name"],
            "description": meta["description"],
            "risk_level": meta["risk_level"],
            "entries": entries,
        }

    return {
        "metadata": {
            "version": "2.1",
            "source": "research-record-projection",
            "policy": "Docs/research/APP_SURFACING_POLICY.md",
        },
        "categories": categories,
    }


def render_manifest() -> str:
    return json.dumps(build_manifest(), indent=2) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate the research app-surface manifest from validated records.")
    parser.add_argument("--write", action="store_true", help="Write the generated manifest to the checked-in path.")
    parser.add_argument("--check", action="store_true", help="Fail if the checked-in manifest differs from generated output.")
    args = parser.parse_args()

    rendered = render_manifest()
    if args.write:
        MANIFEST_PATH.write_text(rendered, encoding="utf-8")
        return 0

    if args.check:
        current = MANIFEST_PATH.read_text(encoding="utf-8-sig")
        if current != rendered:
            print("App-surface manifest is out of date.")
            return 1
        return 0

    print(rendered, end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
