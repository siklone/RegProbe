#!/usr/bin/env python3
from __future__ import annotations

import os

"""
VM backend abstraction for RegProbe research scripts.
Reads REGPROBE_VM_BACKEND env var to select backend.
Supported: kvm (default), vmware, hyperv, virtualbox
"""

BACKEND = os.environ.get("REGPROBE_VM_BACKEND", "kvm").strip().lower() or "kvm"


def _env_or(*names: str, default: str) -> str:
    for name in names:
        value = os.environ.get(name, "").strip()
        if value:
            return value
    return default


def get_vm_domain(default: str = "regprobe-win11") -> str:
    return _env_or("REGPROBE_VM_DOMAIN", "REGPROBE_VM_NAME", default=default)


def get_vm_user(default: str = "Administrator") -> str:
    return _env_or("REGPROBE_VM_USER", "REGPROBE_VM_GUEST_USER", default=default)


def get_vm_snapshot(default: str = "RegProbe-Baseline") -> str:
    return _env_or(
        "REGPROBE_VM_SNAPSHOT",
        "REGPROBE_VM_DEFAULT_SNAPSHOT",
        default=default,
    )


def get_host_user(default: str = "user") -> str:
    return _env_or("REGPROBE_HOST_USER", "USER", "USERNAME", default=default)


def get_bridge_url(default: str = "http://10.0.2.2:8766") -> str:
    return _env_or("REGPROBE_BRIDGE_URL", "REGPROBE_VM_BRIDGE_BASE_URL", default=default)


def get_upload_dir(default: str = "/tmp/regprobe-bridge") -> str:
    return _env_or("REGPROBE_VM_UPLOAD_DIR", default=default)


def get_connect_uri() -> str:
    if BACKEND == "kvm":
        return _env_or("REGPROBE_VM_CONNECT", default="qemu:///session")
    return _env_or("REGPROBE_VM_CONNECT", default="")


def get_backend() -> str:
    return BACKEND

