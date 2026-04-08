#!/usr/bin/env python3
"""Audit the visible callback registration in the generic power-setting setter."""

from __future__ import annotations

import json
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
STDOUT = Path(
    "evidence/files/vm-tooling-staging/"
    "local-kd-powersetting-query-20260408a/stdout.txt"
)
JSON_OUT = Path(
    "registry-research-framework/audit/"
    "execution-required-powersetting-trace-callback-20260408.json"
)
MD_OUT = Path(
    "registry-research-framework/audit/"
    "execution-required-powersetting-trace-callback-20260408.md"
)


def main() -> int:
    text = (REPO_ROOT / STDOUT).read_text(encoding="utf-8", errors="replace")
    payload = {
        "date": "2026-04-08",
        "source_artifact": str(STDOUT),
        "counts": {
            "PopFindPowerSettingConfiguration": text.count("PopFindPowerSettingConfiguration"),
            "PopSetNotificationWork": text.count("PopSetNotificationWork"),
            "PoRegisterPowerSettingCallback": text.count("PoRegisterPowerSettingCallback"),
            "PopTracePowerSettingChange": text.count("PopTracePowerSettingChange"),
        },
        "trace_registration_present": "lea     r8,[nt!PopTracePowerSettingChange" in text
        and "call    nt!PoRegisterPowerSettingCallback" in text,
        "interpretation": [
            "The retained generic power-setting setter path visibly resolves configuration objects, schedules notification work, and performs a callback registration.",
            "That explicit callback registration targets PopTracePowerSettingChange rather than an execution-required pair-specific callback.",
            "This further narrows the visible generic power-setting layer: the retained current-build setter path still does not expose an exact binding site for AllowSystemRequiredPowerRequests or AllowAudioToEnableExecutionRequiredPowerRequests.",
        ],
    }

    (REPO_ROOT / JSON_OUT).write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")

    lines = [
        "# Execution-Required Power-Setting Trace Callback Audit",
        "",
        "Date: 2026-04-08",
        f"Source artifact: `{STDOUT}`",
        "",
        "## Outcome",
        "",
        f"- `PopFindPowerSettingConfiguration` mentions: `{payload['counts']['PopFindPowerSettingConfiguration']}`",
        f"- `PopSetNotificationWork` mentions: `{payload['counts']['PopSetNotificationWork']}`",
        f"- `PoRegisterPowerSettingCallback` mentions: `{payload['counts']['PoRegisterPowerSettingCallback']}`",
        f"- `PopTracePowerSettingChange` mentions: `{payload['counts']['PopTracePowerSettingChange']}`",
        f"- Explicit `PopTracePowerSettingChange -> PoRegisterPowerSettingCallback` registration present: `{payload['trace_registration_present']}`",
        "",
        "## Interpretation",
        "",
        "- The visible generic power-setting setter path still looks like in-memory setting/configuration management plus notification plumbing.",
        "- Its explicit callback registration target is `PopTracePowerSettingChange`, not an exact execution-required pair binding site.",
        "- This leaves the execution-required pair blocked on earlier seeding/binding rather than on the visible generic setter path.",
        "",
        "## Artifacts",
        "",
        f"- `{JSON_OUT}`",
        f"- `{STDOUT}`",
    ]
    (REPO_ROOT / MD_OUT).write_text("\n".join(lines) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
