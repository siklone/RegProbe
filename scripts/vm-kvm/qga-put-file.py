#!/usr/bin/env python3
from __future__ import annotations

import argparse
import base64
import hashlib
import json
import subprocess
from pathlib import Path


def run_agent_command(domain: str, payload: dict[str, object], *, connect: str, timeout: int) -> dict[str, object]:
    cmd = ["virsh"]
    if connect:
        cmd.extend(["-c", connect])
    cmd.extend(["qemu-agent-command", domain, json.dumps(payload), "--timeout", str(timeout)])
    output = subprocess.check_output(cmd, text=True)
    return json.loads(output)["return"]


def sha256_path(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        while True:
            chunk = handle.read(1024 * 1024)
            if not chunk:
                break
            digest.update(chunk)
    return digest.hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser(description="Upload a host file into the guest through qemu guest agent guest-file-* commands.")
    parser.add_argument("--domain", default="regprobe-win11-25h2-session")
    parser.add_argument("--connect", default="")
    parser.add_argument("--source", required=True)
    parser.add_argument("--destination", required=True)
    parser.add_argument("--timeout", type=int, default=10)
    parser.add_argument("--chunk-size", type=int, default=32768)
    parser.add_argument("--mode", default="wb")
    args = parser.parse_args()

    source = Path(args.source).resolve()
    size = source.stat().st_size
    digest = sha256_path(source)

    opened = run_agent_command(
        args.domain,
        {
            "execute": "guest-file-open",
            "arguments": {
                "path": args.destination,
                "mode": args.mode,
            },
        },
        connect=args.connect,
        timeout=args.timeout,
    )
    handle = int(opened)

    written_total = 0
    try:
        with source.open("rb") as infile:
            while True:
                chunk = infile.read(args.chunk_size)
                if not chunk:
                    break
                payload = {
                    "execute": "guest-file-write",
                    "arguments": {
                        "handle": handle,
                        "buf-b64": base64.b64encode(chunk).decode("ascii"),
                    },
                }
                result = run_agent_command(args.domain, payload, connect=args.connect, timeout=args.timeout)
                written_total += int(result.get("count", 0))

        run_agent_command(
            args.domain,
            {"execute": "guest-file-flush", "arguments": {"handle": handle}},
            connect=args.connect,
            timeout=args.timeout,
        )
    finally:
        run_agent_command(
            args.domain,
            {"execute": "guest-file-close", "arguments": {"handle": handle}},
            connect=args.connect,
            timeout=args.timeout,
        )

    print(
        json.dumps(
            {
                "status": "uploaded",
                "domain": args.domain,
                "source": str(source),
                "destination": args.destination,
                "size_bytes": size,
                "written_bytes": written_total,
                "sha256": digest,
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
