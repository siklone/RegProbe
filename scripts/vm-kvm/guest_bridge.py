#!/usr/bin/env python3
from __future__ import annotations

import subprocess
import time
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path


def bridge_health_url_from_guest_url(bridge_base_url: str) -> str:
    parsed = urllib.parse.urlparse(bridge_base_url)
    port = parsed.port or 80
    return f"http://127.0.0.1:{port}/healthz"


def bridge_is_healthy(health_url: str, timeout_seconds: float = 1.5) -> bool:
    try:
        with urllib.request.urlopen(health_url, timeout=timeout_seconds) as response:
            payload = response.read().decode("utf-8", errors="replace").strip()
        return payload == "ok"
    except (urllib.error.URLError, TimeoutError, OSError):
        return False


def ensure_guest_bridge(repo_root: Path, bridge_base_url: str, upload_root: Path) -> dict[str, object]:
    health_url = bridge_health_url_from_guest_url(bridge_base_url)
    result: dict[str, object] = {
        "health_url": health_url,
        "already_healthy": False,
        "launched": False,
        "ready": False,
        "pid": None,
        "error_kind": None,
        "error": "",
        "log_path": "",
    }

    if bridge_is_healthy(health_url):
        result["already_healthy"] = True
        result["ready"] = True
        return result

    parsed = urllib.parse.urlparse(bridge_base_url)
    port = parsed.port or 80
    helper = Path(__file__).resolve().with_name("serve-guest-bridge.py")
    log_dir = repo_root / "dist" / "kvm-generated"
    log_path = log_dir / f"serve-guest-bridge-{port}.log"
    result["log_path"] = str(log_path)

    try:
        log_dir.mkdir(parents=True, exist_ok=True)
        upload_root.mkdir(parents=True, exist_ok=True)
        with log_path.open("ab") as handle:
            proc = subprocess.Popen(
                [
                    "python3",
                    str(helper),
                    "--host",
                    "0.0.0.0",
                    "--port",
                    str(port),
                    "--serve-root",
                    str(repo_root),
                    "--upload-root",
                    str(upload_root),
                ],
                cwd=str(repo_root),
                stdin=subprocess.DEVNULL,
                stdout=handle,
                stderr=subprocess.STDOUT,
                start_new_session=True,
            )
    except OSError as exc:
        result["error_kind"] = "bridge-launch-error"
        result["error"] = str(exc)
        return result

    result["launched"] = True
    result["pid"] = proc.pid
    for _ in range(20):
        if bridge_is_healthy(health_url):
            result["ready"] = True
            break
        time.sleep(0.25)

    if not result["ready"]:
        result["error_kind"] = "bridge-ready-timeout"
        result["error"] = f"guest bridge did not become healthy at {health_url}"

    return result
