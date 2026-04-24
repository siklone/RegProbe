#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import os
import sys
import urllib.error
import urllib.request
from pathlib import Path

SYSTEM_PROMPT = """You are a Windows registry research analyst for the RegProbe project.
Given a JSON research record, generate a card JSON object.

Output ONLY a valid JSON object. No markdown, no explanation, no backticks.

Each card must have:
{
  "id": "<record_id>",
  "key_path": "<full registry path>",
  "value_focus": "<primary value name or subtree label>",
  "category": "<POWER|KERNEL|SYSTEM|NETWORK|PRIVACY|POLICY>",
  "subcategory": "<short label>",
  "evidence_tier": "<A|B|C|D>",
  "what_it_does": "<2-3 sentence plain english explanation>",
  "hold_reason": "<string or null>",
  "values": [{ "name": "...", "type": "REG_DWORD|REG_SZ|REG_BINARY", "default": "...|null", "observed": "..." }],
  "proof": [{ "method": "ETW Stackwalk|Ghidra Static|Procmon|WinDbg|ReactOS|WRK", "tool": "...", "detail": "...", "strength": "high|medium|low", "date": "..." }],
  "stats": { "runtime": 0-100, "static": 0-100, "coverage": 0-100, "confidence": 0-100 }
}

Evidence tier rules:
- A: Multiple high-strength proofs, consumer semantics confirmed, physical artifacts
- B: At least one high-strength runtime proof, some gaps remain
- C: Query confirmed but behavioral impact unknown, or only static
- D: No runtime capture, planning state only

Stats scoring:
- runtime: based on ETW/Procmon/WinDbg evidence strength (0=none, 100=full stack + consumer confirmed)
- static: based on Ghidra/ReactOS/WRK findings (0=none, 100=full function resolution)
- coverage: percentage of value semantics explained
- confidence: overall confidence in the record's claims

Be precise and honest. If something is unknown, say so. Never overclaim."""


def sha256_text(text: str) -> str:
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def call_anthropic(*, api_key: str, model: str, record_payload: dict[str, object], timeout: int) -> dict[str, object]:
    body = {
        "model": model,
        "max_tokens": 4000,
        "system": SYSTEM_PROMPT,
        "messages": [
            {
                "role": "user",
                "content": f"Generate a card for this research record:\n\n{json.dumps(record_payload, indent=2)}",
            }
        ],
    }
    request = urllib.request.Request(
        os.environ.get("ANTHROPIC_BASE_URL", "https://api.anthropic.com/v1/messages"),
        data=json.dumps(body).encode("utf-8"),
        headers={
            "content-type": "application/json",
            "x-api-key": api_key,
            "anthropic-version": "2023-06-01",
        },
        method="POST",
    )
    with urllib.request.urlopen(request, timeout=timeout) as response:
        payload = json.loads(response.read().decode("utf-8"))

    blocks = payload.get("content") or []
    text = "".join(block.get("text", "") for block in blocks if isinstance(block, dict) and block.get("type") == "text").strip()
    if not text:
        raise RuntimeError("Anthropic response did not contain a text payload.")
    return json.loads(text)


def load_state(path: Path) -> dict[str, str]:
    if not path.exists():
        return {}
    try:
        return json.loads(path.read_text())
    except Exception:
        return {}


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate registry research card JSONs from research records.")
    parser.add_argument("--records-dir", type=Path, default=Path("research/records"))
    parser.add_argument("--output-dir", type=Path, default=Path("cards/v25H2"))
    parser.add_argument("--record-id", action="append", default=[], help="Optional record id filter. Repeat for multiple ids.")
    parser.add_argument("--force", action="store_true", help="Regenerate cards even when the record hash is unchanged.")
    parser.add_argument("--timeout", type=int, default=120)
    parser.add_argument("--model", default="claude-sonnet-4-20250514")
    args = parser.parse_args()

    api_key = os.environ.get("ANTHROPIC_API_KEY", "").strip()
    if not api_key:
        print("ANTHROPIC_API_KEY is required to generate cards.", file=sys.stderr)
        return 2

    args.output_dir.mkdir(parents=True, exist_ok=True)
    state_path = args.output_dir / ".state.json"
    state = load_state(state_path)
    requested_ids = set(args.record_id)

    generated = 0
    skipped = 0
    for record_path in sorted(args.records_dir.glob("*.json")):
        payload = json.loads(record_path.read_text())
        record_id = payload.get("record_id") or record_path.stem
        if requested_ids and record_id not in requested_ids:
            continue

        record_text = json.dumps(payload, sort_keys=True)
        record_hash = sha256_text(record_text)
        output_path = args.output_dir / f"{record_id}.card.json"

        if not args.force and output_path.exists() and state.get(record_id) == record_hash:
            skipped += 1
            continue

        card = call_anthropic(
            api_key=api_key,
            model=args.model,
            record_payload=payload,
            timeout=args.timeout,
        )
        card["_meta"] = {
            "record_id": record_id,
            "record_sha256": record_hash,
            "source_record": record_path.as_posix(),
            "model": args.model,
        }
        output_path.write_text(json.dumps(card, indent=2) + "\n")
        state[record_id] = record_hash
        generated += 1

    state_path.write_text(json.dumps(state, indent=2, sort_keys=True) + "\n")
    print(json.dumps({"generated": generated, "skipped": skipped, "output_dir": args.output_dir.as_posix()}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
