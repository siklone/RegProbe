from __future__ import annotations

import json
from pathlib import Path
from typing import Any

REDACTED_USER = "<USER>"
HOME_PATH = str(Path.home())
HOME_PATH_FWD = HOME_PATH.replace("\\", "/")
USER_PATH_REPLACEMENTS = {
    HOME_PATH: HOME_PATH.replace(Path.home().name, REDACTED_USER),
    HOME_PATH_FWD: HOME_PATH_FWD.replace(Path.home().name, REDACTED_USER),
}

CLASS_DEFINITIONS: dict[str, dict[str, str]] = {
    "A": {
        "label": "Class A",
        "title": "App Ready",
        "description": "Key exists, app-exposed values have trusted meanings, validation proof is present, the app mapping matches research, and the restore/default story is known.",
    },
    "B": {
        "label": "Class B",
        "title": "Strong but Partial",
        "description": "Key and primary values are known, but the record is still gated from one-click apply or has incomplete edge semantics.",
    },
    "C": {
        "label": "Class C",
        "title": "Key Known, Value Model Partial",
        "description": "The key existence is solid and some values are known, but value behavior still needs VM diff, benchmark work, or tighter app mapping.",
    },
    "D": {
        "label": "Class D",
        "title": "Key Known, Value Semantics Unknown",
        "description": "The key existence is proven, but the value semantics are not trusted enough yet for an app-ready surface.",
    },
    "E": {
        "label": "Class E",
        "title": "Archived / Audit Trail",
        "description": "Deprecated, blocked, or historical audit-trail record. Keep it in research, not in the normal tweak surface.",
    },
}

RUNTIME_EVIDENCE_KINDS = {
    "procmon-trace",
    "runtime-diff",
    "runtime-trace",
    "vm-test",
    "registry-observation",
}

OFFICIAL_EVIDENCE_KINDS = {
    "official-doc",
    "policy-csp",
    "troubleshoot-doc",
}

PROCMON_EVIDENCE_KINDS = {
    "procmon-trace",
}

GHIDRA_EVIDENCE_KINDS = {
    "decompilation",
    "ghidra-headless",
    "ghidra-trace",
}

WPR_EVIDENCE_KINDS = {
    "wpr-trace",
    "etw-trace",
}

BENCHMARK_EVIDENCE_KINDS = {
    "runtime-benchmark",
}

SEMANTICS_EVIDENCE_KINDS = {
    "official-doc",
    "policy-csp",
    "troubleshoot-doc",
    "repo-doc",
    "repo-code",
    "decompilation",
}

MICROSOFT_SOURCE_HINTS = (
    "learn.microsoft.com",
    "support.microsoft.com",
    "techcommunity.microsoft.com",
    "C:\\Windows\\PolicyDefinitions\\",
    "C:/Windows/PolicyDefinitions/",
)

EARLY_BOOT_PATH_HINTS = (
    "\\system\\currentcontrolset\\services\\",
    "\\controlset001\\services\\",
    "\\control\\session manager\\kernel",
    "\\control\\prioritycontrol",
    "\\control\\graphicsdrivers",
    "\\control\\power",
    "\\services\\stornvme\\",
    "\\services\\usbhub3\\",
)

PERF_HINTS = (
    "benchmark",
    "winsat",
    "diskspd",
    "aida64",
    "cinebench",
    "latency",
    "throughput",
    "fps",
    "dpc",
    "startup",
    "fullscreen",
    "prioritycontrol",
    "paging executive",
    "power throttling",
    "idle states",
    "mmcss",
    "kernel",
    "memory",
    "graphics",
    "network",
    "defender",
)

REBOOT_HINTS = (
    "reboot",
    "restart",
    "lastbootuptime",
    "after boot",
    "after reboot",
    "boot diff",
    "booted",
)


def sanitize_text(value: Any) -> Any:
    if isinstance(value, str):
        text = value
        for source, replacement in USER_PATH_REPLACEMENTS.items():
            text = text.replace(source, replacement)
        return text
    return value


def sanitize_value(value: Any) -> Any:
    if isinstance(value, dict):
        return {key: sanitize_value(item) for key, item in value.items()}
    if isinstance(value, list):
        return [sanitize_value(item) for item in value]
    return sanitize_text(value)


def truncate_text(value: str | None, limit: int = 220) -> str:
    text = sanitize_text(value) if value else ""
    if len(text) <= limit:
        return text
    return text[: limit - 3].rstrip() + "..."


def bool_value(value: Any) -> bool:
    return value is True


def load_json(path: Path) -> Any:
    with path.open("r", encoding="utf-8-sig") as handle:
        return json.load(handle)


def load_provenance_map(path: Path) -> dict[str, dict[str, Any]]:
    if not path.exists():
        return {}

    payload = load_json(path)
    entries = payload.get("Entries") or payload.get("entries") or []
    result: dict[str, dict[str, Any]] = {}
    for entry in entries:
        if not isinstance(entry, dict):
            continue
        entry_id = entry.get("Id") or entry.get("id")
        if entry_id:
            result[str(entry_id)] = entry
    return result


def load_overrides(path: Path) -> dict[str, dict[str, Any]]:
    if not path.exists():
        return {}

    payload = load_json(path)
    entries = payload.get("entries") or []
    result: dict[str, dict[str, Any]] = {}
    for entry in entries:
        if not isinstance(entry, dict):
            continue
        key = entry.get("record_id") or entry.get("tweak_id")
        if key:
            result[str(key)] = entry
    return result


def evidence_kind(item: dict[str, Any]) -> str:
    return str(item.get("kind") or "").strip()


def validation_proof(record: dict[str, Any]) -> dict[str, Any]:
    proof = record.get("validation_proof")
    if isinstance(proof, dict):
        return proof
    return {}


def evidence_items(record: dict[str, Any]) -> list[dict[str, Any]]:
    return [item for item in record.get("evidence", []) or [] if isinstance(item, dict)]


def evidence_kinds(record: dict[str, Any]) -> set[str]:
    return {evidence_kind(item) for item in evidence_items(record) if evidence_kind(item)}


def target_paths(record: dict[str, Any]) -> list[str]:
    return [
        str(target.get("path") or "")
        for target in record.get("setting", {}).get("targets", []) or []
        if isinstance(target, dict)
    ]


def record_text_blob(record: dict[str, Any]) -> str:
    values: list[str] = []
    for key in ("record_id", "summary"):
        value = record.get(key)
        if value:
            values.append(str(value))

    setting = record.get("setting") or {}
    for key in ("name", "category", "area", "scope", "professional_notes", "tradeoffs_overview"):
        value = setting.get(key)
        if value:
            values.append(str(value))

    decision = record.get("decision") or {}
    why = decision.get("why")
    if why:
        values.append(str(why))

    proof = validation_proof(record)
    for key in ("source_url", "exact_quote_or_path", "notes"):
        value = proof.get(key)
        if value:
            values.append(str(value))

    for item in evidence_items(record):
        for key in ("title", "location", "summary"):
            value = item.get(key)
            if value:
                values.append(str(value))

    return " ".join(values).lower()


def source_looks_microsoft(source: str) -> bool:
    normalized = source.strip().lower()
    if not normalized:
        return False
    return any(hint.lower() in normalized for hint in MICROSOFT_SOURCE_HINTS)


def has_unknown_state(record: dict[str, Any]) -> bool:
    for target in record.get("setting", {}).get("targets", []) or []:
        for allowed in target.get("allowed_values", []) or []:
            if allowed.get("state_kind") == "unknown":
                return True
    return False


def has_runtime_evidence(record: dict[str, Any]) -> bool:
    return any(evidence_kind(item) in RUNTIME_EVIDENCE_KINDS for item in evidence_items(record))


def has_semantics_evidence(record: dict[str, Any]) -> bool:
    return any(evidence_kind(item) in SEMANTICS_EVIDENCE_KINDS for item in evidence_items(record))


def has_official_evidence(record: dict[str, Any]) -> bool:
    if evidence_kinds(record) & OFFICIAL_EVIDENCE_KINDS:
        return True
    return source_looks_microsoft(str(validation_proof(record).get("source_url") or ""))


def has_procmon_evidence(record: dict[str, Any]) -> bool:
    return bool(evidence_kinds(record) & PROCMON_EVIDENCE_KINDS)


def has_ghidra_evidence(record: dict[str, Any]) -> bool:
    return bool(evidence_kinds(record) & GHIDRA_EVIDENCE_KINDS)


def has_wpr_evidence(record: dict[str, Any]) -> bool:
    return bool(evidence_kinds(record) & WPR_EVIDENCE_KINDS)


def has_benchmark_evidence(record: dict[str, Any]) -> bool:
    if evidence_kinds(record) & BENCHMARK_EVIDENCE_KINDS:
        return True
    blob = record_text_blob(record)
    return any(keyword in blob for keyword in ("winsat", "diskspd", "aida64", "cinebench", "benchmark"))


def has_reboot_evidence(record: dict[str, Any]) -> bool:
    blob = record_text_blob(record)
    return any(keyword in blob for keyword in REBOOT_HINTS)


def is_early_boot_record(record: dict[str, Any]) -> bool:
    paths = " ".join(target_paths(record)).lower()
    return any(hint in paths for hint in EARLY_BOOT_PATH_HINTS)


def is_perf_sensitive_record(record: dict[str, Any]) -> bool:
    blob = record_text_blob(record)
    return any(hint in blob for hint in PERF_HINTS)


def determine_evidence_lane(record: dict[str, Any]) -> str:
    if has_official_evidence(record):
        return "official-policy"
    if is_early_boot_record(record):
        return "early-boot"
    if is_perf_sensitive_record(record):
        return "system"
    return "runtime"


def has_converged_vm_evidence(record: dict[str, Any]) -> bool:
    lane = determine_evidence_lane(record)
    if lane == "early-boot":
        signals = [
            has_ghidra_evidence(record),
            has_reboot_evidence(record),
            has_wpr_evidence(record),
        ]
        return sum(1 for item in signals if item) >= 2

    if not has_procmon_evidence(record):
        return False
    if not has_ghidra_evidence(record):
        return False
    if lane == "system":
        return has_wpr_evidence(record) or has_benchmark_evidence(record) or has_reboot_evidence(record)
    return True


def restore_story_known(record: dict[str, Any]) -> bool:
    decision = record.get("decision") or {}
    return bool_value(decision.get("restore_default_supported")) or bool_value(decision.get("restore_previous_supported")) or bool(record.get("windows_defaults"))


def extract_app_status(record: dict[str, Any]) -> str:
    app = record.get("app_current_implementation") or {}
    return str(app.get("status") or "unknown")


def summarize_links(items: list[dict[str, Any]], limit: int = 2) -> list[dict[str, str]]:
    links: list[dict[str, str]] = []
    seen: set[tuple[str, str]] = set()
    for item in items:
        title = truncate_text(item.get("title") or item.get("Title") or "Reference", 96)
        url = sanitize_text(item.get("location") or item.get("url") or item.get("Url") or "")
        if not url:
            continue
        key = (title, url)
        if key in seen:
            continue
        seen.add(key)
        links.append(
            {
                "title": title,
                "url": url,
                "kind": str(item.get("kind") or item.get("Kind") or ""),
                "summary": truncate_text(item.get("summary") or item.get("Summary"), 180),
            }
        )
        if len(links) >= limit:
            break
    return sanitize_value(links)


def build_validated_semantics_block(record: dict[str, Any]) -> dict[str, Any]:
    proof = validation_proof(record)
    semantics_evidence = [
        item
        for item in evidence_items(record)
        if evidence_kind(item) in SEMANTICS_EVIDENCE_KINDS
    ]

    if proof:
        summary = (
            truncate_text(proof.get("notes"), 260)
            or truncate_text(proof.get("exact_quote_or_path"), 260)
            or "Validation proof is present for this record."
        )
    elif semantics_evidence:
        summary = truncate_text(semantics_evidence[0].get("summary"), 260) or "Semantics evidence exists, but no dedicated validation proof block is attached."
    else:
        summary = "No dedicated semantics proof block is attached yet."

    links: list[dict[str, str]] = []
    source_url = sanitize_text(proof.get("source_url") or "")
    if source_url:
        links.append(
            {
                "title": "Validation proof source",
                "url": source_url,
                "kind": "validation-proof",
                "summary": truncate_text(proof.get("exact_quote_or_path"), 180),
            }
        )
    links.extend(summarize_links(semantics_evidence, limit=2))

    return sanitize_value(
        {
            "summary": summary,
            "has_validation_proof": bool(proof),
            "has_semantics_evidence": bool(semantics_evidence),
            "links": links[:3],
        }
    )


def build_runtime_proof_block(record: dict[str, Any]) -> dict[str, Any]:
    runtime_evidence = [
        item
        for item in evidence_items(record)
        if evidence_kind(item) in RUNTIME_EVIDENCE_KINDS
    ]
    decision = record.get("decision") or {}

    if runtime_evidence:
        summary = truncate_text(runtime_evidence[0].get("summary"), 260) or "Runtime proof is attached to this record."
    elif bool_value(decision.get("needs_vm_validation")):
        summary = "No runtime proof is attached yet. This record still needs VM validation."
    else:
        summary = "No dedicated runtime capture is attached. This record currently relies on documented semantics and app-mapping evidence."

    return sanitize_value(
        {
            "summary": summary,
            "needs_vm_validation": bool_value(decision.get("needs_vm_validation")),
            "has_runtime_evidence": bool(runtime_evidence),
            "links": summarize_links(runtime_evidence, limit=3),
        }
    )


def build_upstream_lineage_block(record: dict[str, Any], provenance_entry: dict[str, Any] | None) -> dict[str, Any]:
    provenance = provenance_entry or {}
    references = provenance.get("References") or provenance.get("references") or []
    nohuto_refs = [
        ref
        for ref in references
        if str(ref.get("Kind") or ref.get("kind") or "").strip().lower() == "nohuto"
    ]
    source_repositories = provenance.get("SourceRepositories") or provenance.get("source_repositories") or []

    if nohuto_refs:
        summary = "Upstream dump / pseudocode links are attached to this record. They show discovery and naming only, not value semantics."
    elif source_repositories:
        summary = "Repo-level source links are attached, but there is no explicit nohuto reference block on this record."
    else:
        summary = "No upstream nohuto source link is attached to this record."

    return sanitize_value(
        {
            "summary": summary,
            "has_nohuto_lineage": bool(nohuto_refs),
            "source_repositories": source_repositories,
            "links": summarize_links(nohuto_refs, limit=3),
        }
    )


def next_missing_layer(record: dict[str, Any], incident_seen: bool = False) -> str:
    decision = record.get("decision") or {}
    app_status = extract_app_status(record)
    if str(record.get("record_status") or "").strip().lower() == "deprecated":
        return "archived"
    if not validation_proof(record):
        return "validation-proof"
    if app_status != "matches-research":
        return "app-mapping"
    if not restore_story_known(record):
        return "restore-story"
    if incident_seen:
        return "incident-review"

    lane = determine_evidence_lane(record)
    if lane == "official-policy":
        if not has_official_evidence(record):
            return "official-doc"
        if not bool_value(decision.get("apply_allowed")):
            return "decision-gate"
        return "none"

    if lane == "early-boot":
        if not has_ghidra_evidence(record):
            return "ghidra"
        if not has_reboot_evidence(record):
            return "reboot-diff"
        if not has_wpr_evidence(record):
            return "wpr-or-ttd"
        if not bool_value(decision.get("apply_allowed")):
            return "decision-gate"
        return "none"

    if not has_procmon_evidence(record):
        return "procmon"
    if not has_ghidra_evidence(record):
        return "ghidra"
    if lane == "system" and not (has_wpr_evidence(record) or has_benchmark_evidence(record)):
        return "wpr-or-benchmark"
    if not bool_value(decision.get("apply_allowed")):
        return "decision-gate"
    return "none"


def build_gating_reason(class_id: str, record: dict[str, Any]) -> str:
    decision = record.get("decision") or {}
    app_status = extract_app_status(record)
    if class_id == "A":
        return "This record is app-ready and can stay one-click actionable."
    if class_id == "B":
        missing = next_missing_layer(record)
        if not bool_value(decision.get("apply_allowed")):
            return "Primary values are understood, but this record is still intentionally gated from one-click apply."
        if not restore_story_known(record):
            return "Semantics are strong, but the restore/default story is still incomplete."
        if missing not in {"none", "decision-gate"}:
            return f"This record is strong enough to show, but it still needs a tighter {missing} layer before it becomes Class A."
        return "This record is strong enough to show, but it still needs a tighter policy edge or app contract before it becomes Class A."
    if class_id == "C":
        if app_status in {"not-mapped", "partially-matches"}:
            return "The key is understood, but the app mapping is still partial or indirect."
        return "The key is known, but the value model still needs VM diff, benchmark work, or a cleaner runtime story."
    if class_id == "D":
        return "The key exists, but the value semantics are still too weak or ambiguous for an app-ready surface."
    return "Archived audit trail only. Keep this out of the normal tweak surface."


def derive_class_id(record: dict[str, Any]) -> str:
    record_status = str(record.get("record_status") or "unknown")
    if record_status == "deprecated":
        return "E"

    decision = record.get("decision") or {}
    has_validation = bool(validation_proof(record))
    app_status = extract_app_status(record)
    apply_allowed = bool_value(decision.get("apply_allowed"))
    confidence = str(decision.get("confidence") or "").lower()
    has_blockers = bool(decision.get("blocking_issues"))
    needs_vm = bool_value(decision.get("needs_vm_validation"))
    unknown_state = has_unknown_state(record)

    if (
        has_validation
        and app_status == "matches-research"
        and apply_allowed
        and confidence == "high"
        and restore_story_known(record)
        and not has_blockers
        and not needs_vm
        and not unknown_state
        and (has_official_evidence(record) or has_converged_vm_evidence(record))
    ):
        return "A"

    if unknown_state or app_status in {"unknown", "mismatch-suspected"} or not has_validation:
        return "D"

    if app_status == "matches-research" and restore_story_known(record):
        return "B"

    return "C"


def build_class_entry(
    record: dict[str, Any],
    provenance_entry: dict[str, Any] | None = None,
    override: dict[str, Any] | None = None,
) -> dict[str, Any]:
    class_id = derive_class_id(record)
    if override and override.get("evidence_class") in CLASS_DEFINITIONS:
        class_id = str(override["evidence_class"])

    definition = CLASS_DEFINITIONS[class_id]
    decision = record.get("decision") or {}
    app_status = extract_app_status(record)
    actionable = class_id == "A"
    show_in_app = class_id != "E"

    gating_reason = truncate_text(build_gating_reason(class_id, record), 220)
    if override and override.get("gating_reason"):
        gating_reason = truncate_text(str(override["gating_reason"]), 220)

    return sanitize_value(
        {
            "record_id": record.get("record_id"),
            "tweak_id": record.get("tweak_id"),
            "record_status": record.get("record_status"),
            "evidence_class": class_id,
            "class_label": definition["label"],
            "class_title": definition["title"],
            "class_description": definition["description"],
            "show_in_app": show_in_app,
            "is_actionable": actionable,
            "is_archived": class_id == "E",
            "action_state": "actionable" if actionable else ("archived" if class_id == "E" else "research-gated"),
            "gating_reason": gating_reason,
            "confidence": decision.get("confidence"),
            "app_mapping_status": app_status,
            "restore_story_known": restore_story_known(record),
            "validated_semantics": build_validated_semantics_block(record),
            "runtime_proof": build_runtime_proof_block(record),
            "upstream_lineage": build_upstream_lineage_block(record, provenance_entry),
        }
    )
