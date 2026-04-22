from __future__ import annotations

import json
from typing import Any


def parse_command_json(stdout: str, *, stderr: str = "") -> dict[str, Any]:
    text = stdout.strip()
    if not text:
        return {}
    try:
        payload = json.loads(text)
    except json.JSONDecodeError as exc:
        return {
            "status": "error",
            "stdout": text,
            "stderr": stderr.strip(),
            "stdout_parse_error": str(exc),
        }
    if not isinstance(payload, dict):
        return {
            "status": "error",
            "stdout": text,
            "stderr": stderr.strip(),
            "stdout_parse_error": "stdout JSON payload is not an object",
        }
    return payload


def parse_nested_stdout_json(payload: dict[str, Any], *, context: str) -> Any:
    stdout = str(payload.get("stdout") or "").strip()
    if not stdout:
        return None
    try:
        parsed = json.loads(stdout)
    except json.JSONDecodeError as exc:
        return {
            "_parse_error": str(exc),
            "_raw_stdout": stdout,
            "_context": context,
        }
    if not isinstance(parsed, dict):
        return {
            "_parse_error": "stdout JSON payload is not an object",
            "_raw_stdout": stdout,
            "_context": context,
        }
    return parsed
