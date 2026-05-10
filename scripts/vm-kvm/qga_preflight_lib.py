from __future__ import annotations

import base64
import json
import subprocess
import time
from pathlib import Path
from typing import Any

from qga_response_lib import parse_qga_return
from summary_contract_lib import apply_summary_contract, write_summary_contract


QGA_PREFLIGHT_ERROR_KIND = "qga-preflight-failed"
QGA_PREFLIGHT_RECOVERY_ACTION = "repair-qga-or-run-vm-health-check"
QGA_PREFLIGHT_TRANSPORT_BLOCKER = "qga-agent-command"


class QgaPreflightError(RuntimeError):
    def __init__(self, preflight: dict[str, Any]):
        super().__init__(str(preflight.get("error") or "QGA preflight failed."))
        self.preflight = preflight


def virsh_command(connect: str, args: list[str]) -> list[str]:
    command = ["virsh"]
    if connect:
        command.extend(["-c", connect])
    command.extend(args)
    return command


def run_virsh(connect: str, args: list[str], *, timeout: int) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        virsh_command(connect, args),
        check=False,
        capture_output=True,
        text=True,
        timeout=timeout,
    )


def check_domstate(domain: str, connect: str, *, timeout: int) -> dict[str, Any]:
    command = virsh_command(connect, ["domstate", domain])
    try:
        result = run_virsh(connect, ["domstate", domain], timeout=timeout)
    except subprocess.TimeoutExpired as exc:
        return {
            "status": "timeout",
            "command": command,
            "timeout_seconds": timeout,
            "error": f"virsh domstate timed out after {timeout}s",
            "stdout": (exc.stdout or "").strip() if isinstance(exc.stdout, str) else "",
            "stderr": (exc.stderr or "").strip() if isinstance(exc.stderr, str) else "",
        }

    stdout = (result.stdout or "").strip()
    stderr = (result.stderr or "").strip()
    payload: dict[str, Any] = {
        "status": "ok" if result.returncode == 0 else "error",
        "command": command,
        "returncode": result.returncode,
        "state": stdout,
        "stdout": stdout,
        "stderr": stderr,
    }
    if result.returncode != 0:
        payload["error"] = stderr or stdout or f"virsh domstate exited with {result.returncode}"
    elif stdout.lower() != "running":
        payload["status"] = "error"
        payload["error"] = f"VM is not running: {stdout or 'unknown'}"
    return payload


def run_agent_command(domain: str, payload: dict[str, Any], *, connect: str, timeout: int) -> tuple[dict[str, Any], Any | None]:
    payload_text = json.dumps(payload, separators=(",", ":"))
    command = virsh_command(connect, ["qemu-agent-command", domain, payload_text])
    try:
        result = run_virsh(connect, ["qemu-agent-command", domain, payload_text], timeout=timeout)
    except subprocess.TimeoutExpired as exc:
        return (
            {
                "status": "timeout",
                "command": command,
                "timeout_seconds": timeout,
                "error": f"qemu-agent-command timed out after {timeout}s",
                "stdout": (exc.stdout or "").strip() if isinstance(exc.stdout, str) else "",
                "stderr": (exc.stderr or "").strip() if isinstance(exc.stderr, str) else "",
            },
            None,
        )

    stdout = (result.stdout or "").strip()
    stderr = (result.stderr or "").strip()
    base: dict[str, Any] = {
        "status": "ok" if result.returncode == 0 else "error",
        "command": command,
        "returncode": result.returncode,
        "stdout": stdout,
        "stderr": stderr,
    }
    if result.returncode != 0:
        base["error"] = stderr or stdout or f"qemu-agent-command exited with {result.returncode}"
        return base, None
    try:
        parsed = parse_qga_return(stdout)
    except ValueError as exc:
        base["status"] = "error"
        base["error"] = str(exc)
        return base, None
    base["return"] = parsed
    return base, parsed


def decode_qga_text(value: object) -> str:
    if not isinstance(value, str) or not value:
        return ""
    try:
        return base64.b64decode(value).decode("utf-8", errors="replace")
    except (ValueError, OSError):
        return value


def check_guest_exec(
    domain: str,
    connect: str,
    *,
    timeout: int,
    wait_timeout: int,
    poll_interval: float,
) -> dict[str, Any]:
    start_payload = {
        "execute": "guest-exec",
        "arguments": {
            "path": r"C:\Windows\System32\cmd.exe",
            "arg": ["/c", "whoami"],
            "capture-output": True,
        },
    }
    start_check, start_return = run_agent_command(domain, start_payload, connect=connect, timeout=timeout)
    if start_check.get("status") != "ok":
        start_check["phase"] = "guest-exec"
        return start_check
    if not isinstance(start_return, dict) or not isinstance(start_return.get("pid"), int):
        start_check["status"] = "error"
        start_check["phase"] = "guest-exec"
        start_check["error"] = "guest-exec did not return a numeric pid"
        return start_check

    pid = int(start_return["pid"])
    deadline = time.time() + max(wait_timeout, 1)
    last_check: dict[str, Any] | None = None
    while time.time() < deadline:
        status_check, status_return = run_agent_command(
            domain,
            {
                "execute": "guest-exec-status",
                "arguments": {"pid": pid},
            },
            connect=connect,
            timeout=timeout,
        )
        status_check["phase"] = "guest-exec-status"
        status_check["pid"] = pid
        last_check = status_check
        if status_check.get("status") != "ok":
            return status_check
        if isinstance(status_return, dict) and status_return.get("exited") is True:
            exitcode = int(status_return.get("exitcode", 0))
            payload = dict(status_check)
            payload["status"] = "ok" if exitcode == 0 else "error"
            payload["exitcode"] = exitcode
            payload["out_data"] = decode_qga_text(status_return.get("out-data"))
            payload["err_data"] = decode_qga_text(status_return.get("err-data"))
            if exitcode != 0:
                payload["error"] = payload.get("err_data") or payload.get("out_data") or f"whoami exited with {exitcode}"
            return payload
        time.sleep(max(poll_interval, 0.1))

    return {
        "status": "timeout",
        "phase": "guest-exec-status",
        "pid": pid,
        "timeout_seconds": wait_timeout,
        "error": f"guest-exec-status did not exit after {wait_timeout}s",
        "last_status": last_check,
    }


def skipped_check(reason: str) -> dict[str, Any]:
    return {
        "status": "skipped",
        "reason": reason,
    }


def run_qga_preflight(
    *,
    domain: str,
    connect: str,
    timeout: int = 10,
    wait_timeout: int = 30,
    poll_interval: float = 1.0,
) -> dict[str, Any]:
    checks: dict[str, Any] = {}
    checks["domstate"] = check_domstate(domain, connect, timeout=timeout)
    if checks["domstate"].get("status") != "ok":
        checks["guest_ping"] = skipped_check("domstate-not-running")
        checks["guest_info"] = skipped_check("domstate-not-running")
        checks["guest_exec"] = skipped_check("domstate-not-running")
        return preflight_payload(domain=domain, connect=connect, checks=checks)

    guest_ping, _ = run_agent_command(domain, {"execute": "guest-ping"}, connect=connect, timeout=timeout)
    checks["guest_ping"] = guest_ping
    if guest_ping.get("status") == "ok":
        guest_info, _ = run_agent_command(domain, {"execute": "guest-info"}, connect=connect, timeout=timeout)
        checks["guest_info"] = guest_info
        checks["guest_exec"] = check_guest_exec(
            domain,
            connect,
            timeout=timeout,
            wait_timeout=wait_timeout,
            poll_interval=poll_interval,
        )
    else:
        checks["guest_info"] = skipped_check("guest-ping-failed")
        checks["guest_exec"] = skipped_check("guest-ping-failed")
    return preflight_payload(domain=domain, connect=connect, checks=checks)


def preflight_payload(*, domain: str, connect: str, checks: dict[str, Any]) -> dict[str, Any]:
    failed = [
        name
        for name, check in checks.items()
        if str((check or {}).get("status") or "").lower() not in {"ok"}
    ]
    status = "ok" if not failed else "error"
    payload = {
        "status": status,
        "summary_source": "qga-preflight",
        "domain": domain,
        "connect": connect,
        "checks": checks,
        "failed_checks": failed,
    }
    if status != "ok":
        payload.update(
            {
                "error_kind": QGA_PREFLIGHT_ERROR_KIND,
                "transport_blocker": QGA_PREFLIGHT_TRANSPORT_BLOCKER,
                "recovery_action": QGA_PREFLIGHT_RECOVERY_ACTION,
                "guest_health": "degraded",
                "error": "QGA preflight failed before guest launch.",
            }
        )
    return apply_summary_contract(
        payload,
        default_error_kind=QGA_PREFLIGHT_ERROR_KIND if status != "ok" else None,
        default_recovery_action=QGA_PREFLIGHT_RECOVERY_ACTION if status != "ok" else "none",
        default_transport_blocker=QGA_PREFLIGHT_TRANSPORT_BLOCKER if status != "ok" else "none",
        default_guest_health="degraded" if status != "ok" else "stable",
    )


def require_qga_preflight(
    *,
    domain: str,
    connect: str,
    preflight_mode: str,
    timeout: int = 10,
    wait_timeout: int = 30,
    poll_interval: float = 1.0,
) -> dict[str, Any] | None:
    if preflight_mode == "off":
        return None
    preflight = run_qga_preflight(
        domain=domain,
        connect=connect,
        timeout=timeout,
        wait_timeout=wait_timeout,
        poll_interval=poll_interval,
    )
    if preflight.get("status") == "ok":
        return preflight
    if preflight_mode == "warn":
        return preflight
    raise QgaPreflightError(preflight)


def write_qga_preflight_summary(
    summary_path: Path,
    *,
    domain: str,
    connect: str,
    launch_transport: str,
    preflight: dict[str, Any],
    extra: dict[str, Any] | None = None,
) -> dict[str, Any]:
    payload: dict[str, Any] = {
        "status": "error",
        "summary_source": "qga-preflight",
        "error_kind": QGA_PREFLIGHT_ERROR_KIND,
        "transport_blocker": QGA_PREFLIGHT_TRANSPORT_BLOCKER,
        "recovery_action": QGA_PREFLIGHT_RECOVERY_ACTION,
        "guest_health": "degraded",
        "domain": domain,
        "connect": connect,
        "launch_transport": launch_transport,
        "preflight": preflight,
        "failed_checks": preflight.get("failed_checks"),
        "error": preflight.get("error") or "QGA preflight failed before guest launch.",
    }
    if extra:
        payload.update(extra)
    return write_summary_contract(
        summary_path,
        payload,
        default_error_kind=QGA_PREFLIGHT_ERROR_KIND,
        default_recovery_action=QGA_PREFLIGHT_RECOVERY_ACTION,
        default_transport_blocker=QGA_PREFLIGHT_TRANSPORT_BLOCKER,
        default_guest_health="degraded",
    )
