#!/usr/bin/env python3
from __future__ import annotations

import importlib.util
from pathlib import Path


def _load_vm_backend():
    module_path = Path(__file__).resolve().with_name("vm_backend.py")
    spec = importlib.util.spec_from_file_location("regprobe_vm_backend", module_path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Unable to load vm backend module from {module_path}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


_BACKEND = _load_vm_backend()
BACKEND = _BACKEND.BACKEND
get_backend = _BACKEND.get_backend
get_bridge_url = _BACKEND.get_bridge_url
get_connect_uri = _BACKEND.get_connect_uri
get_host_user = _BACKEND.get_host_user
get_upload_dir = _BACKEND.get_upload_dir
get_vm_domain = _BACKEND.get_vm_domain
get_vm_snapshot = _BACKEND.get_vm_snapshot
get_vm_user = _BACKEND.get_vm_user


def env_or(*names: str, default: str) -> str:
    for name in names:
        import os

        value = os.environ.get(name, "").strip()
        if value:
            return value
    return default


def vm_domain(default: str = "regprobe-win11") -> str:
    return get_vm_domain(default)


def vm_connect(default: str = "qemu:///session") -> str:
    value = get_connect_uri()
    return value or default


def vm_snapshot(default: str = "RegProbe-Baseline") -> str:
    return get_vm_snapshot(default)


def vm_user(default: str = "Administrator") -> str:
    return get_vm_user(default)


def host_user(default: str = "user") -> str:
    return get_host_user(default)


def bridge_base_url(default: str = "http://10.0.2.2:8766") -> str:
    return get_bridge_url(default)


def upload_dir(default: str = "/tmp/regprobe-bridge") -> str:
    return get_upload_dir(default)


def guest_scripts_root(default: str = r"C:\RegProbe-Diag\bootstrap") -> str:
    return env_or("REGPROBE_VM_GUEST_SCRIPTS_ROOT", default=default)


def crash_log_dir(default: str | None = None) -> str:
    if default:
        return env_or("REGPROBE_VM_CRASH_LOG_DIR", default=default)
    return env_or(
        "REGPROBE_VM_CRASH_LOG_DIR",
        default=rf"C:\Users\{vm_user()}\AppData\Local\RegProbe\CrashLogs",
    )


def libvirt_state_root(default: str | None = None) -> str:
    if default:
        return env_or("REGPROBE_LIBVIRT_STATE_ROOT", default=default)
    return env_or(
        "REGPROBE_LIBVIRT_STATE_ROOT",
        default=f"/home/{host_user()}/.config/libvirt/qemu/lib",
    )
