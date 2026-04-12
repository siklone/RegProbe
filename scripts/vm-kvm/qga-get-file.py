#!/usr/bin/env python3
from __future__ import annotations

import argparse
import base64
import hashlib
import json
import subprocess
from pathlib import Path


def run_agent_command(domain: str, payload: dict[str, object], *, connect: str, timeout: int) -> object:
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
    parser = argparse.ArgumentParser(description="Download a guest file through qemu guest agent guest-file-* commands.")
    parser.add_argument("--domain", default="regprobe-win11-25h2-session")
    parser.add_argument("--connect", default="")
    parser.add_argument("--source", required=True, help="Guest file path.")
    parser.add_argument("--destination", required=True, help="Host output path.")
    parser.add_argument("--timeout", type=int, default=10)
    parser.add_argument("--chunk-size", type=int, default=32768)
    args = parser.parse_args()

    destination = Path(args.destination).resolve()
    destination.parent.mkdir(parents=True, exist_ok=True)

    opened = run_agent_command(
        args.domain,
        {
            "execute": "guest-file-open",
            "arguments": {
                "path": args.source,
                "mode": "rb",
            },
        },
        connect=args.connect,
        timeout=args.timeout,
    )
    handle = int(opened)

    read_total = 0
    try:
        with destination.open("wb") as outfile:
            while True:
                result = run_agent_command(
                    args.domain,
                    {
                        "execute": "guest-file-read",
                        "arguments": {
                            "handle": handle,
                            "count": args.chunk_size,
                        },
                    },
                    connect=args.connect,
                    timeout=args.timeout,
                )
                if not isinstance(result, dict):
                    raise RuntimeError(f"Unexpected guest-file-read result: {result!r}")

                data = base64.b64decode(str(result.get("buf-b64", "")))
                if data:
                    outfile.write(data)
                    read_total += len(data)
                if bool(result.get("eof")):
                    break
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
                "status": "downloaded",
                "domain": args.domain,
                "source": args.source,
                "destination": str(destination),
                "read_bytes": read_total,
                "size_bytes": destination.stat().st_size,
                "sha256": sha256_path(destination),
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
