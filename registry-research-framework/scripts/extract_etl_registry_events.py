#!/usr/bin/env python3
"""Extract normalized registry events from an ETL or retained tracerpt XML sidecar."""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"
sys.path.insert(0, str(SCRIPTS_ROOT))

from research_v36_lib import (  # noqa: E402
    _normalize_registry_path,
    _parse_etw_numeric,
    _registry_operation_for_event,
    _xml_local_name,
    _xml_text_or_attribute,
)

REGISTRY_PROVIDER_GUID = "ae53722e-c863-11d2-8659-00c04fa321a1"
REQUIRED_EVENT_FIELDS = (
    "timestamp",
    "process_name",
    "pid",
    "operation",
    "key_path",
    "value_name",
    "result",
    "detail",
)

OPCODE_OPERATION_OVERRIDES = {
    "open": "RegOpenKey",
    "create": "RegCreateKey",
    "kcbcreate": "RegCreateKey",
    "close": "RegCloseKey",
    "queryvalue": "RegQueryValue",
    "setvalue": "RegSetValue",
    "deletevalue": "RegDeleteValue",
    "kcbrundownend": "RegistryRundown",
}


def utc_now() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def repo_relative(path: Path) -> str:
    try:
        return path.resolve().relative_to(REPO_ROOT.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def artifact_metadata(path: Path, collected_utc: str) -> dict[str, Any]:
    return {
        "path": repo_relative(path),
        "sha256": sha256_file(path),
        "size": path.stat().st_size,
        "collected_utc": collected_utc,
    }


def sidecar_xml_candidates(etl_path: Path) -> list[Path]:
    return [
        etl_path.with_suffix(".etl.xml"),
        etl_path.with_suffix(".xml"),
    ]


def run_converter(command: list[str]) -> dict[str, Any]:
    executable = command[0]
    if shutil.which(executable) is None:
        return {
            "tool": executable,
            "status": "unavailable",
            "reason": f"{executable} was not found on PATH",
            "command": command,
        }

    completed = subprocess.run(command, capture_output=True, text=True, check=False)
    return {
        "tool": executable,
        "status": "ok" if completed.returncode == 0 else "failed",
        "exit_code": completed.returncode,
        "stdout": (completed.stdout or "").strip()[:2000],
        "stderr": (completed.stderr or "").strip()[:2000],
        "command": command,
    }


def resolve_xml_input(etl_path: Path, explicit_xml: Path | None) -> tuple[Path, list[dict[str, Any]], str]:
    attempts: list[dict[str, Any]] = []
    if explicit_xml is not None:
        if not explicit_xml.exists():
            raise FileNotFoundError(f"Explicit XML input not found: {explicit_xml}")
        attempts.append({"tool": "xml-sidecar", "status": "ok", "path": repo_relative(explicit_xml)})
        return explicit_xml, attempts, "explicit-xml"

    for candidate in sidecar_xml_candidates(etl_path):
        if candidate.exists():
            attempts.append({"tool": "xml-sidecar", "status": "ok", "path": repo_relative(candidate)})
            attempts.extend(
                [
                    {
                        "tool": "tracerpt",
                        "status": "skipped",
                        "reason": "existing tracerpt XML sidecar was retained with the ETL artifact",
                    },
                    {
                        "tool": "xperf",
                        "status": "skipped",
                        "reason": "XML sidecar is the lossless input for this normalized extractor",
                    },
                    {
                        "tool": "wpr",
                        "status": "skipped",
                        "reason": "XML sidecar is the lossless input for this normalized extractor",
                    },
                ]
            )
            return candidate, attempts, "retained-xml-sidecar"

    with tempfile.TemporaryDirectory(prefix="regprobe-etl-") as temp_dir:
        temp_root = Path(temp_dir)
        tracerpt_xml = temp_root / f"{etl_path.stem}.tracerpt.xml"
        tracerpt_attempt = run_converter(["tracerpt", str(etl_path), "-o", str(tracerpt_xml), "-of", "XML", "-lr"])
        attempts.append(tracerpt_attempt)
        if tracerpt_attempt["status"] == "ok" and tracerpt_xml.exists():
            retained_xml = etl_path.with_suffix(".etl.xml")
            retained_xml.write_text(tracerpt_xml.read_text(encoding="utf-8-sig"), encoding="utf-8")
            return retained_xml, attempts, "tracerpt"

        xperf_csv = temp_root / f"{etl_path.stem}.xperf.csv"
        attempts.append(run_converter(["xperf", "-i", str(etl_path), "-o", str(xperf_csv)]))
        wpr_xml = temp_root / f"{etl_path.stem}.wpr.xml"
        attempts.append(run_converter(["wpr", "-convert", str(etl_path), str(wpr_xml)]))

    raise RuntimeError(f"No parseable XML source found for {etl_path}; attempts={attempts}")


def event_data_from_xml(event: ET.Element) -> dict[str, str]:
    event_data: dict[str, str] = {}
    for child in event:
        if _xml_local_name(child.tag).lower() != "eventdata":
            continue
        for data_child in child:
            if _xml_local_name(data_child.tag).lower() != "data":
                continue
            name = str(data_child.attrib.get("Name") or "").strip()
            if name:
                event_data[name] = _xml_text_or_attribute(data_child)
    return event_data


def system_fields_from_xml(event: ET.Element) -> dict[str, str | None]:
    fields: dict[str, str | None] = {
        "provider": None,
        "event_id": None,
        "timestamp": None,
        "pid": None,
        "thread_id": None,
    }
    for child in event:
        if _xml_local_name(child.tag).lower() != "system":
            continue
        for system_child in child:
            tag = _xml_local_name(system_child.tag).lower()
            if tag == "provider":
                fields["provider"] = (
                    system_child.attrib.get("Guid")
                    or system_child.attrib.get("GUID")
                    or system_child.attrib.get("Name")
                )
            elif tag == "eventid":
                fields["event_id"] = _xml_text_or_attribute(system_child)
            elif tag == "timecreated":
                fields["timestamp"] = system_child.attrib.get("SystemTime")
            elif tag == "execution":
                fields["pid"] = system_child.attrib.get("ProcessID")
                fields["thread_id"] = system_child.attrib.get("ThreadID")
    return fields


def rendering_fields_from_xml(event: ET.Element) -> dict[str, str | None]:
    fields: dict[str, str | None] = {
        "event_name": None,
        "opcode": None,
        "provider": None,
    }
    for child in event:
        if _xml_local_name(child.tag).lower() != "renderinginfo":
            continue
        for rendering_child in child:
            tag = _xml_local_name(rendering_child.tag).lower()
            if tag == "eventname":
                fields["event_name"] = _xml_text_or_attribute(rendering_child)
            elif tag == "opcode":
                fields["opcode"] = _xml_text_or_attribute(rendering_child)
            elif tag == "provider":
                fields["provider"] = _xml_text_or_attribute(rendering_child)
    return fields


def process_name_map_from_xml(xml_path: Path) -> dict[str, str]:
    process_names: dict[str, str] = {}
    try:
        iterator = ET.iterparse(xml_path, events=("end",))
    except ET.ParseError:
        return process_names

    for _, event in iterator:
        if _xml_local_name(event.tag).lower() != "event":
            continue

        rendering_event_name = ""
        event_data = event_data_from_xml(event)
        for child in event:
            if _xml_local_name(child.tag).lower() != "renderinginfo":
                continue
            for rendering_child in child:
                if _xml_local_name(rendering_child.tag).lower() == "eventname":
                    rendering_event_name = _xml_text_or_attribute(rendering_child)
                    break

        if rendering_event_name.strip().lower() != "process":
            event.clear()
            continue

        pid = _parse_etw_numeric(event_data.get("ProcessId"))
        image_name = (
            str(event_data.get("ImageFileName") or event_data.get("ProcessName") or "").strip() or None
        )
        if pid is not None and image_name:
            process_names[str(pid)] = image_name
        event.clear()

    return process_names


def registry_operation(event_id: str | None, opcode: str | None, event_data: dict[str, str]) -> str:
    opcode_key = str(opcode or "").strip().lower()
    if opcode_key in OPCODE_OPERATION_OVERRIDES:
        return OPCODE_OPERATION_OVERRIDES[opcode_key]
    text_blob = " ".join([str(opcode or ""), *[f"{name}={value}" for name, value in event_data.items()]])
    return _registry_operation_for_event(event_id, text_blob, rendering_opcode=opcode)


def extract_events(
    xml_path: Path,
    *,
    etl_path: Path,
    target_filter: str | None,
    parser_attempts: list[dict[str, Any]],
    parser_source: str,
) -> list[dict[str, Any]]:
    collected_utc = utc_now()
    etl_metadata = artifact_metadata(etl_path, collected_utc)
    xml_metadata = artifact_metadata(xml_path, collected_utc)
    filter_text = str(target_filter or "").lower().strip()
    process_names = process_name_map_from_xml(xml_path)
    events: list[dict[str, Any]] = []
    seen: set[tuple[str, str, str | None, str | None, str | None]] = set()

    for _, event in ET.iterparse(xml_path, events=("end",)):
        if _xml_local_name(event.tag).lower() != "event":
            continue

        system = system_fields_from_xml(event)
        rendering = rendering_fields_from_xml(event)
        event_data = event_data_from_xml(event)
        provider = str(system.get("provider") or "")
        event_name = str(rendering.get("event_name") or "")
        is_registry_event = (
            REGISTRY_PROVIDER_GUID in provider.lower().strip("{}")
            or event_name.strip().lower() == "registry"
        )
        if not is_registry_event:
            event.clear()
            continue

        key_path = _normalize_registry_path(
            event_data.get("KeyName")
            or event_data.get("PathName")
            or event_data.get("Path")
            or event_data.get("BaseName")
        )
        value_name = event_data.get("ValueName") or None
        if not key_path and not value_name:
            event.clear()
            continue

        raw_text = json.dumps(event_data, sort_keys=True)
        if filter_text and filter_text not in f"{key_path or ''} {value_name or ''} {raw_text}".lower():
            event.clear()
            continue

        operation = registry_operation(system.get("event_id"), rendering.get("opcode"), event_data)
        result = str(event_data.get("Status") or event_data.get("Result") or "").strip() or None
        pid = str(system.get("pid") or event_data.get("ProcessId") or "").strip() or None
        process_name = str(event_data.get("ProcessName") or event_data.get("ImageName") or "").strip()
        if not process_name and pid:
            process_name = process_names.get(pid) or process_names.get(str(_parse_etw_numeric(pid) or "")) or ""
        if not process_name and pid:
            process_name = f"pid:{pid}"

        dedupe_key = (
            str(system.get("timestamp") or ""),
            operation,
            key_path,
            value_name,
            pid,
        )
        if dedupe_key in seen:
            event.clear()
            continue
        seen.add(dedupe_key)

        detail = {
            "event_id": system.get("event_id"),
            "thread_id": system.get("thread_id"),
            "provider_guid": provider or None,
            "rendering_provider": rendering.get("provider"),
            "rendering_event_name": rendering.get("event_name"),
            "rendering_opcode": rendering.get("opcode"),
            "raw_event_data": event_data,
            "parser_source": parser_source,
            "parser_attempts": parser_attempts,
            "artifacts": {
                "etl": etl_metadata,
                "xml": xml_metadata,
            },
        }
        events.append(
            {
                "timestamp": system.get("timestamp"),
                "process_name": process_name or None,
                "pid": pid,
                "operation": operation,
                "key_path": key_path,
                "value_name": value_name,
                "result": result,
                "detail": detail,
            }
        )
        event.clear()

    return events


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", required=True, type=Path, help="Source ETL path.")
    parser.add_argument("--xml", type=Path, help="Optional retained tracerpt XML path.")
    parser.add_argument("--output", required=True, type=Path, help="JSON array output path.")
    parser.add_argument("--filter", default=None, help="Case-insensitive key/value/event-data substring filter.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    etl_path = args.input.resolve()
    if not etl_path.exists():
        raise FileNotFoundError(f"ETL input not found: {etl_path}")

    explicit_xml = args.xml.resolve() if args.xml else None
    xml_path, attempts, parser_source = resolve_xml_input(etl_path, explicit_xml)
    events = extract_events(
        xml_path,
        etl_path=etl_path,
        target_filter=args.filter,
        parser_attempts=attempts,
        parser_source=parser_source,
    )
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(events, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(
        json.dumps(
            {
                "output": repo_relative(args.output),
                "event_count": len(events),
                "parser_source": parser_source,
                "attempts": attempts,
            },
            sort_keys=True,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
