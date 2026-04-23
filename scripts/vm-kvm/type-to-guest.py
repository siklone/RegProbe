#!/usr/bin/env python3
"""Type ASCII text into a libvirt guest by replaying virsh send-key events."""

from __future__ import annotations

import argparse
import subprocess
import sys
import time

from vm_env import vm_connect


SHIFT = "KEY_LEFTSHIFT"


def build_keymap() -> dict[str, tuple[str, bool]]:
    mapping: dict[str, tuple[str, bool]] = {}

    for char in "abcdefghijklmnopqrstuvwxyz":
        mapping[char] = (f"KEY_{char.upper()}", False)
        mapping[char.upper()] = (f"KEY_{char.upper()}", True)

    for digit in "0123456789":
        mapping[digit] = (f"KEY_{digit}", False)

    mapping.update(
        {
            " ": ("KEY_SPACE", False),
            "\n": ("KEY_ENTER", False),
            "\r": ("KEY_ENTER", False),
            "\t": ("KEY_TAB", False),
            "-": ("KEY_MINUS", False),
            "_": ("KEY_MINUS", True),
            "=": ("KEY_EQUAL", False),
            "+": ("KEY_EQUAL", True),
            "[": ("KEY_LEFTBRACE", False),
            "{": ("KEY_LEFTBRACE", True),
            "]": ("KEY_RIGHTBRACE", False),
            "}": ("KEY_RIGHTBRACE", True),
            "\\": ("KEY_BACKSLASH", False),
            "|": ("KEY_BACKSLASH", True),
            ";": ("KEY_SEMICOLON", False),
            ":": ("KEY_SEMICOLON", True),
            "'": ("KEY_APOSTROPHE", False),
            '"': ("KEY_APOSTROPHE", True),
            ",": ("KEY_COMMA", False),
            "<": ("KEY_COMMA", True),
            ".": ("KEY_DOT", False),
            ">": ("KEY_DOT", True),
            "/": ("KEY_SLASH", False),
            "?": ("KEY_SLASH", True),
            "`": ("KEY_GRAVE", False),
            "~": ("KEY_GRAVE", True),
            "!": ("KEY_1", True),
            "@": ("KEY_2", True),
            "#": ("KEY_3", True),
            "$": ("KEY_4", True),
            "%": ("KEY_5", True),
            "^": ("KEY_6", True),
            "&": ("KEY_7", True),
            "*": ("KEY_8", True),
            "(": ("KEY_9", True),
            ")": ("KEY_0", True),
        }
    )
    return mapping


def send_key(connect: str, domain: str, key: str, shifted: bool) -> None:
    args = ["virsh", "-c", connect, "send-key", domain]
    if shifted:
        args.append(SHIFT)
    args.append(key)
    subprocess.run(args, check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("domain", help="libvirt domain name")
    parser.add_argument("text", nargs="?", default="", help="text to type")
    parser.add_argument("--connect", default=vm_connect("qemu:///session"), help="libvirt connection URI")
    parser.add_argument("--delay-ms", type=float, default=20.0, help="delay between key presses")
    parser.add_argument("--enter", action="store_true", help="press Enter after typing the text")
    parser.add_argument("--wake-key", default="", help="optional key to send before typing, e.g. KEY_ENTER")
    args = parser.parse_args()

    keymap = build_keymap()
    delay_seconds = max(args.delay_ms, 0.0) / 1000.0

    try:
        if args.wake_key:
            send_key(args.connect, args.domain, args.wake_key, False)
            time.sleep(delay_seconds)

        for char in args.text:
            key, shifted = keymap[char]
            send_key(args.connect, args.domain, key, shifted)
            time.sleep(delay_seconds)

        if args.enter:
            send_key(args.connect, args.domain, "KEY_ENTER", False)
    except KeyError as exc:
        print(f"unsupported character for send-key typing: {exc.args[0]!r}", file=sys.stderr)
        return 2
    except subprocess.CalledProcessError as exc:
        return_code = exc.returncode or 1
        print(f"virsh send-key failed with exit code {return_code}", file=sys.stderr)
        return return_code

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
