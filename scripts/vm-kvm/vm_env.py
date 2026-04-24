#!/usr/bin/env python3
from __future__ import annotations

import importlib.util
from pathlib import Path


def _load_core_vm_env():
    module_path = Path(__file__).resolve().parents[1] / "vm-core" / "vm_env.py"
    spec = importlib.util.spec_from_file_location("regprobe_vm_core_env", module_path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Unable to load vm-core environment module from {module_path}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


_CORE = _load_core_vm_env()
BACKEND = _CORE.BACKEND
get_backend = _CORE.get_backend
get_bridge_url = _CORE.get_bridge_url
get_connect_uri = _CORE.get_connect_uri
get_host_user = _CORE.get_host_user
get_upload_dir = _CORE.get_upload_dir
get_vm_domain = _CORE.get_vm_domain
get_vm_snapshot = _CORE.get_vm_snapshot
get_vm_user = _CORE.get_vm_user
env_or = _CORE.env_or
vm_domain = _CORE.vm_domain
vm_connect = _CORE.vm_connect
vm_snapshot = _CORE.vm_snapshot
vm_user = _CORE.vm_user
host_user = _CORE.host_user
bridge_base_url = _CORE.bridge_base_url
upload_dir = _CORE.upload_dir
guest_scripts_root = _CORE.guest_scripts_root
crash_log_dir = _CORE.crash_log_dir
libvirt_state_root = _CORE.libvirt_state_root
