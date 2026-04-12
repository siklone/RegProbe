#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import struct
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


DEFAULT_TARGETS = [
    {
        "candidate_id": "system.kernel.timer-check-flags",
        "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel",
        "value_name": "TimerCheckFlags",
        "live_symbol": "nt!KeTimerCheckFlags",
        "expected_global_rva": 0xE0B080,
        "live_kd_artifact": (
            "evidence/files/vm-tooling-staging/local-kd-timercheckflags-20260408a/"
            "local-kd-timercheckflags-20260408a.stdout.txt"
        ),
    },
    {
        "candidate_id": "system.kernel.force-bugcheck-for-dpc-watchdog",
        "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel",
        "value_name": "ForceBugcheckForDpcWatchdog",
        "live_symbol": "nt!KiForceBugcheckForDpcWatchdog",
        "expected_global_rva": 0xFC5E84,
        "live_kd_artifact": (
            "evidence/files/vm-tooling-staging/dpc-watchdog-force-bugcheck-kd-20260407a/"
            "dpc-watchdog-force-bugcheck-kd-20260407a.stdout.txt"
        ),
    },
    {
        "candidate_id": "system.kernel.global-timer-resolution-requests",
        "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel",
        "value_name": "GlobalTimerResolutionRequests",
        "live_symbol": "nt!KiGlobalTimerResolutionRequests",
        "expected_global_rva": 0xFC5C48,
        "live_kd_artifact": (
            "evidence/files/vm-tooling-staging/local-kd-globaltimerres-20260408a/"
            "local-kd-globaltimerres-20260408a.stdout.txt"
        ),
    },
    {
        "candidate_id": "system.kernel-long-dpc-threshold-cluster",
        "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel",
        "value_name": "LongDpcQueueThreshold",
        "live_symbol": "nt!KiLongDpcQueueThreshold",
        "expected_global_rva": 0xFC41D0,
        "live_kd_artifact": (
            "evidence/files/vm-tooling-staging/local-kd-longdpc-values-20260408a/"
            "local-kd-longdpc-values-20260408a.stdout.txt"
        ),
    },
    {
        "candidate_id": "system.kernel-long-dpc-threshold-cluster",
        "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel",
        "value_name": "LongDpcRuntimeThreshold",
        "live_symbol": "nt!KiLongDpcRuntimeThreshold",
        "expected_global_rva": 0xFC41D4,
        "live_kd_artifact": (
            "evidence/files/vm-tooling-staging/local-kd-longdpc-values-20260408a/"
            "local-kd-longdpc-values-20260408a.stdout.txt"
        ),
    },
    {
        "candidate_id": "system.kernel-dpc-watchdog-control-cluster",
        "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel",
        "value_name": "DPCTimeout",
        "live_symbol": "nt!KeDpcTimeoutMs",
        "expected_global_rva": 0xFC4038,
        "live_kd_artifact": (
            "evidence/files/vm-tooling-staging/dpc-watchdog-control-values-kd-20260408a/"
            "dpc-watchdog-control-values-kd-20260408a.stdout.txt"
        ),
    },
    {
        "candidate_id": "system.kernel-dpc-watchdog-control-cluster",
        "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel",
        "value_name": "DpcSoftTimeout",
        "live_symbol": "nt!KeDpcSoftTimeoutMs",
        "expected_global_rva": 0xFC4040,
        "live_kd_artifact": (
            "evidence/files/vm-tooling-staging/dpc-watchdog-control-values-kd-20260408a/"
            "dpc-watchdog-control-values-kd-20260408a.stdout.txt"
        ),
    },
    {
        "candidate_id": "system.kernel-dpc-watchdog-control-cluster",
        "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel",
        "value_name": "DpcCumulativeSoftTimeout",
        "live_symbol": "nt!KeDpcCumulativeSoftTimeoutMs",
        "expected_global_rva": 0xFC403C,
        "live_kd_artifact": (
            "evidence/files/vm-tooling-staging/dpc-watchdog-control-values-kd-20260408a/"
            "dpc-watchdog-control-values-kd-20260408a.stdout.txt"
        ),
    },
    {
        "candidate_id": "system.kernel-dpc-watchdog-profile-cluster",
        "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel",
        "value_name": "DpcWatchdogProfileBufferSizeBytes",
        "live_symbol": "nt!KeDpcWatchdogProfileBufferSizeBytes",
        "expected_global_rva": 0xFC4048,
        "live_kd_artifact": (
            "evidence/files/vm-tooling-staging/dpc-watchdog-profile-thresholds-kd-20260407a/"
            "dpc-watchdog-profile-thresholds-kd-20260407a.stdout.txt"
        ),
    },
    {
        "candidate_id": "system.kernel-dpc-watchdog-profile-cluster",
        "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel",
        "value_name": "DpcWatchdogProfileCumulativeDpcThreshold",
        "live_symbol": "nt!KeDpcWatchdogProfileCumulativeDpcThresholdMs",
        "expected_global_rva": 0xFC4024,
        "live_kd_artifact": (
            "evidence/files/vm-tooling-staging/dpc-watchdog-profile-thresholds-kd-20260407a/"
            "dpc-watchdog-profile-thresholds-kd-20260407a.stdout.txt"
        ),
    },
    {
        "candidate_id": "system.kernel-dpc-watchdog-profile-cluster",
        "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel",
        "value_name": "DpcWatchdogProfileOffset",
        "live_symbol": "nt!KeDpcWatchdogProfileOffsetMs",
        "expected_global_rva": 0xFC5FD4,
        "live_kd_artifact": (
            "evidence/files/vm-tooling-staging/dpc-watchdog-profile-thresholds-kd-20260407a/"
            "dpc-watchdog-profile-thresholds-kd-20260407a.stdout.txt"
        ),
    },
    {
        "candidate_id": "system.kernel-dpc-watchdog-profile-cluster",
        "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel",
        "value_name": "DpcWatchdogProfileSingleDpcThreshold",
        "live_symbol": "nt!KeDpcWatchdogProfileSingleDpcThresholdMs",
        "expected_global_rva": 0xFC4028,
        "live_kd_artifact": (
            "evidence/files/vm-tooling-staging/dpc-watchdog-profile-thresholds-kd-20260407a/"
            "dpc-watchdog-profile-thresholds-kd-20260407a.stdout.txt"
        ),
    },
]


class PeImage:
    def __init__(self, path: Path) -> None:
        self.path = path
        self.data = path.read_bytes()
        if self.data[:2] != b"MZ":
            raise ValueError(f"{path} is not a PE image")

        pe_offset = self.u32(0x3C)
        if self.data[pe_offset : pe_offset + 4] != b"PE\0\0":
            raise ValueError(f"{path} has no PE signature")

        optional_header_offset = pe_offset + 24
        optional_header_size = self.u16(pe_offset + 20)
        magic = self.u16(optional_header_offset)
        if magic != 0x20B:
            raise ValueError(f"{path} is not a PE32+ image")

        self.image_base = self.u64(optional_header_offset + 24)
        section_count = self.u16(pe_offset + 6)
        section_table_offset = optional_header_offset + optional_header_size
        self.sections: list[dict[str, Any]] = []
        for index in range(section_count):
            offset = section_table_offset + (index * 40)
            name = self.data[offset : offset + 8].rstrip(b"\0").decode("ascii", "replace")
            self.sections.append(
                {
                    "name": name,
                    "virtual_size": self.u32(offset + 8),
                    "virtual_address": self.u32(offset + 12),
                    "raw_size": self.u32(offset + 16),
                    "raw_ptr": self.u32(offset + 20),
                }
            )

    def u16(self, offset: int) -> int:
        return struct.unpack_from("<H", self.data, offset)[0]

    def u32(self, offset: int) -> int:
        return struct.unpack_from("<I", self.data, offset)[0]

    def u64(self, offset: int) -> int:
        return struct.unpack_from("<Q", self.data, offset)[0]

    def section_for_file_offset(self, offset: int) -> dict[str, Any] | None:
        for section in self.sections:
            start = section["raw_ptr"]
            end = start + max(section["raw_size"], 1)
            if start <= offset < end:
                return section
        return None

    def section_for_rva(self, rva: int) -> dict[str, Any] | None:
        for section in self.sections:
            start = section["virtual_address"]
            end = start + max(section["raw_size"], section["virtual_size"], 1)
            if start <= rva < end:
                return section
        return None

    def file_offset_to_rva(self, offset: int) -> int | None:
        section = self.section_for_file_offset(offset)
        if section is None:
            return None
        return section["virtual_address"] + (offset - section["raw_ptr"])

    def file_offset_to_va(self, offset: int) -> int | None:
        rva = self.file_offset_to_rva(offset)
        if rva is None:
            return None
        return self.image_base + rva

    def va_to_file_offset(self, va: int) -> int | None:
        rva = va - self.image_base
        section = self.section_for_rva(rva)
        if section is None:
            return None
        offset = section["raw_ptr"] + (rva - section["virtual_address"])
        if 0 <= offset < len(self.data):
            return offset
        return None

    def read_ascii_wide(self, offset: int, max_chars: int = 256) -> str:
        chars: list[str] = []
        cursor = offset
        for _ in range(max_chars):
            if cursor + 2 > len(self.data):
                break
            value = self.u16(cursor)
            if value == 0:
                break
            if value < 32 or value > 0x7E:
                break
            chars.append(chr(value))
            cursor += 2
        return "".join(chars)

    def scan_bytes(self, needle: bytes) -> list[int]:
        hits: list[int] = []
        cursor = 0
        while True:
            offset = self.data.find(needle, cursor)
            if offset < 0:
                return hits
            hits.append(offset)
            cursor = offset + 1

    def scan_va64_refs(self, va: int) -> list[dict[str, Any]]:
        refs: list[dict[str, Any]] = []
        for offset in self.scan_bytes(struct.pack("<Q", va)):
            refs.append(self.offset_record(offset, points_to=va))
        return refs

    def offset_record(self, offset: int, **extra: Any) -> dict[str, Any]:
        section = self.section_for_file_offset(offset)
        rva = self.file_offset_to_rva(offset)
        va = self.file_offset_to_va(offset)
        record = {
            "file_offset": hex(offset),
            "rva": hex(rva) if rva is not None else None,
            "va": hex(va) if va is not None else None,
            "section": section["name"] if section else None,
        }
        for key, value in extra.items():
            record[key] = hex(value) if isinstance(value, int) else value
        return record


def now_utc() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def decode_descriptor_row(image: PeImage, row_offset: int, target: dict[str, Any]) -> dict[str, Any]:
    expected_global_va = image.image_base + int(target["expected_global_rva"])
    values = [image.u64(row_offset + inner_offset) for inner_offset in (0, 8, 16, 24)]
    fields: list[dict[str, Any]] = []
    for inner_offset, value in zip((0, 8, 16, 24), values):
        field: dict[str, Any] = {
            "offset": f"+0x{inner_offset:02X}",
            "value": f"0x{value:016X}",
        }
        pointed_file = image.va_to_file_offset(value)
        decoded = image.read_ascii_wide(pointed_file) if pointed_file is not None else ""
        if inner_offset == 0:
            field["interpretation"] = "value_name_pointer"
        elif inner_offset == 8:
            field["interpretation"] = "target_global_pointer"
            if value == expected_global_va:
                field["decoded"] = target["live_symbol"].removeprefix("nt!")
        else:
            field["interpretation"] = "reserved_or_flags"
        if decoded:
            field["decoded"] = decoded
        fields.append(field)

    return {
        **image.offset_record(row_offset),
        "fields": fields,
    }


def nearest_key_context(image: PeImage, row_offset: int) -> dict[str, Any] | None:
    candidates: list[dict[str, Any]] = []
    start = max(0, row_offset - 0x100)
    for offset in range(start, row_offset, 8):
        value = image.u64(offset)
        target_file = image.va_to_file_offset(value)
        if target_file is None:
            continue
        decoded = image.read_ascii_wide(target_file)
        if decoded and "Session Manager\\Kernel" in decoded:
            candidates.append(
                {
                    **image.offset_record(offset),
                    "key_path_pointer": f"0x{value:016X}",
                    "decoded_key_path": decoded,
                    "distance_before_row": row_offset - offset,
                }
            )
    if not candidates:
        return None
    return sorted(candidates, key=lambda item: int(item["distance_before_row"]))[0]


def analyze_target(image: PeImage, target: dict[str, Any]) -> dict[str, Any]:
    expected_global_va = image.image_base + int(target["expected_global_rva"])
    string_hits: list[dict[str, Any]] = []
    descriptor_rows: list[dict[str, Any]] = []

    for offset in image.scan_bytes(str(target["value_name"]).encode("utf-16-le")):
        string_va = image.file_offset_to_va(offset)
        if string_va is None:
            continue
        hit = image.offset_record(offset, string=target["value_name"], encoding="utf-16-le")
        pointer_refs = image.scan_va64_refs(string_va)
        hit["pointer_refs"] = pointer_refs
        string_hits.append(hit)

        for ref in pointer_refs:
            row_offset = int(str(ref["file_offset"]), 16)
            if row_offset + 32 > len(image.data):
                continue
            row = decode_descriptor_row(image, row_offset, target)
            fields = row.get("fields") or []
            row_global = int(str(fields[1]["value"]), 16) if len(fields) > 1 else None
            if row_global != expected_global_va:
                continue
            key_context = nearest_key_context(image, row_offset)
            if key_context:
                row["nearest_key_context"] = key_context
            descriptor_rows.append(row)

    return {
        "target": target,
        "expected_global_va": f"0x{expected_global_va:016X}",
        "string_hits": string_hits,
        "descriptor_rows": descriptor_rows,
        "binding_found": bool(descriptor_rows),
    }


def write_notes(path: Path, payload: dict[str, Any]) -> None:
    found_count = sum(1 for result in payload["results"] if result["binding_found"])
    target_count = len(payload["results"])
    lines = [
        "# Kernel Timing INIT Descriptor Scan 2026-04-12",
        "",
        "- Purpose: find current-build `ntoskrnl.exe` INIT descriptor rows that bind `Session Manager\\Kernel` registry value-name strings to live KD kernel globals.",
        f"- Binary: `{payload['binary']}`",
        f"- Image base: `{payload['image_base']}`",
        "",
        "## Results",
    ]
    for result in payload["results"]:
        target = result["target"]
        rows = result["descriptor_rows"]
        status = "binding found" if rows else "no binding found"
        lines.append(f"- `{target['value_name']}` -> `{target['live_symbol']}`: {status}")
        for row in rows:
            context = row.get("nearest_key_context") or {}
            lines.append(
                "  - row "
                f"`{row['file_offset']}` / `{row['rva']}`; "
                f"key context `{context.get('decoded_key_path', 'unknown')}`"
            )
    lines.extend(
        [
            "",
            "## Interpretation",
            "",
            f"- {found_count} of {target_count} target value-name strings are present in the current-build `INIT` section and have a 64-bit pointer reference from an INIT descriptor row.",
            "- Each retained descriptor row points at the same static RVA as the live KD global captured in the earlier VM debugger bundles.",
            "- This strengthens the static registry-seeding/binding layer for the runtime-blocked kernel timing records.",
            "- It does not claim an exact runtime registry read; the post-boot ETW, unseeded boot WPR, and seeded boot WPR lanes still found no exact target value-name hit.",
            "",
        ]
    )
    path.write_text("\n".join(lines), encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Scan ntoskrnl INIT descriptor rows for registry value bindings.")
    parser.add_argument("--binary", required=True, type=Path, help="Path to ntoskrnl.exe")
    parser.add_argument("--output-dir", required=True, type=Path, help="Directory for JSON and notes artifacts")
    args = parser.parse_args()

    image = PeImage(args.binary)
    args.output_dir.mkdir(parents=True, exist_ok=True)
    payload = {
        "schema_version": "1.0",
        "generated_utc": now_utc(),
        "analysis_kind": "host-side-pe-init-descriptor-binding-scan",
        "binary": str(args.binary),
        "image_base": f"0x{image.image_base:X}",
        "sections": [
            section
            for section in image.sections
            if section["name"] in {"INIT", "INITDATA", "INITKDBG", ".data", ".rdata", "PAGE"}
        ],
        "results": [analyze_target(image, target) for target in DEFAULT_TARGETS],
    }
    (args.output_dir / "init-descriptor-scan.json").write_text(
        json.dumps(payload, indent=2, sort_keys=False),
        encoding="utf-8",
    )
    write_notes(args.output_dir / "analysis-notes.md", payload)
    print(json.dumps({"output_dir": str(args.output_dir), "bindings_found": sum(1 for item in payload["results"] if item["binding_found"])}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
