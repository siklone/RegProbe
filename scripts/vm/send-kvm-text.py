#!/usr/bin/env python3
from __future__ import annotations

import argparse
import subprocess
import sys
import time
from pathlib import Path


DEFAULT_DOMAIN = "regprobe-win11-25h2-session"
DEFAULT_CODESET = "linux"


def vk_letter(ch: str) -> list[str]:
    key = f"KEY_{ch.upper()}"
    if ch.isupper():
        return ["KEY_LEFTSHIFT", key]
    return [key]


def vk_digit(ch: str) -> list[str]:
    return [f"KEY_{ch}"]


CHAR_MAP: dict[str, list[str]] = {
    " ": ["KEY_SPACE"],
    "\n": ["KEY_ENTER"],
    "\r": ["KEY_ENTER"],
    "\t": ["KEY_TAB"],
    ".": ["KEY_DOT"],
    ",": ["KEY_COMMA"],
    "-": ["KEY_MINUS"],
    "_": ["KEY_LEFTSHIFT", "KEY_MINUS"],
    "/": ["KEY_SLASH"],
    "?": ["KEY_LEFTSHIFT", "KEY_SLASH"],
    "\\": ["KEY_BACKSLASH"],
    "|": ["KEY_LEFTSHIFT", "KEY_BACKSLASH"],
    ";": ["KEY_SEMICOLON"],
    ":": ["KEY_LEFTSHIFT", "KEY_SEMICOLON"],
    "'": ["KEY_APOSTROPHE"],
    '"': ["KEY_LEFTSHIFT", "KEY_APOSTROPHE"],
    "=": ["KEY_EQUAL"],
    "+": ["KEY_LEFTSHIFT", "KEY_EQUAL"],
    "[": ["KEY_LEFTBRACE"],
    "{": ["KEY_LEFTSHIFT", "KEY_LEFTBRACE"],
    "]": ["KEY_RIGHTBRACE"],
    "}": ["KEY_LEFTSHIFT", "KEY_RIGHTBRACE"],
    "(": ["KEY_LEFTSHIFT", "KEY_9"],
    ")": ["KEY_LEFTSHIFT", "KEY_0"],
    "!": ["KEY_LEFTSHIFT", "KEY_1"],
}


def char_to_keys(ch: str) -> list[str]:
    if ch.isalpha():
        return vk_letter(ch)
    if ch.isdigit():
        return vk_digit(ch)
    if ch in CHAR_MAP:
        return CHAR_MAP[ch]
    raise ValueError(f"Unsupported character for virsh send-key mapping: {ch!r}")


def run_send_key(domain: str, keys: list[str], codeset: str, holdtime: int) -> None:
    proc = subprocess.run(
        ["virsh", "send-key", domain, "--codeset", codeset, "--holdtime", str(holdtime), *keys],
        capture_output=True,
        text=True,
    )
    if proc.returncode != 0:
        raise RuntimeError(proc.stderr.strip() or proc.stdout.strip() or "virsh send-key failed")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Type text into the focused KVM guest window via virsh send-key.")
    parser.add_argument("--domain", default=DEFAULT_DOMAIN, help="libvirt domain name")
    parser.add_argument("--codeset", default=DEFAULT_CODESET, help="virsh key code set")
    parser.add_argument("--holdtime", type=int, default=40, help="Key hold time in milliseconds")
    parser.add_argument("--delay-ms", type=int, default=35, help="Delay between characters in milliseconds")
    parser.add_argument("--text", help="Text to type. If omitted, read from stdin.")
    parser.add_argument("--screenshot", type=Path, help="Optional screenshot path to capture after typing.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    text = args.text if args.text is not None else sys.stdin.read()
    if not text:
        raise SystemExit("No text provided")

    for ch in text:
        run_send_key(args.domain, char_to_keys(ch), args.codeset, args.holdtime)
        time.sleep(args.delay_ms / 1000.0)

    if args.screenshot:
        proc = subprocess.run(
            ["virsh", "screenshot", args.domain, str(args.screenshot)],
            capture_output=True,
            text=True,
        )
        if proc.returncode != 0:
            raise RuntimeError(proc.stderr.strip() or proc.stdout.strip() or "virsh screenshot failed")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
