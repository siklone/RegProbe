#!/usr/bin/env python3
from __future__ import annotations

import argparse
import importlib.util
import json
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
SCRIPTS_ROOT = REPO_ROOT / "scripts"


def load_local_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


research_v36_lib = load_local_module("etw_stackwalk_bundle_research_v36_lib", SCRIPTS_ROOT / "research_v36_lib.py")

DEFAULT_SOURCE_FIELDS = [
    "Stack",
    "CallStack",
    "Call Stack",
    "StackTrace",
    "Stack Trace",
    "UserStack",
    "User Stack",
]


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def portable_path(path: Path) -> str:
    resolved = path.resolve()
    try:
        return resolved.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return resolved.as_posix()


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def parse_pid(value: Any) -> int | None:
    text = str(value or "").strip()
    if not text:
        return None
    try:
        return int(text)
    except ValueError:
        return None


def detect_hive(key_path: Any) -> str | None:
    text = str(key_path or "").strip().replace("/", "\\")
    for hive in ("HKLM", "HKCU", "HKCR", "HKU", "HKCC"):
        if text.upper().startswith(hive + "\\"):
            return hive
    return None


def bundle_from_parse_result(
    parse_result: dict[str, Any],
    *,
    run_id: str,
    capture_phase: str = "runtime",
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    etl_path = str(parse_result.get("etl_path") or "")
    xml_output = str(parse_result.get("xml_output") or "")
    touches = parse_result.get("registry_touches") or []
    source_fields = list(DEFAULT_SOURCE_FIELDS)
    events: list[dict[str, Any]] = []
    caller_stack_event_count = 0
    evidence_refs = [ref for ref in (etl_path, xml_output) if ref]

    for touch in touches:
        caller_stack = [str(frame).strip() for frame in (touch.get("caller_stack") or []) if str(frame).strip()]
        if caller_stack:
            caller_stack_event_count += 1
        event = {
            "run_id": run_id,
            "source_tool": "etw",
            "capture_phase": capture_phase,
            "process_name": touch.get("process_name"),
            "pid": parse_pid(touch.get("process_id")),
            "operation": touch.get("operation") or "registry-touch",
            "timestamp_utc": touch.get("timestamp_utc"),
            "hive": detect_hive(touch.get("key_path")),
            "key_path": touch.get("key_path"),
            "value_name": touch.get("value_name"),
            "value_type": touch.get("value_type"),
            "data_text": touch.get("raw_excerpt"),
            "result": touch.get("result"),
            "evidence_refs": evidence_refs,
        }
        if caller_stack:
            event["caller_stack"] = caller_stack
        events.append(event)

    notes = [str(note).strip() for note in (parse_result.get("notes") or []) if str(note).strip()]
    parse_status = str(parse_result.get("status") or "")
    ok_statuses = {"parsed", "parsed-sidecar-xml", "parsed-sidecar-json"}
    bundle_status = "ok" if parse_status in ok_statuses else "error"

    return {
        "$schema": "registry-research-framework/schemas/normalized-registry-bundle.schema.json",
        "run_id": run_id,
        "source_tool": "etw",
        "capture_phase": capture_phase,
        "generated_utc": generated_utc,
        "normalizer_name": "EtwStackwalkTracerptXmlNormalizer",
        "input_path": etl_path,
        "status": bundle_status,
        "error_kind": None if bundle_status == "ok" else parse_status or "parse-failed",
        "errors": [] if bundle_status == "ok" else notes or [parse_status or "parse-failed"],
        "event_count": len(events),
        "filtered_event_count": len(events),
        "evidence_refs": evidence_refs,
        "stack_capture": {
            "parser_supported": True,
            "captured_event_count": caller_stack_event_count,
            "source_fields": source_fields,
            "parser_status": parse_status,
        },
        "events": events,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate a normalized registry bundle from an ETW stackwalk ETL capture.")
    parser.add_argument("--input", type=Path, required=True, help="Repo-relative or absolute ETL path.")
    parser.add_argument("--output", type=Path, default=None, help="Bundle output path. Defaults to sibling normalized-registry-bundle.json.")
    parser.add_argument("--run-id", default=None, help="Override run id. Defaults to ETL stem.")
    parser.add_argument("--capture-phase", default="runtime")
    parser.add_argument("--parser", default=None, help="ETL parser name. Defaults to config value.")
    args = parser.parse_args()

    etl_path = args.input if args.input.is_absolute() else (REPO_ROOT / args.input)
    etl_path = etl_path.resolve()
    bundle_output = (args.output if args.output and args.output.is_absolute() else (REPO_ROOT / args.output)).resolve() if args.output else (etl_path.parent / "normalized-registry-bundle.json")

    config = research_v36_lib.load_etl_parser_config()
    parser_name = args.parser or config.get("default_parser") or "tracerpt"
    provider_guid = config.get("provider_guid")
    parse_result = research_v36_lib.parse_etl_registry_touches(etl_path, parser=parser_name, provider_guid=provider_guid)
    run_id = args.run_id or etl_path.stem
    payload = bundle_from_parse_result(
        parse_result,
        run_id=run_id,
        capture_phase=args.capture_phase,
    )
    write_json(bundle_output, payload)
    print(
        json.dumps(
            {
                "bundle": portable_path(bundle_output),
                "status": payload.get("status"),
                "event_count": payload.get("event_count"),
                "caller_stack_event_count": (payload.get("stack_capture") or {}).get("captured_event_count"),
            },
            indent=2,
        )
    )
    return 0 if payload.get("status") == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
