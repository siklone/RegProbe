#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path


QGA_TARGET_NAME = "org.qemu.guest_agent.0"


def run_command(*args: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run(args, capture_output=True, text=True)


def qga_channel_present(domain_xml: str) -> bool:
    root = ET.fromstring(domain_xml)
    for channel in root.findall("./devices/channel"):
        target = channel.find("./target")
        if target is not None and target.get("name") == QGA_TARGET_NAME:
            return True
    return False


def main() -> int:
    parser = argparse.ArgumentParser(description="Ensure the KVM qemu guest agent channel is attached.")
    parser.add_argument("--domain", default="regprobe-win11-25h2-session")
    parser.add_argument("--emit-json", action="store_true")
    args = parser.parse_args()

    dumpxml = run_command("virsh", "dumpxml", args.domain)
    if dumpxml.returncode != 0:
        raise SystemExit(dumpxml.stderr.strip() or dumpxml.stdout.strip() or "virsh dumpxml failed")

    initially_present = qga_channel_present(dumpxml.stdout)
    attached = False

    if not initially_present:
        xml_payload = """<channel type='unix'>
  <source mode='bind'/>
  <target type='virtio' name='org.qemu.guest_agent.0'/>
</channel>
"""
        temp_path = None
        try:
            with tempfile.NamedTemporaryFile("w", suffix=".xml", delete=False, encoding="utf-8") as handle:
                handle.write(xml_payload)
                temp_path = handle.name
            attach = run_command("virsh", "attach-device", args.domain, temp_path, "--live", "--config")
            if attach.returncode != 0:
                raise SystemExit(attach.stderr.strip() or attach.stdout.strip() or "virsh attach-device failed")
            attached = True
        finally:
            if temp_path:
                try:
                    Path(temp_path).unlink()
                except FileNotFoundError:
                    pass

    dumpxml_after = run_command("virsh", "dumpxml", args.domain)
    present_after = dumpxml_after.returncode == 0 and qga_channel_present(dumpxml_after.stdout)
    guest_ping = run_command("virsh", "qemu-agent-command", args.domain, '{"execute":"guest-ping"}')

    payload = {
        "domain": args.domain,
        "initially_present": initially_present,
        "attached": attached,
        "present_after": present_after,
        "guest_ping_returncode": guest_ping.returncode,
        "guest_ping_stdout": guest_ping.stdout.strip(),
        "guest_ping_stderr": guest_ping.stderr.strip(),
        "guest_agent_connected": guest_ping.returncode == 0,
    }

    if args.emit_json:
        print(json.dumps(payload, indent=2))
    else:
        print(
            f"domain={args.domain} initially_present={initially_present} attached={attached} "
            f"present_after={present_after} guest_agent_connected={payload['guest_agent_connected']}"
        )
        if payload["guest_ping_stderr"]:
            print(payload["guest_ping_stderr"])
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
