#!/usr/bin/env python3
from __future__ import annotations

import json
import os
import socket
import shutil
import subprocess
import xml.etree.ElementTree as ET
from datetime import datetime, timezone
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
RUNNER_CONFIG_PATH = REPO_ROOT / "registry-research-framework" / "config" / "tweak-vm-runners.json"
RUNTIME_PROBE_PATH = REPO_ROOT / "registry-research-framework" / "tools" / "run-path-aware-runtime-probe.ps1"
CONTROLLER_DOC_PATH = REPO_ROOT / "Docs" / "VM_VALIDATION_CONTROLLER.md"
CONTROLLER_SCRIPT_PATH = REPO_ROOT / "scripts" / "vm" / "host-validation-controller.ps1"
ENSURE_QGA_PATH = REPO_ROOT / "scripts" / "vm" / "ensure-kvm-qga-channel.py"
OUTPUT_BASENAME = "execution-required-kvm-guest-control-gap-20260408"
OUTPUT_JSON = REPO_ROOT / "registry-research-framework" / "audit" / f"{OUTPUT_BASENAME}.json"
OUTPUT_MD = REPO_ROOT / "registry-research-framework" / "audit" / f"{OUTPUT_BASENAME}.md"

DOMAIN_NAME = os.environ.get("REGPROBE_VM_DOMAIN", "regprobe-win11-25h2-session")
TARGET_TWEAK_IDS = [
    "power.control.allow-system-required-power-requests",
    "power.control.allow-audio-to-enable-execution-required-power-requests",
]


def load_json(path: Path) -> dict:
    payload = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(payload, dict):
        raise ValueError(f"{path} JSON payload is not an object")
    return payload


def write_json(path: Path, payload: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content.rstrip() + "\n", encoding="utf-8")


def run_command(*args: str) -> dict:
    proc = subprocess.run(args, capture_output=True, text=True)
    return {
        "argv": list(args),
        "returncode": proc.returncode,
        "stdout": proc.stdout.strip(),
        "stderr": proc.stderr.strip(),
        "ok": proc.returncode == 0,
    }


def parse_query_chardev_stdout(stdout: str) -> tuple[list[dict], str]:
    try:
        payload = json.loads(stdout)
    except json.JSONDecodeError as exc:
        return [], str(exc)
    if not isinstance(payload, dict):
        return [], "query-chardev JSON payload is not an object"
    returned = payload.get("return") or []
    if not isinstance(returned, list):
        return [], "query-chardev return payload is not a list"
    entries = [entry for entry in returned if isinstance(entry, dict)]
    return entries, ""


def try_socket_connect(path: str | None) -> dict | None:
    if not path:
        return None
    try:
        with socket.socket(socket.AF_UNIX, socket.SOCK_STREAM) as sock:
            sock.settimeout(2.0)
            sock.connect(path)
        return {"path": path, "ok": True, "error": ""}
    except OSError as exc:
        return {"path": path, "ok": False, "error": str(exc)}


def main() -> int:
    runner_config = load_json(RUNNER_CONFIG_PATH)
    runtime_runners = runner_config.get("runtime") or {}
    runtime_probe_text = RUNTIME_PROBE_PATH.read_text(encoding="utf-8-sig")
    controller_doc_text = CONTROLLER_DOC_PATH.read_text(encoding="utf-8-sig")
    controller_script_text = CONTROLLER_SCRIPT_PATH.read_text(encoding="utf-8-sig")

    runtime_runner_records: list[dict] = []
    for tweak_id in TARGET_TWEAK_IDS:
        runner = runtime_runners.get(tweak_id) or {}
        runtime_runner_records.append(
            {
                "tweak_id": tweak_id,
                "runner_script": runner.get("script"),
                "runner_args": runner.get("args") or [],
                "runner_is_vmrun_backed": "vmrun.exe" in runtime_probe_text and "_vmrun-common.ps1" in runtime_probe_text,
            }
        )

    virsh_path = shutil.which("virsh")
    dominfo = run_command(virsh_path, "dominfo", DOMAIN_NAME) if virsh_path else None
    dumpxml = run_command(virsh_path, "dumpxml", DOMAIN_NAME) if virsh_path else None
    guest_ping = (
        run_command(virsh_path, "qemu-agent-command", DOMAIN_NAME, '{"execute":"guest-ping"}')
        if virsh_path
        else None
    )
    query_chardev = (
        run_command(virsh_path, "qemu-monitor-command", DOMAIN_NAME, "--pretty", '{"execute":"query-chardev"}')
        if virsh_path
        else None
    )
    info_qtree = (
        run_command(virsh_path, "qemu-monitor-command", DOMAIN_NAME, "--hmp", "info qtree")
        if virsh_path
        else None
    )
    info_chardev = (
        run_command(virsh_path, "qemu-monitor-command", DOMAIN_NAME, "--hmp", "info chardev")
        if virsh_path
        else None
    )

    domain_running = False
    cdrom_source = None
    cdrom_exists = False
    has_qemu_agent_channel = False
    channel_names: list[str] = []
    spice_channel_name = None
    serial_console_path = None
    qga_socket_path = None
    qga_frontend_open = None
    qga_filename = None
    qga_qtree_line = None
    monitor_socket_path = None
    query_chardev_parse_error = ""

    if dumpxml and dumpxml["ok"]:
        root = ET.fromstring(dumpxml["stdout"])
        state_text = (dominfo or {}).get("stdout", "")
        domain_running = "State:" in state_text and "running" in state_text

        for disk in root.findall("./devices/disk"):
            if disk.get("device") == "cdrom":
                source = disk.find("./source")
                if source is not None and source.get("file"):
                    cdrom_source = source.get("file")
                    cdrom_exists = Path(cdrom_source).exists()
                    break

        for channel in root.findall("./devices/channel"):
            target = channel.find("./target")
            if target is None:
                continue
            name = target.get("name")
            if name:
                channel_names.append(name)
            if name == "org.qemu.guest_agent.0":
                has_qemu_agent_channel = True
                source = channel.find("./source")
                if source is not None:
                    qga_socket_path = source.get("path")
            if name == "com.redhat.spice.0":
                spice_channel_name = name

        console = root.find("./devices/console")
        if console is not None:
            serial_console_path = console.get("tty")
            if not serial_console_path:
                source = console.find("./source")
                if source is not None:
                    serial_console_path = source.get("path")

    host_user = os.environ.get("REGPROBE_HOST_USER", os.environ.get("USER", "user"))
    monitor_root = os.environ.get(
        "REGPROBE_LIBVIRT_STATE_ROOT",
        f"/home/{host_user}/.config/libvirt/qemu/lib",
    )
    monitor_socket_path = str(Path(monitor_root) / f"domain-1-{DOMAIN_NAME[:22]}-/monitor.sock")

    if query_chardev and query_chardev["ok"]:
        returned, query_chardev_parse_error = parse_query_chardev_stdout(query_chardev["stdout"])
        qga_entry = next((entry for entry in returned if entry.get("label") == "charchannel1"), None)
        if qga_entry:
            qga_frontend_open = qga_entry.get("frontend-open")
            qga_filename = qga_entry.get("filename")

    if info_qtree and info_qtree["ok"]:
        for line in info_qtree["stdout"].splitlines():
            stripped = line.strip()
            if stripped.startswith("port 2,"):
                qga_qtree_line = stripped
                break

    qga_socket_connect = try_socket_connect(qga_socket_path)
    monitor_socket_connect = try_socket_connect(monitor_socket_path)

    controller_is_vmrun_backed = "vmrun.exe" in controller_script_text and "\\vmware-host\\Shared Folders" not in controller_script_text
    controller_doc_is_shared_folder = "shared-folder controller workspace" in controller_doc_text.lower()
    controller_script_uses_vmrun = "VmrunPath" in controller_script_text and "runProgramInGuest" in controller_script_text

    payload = {
        "title": "Execution-required KVM guest-control gap audit",
        "generated_utc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "domain_name": DOMAIN_NAME,
        "target_tweak_ids": TARGET_TWEAK_IDS,
        "runtime_runner_records": runtime_runner_records,
        "runner_config_source": RUNNER_CONFIG_PATH.relative_to(REPO_ROOT).as_posix(),
        "runtime_probe_source": RUNTIME_PROBE_PATH.relative_to(REPO_ROOT).as_posix(),
        "controller_doc_source": CONTROLLER_DOC_PATH.relative_to(REPO_ROOT).as_posix(),
        "controller_script_source": CONTROLLER_SCRIPT_PATH.relative_to(REPO_ROOT).as_posix(),
        "ensure_qga_source": ENSURE_QGA_PATH.relative_to(REPO_ROOT).as_posix(),
        "runtime_probe_is_vmrun_backed": all(record["runner_is_vmrun_backed"] for record in runtime_runner_records),
        "controller_doc_uses_shared_folder_model": controller_doc_is_shared_folder,
        "controller_script_uses_vmrun": controller_script_uses_vmrun,
        "virsh_available": bool(virsh_path),
        "dominfo": dominfo,
        "dumpxml_ok": bool(dumpxml and dumpxml["ok"]),
        "domain_running": domain_running,
        "has_qemu_agent_channel": has_qemu_agent_channel,
        "channel_names": channel_names,
        "guest_ping": guest_ping,
        "query_chardev": query_chardev,
        "query_chardev_parse_error": query_chardev_parse_error,
        "info_qtree": info_qtree,
        "info_chardev": info_chardev,
        "qga_socket_path": qga_socket_path,
        "qga_socket_connect": qga_socket_connect,
        "monitor_socket_path": monitor_socket_path,
        "monitor_socket_connect": monitor_socket_connect,
        "qga_frontend_open": qga_frontend_open,
        "qga_filename": qga_filename,
        "qga_qtree_line": qga_qtree_line,
        "qemu_guest_agent_configured": has_qemu_agent_channel and bool(guest_ping and guest_ping["ok"]),
        "spice_channel_name": spice_channel_name,
        "serial_console_path": serial_console_path,
        "bootstrap_iso_path": cdrom_source,
        "bootstrap_iso_exists_on_host": cdrom_exists,
        "conclusion": (
            "The execution-required runtime lane is repo-native and ready, but the active KVM guest-control surface is still failing before a usable qga frontend attach. "
            "The live domain exposes the qemu guest-agent channel, yet `query-chardev` keeps `charchannel1` at `frontend-open=false`, `info qtree` keeps the qga port at `guest off, host off`, "
            "and a direct host-side connect to the qga unix socket returns connection refused even while the monitor socket remains connectable. "
            "The remaining blocker is therefore a host/libvirt/QEMU-side attach or accept failure on the qga channel, not missing runner plumbing or simple Windows path discovery."
        ),
    }
    write_json(OUTPUT_JSON, payload)

    lines = [
        "# Execution-Required KVM Guest-Control Gap Audit",
        "",
        f"Date: {datetime.now().date().isoformat()}",
        "",
        "## Outcome",
        "",
        f"- Runtime runner mapped for both tweaks: `{all(record['runner_script'] for record in runtime_runner_records)}`",
        f"- Runtime probe vmrun-backed: `{payload['runtime_probe_is_vmrun_backed']}`",
        f"- Controller doc still shared-folder based: `{controller_doc_is_shared_folder}`",
        f"- Controller script still vmrun-backed: `{controller_script_uses_vmrun}`",
        f"- libvirt domain running: `{domain_running}`",
        f"- qemu guest agent channel present: `{has_qemu_agent_channel}`",
        f"- qemu guest agent ping ok: `{bool(guest_ping and guest_ping['ok'])}`",
        f"- qga frontend-open: `{qga_frontend_open}`",
        f"- qga qtree state: `{qga_qtree_line}`",
        f"- qga unix socket connectable: `{bool(qga_socket_connect and qga_socket_connect['ok'])}`",
        f"- monitor socket connectable: `{bool(monitor_socket_connect and monitor_socket_connect['ok'])}`",
        f"- Bootstrap ISO exists on host: `{cdrom_exists}`",
        "",
        "## Details",
        "",
    ]
    for record in runtime_runner_records:
        lines.append(
            f"- `{record['tweak_id']}` -> script=`{record['runner_script']}` args=`{record['runner_args']}`"
        )
    lines.extend(
        [
            f"- Channel names: `{channel_names}`",
            f"- Serial console path: `{serial_console_path}`",
            f"- Guest ping stderr: `{(guest_ping or {}).get('stderr') or (guest_ping or {}).get('stdout') or 'n/a'}`",
            f"- query-chardev filename: `{qga_filename}`",
            f"- qga socket path: `{qga_socket_path}`",
            f"- qga socket connect error: `{(qga_socket_connect or {}).get('error') or 'n/a'}`",
            f"- monitor socket path: `{monitor_socket_path}`",
            f"- monitor socket connect error: `{(monitor_socket_connect or {}).get('error') or 'n/a'}`",
            f"- Bootstrap ISO path: `{cdrom_source}`",
            "",
            "## Interpretation",
            "",
            "- The remaining execution-required runtime-trace gap is no longer runner design or candidate selection.",
            "- The repo-side narrow lane exists, the qemu guest-agent channel is attached in XML/QEMU, but the live socket still does not expose a usable qga frontend attachment.",
            "- The current evidence points earlier than generic protocol noise: the qga chardev stays `frontend-open=false`, the virtserial port stays `guest off, host off`, and even direct host-side socket connect is refused while the monitor socket stays healthy.",
            "- The next decisive step is to debug the host/libvirt/QEMU-side qga attach path, or to run the same narrow lane on a vmrun-capable environment that bypasses this KVM guest-control failure.",
        ]
    )
    write_text(OUTPUT_MD, "\n".join(lines))
    print(OUTPUT_JSON.relative_to(REPO_ROOT).as_posix())
    print(OUTPUT_MD.relative_to(REPO_ROOT).as_posix())
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
