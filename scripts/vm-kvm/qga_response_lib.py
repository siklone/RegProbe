from __future__ import annotations

import json
from typing import Any


def parse_qga_return(output: str) -> Any:
    text = output.strip()
    if not text:
        raise ValueError("qga stdout was empty")

    try:
        payload = json.loads(text)
    except json.JSONDecodeError as exc:
        preview = text[:500].replace("\n", "\\n")
        raise ValueError(f"qga stdout did not contain valid JSON: {preview}") from exc

    if not isinstance(payload, dict):
        raise ValueError("qga stdout JSON payload is not an object")
    if "return" not in payload:
        raise ValueError("qga stdout JSON payload is missing 'return'")
    return payload["return"]
