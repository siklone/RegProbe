from __future__ import annotations

import json
from functools import lru_cache
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[1]
CONFIG_ROOT = REPO_ROOT / "registry-research-framework" / "config"
INTERACTION_GRAPH_PATH = CONFIG_ROOT / "interaction-graph.json"
TWEAK_DEPENDENCIES_PATH = CONFIG_ROOT / "tweak-dependencies.json"
ANTICHEAT_RISK_PATH = CONFIG_ROOT / "anticheat-risk-overrides.json"
REPRODUCIBILITY_MANIFEST_PATH = CONFIG_ROOT / "reproducibility-manifest.json"


def load_json_if_exists(path: Path) -> Any:
    if not path.exists():
        return None
    with path.open("r", encoding="utf-8-sig") as handle:
        return json.load(handle)


@lru_cache(maxsize=1)
def interaction_graph() -> dict[str, Any]:
    payload = load_json_if_exists(INTERACTION_GRAPH_PATH)
    return payload if isinstance(payload, dict) else {}


@lru_cache(maxsize=1)
def tweak_dependencies() -> dict[str, Any]:
    payload = load_json_if_exists(TWEAK_DEPENDENCIES_PATH)
    return payload if isinstance(payload, dict) else {}


@lru_cache(maxsize=1)
def anticheat_risk_map() -> dict[str, Any]:
    payload = load_json_if_exists(ANTICHEAT_RISK_PATH)
    return payload if isinstance(payload, dict) else {}


@lru_cache(maxsize=1)
def reproducibility_manifest() -> dict[str, Any]:
    payload = load_json_if_exists(REPRODUCIBILITY_MANIFEST_PATH)
    return payload if isinstance(payload, dict) else {}


def tested_on_items(record: dict[str, Any]) -> list[dict[str, Any]]:
    return [item for item in (record.get("tested_on") or []) if isinstance(item, dict)]


def latest_tested_on(record: dict[str, Any]) -> dict[str, Any]:
    items = tested_on_items(record)
    if not items:
        return {}
    items.sort(key=lambda item: str(item.get("build") or item.get("os") or ""), reverse=True)
    return items[0]


def record_os_build(record: dict[str, Any], repro: dict[str, Any] | None = None) -> str | None:
    latest = latest_tested_on(record)
    build = str(latest.get("build") or "").strip()
    if build:
        return build
    manifest = repro if isinstance(repro, dict) else reproducibility_manifest()
    fallback = str(manifest.get("os_build") or "").strip()
    return fallback or None


def evidence_freshness(record: dict[str, Any], repro: dict[str, Any] | None = None) -> dict[str, Any]:
    manifest = repro if isinstance(repro, dict) else reproducibility_manifest()
    return {
        "os_build": record_os_build(record, manifest),
        "evidence_collected_utc": record.get("last_reviewed_utc"),
        "revalidation_needed_on_major_update": True,
        "expires_after_build": None,
    }


def interaction_groups_for_tweak(tweak_id: str) -> list[dict[str, Any]]:
    payload = interaction_graph()
    groups = []
    for entry in payload.get("groups") or []:
        if not isinstance(entry, dict):
            continue
        if tweak_id in (entry.get("keys") or []):
            groups.append(
                {
                    "group_id": entry.get("group_id"),
                    "label": entry.get("label"),
                    "interaction": entry.get("interaction"),
                    "partial_risk": entry.get("partial_risk"),
                    "keys": entry.get("keys") or [],
                    "recommended_validation": entry.get("recommended_validation") or [],
                }
            )
    return groups


def dependency_entry_for_tweak(tweak_id: str) -> dict[str, Any]:
    payload = tweak_dependencies()
    entries = payload.get("entries") if isinstance(payload.get("entries"), dict) else {}
    entry = entries.get(tweak_id)
    if not isinstance(entry, dict):
        return {
            "requires": [],
            "conflicts_with": [],
            "recommended_with": [],
            "notes": None,
        }
    return {
        "requires": [str(item) for item in (entry.get("requires") or []) if item],
        "conflicts_with": [str(item) for item in (entry.get("conflicts_with") or []) if item],
        "recommended_with": [str(item) for item in (entry.get("recommended_with") or []) if item],
        "notes": entry.get("notes"),
    }


def anticheat_risk_for_tweak(tweak_id: str) -> dict[str, Any]:
    payload = anticheat_risk_map()
    entries = payload.get("entries") if isinstance(payload.get("entries"), dict) else {}
    entry = entries.get(tweak_id)
    if not isinstance(entry, dict):
        return {
            "anticheat_risk": "unknown",
            "anticheat_details": None,
            "gaming_safe": None,
        }
    return {
        "anticheat_risk": entry.get("anticheat_risk") or "unknown",
        "anticheat_details": entry.get("anticheat_details"),
        "gaming_safe": entry.get("gaming_safe"),
    }
