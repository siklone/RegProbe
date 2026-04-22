from __future__ import annotations

import json
from pathlib import Path
from typing import Any


def read_json_object(path: Path, *, context: str) -> dict[str, Any]:
    payload = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(payload, dict):
        raise ValueError(f"{context} JSON payload is not an object")
    return payload


def apply_summary_contract(
    summary: dict[str, Any],
    *,
    default_error_kind: str | None = None,
    default_recovery_action: str | None = None,
    default_transport_blocker: str | None = None,
    default_guest_health: str | None = None,
) -> dict[str, Any]:
    payload = dict(summary)
    status = str(payload.get("status") or "").strip().lower()

    if status == "timeout":
        payload.setdefault("error_kind", default_error_kind or "runner-timeout")
        payload.setdefault("recovery_action", default_recovery_action or "rerun-runner")
        payload.setdefault("transport_blocker", default_transport_blocker or "timeout")
        payload.setdefault("guest_health", default_guest_health or "unknown")
        return payload

    if status == "error":
        payload.setdefault("error_kind", default_error_kind or "runner-error")
        payload.setdefault("recovery_action", default_recovery_action or "inspect-summary")
        payload.setdefault("transport_blocker", default_transport_blocker or str(payload.get("error_kind") or "error"))
        payload.setdefault("guest_health", default_guest_health or "degraded")
        return payload

    payload.setdefault("error_kind", default_error_kind)
    payload.setdefault("recovery_action", default_recovery_action or "none")
    payload.setdefault("transport_blocker", default_transport_blocker or "none")
    payload.setdefault("guest_health", default_guest_health or "stable")
    return payload


def write_summary_contract(
    path: Path,
    summary: dict[str, Any],
    *,
    default_error_kind: str | None = None,
    default_recovery_action: str | None = None,
    default_transport_blocker: str | None = None,
    default_guest_health: str | None = None,
) -> dict[str, Any]:
    payload = apply_summary_contract(
        summary,
        default_error_kind=default_error_kind,
        default_recovery_action=default_recovery_action,
        default_transport_blocker=default_transport_blocker,
        default_guest_health=default_guest_health,
    )
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    return payload
