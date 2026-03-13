#!/usr/bin/env python3
from __future__ import annotations

import json
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
RESEARCH_ROOT = REPO_ROOT / "Docs" / "tweaks" / "research"
RECORDS_ROOT = RESEARCH_ROOT / "records"
OUTPUT_JSON = RESEARCH_ROOT / "review-required-backlog.json"
FIX_OUTPUT_JSON = RESEARCH_ROOT / "fix-backlog.json"
RENAME_OUTPUT_JSON = RESEARCH_ROOT / "rename-backlog.json"
SPLIT_OUTPUT_JSON = RESEARCH_ROOT / "split-backlog.json"
DOCS_FIRST_OUTPUT_JSON = RESEARCH_ROOT / "docs-first-backlog.json"
ROADMAP_OUTPUT_JSON = RESEARCH_ROOT / "roadmap-implementation.json"

VALID_LABELS = (
    "implementation-mismatch",
    "rename",
    "split-needed",
    "docs-first",
)

PROOF_GATED_SCHEMA_VERSIONS = {"1.1"}

MANUAL_LABELS = {
    "rename": {
        "developer.enable-windows-long-paths",
        "network.require-ntlm-ssp-client-session-security",
        "privacy.set-diagnostic-data-to-minimum-supported-level",
        "privacy.turn-off-sync-by-default-allow-user-override",
    },
    "split-needed": {
        "audio.disable-system-sounds",
        "network.optimize-smb",
        "power.disable-network-power-saving",
        "power.optimize-performance",
        "privacy.deny-app-access",
        "privacy.disable-app-suggestions",
        "privacy.disable-application-compatibility",
        "privacy.disable-application-compatibility.tasks",
        "privacy.disable-ceip",
        "privacy.disable-cross-device-experiences",
        "privacy.disable-inking-typing-personalization",
        "privacy.disable-offline-files",
        "privacy.disable-offline-files.binary",
        "privacy.disable-offline-files.services",
        "privacy.disable-offline-files.tasks",
        "privacy.disable-sleep-study-diagnostics",
        "privacy.disable-suggestions",
        "privacy.disable-suggestions-cdm",
        "privacy.disable-windows-tips",
        "privacy.disable-wmplayer-telemetry",
        "privacy.troubleshooter-dont-run",
        "security.disable-vbs",
        "security.disable-windows-update",
        "system.disable-scheduled-tasks",
        "system.reduce-shutdown-timeouts",
    },
    "docs-first": {
        "audio.disable-beep",
        "audio.disable-spatial-audio",
        "audio.show-disconnected-devices",
        "audio.show-hidden-devices",
        "developer.docker-performance",
        "developer.ssh-agent-autostart",
        "developer.terminal-dev-mode",
        "developer.vs-intellisense-cache",
        "developer.vs-solution-load",
        "developer.vscode-git-autofetch",
        "explorer.show-full-path",
        "explorer.enable-explorer-compact-mode",
        "notifications.disable-feedback-frequency",
        "performance.disable-animations",
        "performance.disable-menu-show-delay",
        "performance.disable-taskbar-animations",
        "peripheral.autoplay-take-no-action",
        "peripheral.disable-sticky-keys-prompt",
        "privacy.disable-app-launch-tracking",
        "privacy.disable-f1-help",
        "privacy.disable-language-list-access",
        "privacy.disable-location-consent-system",
        "security.trusted-path-credential-prompting",
        "system.bsod-display-parameters",
        "system.disable-auto-maintenance",
        "system.disable-fullscreen-optimizations",
        "system.disable-jpeg-reduction",
        "system.disable-restartable-apps",
        "system.disable-service-splitting",
        "system.disable-startup-delay",
        "system.dwm-disable-mpo",
        "system.enable-game-mode",
        "system.enable-hags",
        "system.graphics-disable-overlays",
        "system.graphics-page-fault-debug-mode",
        "system.kernel-adjust-dpc-threshold",
        "system.kernel-cache-aware-scheduling",
        "system.kernel-default-dynamic-hetero-cpu-policy",
        "system.kernel-disable-low-qos-timer-resolution",
        "system.kernel-dpc-queue-depth",
        "system.kernel-dpc-watchdog-period",
        "system.kernel-ideal-dpc-rate",
        "system.kernel-minimum-dpc-rate",
        "system.kernel-serialize-timer-expiration",
        "system.memory-large-system-cache-client",
        "system.priority-control",
        "system.services.disable-sysmain",
        "system.services.disable-wap-push-routing",
        "system.services.disable-windows-error-reporting",
        "visibility.hide-language-bar",
        "visibility.restore-classic-context-menu",
    },
}

MANUAL_LOOKUP = {
    record_id: label
    for label, record_ids in MANUAL_LABELS.items()
    for record_id in record_ids
}

RENAME_KEYWORDS = (
    "scope and naming",
    "naming accuracy",
    "tweak name",
    "name suggests",
    "name overstates",
    "scope are aligned",
    "label collapses",
)

SPLIT_KEYWORDS = (
    "top-level toggle",
    "composite",
    "bundle",
    "bundles",
    "combined",
    "combines",
    "mixes",
    "mixed batch",
    "mixed registry bundle",
    "task-by-task",
    "split into",
    "split or narrowed",
    "parent toggle",
    "preset model",
    "multiple windows feature areas",
    "multiple policy families",
    "broader than the official policy surface",
)

DOCS_FIRST_KEYWORDS = (
    "observed implementation only",
    "not an official policy surface",
    "runtime consent-store",
    "needs stronger provenance",
    "did not capture a primary microsoft source",
    "did not capture a primary",
    "no primary microsoft source",
    "not backed by a primary",
    "not yet backed by a primary",
    "feature area is well understood",
    "known workaround",
    "obsolete and unsupported",
)

IMPLEMENTATION_MISMATCH_KEYWORDS = (
    "instead writes",
    "does not match",
    "do not match",
    "do not line up",
    "does not line up",
    "path appears to disagree",
    "writes only",
    "does not use those policy values",
    "non-policy preference value",
    "different path",
    "different current-user",
    "extra runtime",
    "extra winlogon",
    "non-policy pause timestamp",
    "runtime path",
    "policymanager",
)

LABEL_EXPLANATIONS = {
    "implementation-mismatch": "The shipped tweak writes the wrong path, value, or surface for the researched behavior and should be realigned before publish/apply.",
    "rename": "The current UX name or scope over-promises, under-scopes, or points in the wrong direction even if part of the implementation is real.",
    "split-needed": "The current tweak bundles multiple surfaces or sub-features and should be broken into narrower controls before validation.",
    "docs-first": "The current record is useful as reference, but the evidence or supported control surface is not strong enough for a normal apply path yet.",
}

ACTION_HINTS = {
    "implementation-mismatch": "Align the implementation to the official or explicitly documented surface, or narrow the tweak to the behavior the app actually writes.",
    "rename": "Rename the tweak and UX copy so the title, description, and risk text match the actual scope and behavior.",
    "split-needed": "Break the tweak into smaller records or controls before validation so each surface can be sourced and tested separately.",
    "docs-first": "Keep this as research/reference-only until a stronger primary source or supported control surface is captured.",
}

RENAME_SUGGESTIONS = {
    "developer.enable-windows-long-paths": "Enable Windows Long Paths",
    "network.require-ntlm-ssp-client-session-security": "Require NTLM SSP Client Session Security",
    "privacy.set-diagnostic-data-to-minimum-supported-level": "Set Diagnostic Data to Minimum Supported Level",
    "privacy.turn-off-sync-by-default-allow-user-override": "Turn Off Sync by Default (Allow User Override)",
}

SPLIT_PLANS = {
    "audio.disable-system-sounds": {
        "estimated_split_parts": 3,
        "suggested_splits": [
            "System event sound assignments",
            "Notification sound assignments",
            "Explorer and shell sound assignments",
        ],
    },
    "network.optimize-smb": {
        "estimated_split_parts": 3,
        "suggested_splits": [
            "SMB metadata cache tuning",
            "SMB command/window tuning",
            "SMB throttle or timeout tuning",
        ],
    },
    "power.disable-network-power-saving": {
        "estimated_split_parts": 3,
        "suggested_splits": [
            "TCP/IP task offload switch",
            "MMCSS SystemResponsiveness tuning",
            "NetworkThrottlingIndex handling",
        ],
    },
    "power.optimize-performance": {
        "estimated_split_parts": 4,
        "suggested_splits": [
            "Processor policy values",
            "Timer or latency values",
            "QoS or multimedia scheduler values",
            "Raw power-manager overrides",
        ],
    },
    "privacy.deny-app-access": {
        "estimated_split_parts": 4,
        "suggested_splits": [
            "Capability access deny bundle",
            "Microphone-specific handling",
            "User info access policy",
            "Per-capability UX copy and risk gating",
        ],
    },
    "security.disable-vbs": {
        "estimated_split_parts": 4,
        "suggested_splits": [
            "Core VBS policy handling",
            "HVCI mode handling",
            "Credential Guard state and lock handling",
            "Windows Hello or PIN dependency checks and UX gating",
        ],
    },
    "privacy.disable-app-suggestions": {
        "estimated_split_parts": 2,
        "suggested_splits": [
            "Official CloudContent policy path",
            "Observed ContentDeliveryManager preference path",
        ],
    },
    "privacy.disable-application-compatibility": {
        "estimated_split_parts": 2,
        "suggested_splits": [
            "Application compatibility policy bundle",
            "Application Experience scheduled task bundle",
        ],
    },
    "privacy.disable-application-compatibility.tasks": {
        "estimated_split_parts": 3,
        "suggested_splits": [
            "Application Experience telemetry tasks",
            "Compatibility analysis tasks",
            "Version-specific inventory tasks",
        ],
    },
    "privacy.disable-ceip": {
        "estimated_split_parts": 3,
        "suggested_splits": [
            "SQMClient controls",
            "App-V CEIP controls",
            "Messenger or legacy product telemetry controls",
        ],
    },
    "privacy.disable-cross-device-experiences": {
        "estimated_split_parts": 3,
        "suggested_splits": [
            "Official device-wide EnableCdp policy",
            "Observed user-side CDP registry values",
            "Preset UX or profile selection model",
        ],
    },
    "privacy.disable-inking-typing-personalization": {
        "estimated_split_parts": 2,
        "suggested_splits": [
            "Typing personalization controls",
            "Windows Ink Workspace controls",
        ],
    },
    "privacy.disable-offline-files": {
        "estimated_split_parts": 4,
        "suggested_splits": [
            "Official Offline Files policy",
            "Offline Files services",
            "Offline Files scheduled tasks",
            "Sync Center binary or entry-point changes",
        ],
    },
    "privacy.disable-offline-files.binary": {
        "estimated_split_parts": 2,
        "suggested_splits": [
            "Sync Center entry-point behavior",
            "mobsync.exe binary rename workflow",
        ],
    },
    "privacy.disable-offline-files.services": {
        "estimated_split_parts": 2,
        "suggested_splits": [
            "CSC service handling",
            "CscService handling",
        ],
    },
    "privacy.disable-offline-files.tasks": {
        "estimated_split_parts": 2,
        "suggested_splits": [
            "Background sync task",
            "Logon sync task",
        ],
    },
    "privacy.disable-sleep-study-diagnostics": {
        "estimated_split_parts": 3,
        "suggested_splits": [
            "Sleep Study event channels",
            "Power diagnostics event channels",
            "User-facing diagnostic justification and rollback",
        ],
    },
    "privacy.disable-suggestions": {
        "estimated_split_parts": 3,
        "suggested_splits": [
            "Start or shell suggestions",
            "Settings or tips suggestions",
            "Opaque ContentDeliveryManager identifiers",
        ],
    },
    "privacy.disable-suggestions-cdm": {
        "estimated_split_parts": 4,
        "suggested_splits": [
            "Third-party suggestions mapping",
            "Settings suggestions mapping",
            "Welcome experience mapping",
            "SubscribedContent identifier mapping",
        ],
    },
    "privacy.disable-windows-tips": {
        "estimated_split_parts": 2,
        "suggested_splits": [
            "Official DisableSoftLanding policy",
            "Observed ContentDeliveryManager preference",
        ],
    },
    "privacy.disable-wmplayer-telemetry": {
        "estimated_split_parts": 3,
        "suggested_splits": [
            "Metadata retrieval preferences",
            "Usage or recommendations preferences",
            "Windows Media Player product-specific UX copy",
        ],
    },
    "privacy.troubleshooter-dont-run": {
        "estimated_split_parts": 2,
        "suggested_splits": [
            "Official troubleshooting policy dropdown",
            "WindowsMitigation companion preference",
        ],
    },
    "security.disable-windows-update": {
        "estimated_split_parts": 2,
        "suggested_splits": [
            "Official Windows Update policy controls",
            "Runtime pause timestamp handling",
        ],
    },
    "system.disable-scheduled-tasks": {
        "estimated_split_parts": 5,
        "suggested_splits": [
            "Application compatibility tasks",
            "CEIP tasks",
            "Cleanup tasks",
            "Feedback tasks",
            "Error-reporting tasks",
        ],
    },
    "system.reduce-shutdown-timeouts": {
        "estimated_split_parts": 4,
        "suggested_splits": [
            "WaitToKillServiceTimeout",
            "WaitToKillAppTimeout or WaitToKillTimeout",
            "HungAppTimeout",
            "AutoEndTasks",
        ],
    },
}


def proof_gate_applies(record: dict) -> bool:
    if record.get("schema_version") in PROOF_GATED_SCHEMA_VERSIONS:
        return True
    return "validation_proof" in record


def proof_gate_failed(record: dict) -> bool:
    if not proof_gate_applies(record):
        return False
    proof = record.get("validation_proof")
    if not proof:
        return True
    return proof.get("key_found_on_page") is not True


def should_include_in_backlog(record: dict) -> bool:
    status = record.get("record_status")
    if status == "review-required":
        return True
    if status in {"validated", "published"} and proof_gate_failed(record):
        return True
    return False


def load_review_records() -> list[dict]:
    records: list[dict] = []
    for path in sorted(RECORDS_ROOT.glob("*.json")):
        data = json.loads(path.read_text(encoding="utf-8"))
        if not should_include_in_backlog(data):
            continue
        data["_source_file"] = str(path.relative_to(REPO_ROOT)).replace("\\", "/")
        records.append(data)
    return records


def gather_text(record: dict) -> str:
    setting = record.get("setting", {})
    decision = record.get("decision", {})
    implementation = record.get("app_current_implementation", {})
    parts = [
        record.get("summary", ""),
        setting.get("area", ""),
        setting.get("professional_notes", ""),
        decision.get("why", ""),
    ]
    notes = implementation.get("notes", "")
    if isinstance(notes, list):
        parts.extend(notes)
    else:
        parts.append(notes)
    blockers = decision.get("blocking_issues", [])
    parts.extend(blockers)
    return " ".join(str(part) for part in parts if part).lower()


def classify_record(record: dict) -> tuple[str, str]:
    record_id = record["record_id"]
    text = gather_text(record)
    implementation_status = record.get("app_current_implementation", {}).get("status", "").lower()

    manual_label = MANUAL_LOOKUP.get(record_id)
    if manual_label:
        return manual_label, "manual-override"

    if proof_gate_failed(record):
        return "docs-first", "proof-gate"

    if any(keyword in text for keyword in RENAME_KEYWORDS):
        return "rename", "heuristic"

    if implementation_status in {"mismatch-suspected", "partially-matches"} or any(keyword in text for keyword in IMPLEMENTATION_MISMATCH_KEYWORDS):
        return "implementation-mismatch", "heuristic"

    if any(keyword in text for keyword in SPLIT_KEYWORDS):
        return "split-needed", "heuristic"

    if implementation_status in {"unknown", "matches-research"}:
        return "docs-first", "heuristic"

    if any(keyword in text for keyword in DOCS_FIRST_KEYWORDS):
        return "docs-first", "heuristic"

    return "implementation-mismatch", "heuristic"


def compact_value(value) -> str:
    if value is None:
        return "missing"
    if isinstance(value, bool):
        return "true" if value else "false"
    return str(value)


def compact_lower(value) -> str:
    return compact_value(value).lower()


def slugify(text: str) -> str:
    chars = []
    previous_dash = False
    for char in text.lower():
        if char.isalnum():
            chars.append(char)
            previous_dash = False
        elif not previous_dash:
            chars.append("-")
            previous_dash = True
    return "".join(chars).strip("-")


def format_write_surface(write: dict) -> dict:
    return {
        "location_kind": write.get("location_kind") or "registry",
        "path": write.get("path"),
        "value_name": write.get("value_name"),
        "value_type": write.get("value_type"),
        "state_kind": write.get("state_kind"),
        "value": write.get("value"),
    }


def is_observed_target(target: dict) -> bool:
    target_id = (target.get("target_id") or "").lower()
    notes = (target.get("notes") or "").lower()
    return "observed" in target_id or "observed" in notes or "app write only" in notes


def format_target_surface(target: dict) -> dict:
    allowed_values = target.get("allowed_values", [])
    recommended_value = None
    for allowed_value in allowed_values:
        if allowed_value.get("state_kind") != "missing":
            recommended_value = allowed_value.get("value")
            break
    return {
        "location_kind": target.get("location_kind"),
        "path": target.get("path"),
        "value_name": target.get("value_name"),
        "value_type": target.get("value_type"),
        "example_value": recommended_value,
    }


def build_current_state(record: dict) -> str:
    implementation_status = record.get("app_current_implementation", {}).get("status", "unknown")
    area = record.get("setting", {}).get("area", "Unknown area")
    return f"{implementation_status} | {area}"


def build_fix_note(record: dict) -> str:
    writes = [format_write_surface(write) for write in record.get("app_current_implementation", {}).get("writes", [])]
    targets = [
        format_target_surface(target)
        for target in record.get("setting", {}).get("targets", [])
        if not is_observed_target(target)
    ]

    if writes and targets:
        current_bits = [
            f"{surface['path']}::{surface.get('value_name') or '(default)'}={compact_value(surface.get('value'))}"
            for surface in writes
        ]
        target_bits = [
            f"{surface['path']}::{surface.get('value_name') or '(default)'}"
            + (
                f" (example {compact_value(surface.get('example_value'))})"
                if surface.get("example_value") is not None
                else ""
            )
            for surface in targets
        ]
        return (
            "Move the implementation from "
            + "; ".join(current_bits)
            + " to the researched surface "
            + "; ".join(target_bits)
            + ". "
            + ACTION_HINTS["implementation-mismatch"]
        )
    return ACTION_HINTS["implementation-mismatch"]


def build_rename_note(record: dict) -> str:
    suggested_name = RENAME_SUGGESTIONS.get(record["record_id"], "Rename this tweak so the title matches the actual scope")
    return f"Suggested name: {suggested_name}. {ACTION_HINTS['rename']}"


def infer_research_tracks(record: dict) -> list[str]:
    text = gather_text(record)
    area = (record.get("setting", {}).get("area") or "").lower()
    category = (record.get("setting", {}).get("category") or "").lower()
    tracks: list[str] = []

    if "policy" in area or "policy csp" in text or "admx" in text:
        tracks.append("ADMX / Policy CSP mapping")
    if any(keyword in text for keyword in ("observed", "current-user", "preference", "contentdeliverymanager", "consent-store", "runtime")):
        tracks.append("Procmon or settings-diff capture")
    if any(keyword in text for keyword in ("kernel", "graphics", "dpc", "overlay", "hags", "dwm")):
        tracks.append("ReactOS-adjacent or driver-source comparison")
    if category in {"developer", "audio"} or any(keyword in text for keyword in ("visual studio", "vs code", "docker", "wsl", "python", "terminal", "media player")):
        tracks.append("Vendor docs or product settings schema")
    if any(keyword in text for keyword in ("service", "task", "scheduled task")):
        tracks.append("Service or scheduled-task baseline diff")

    if not tracks:
        tracks.append("Procmon or VM toggle-diff validation")

    # Preserve order while deduplicating.
    seen = set()
    unique_tracks = []
    for track in tracks:
        if track not in seen:
            unique_tracks.append(track)
            seen.add(track)
    return unique_tracks[:3]


def docs_reason(record: dict) -> str:
    if proof_gate_failed(record):
        proof = record.get("validation_proof") or {}
        exact = proof.get("exact_quote_or_path")
        notes = proof.get("notes")
        if exact:
            reason = (
                "Validation proof did not confirm the exact key or path on the cited page: "
                + exact
            )
            if notes:
                reason += " " + notes
            return reason
        if proof_gate_applies(record) and not proof:
            return (
                "Validated or published records under the proof-gated workflow must include "
                "validation_proof before they can stay out of docs-first."
            )
        if notes:
            return "Validation proof exists, but key_found_on_page is false. " + notes
        return "Validation proof exists, but key_found_on_page is false."
    blockers = record.get("decision", {}).get("blocking_issues", [])
    if blockers:
        return blockers[0]
    return record.get("decision", {}).get("why") or record.get("summary") or ACTION_HINTS["docs-first"]


def evidence_locations_for_record(record: dict, kinds: set[str]) -> list[str]:
    locations: list[str] = []
    for evidence in record.get("evidence", []):
        if evidence.get("kind") not in kinds:
            continue
        location = evidence.get("location")
        title = evidence.get("title")
        if location and title:
            locations.append(f"{title} ({location})")
        elif location:
            locations.append(location)
    return locations


def next_source_for_track(record: dict, track: str) -> str:
    if track == "ADMX / Policy CSP mapping":
        sources = evidence_locations_for_record(record, {"policy-csp", "official-doc"})
        if sources:
            return sources[0]
        return "Local ADMX/ADML under C:\\Windows\\PolicyDefinitions plus the matching Microsoft Learn policy or CSP page"
    if track == "Procmon or settings-diff capture":
        return "Procmon trace plus a before/after UI toggle diff on a clean Windows VM"
    if track == "Procmon or VM toggle-diff validation":
        return "Procmon plus a clean-VM toggle diff that captures the exact registry or file changes"
    if track == "ReactOS-adjacent or driver-source comparison":
        return "ReactOS-adjacent code search, open-source driver references, or low-level graphics/kernel docs for the same subsystem"
    if track == "Vendor docs or product settings schema":
        sources = evidence_locations_for_record(record, {"official-doc"})
        if sources:
            return sources[0]
        return "The product's official settings reference or vendor documentation for the exact setting surface"
    if track == "Service or scheduled-task baseline diff":
        return "services.msc / sc qc / schtasks output captured before and after the matching Windows feature toggle"
    return "Primary vendor or Microsoft documentation plus a clean before/after system diff"


def build_evidence_track_entries(record: dict) -> list[dict]:
    reason = docs_reason(record)
    return [
        {
            "track": track,
            "why_not_validated": reason,
            "next_source": next_source_for_track(record, track),
        }
        for track in infer_research_tracks(record)
    ]


def build_docs_note(record: dict) -> str:
    tracks = [entry["track"] for entry in build_evidence_track_entries(record)]
    return "Evidence first. Prioritize: " + ", ".join(tracks) + ". " + ACTION_HINTS["docs-first"]


def build_split_note(record: dict) -> str:
    plan = SPLIT_PLANS.get(record["record_id"])
    if not plan:
        return ACTION_HINTS["split-needed"]
    parts = ", ".join(plan["suggested_splits"])
    return (
        f"Split into {plan['estimated_split_parts']} parts: {parts}. "
        + ACTION_HINTS["split-needed"]
    )


def make_backlog_entry(record: dict, label: str) -> dict:
    base = {
        "record_id": record["record_id"],
        "current_state": build_current_state(record),
        "notes": "",
        "technical_area": record.get("setting", {}).get("area"),
        "implementation_status": record.get("app_current_implementation", {}).get("status"),
        "source_file": record["_source_file"],
    }

    if label == "implementation-mismatch":
        base["notes"] = build_fix_note(record)
        base["current_surfaces"] = [
            format_write_surface(write)
            for write in record.get("app_current_implementation", {}).get("writes", [])
        ]
        base["expected_surfaces"] = [
            format_target_surface(target)
            for target in record.get("setting", {}).get("targets", [])
            if not is_observed_target(target)
        ]
    elif label == "rename":
        base["notes"] = build_rename_note(record)
        base["suggested_name"] = RENAME_SUGGESTIONS.get(record["record_id"])
    elif label == "split-needed":
        plan = SPLIT_PLANS.get(record["record_id"], {"estimated_split_parts": 2, "suggested_splits": []})
        base["notes"] = build_split_note(record)
        base["estimated_split_parts"] = plan["estimated_split_parts"]
        base["suggested_splits"] = plan["suggested_splits"]
    elif label == "docs-first":
        base["notes"] = build_docs_note(record)
        base["suggested_evidence_tracks"] = build_evidence_track_entries(record)
    else:
        raise ValueError(f"Unsupported label: {label}")

    return base


def write_label_backlog(path: Path, label: str, records: list[dict], priorities: list[str]) -> None:
    entries = [make_backlog_entry(record, label) for record in records]
    entries.sort(key=lambda entry: entry["record_id"])
    payload = {
        "schema_version": "1.0",
        "generated_utc": datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z"),
        "backlog_type": label,
        "priority_order": priorities,
        "entry_count": len(entries),
        "entries": entries,
    }
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def surface_signature(surface: dict) -> tuple[str, str, str]:
    return (
        (surface.get("location_kind") or "").lower(),
        (surface.get("path") or "").lower(),
        (surface.get("value_name") or "").lower(),
    )


def hive_rank(path: str | None) -> int:
    if not path:
        return 3
    upper = path.upper()
    if upper.startswith("HKCU\\"):
        return 0
    if upper.startswith("HKLM\\"):
        return 1
    if upper.startswith("HKCR\\") or upper.startswith("HKU\\") or upper.startswith("HKCC\\"):
        return 2
    return 3


def fix_gap_score(entry: dict) -> int:
    current = entry.get("current_surfaces", [])
    expected = entry.get("expected_surfaces", [])
    current_map = {surface_signature(surface): surface for surface in current}
    expected_map = {surface_signature(surface): surface for surface in expected}
    overlap = set(current_map) & set(expected_map)
    key_gap = len(current_map) + len(expected_map) - 2 * len(overlap)
    count_gap = abs(len(current) - len(expected))
    value_gap = 0
    for signature in overlap:
        current_value = compact_lower(current_map[signature].get("value"))
        expected_value = compact_lower(expected_map[signature].get("example_value"))
        if current_value != expected_value:
            value_gap += 1
    return (key_gap * 3) + count_gap + value_gap


def fix_hive_priority(entry: dict) -> int:
    surfaces = entry.get("current_surfaces", [])
    if not surfaces:
        return 3
    return max(hive_rank(surface.get("path")) for surface in surfaces)


def estimate_fix_effort(entry: dict) -> str:
    gap = fix_gap_score(entry)
    hive = fix_hive_priority(entry)
    surface_count = len(entry.get("current_surfaces", [])) + len(entry.get("expected_surfaces", []))
    if gap <= 2 and hive == 0 and surface_count <= 3:
        return "small"
    if gap <= 5 and hive <= 1 and surface_count <= 4:
        return "medium"
    return "large"


def estimate_split_effort(entry: dict) -> str:
    parts = entry.get("estimated_split_parts", 2)
    if parts <= 2:
        return "small"
    if parts <= 4:
        return "medium"
    return "large"


def track_priority(track: str) -> int:
    if isinstance(track, dict):
        track = track.get("track", "")
    lowered = track.lower()
    if "admx" in lowered or "policy csp" in lowered:
        return 0
    if "procmon" in lowered or "settings-diff" in lowered:
        return 0
    if "service" in lowered or "scheduled-task" in lowered or "toggle-diff" in lowered:
        return 1
    if "vendor docs" in lowered:
        return 2
    if "reactos" in lowered or "driver-source" in lowered or "wayback" in lowered:
        return 4
    return 3


def estimate_docs_effort(entry: dict) -> str:
    tracks = entry.get("suggested_evidence_tracks", [])
    if not tracks:
        return "medium"
    priorities = [track_priority(track) for track in tracks]
    if max(priorities) >= 4:
        return "large"
    if len(tracks) == 1 and priorities[0] <= 1:
        return "small"
    return "medium"


def estimate_rename_effort(_: dict) -> str:
    return "small"


def split_dependencies(entry: dict) -> list[str]:
    record_id = entry["record_id"]
    parts = entry.get("suggested_splits", [])
    return [f"{record_id}.{slugify(part)}" for part in parts]


def roadmap_item(entry: dict, record: dict, category: str, sprint_number: int, estimated_effort: str, depends_on: list[str], priority_index: int) -> dict:
    return {
        "sprint_number": sprint_number,
        "priority_index": priority_index,
        "record_id": entry["record_id"],
        "category": category,
        "estimated_effort": estimated_effort,
        "blocking_issues": record.get("decision", {}).get("blocking_issues", []),
        "depends_on": depends_on,
        "current_state": entry.get("current_state"),
        "notes": entry.get("notes"),
        "source_file": entry.get("source_file"),
    }


def build_roadmap(
    records_by_label: dict[str, list[dict]],
    backlog_entries_by_label: dict[str, list[dict]],
) -> dict:
    record_lookup = {record["record_id"]: record for records in records_by_label.values() for record in records}
    roadmap_items: list[dict] = []

    rename_entries = sorted(backlog_entries_by_label["rename"], key=lambda entry: entry["record_id"])
    for index, entry in enumerate(rename_entries, start=1):
        roadmap_items.append(
            roadmap_item(
                entry,
                record_lookup[entry["record_id"]],
                "rename",
                1,
                estimate_rename_effort(entry),
                [],
                index,
            )
        )

    fix_entries = sorted(
        backlog_entries_by_label["implementation-mismatch"],
        key=lambda entry: (
            fix_gap_score(entry),
            fix_hive_priority(entry),
            len(entry.get("current_surfaces", [])) + len(entry.get("expected_surfaces", [])),
            entry["record_id"],
        ),
    )
    for index, entry in enumerate(fix_entries, start=1):
        roadmap_items.append(
            roadmap_item(
                entry,
                record_lookup[entry["record_id"]],
                "fix",
                2,
                estimate_fix_effort(entry),
                [],
                index,
            )
        )

    split_entries = sorted(
        backlog_entries_by_label["split-needed"],
        key=lambda entry: (
            entry.get("estimated_split_parts", 99),
            entry["record_id"],
        ),
    )
    for index, entry in enumerate(split_entries, start=1):
        roadmap_items.append(
            roadmap_item(
                entry,
                record_lookup[entry["record_id"]],
                "split",
                3,
                estimate_split_effort(entry),
                split_dependencies(entry),
                index,
            )
        )

    docs_entries = sorted(
        backlog_entries_by_label["docs-first"],
        key=lambda entry: (
            min(track_priority(track) for track in entry.get("suggested_evidence_tracks", [""])) if entry.get("suggested_evidence_tracks") else 3,
            max(track_priority(track) for track in entry.get("suggested_evidence_tracks", [""])) if entry.get("suggested_evidence_tracks") else 3,
            len(entry.get("suggested_evidence_tracks", [])),
            entry["record_id"],
        ),
    )
    for index, entry in enumerate(docs_entries, start=1):
        roadmap_items.append(
            roadmap_item(
                entry,
                record_lookup[entry["record_id"]],
                "docs-first",
                4,
                estimate_docs_effort(entry),
                [],
                index,
            )
        )

    return {
        "schema_version": "1.0",
        "generated_utc": datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z"),
        "sprint_order": [
            {"sprint_number": 1, "category": "rename"},
            {"sprint_number": 2, "category": "fix"},
            {"sprint_number": 3, "category": "split"},
            {"sprint_number": 4, "category": "docs-first"},
        ],
        "items": roadmap_items,
    }


def build_entry(record: dict) -> dict:
    label, method = classify_record(record)
    decision = record.get("decision", {})
    implementation = record.get("app_current_implementation", {})
    blockers = decision.get("blocking_issues", [])
    reason = blockers[0] if blockers else decision.get("why") or record.get("summary", "")

    entry = {
        "record_id": record["record_id"],
        "tweak_id": record.get("tweak_id", record["record_id"]),
        "source_record_status": record.get("record_status"),
        "label": label,
        "classification_method": method,
        "classification_reason": reason,
        "suggested_action": ACTION_HINTS[label],
        "category": record.get("setting", {}).get("category"),
        "technical_area": record.get("setting", {}).get("area"),
        "implementation_status": implementation.get("status"),
        "apply_allowed": decision.get("apply_allowed"),
        "source_file": record["_source_file"],
        "summary": record.get("summary"),
        "decision_why": decision.get("why"),
        "blocking_issues": blockers,
    }
    if "validation_proof" in record:
        entry["validation_proof"] = record.get("validation_proof")
    return entry


def main() -> None:
    records = load_review_records()
    entries = [build_entry(record) for record in records]
    entries.sort(key=lambda entry: (entry["label"], entry["record_id"]))

    label_counts = Counter(entry["label"] for entry in entries)
    method_counts = Counter(entry["classification_method"] for entry in entries)
    unknown_labels = sorted(set(label_counts) - set(VALID_LABELS))
    if unknown_labels:
        raise SystemExit(f"Unexpected labels found: {', '.join(unknown_labels)}")

    output = {
        "schema_version": "1.0",
        "generated_utc": datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z"),
        "source_record_statuses": sorted({entry["source_record_status"] for entry in entries}),
        "source_record_count": len(entries),
        "label_explanations": LABEL_EXPLANATIONS,
        "counts_by_label": {label: label_counts.get(label, 0) for label in VALID_LABELS},
        "counts_by_method": dict(sorted(method_counts.items())),
        "entries": entries,
    }

    OUTPUT_JSON.write_text(json.dumps(output, indent=2) + "\n", encoding="utf-8")
    priority_order = ["rename", "implementation-mismatch", "split-needed", "docs-first"]
    records_by_label: dict[str, list[dict]] = {label: [] for label in VALID_LABELS}
    for record in records:
        label, _ = classify_record(record)
        records_by_label[label].append(record)

    backlog_entries_by_label = {
        label: [make_backlog_entry(record, label) for record in records_by_label[label]]
        for label in VALID_LABELS
    }

    write_label_backlog(FIX_OUTPUT_JSON, "implementation-mismatch", records_by_label["implementation-mismatch"], priority_order)
    write_label_backlog(RENAME_OUTPUT_JSON, "rename", records_by_label["rename"], priority_order)
    write_label_backlog(SPLIT_OUTPUT_JSON, "split-needed", records_by_label["split-needed"], priority_order)
    write_label_backlog(DOCS_FIRST_OUTPUT_JSON, "docs-first", records_by_label["docs-first"], priority_order)
    roadmap = build_roadmap(records_by_label, backlog_entries_by_label)
    ROADMAP_OUTPUT_JSON.write_text(json.dumps(roadmap, indent=2) + "\n", encoding="utf-8")

    print(f"Wrote {OUTPUT_JSON}")
    print(json.dumps(output["counts_by_label"], indent=2))


if __name__ == "__main__":
    main()
