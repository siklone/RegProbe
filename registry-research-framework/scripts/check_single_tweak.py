#!/usr/bin/env python3
from __future__ import annotations

import argparse
import csv
import json
import re
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[2]
RECORDS_ROOT = REPO_ROOT / "research" / "records"
PROMOTION_GATES_PATH = REPO_ROOT / "research" / "promotion-gates.json"
CATALOG_CSV_PATH = REPO_ROOT / "Docs" / "tweaks" / "tweak-catalog.csv"
APP_SURFACE_PATH = REPO_ROOT / "Docs" / "research" / "app-surface" / "validated-registry-values.json"
SOURCE_SEARCH_ROOTS = ("app", "engine", "cli", "core", "infrastructure")
RUNTIME_EVIDENCE_KINDS = {
    "vm-test",
    "registry-observation",
    "procmon-trace",
    "runtime-trace",
    "runtime-diff",
    "etw-trace",
    "wpr-trace",
}
PROMOTION_STATE_WEIGHT = {
    "promoted": 40,
    "promotion-eligible": 30,
    "blocked": 20,
    "revalidation-pending": 10,
    "rejected": 0,
}


def load_json(path: Path) -> dict[str, Any] | list[Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def normalize_text(value: Any) -> str:
    if value is None:
        return ""
    return str(value).strip()


def normalize_search_text(value: Any) -> str:
    return normalize_text(value).lower()


def value_matches(expected_text: str, actual_value: Any) -> bool:
    expected = normalize_text(expected_text)
    if not expected:
        return False

    if isinstance(actual_value, bool):
        return expected.lower() in {"true", "1"} if actual_value else expected.lower() in {"false", "0"}

    if isinstance(actual_value, int) and not isinstance(actual_value, bool):
        try:
            parsed = int(expected, 0)
            return parsed == actual_value
        except ValueError:
            return normalize_search_text(expected) == normalize_search_text(actual_value)

    if isinstance(actual_value, float):
        try:
            return float(expected) == actual_value
        except ValueError:
            return normalize_search_text(expected) == normalize_search_text(actual_value)

    return normalize_search_text(expected) == normalize_search_text(actual_value)


def flatten_catalog(catalog_csv: Path) -> dict[str, dict[str, str]]:
    if not catalog_csv.exists():
        return {}

    entries: dict[str, dict[str, str]] = {}
    with catalog_csv.open("r", encoding="utf-8", newline="") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            tweak_id = normalize_text(row.get("id"))
            if not tweak_id:
                continue
            entries[tweak_id.lower()] = {key: normalize_text(value) for key, value in row.items()}
    return entries


def flatten_validated_surface(surface_json: Path) -> dict[str, dict[str, Any]]:
    if not surface_json.exists():
        return {}

    payload = load_json(surface_json)
    categories = payload.get("categories") if isinstance(payload, dict) else None
    if not isinstance(categories, dict):
        return {}

    entries: dict[str, dict[str, Any]] = {}
    for category_key, category_payload in categories.items():
        category_name = normalize_text((category_payload or {}).get("name")) or category_key
        for entry in (category_payload or {}).get("entries", []):
            if not isinstance(entry, dict):
                continue
            entry_id = normalize_text(entry.get("id"))
            if not entry_id:
                continue
            copied = dict(entry)
            copied["category_key"] = category_key
            copied["category_name"] = category_name
            entries[entry_id.lower()] = copied
    return entries


def flatten_promotion_gates(gates_json: Path) -> dict[str, dict[str, Any]]:
    if not gates_json.exists():
        return {}

    payload = load_json(gates_json)
    entries = payload.get("entries") if isinstance(payload, dict) else None
    if not isinstance(entries, list):
        return {}

    flattened: dict[str, dict[str, Any]] = {}
    for entry in entries:
        if not isinstance(entry, dict):
            continue
        ids = {
            normalize_search_text(entry.get("candidate_id")),
            normalize_search_text(entry.get("record_id")),
            normalize_search_text(entry.get("tweak_id")),
        }
        for candidate_id in ids:
            if candidate_id:
                flattened[candidate_id] = entry
    return flattened


def collect_record_targets(record: dict[str, Any]) -> list[dict[str, Any]]:
    targets: list[dict[str, Any]] = []
    for section_name, section_targets in (
        ("setting.targets", ((record.get("setting") or {}).get("targets") or [])),
        ("targets", record.get("targets") or []),
    ):
        for target in section_targets:
            if not isinstance(target, dict):
                continue
            copied = dict(target)
            copied["_source"] = section_name
            targets.append(copied)
    return targets


def collect_app_write_targets(record: dict[str, Any]) -> list[dict[str, Any]]:
    app_impl = record.get("app_current_implementation") or {}
    writes = app_impl.get("writes") or []
    results: list[dict[str, Any]] = []
    for write in writes:
        if not isinstance(write, dict):
            continue
        copied = dict(write)
        copied["_source"] = "app_current_implementation.writes"
        results.append(copied)
    return results


def collect_surface_targets(surface_entry: dict[str, Any] | None) -> list[dict[str, Any]]:
    if not isinstance(surface_entry, dict):
        return []

    results: list[dict[str, Any]] = []

    for batch_entry in surface_entry.get("batch_entries") or []:
        if not isinstance(batch_entry, dict):
            continue
        copied = dict(batch_entry)
        copied["_source"] = "validated-surface.batch_entries"
        results.append(copied)

    direct_path = normalize_text(surface_entry.get("path"))
    direct_value_name = normalize_text(surface_entry.get("value_name"))
    if direct_path or direct_value_name:
        results.append(
            {
                "_source": "validated-surface.direct",
                "path": direct_path,
                "value_name": direct_value_name,
                "type": normalize_text(surface_entry.get("type")),
                "target_value": surface_entry.get("recommended_value"),
            }
        )

    for preset in surface_entry.get("presets") or []:
        if not isinstance(preset, dict):
            continue
        for preset_entry in preset.get("entries") or []:
            if not isinstance(preset_entry, dict):
                continue
            copied = dict(preset_entry)
            copied["_source"] = f"validated-surface.preset:{normalize_text(preset.get('key'))}"
            copied["_preset_label"] = normalize_text(preset.get("label"))
            results.append(copied)

    return results


def collect_profile_states(record: dict[str, Any]) -> list[dict[str, Any]]:
    profiles: list[dict[str, Any]] = []

    for default in record.get("windows_defaults") or []:
        if not isinstance(default, dict):
            continue
        profiles.append(
            {
                "profile_type": "windows-default",
                "profile_id": normalize_text(default.get("label")) or "windows-default",
                "label": normalize_text(default.get("label")),
                "states": default.get("states") or [],
            }
        )

    for profile in record.get("recommended_profiles") or []:
        if not isinstance(profile, dict):
            continue
        profiles.append(
            {
                "profile_type": "recommended-profile",
                "profile_id": normalize_text(profile.get("profile_id")),
                "label": normalize_text(profile.get("label")),
                "apply_allowed": bool(profile.get("apply_allowed")),
                "states": profile.get("states") or [],
            }
        )

    return profiles


def find_runtime_read_signals(record: dict[str, Any]) -> list[dict[str, str]]:
    signals: list[dict[str, str]] = []
    for evidence in record.get("evidence") or []:
        if not isinstance(evidence, dict):
            continue
        kind = normalize_text(evidence.get("kind"))
        if kind not in RUNTIME_EVIDENCE_KINDS:
            continue
        signals.append(
            {
                "kind": kind,
                "title": normalize_text(evidence.get("title")),
                "location": normalize_text(evidence.get("location")),
                "summary": normalize_text(evidence.get("summary")),
            }
        )
    return signals


def score_record_match(
    query: str,
    record: dict[str, Any],
    catalog_entry: dict[str, str] | None,
    surface_entry: dict[str, Any] | None,
    exact: bool,
) -> tuple[int, list[str]]:
    normalized_query = normalize_search_text(query)
    reasons: list[str] = []
    score = 0
    matched_structural = False
    matched_any = False

    exact_ids = {
        normalize_search_text(record.get("record_id")),
        normalize_search_text(record.get("tweak_id")),
        normalize_search_text((catalog_entry or {}).get("id")),
        normalize_search_text((surface_entry or {}).get("id")),
    }
    if normalized_query in {item for item in exact_ids if item}:
        score += 150
        reasons.append("query matched record/tweak id")
        matched_structural = True
        matched_any = True
    elif not exact and any(normalized_query in item for item in exact_ids if item):
        score += 90
        reasons.append("query matched record/tweak id fragment")
        matched_structural = True
        matched_any = True

    names = [
        normalize_text((record.get("setting") or {}).get("name")),
        normalize_text((catalog_entry or {}).get("name")),
        normalize_text((surface_entry or {}).get("name")),
    ]
    if any(normalized_query == normalize_search_text(name) for name in names if name):
        score += 110
        reasons.append("query matched setting/card name")
        matched_structural = True
        matched_any = True
    elif not exact and any(normalized_query in normalize_search_text(name) for name in names if name):
        score += 80
        reasons.append("query matched setting/card name fragment")
        matched_structural = True
        matched_any = True

    target_match_score = 0
    for target in collect_record_targets(record) + collect_app_write_targets(record) + collect_surface_targets(surface_entry):
        value_name = normalize_text(target.get("value_name"))
        path = normalize_text(target.get("path"))
        if normalized_query == normalize_search_text(value_name) and value_name:
            target_match_score = max(target_match_score, 100)
            reasons.append(f"query matched value name {value_name}")
            matched_structural = True
            matched_any = True
        elif normalized_query in normalize_search_text(value_name) and value_name and not exact:
            target_match_score = max(target_match_score, 75)
            reasons.append(f"query matched value-name fragment {value_name}")
            matched_structural = True
            matched_any = True

        if normalized_query == normalize_search_text(path) and path:
            target_match_score = max(target_match_score, 90)
            reasons.append(f"query matched registry path {path}")
            matched_structural = True
            matched_any = True
        elif normalized_query in normalize_search_text(path) and path and not exact:
            target_match_score = max(target_match_score, 65)
            reasons.append(f"query matched registry-path fragment {path}")
            matched_structural = True
            matched_any = True

    score += target_match_score

    text_fields = [
        normalize_text(record.get("summary")),
        normalize_text((catalog_entry or {}).get("description")),
        normalize_text((surface_entry or {}).get("description")),
        normalize_text(((record.get("validation_proof") or {}).get("exact_quote_or_path"))),
    ]
    if any(normalized_query == normalize_search_text(field) for field in text_fields if field):
        score += 45
        reasons.append("query matched descriptive text exactly")
        matched_any = True
    elif not exact and any(normalized_query in normalize_search_text(field) for field in text_fields if field):
        score += 20
        reasons.append("query matched descriptive text")
        matched_any = True

    promotion_state = normalize_search_text(((record.get("_promotion_gate") or {}).get("promotion_state")))
    if matched_structural:
        score += PROMOTION_STATE_WEIGHT.get(promotion_state, 0)
    if matched_any and promotion_state:
        reasons.append(f"promotion state weight: {promotion_state}")

    return score, reasons


def extract_expected_value_checks(
    expected_values: list[str],
    record_targets: list[dict[str, Any]],
    app_writes: list[dict[str, Any]],
    surface_targets: list[dict[str, Any]],
    profiles: list[dict[str, Any]],
    validation_proof: dict[str, Any],
) -> list[dict[str, Any]]:
    checks: list[dict[str, Any]] = []
    exact_quote_or_path = normalize_text(validation_proof.get("exact_quote_or_path"))

    for expected in expected_values:
        allowed_value_hits: list[dict[str, Any]] = []
        app_write_hits: list[dict[str, Any]] = []
        surface_hits: list[dict[str, Any]] = []
        profile_hits: list[dict[str, Any]] = []

        for target in record_targets:
            for allowed in target.get("allowed_values") or []:
                if isinstance(allowed, dict) and value_matches(expected, allowed.get("value")):
                    allowed_value_hits.append(
                        {
                            "target_id": normalize_text(target.get("target_id")),
                            "path": normalize_text(target.get("path")),
                            "value_name": normalize_text(target.get("value_name")),
                            "value": allowed.get("value"),
                            "label": normalize_text(allowed.get("label")),
                        }
                    )

        for write in app_writes:
            if value_matches(expected, write.get("value")):
                app_write_hits.append(
                    {
                        "target_id": normalize_text(write.get("target_id")),
                        "path": normalize_text(write.get("path")),
                        "value_name": normalize_text(write.get("value_name")),
                        "value": write.get("value"),
                    }
                )

        for target in surface_targets:
            if value_matches(expected, target.get("target_value")):
                surface_hits.append(
                    {
                        "path": normalize_text(target.get("path")),
                        "value_name": normalize_text(target.get("value_name")),
                        "value": target.get("target_value"),
                        "source": normalize_text(target.get("_source")),
                    }
                )

        for profile in profiles:
            for state in profile.get("states") or []:
                if isinstance(state, dict) and value_matches(expected, state.get("value")):
                    profile_hits.append(
                        {
                            "profile_type": normalize_text(profile.get("profile_type")),
                            "profile_id": normalize_text(profile.get("profile_id")),
                            "label": normalize_text(profile.get("label")),
                            "target_id": normalize_text(state.get("target_id")),
                            "value": state.get("value"),
                        }
                    )

        checks.append(
            {
                "expected_value": expected,
                "matched_in_allowed_values": allowed_value_hits,
                "matched_in_app_writes": app_write_hits,
                "matched_in_surface_targets": surface_hits,
                "matched_in_profiles": profile_hits,
                "matched_in_validation_proof_text": expected in exact_quote_or_path,
                "found_any": bool(
                    allowed_value_hits
                    or app_write_hits
                    or surface_hits
                    or profile_hits
                    or expected in exact_quote_or_path
                ),
            }
        )

    return checks


def scan_source_hits(repo_root: Path, query: str, exact: bool, limit: int = 8) -> list[dict[str, Any]]:
    normalized_query = normalize_search_text(query)
    if not normalized_query:
        return []

    pattern = re.compile(rf"\b{re.escape(query)}\b", re.IGNORECASE) if exact else None
    hits: list[dict[str, Any]] = []

    for relative_root in SOURCE_SEARCH_ROOTS:
        root = repo_root / relative_root
        if not root.exists():
            continue
        for path in root.rglob("*.cs"):
            try:
                lines = path.read_text(encoding="utf-8", errors="ignore").splitlines()
            except OSError:
                continue
            for index, line in enumerate(lines, start=1):
                haystack = normalize_search_text(line)
                matched = bool(pattern.search(line)) if pattern else normalized_query in haystack
                if not matched:
                    continue
                hits.append(
                    {
                        "file": str(path.relative_to(repo_root)),
                        "line": index,
                        "text": line.strip(),
                    }
                )
                if len(hits) >= limit:
                    return hits
    return hits


def build_single_tweak_report(
    query: str,
    *,
    expected_values: list[str] | None = None,
    exact: bool = False,
    limit: int = 5,
    repo_root: Path | None = None,
) -> dict[str, Any]:
    root = repo_root or REPO_ROOT
    promotion_gates_path = root / "research" / "promotion-gates.json"
    catalog_csv_path = root / "Docs" / "tweaks" / "tweak-catalog.csv"
    app_surface_path = root / "Docs" / "research" / "app-surface" / "validated-registry-values.json"
    records_root = root / "research" / "records"
    query = normalize_text(query)
    expected_values = [normalize_text(value) for value in (expected_values or []) if normalize_text(value)]

    report: dict[str, Any] = {
        "query": query,
        "expected_values": expected_values,
        "exact": exact,
        "limit": limit,
        "status": "ok",
        "match_count": 0,
        "matches": [],
        "sources": {
            "promotion_gates": str(promotion_gates_path.relative_to(root)),
            "catalog_csv": str(catalog_csv_path.relative_to(root)),
            "validated_surface": str(app_surface_path.relative_to(root)),
            "records_root": str(records_root.relative_to(root)),
        },
    }

    if not query:
        report["status"] = "error"
        report["error"] = "query must not be empty"
        return report

    catalog = flatten_catalog(catalog_csv_path)
    surface_map = flatten_validated_surface(app_surface_path)
    promotion_gates = flatten_promotion_gates(promotion_gates_path)
    source_hits = scan_source_hits(root, query, exact)

    matches: list[tuple[int, dict[str, Any]]] = []
    for record_path in sorted(records_root.glob("*.json")):
        record = load_json(record_path)
        if not isinstance(record, dict):
            continue

        record_id = normalize_text(record.get("record_id"))
        tweak_id = normalize_text(record.get("tweak_id")) or record_id
        candidate_key = normalize_search_text(record_id or tweak_id)
        promotion_gate = promotion_gates.get(candidate_key) or promotion_gates.get(normalize_search_text(tweak_id))
        catalog_entry = catalog.get(normalize_search_text(tweak_id))
        surface_entry = surface_map.get(normalize_search_text(tweak_id)) or surface_map.get(normalize_search_text(record_id))

        enriched_record = dict(record)
        enriched_record["_promotion_gate"] = promotion_gate or {}
        score, match_reasons = score_record_match(query, enriched_record, catalog_entry, surface_entry, exact)
        if score < 50:
            continue

        record_targets = collect_record_targets(record)
        app_writes = collect_app_write_targets(record)
        surface_targets = collect_surface_targets(surface_entry)
        profiles = collect_profile_states(record)
        validation_proof = record.get("validation_proof") or {}

        match_payload = {
            "candidate_id": normalize_text((promotion_gate or {}).get("candidate_id")) or tweak_id or record_id,
            "record_id": record_id,
            "tweak_id": tweak_id,
            "record_file": str(record_path.relative_to(root)),
            "record_status": normalize_text(record.get("record_status")),
            "promotion_state": normalize_text((promotion_gate or {}).get("promotion_state")),
            "apply_allowed": (
                bool((promotion_gate or {}).get("record_promotion_allowed"))
                if (promotion_gate or {}).get("record_promotion_allowed") is not None
                else bool((promotion_gate or {}).get("apply_allowed"))
            )
            if promotion_gate
            else bool(((record.get("decision") or {}).get("apply_allowed"))),
            "restore_default_supported": bool(((record.get("decision") or {}).get("restore_default_supported"))),
            "restore_previous_supported": bool(((record.get("decision") or {}).get("restore_previous_supported"))),
            "app_mapping_status": normalize_text(((promotion_gate or {}).get("app_mapping_status")))
            or normalize_text(((record.get("app_current_implementation") or {}).get("status"))),
            "catalog_entry": catalog_entry or {},
            "app_surface_entry": {
                "present": bool(surface_entry),
                "category": normalize_text((surface_entry or {}).get("category_name")),
                "name": normalize_text((surface_entry or {}).get("name")),
                "description": normalize_text((surface_entry or {}).get("description")),
                "documentation": normalize_text((surface_entry or {}).get("documentation")),
            },
            "record_summary": normalize_text(record.get("summary")),
            "record_targets": record_targets,
            "app_write_targets": app_writes,
            "surface_targets": surface_targets,
            "windows_and_recommended_profiles": profiles,
            "validation_proof": {
                "source_url": normalize_text(validation_proof.get("source_url")),
                "exact_quote_or_path": normalize_text(validation_proof.get("exact_quote_or_path")),
                "key_found_on_page": bool(validation_proof.get("key_found_on_page")),
                "notes": normalize_text(validation_proof.get("notes")),
            },
            "decision_notes": {
                "why": normalize_text((record.get("decision") or {}).get("why")),
            },
            "app_implementation_notes": normalize_text(((record.get("app_current_implementation") or {}).get("notes"))),
            "runtime_read_signals": find_runtime_read_signals(record),
            "evidence": record.get("evidence") or [],
            "expected_value_checks": extract_expected_value_checks(
                expected_values,
                record_targets,
                app_writes,
                surface_targets,
                profiles,
                validation_proof,
            ),
            "match_reasons": sorted(set(match_reasons)),
            "score": score,
        }

        preferred_locations = {
            normalize_text((catalog_entry or {}).get("source")),
            normalize_text(((record.get("app_current_implementation") or {}).get("provider_source"))),
        }
        preferred_locations.update(
            normalize_text(item.get("location"))
            for item in (record.get("evidence") or [])
            if isinstance(item, dict) and normalize_text(item.get("kind")) == "repo-code"
        )
        code_hits: list[dict[str, Any]] = []
        seen_code_hits: set[tuple[str, int]] = set()
        for location in sorted(location for location in preferred_locations if location):
            file_path = location.split("#", 1)[0]
            if not file_path.endswith(".cs"):
                continue
            path = root / file_path
            if not path.exists():
                continue
            snippet = None
            line_number = 0
            anchor = location.split("#", 1)[1] if "#" in location else ""
            if anchor.startswith("L"):
                try:
                    line_number = int(anchor[1:])
                except ValueError:
                    line_number = 0
            if line_number > 0:
                lines = path.read_text(encoding="utf-8", errors="ignore").splitlines()
                if 1 <= line_number <= len(lines):
                    snippet = lines[line_number - 1].strip()
            code_hits.append(
                {
                    "file": file_path,
                    "line": line_number,
                    "text": snippet or "",
                    "source": "declared-location",
                }
            )
            seen_code_hits.add((file_path, line_number))

        for hit in source_hits:
            key = (normalize_text(hit.get("file")), int(hit.get("line") or 0))
            if key in seen_code_hits:
                continue
            code_hits.append({**hit, "source": "source-scan"})
            seen_code_hits.add(key)
            if len(code_hits) >= 8:
                break

        match_payload["code_hits"] = code_hits
        matches.append((score, match_payload))

    matches.sort(
        key=lambda item: (
            -item[0],
            normalize_search_text(item[1].get("promotion_state")),
            normalize_search_text(item[1].get("record_id")),
        )
    )
    trimmed_matches = [payload for _, payload in matches[: max(1, limit)]]

    report["match_count"] = len(matches)
    report["matches"] = trimmed_matches
    if not matches:
        report["status"] = "no-match"
    return report


def render_single_tweak_report(report: dict[str, Any]) -> str:
    lines: list[str] = []
    lines.append(f"Query: {report.get('query')}")
    if report.get("expected_values"):
        lines.append(f"Expected values: {', '.join(report['expected_values'])}")
    lines.append(f"Status: {report.get('status')}")
    lines.append(f"Matches: {report.get('match_count')}")

    matches = report.get("matches") or []
    if not matches:
        lines.append("No matching record, tweak, value name, or registry path was found.")
        return "\n".join(lines)

    for index, match in enumerate(matches, start=1):
        lines.append("")
        lines.append(f"[{index}] {match.get('candidate_id')}")
        lines.append(
            "  promotion: "
            f"{match.get('promotion_state') or 'unknown'} | "
            f"record: {match.get('record_status') or 'unknown'} | "
            f"apply_allowed: {str(bool(match.get('apply_allowed'))).lower()}"
        )
        lines.append(
            "  rollback: "
            f"restore_default={str(bool(match.get('restore_default_supported'))).lower()} | "
            f"restore_previous={str(bool(match.get('restore_previous_supported'))).lower()}"
        )
        lines.append(f"  record_file: {match.get('record_file')}")

        catalog = match.get("catalog_entry") or {}
        if catalog:
            lines.append(f"  app_card: {catalog.get('name')} [{catalog.get('category')}/{catalog.get('area')}]")
            lines.append(f"  card_description: {catalog.get('description')}")

        surface = match.get("app_surface_entry") or {}
        lines.append(
            "  research_surface: "
            f"{'present' if surface.get('present') else 'missing'}"
            + (f" ({surface.get('category')})" if surface.get("category") else "")
        )
        if surface.get("documentation"):
            lines.append(f"  research_doc: {surface.get('documentation')}")

        lines.append(f"  summary: {match.get('record_summary')}")

        record_targets = match.get("record_targets") or []
        if record_targets:
            lines.append("  tracked_targets:")
            for target in record_targets:
                lines.append(
                    "    - "
                    f"{target.get('path')} :: {target.get('value_name')} [{target.get('value_type')}]"
                )
                allowed_values = target.get("allowed_values") or []
                for allowed in allowed_values:
                    if not isinstance(allowed, dict):
                        continue
                    lines.append(
                        "      * "
                        f"{allowed.get('value')} -> {allowed.get('label') or allowed.get('meaning')}"
                    )

        app_writes = match.get("app_write_targets") or []
        if app_writes:
            lines.append("  app_writes:")
            for write in app_writes:
                lines.append(
                    "    - "
                    f"{write.get('path')} :: {write.get('value_name')} = {write.get('value')}"
                )

        validation_proof = match.get("validation_proof") or {}
        if any(validation_proof.values()):
            lines.append("  validation_proof:")
            if validation_proof.get("source_url"):
                lines.append(f"    - source_url: {validation_proof.get('source_url')}")
            if validation_proof.get("exact_quote_or_path"):
                lines.append(f"    - exact_quote_or_path: {validation_proof.get('exact_quote_or_path')}")
            lines.append(
                "    - key_found_on_page: "
                f"{str(bool(validation_proof.get('key_found_on_page'))).lower()}"
            )

        runtime_signals = match.get("runtime_read_signals") or []
        if runtime_signals:
            lines.append("  runtime_read_signals:")
            for signal in runtime_signals[:4]:
                lines.append(
                    "    - "
                    f"{signal.get('kind')}: {signal.get('title')} ({signal.get('location')})"
                )
        else:
            lines.append("  runtime_read_signals: no direct runtime read artifact linked on this record")

        for expected_check in match.get("expected_value_checks") or []:
            lines.append(
                "  expected_value "
                f"{expected_check.get('expected_value')}: "
                f"{'matched' if expected_check.get('found_any') else 'not found'}"
            )
            if expected_check.get("matched_in_allowed_values"):
                lines.append("    - allowed_values:")
                for hit in expected_check["matched_in_allowed_values"]:
                    lines.append(
                        "      * "
                        f"{hit.get('value_name')} = {hit.get('value')} ({hit.get('label')})"
                    )
            if expected_check.get("matched_in_app_writes"):
                lines.append("    - app_writes:")
                for hit in expected_check["matched_in_app_writes"]:
                    lines.append(
                        "      * "
                        f"{hit.get('value_name')} = {hit.get('value')}"
                    )
            if expected_check.get("matched_in_profiles"):
                lines.append("    - profiles:")
                for hit in expected_check["matched_in_profiles"]:
                    lines.append(
                        "      * "
                        f"{hit.get('label')} -> {hit.get('target_id')} = {hit.get('value')}"
                    )
            if expected_check.get("matched_in_validation_proof_text"):
                lines.append("    - validation_proof_text: matched")

        code_hits = match.get("code_hits") or []
        if code_hits:
            lines.append("  code_hits:")
            for hit in code_hits[:6]:
                line_suffix = f":{hit.get('line')}" if hit.get("line") else ""
                text = normalize_text(hit.get("text"))
                lines.append(
                    "    - "
                    f"{hit.get('file')}{line_suffix}"
                    + (f" -> {text}" if text else "")
                )

    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Inspect one tweak, record, registry value, or path across research records, app surface, and code mappings."
    )
    parser.add_argument("query", help="Tweak id, record id, registry value name, or registry path fragment")
    parser.add_argument(
        "--expected-value",
        dest="expected_values",
        action="append",
        default=[],
        help="Optional value to verify against tracked targets, app writes, and profile states. Repeat as needed.",
    )
    parser.add_argument("--exact", action="store_true", help="Require exact token matches instead of substring matches.")
    parser.add_argument("--limit", type=int, default=5, help="Maximum match count to emit (default: 5).")
    parser.add_argument("--json", action="store_true", help="Emit the report as JSON.")
    args = parser.parse_args()

    report = build_single_tweak_report(
        args.query,
        expected_values=args.expected_values,
        exact=args.exact,
        limit=max(1, args.limit),
    )
    if args.json:
        print(json.dumps(report, indent=2))
    else:
        print(render_single_tweak_report(report))
    return 0 if report.get("status") != "error" else 1


if __name__ == "__main__":
    raise SystemExit(main())
