#!/usr/bin/env python3
from __future__ import annotations

import os


def env_or(name: str, default: str) -> str:
    value = os.environ.get(name, "").strip()
    return value or default


def vm_domain(default: str = "regprobe-win11-25h2-session") -> str:
    return env_or("REGPROBE_VM_DOMAIN", default)


def vm_connect(default: str = "qemu:///session") -> str:
    return env_or("REGPROBE_VM_CONNECT", default)


def vm_snapshot(default: str = "RegProbe-Baseline") -> str:
    return env_or("REGPROBE_VM_SNAPSHOT", default)


def vm_user(default: str = "Administrator") -> str:
    return env_or("REGPROBE_VM_USER", env_or("REGPROBE_VM_GUEST_USER", default))


def host_user(default: str = "user") -> str:
    return env_or("REGPROBE_HOST_USER", env_or("USER", env_or("USERNAME", default)))


def bridge_base_url(default: str = "http://10.0.2.2:8766") -> str:
    return env_or("REGPROBE_VM_BRIDGE_BASE_URL", default)


def upload_dir(default: str = "/tmp/regprobe-bridge") -> str:
    return env_or("REGPROBE_VM_UPLOAD_DIR", default)


def guest_scripts_root(default: str = r"C:\RegProbe-Diag\bootstrap") -> str:
    return env_or("REGPROBE_VM_GUEST_SCRIPTS_ROOT", default)


def crash_log_dir(default: str | None = None) -> str:
    if default:
        return env_or("REGPROBE_VM_CRASH_LOG_DIR", default)

    return env_or(
        "REGPROBE_VM_CRASH_LOG_DIR",
        rf"C:\Users\{vm_user()}\AppData\Local\RegProbe\CrashLogs",
    )


def libvirt_state_root(default: str | None = None) -> str:
    if default:
        return env_or("REGPROBE_LIBVIRT_STATE_ROOT", default)

    return env_or(
        "REGPROBE_LIBVIRT_STATE_ROOT",
        f"/home/{host_user()}/.config/libvirt/qemu/lib",
    )
