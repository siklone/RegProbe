#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
AUDIT_ROOT = REPO_ROOT / "registry-research-framework" / "audit"
RUBRIC_PATH = AUDIT_ROOT / "power-request-override-reader-binding-review-rubric-20260419.json"
TEMPLATE_PATH = AUDIT_ROOT / "power-request-override-reader-binding-result-ledger-template-20260419.json"


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def portable_path(path: Path, *, repo_root: Path = REPO_ROOT) -> str:
    try:
        return str(path.resolve().relative_to(repo_root)).replace("\\", "/")
    except ValueError:
        return str(path.resolve()).replace("\\", "/")


def artifact_display_path(path: Path, *, repo_root: Path = REPO_ROOT) -> str:
    try:
        return str(path.resolve().relative_to(repo_root)).replace("\\", "/")
    except ValueError:
        parent = path.parent.name or "external"
        return f"external-artifacts/{parent}/{path.name}"


def collect_markers(text: str, markers: list[str]) -> list[str]:
    return [marker for marker in markers if marker in text]


def infer_outcome(response_strong: list[str], umpo_strong: list[str], red_flags: list[str]) -> str:
    if any(marker in response_strong for marker in ("CmQueryValueKey", "ZwQueryValueKey", "NtQueryValueKey")):
        return "direct-registry-read"
    if any(marker in response_strong for marker in ("Process", "Service", "Driver")):
        return "consumer-semantics-without-read"
    if any(marker in umpo_strong for marker in ("ALPC", "rpc", "opcode", "requester")):
        return "umpo-boundary-is-best-signal"
    if red_flags:
        return "wrapper-only-path"
    return "wrapper-only-path"


def build_artifact_review(
    *,
    artifact_id: str,
    stdout_path: Path,
    summary_path: Path,
    command_file: str,
    rubric_entry: dict[str, Any],
) -> tuple[dict[str, Any], list[str]]:
    red_flags: list[str] = []
    stdout_text = ""
    if stdout_path.exists():
        stdout_text = stdout_path.read_text(encoding="utf-8-sig", errors="ignore")
    else:
        red_flags.append(f"missing stdout: {artifact_display_path(stdout_path)}")

    required_markers = list(rubric_entry.get("required_markers") or [])
    good_markers = list(rubric_entry.get("good_signal_markers") or [])
    weak_markers = list(rubric_entry.get("weak_signal_markers") or [])

    required_seen = collect_markers(stdout_text, required_markers)
    strong_seen = collect_markers(stdout_text, good_markers)
    weak_seen = collect_markers(stdout_text, weak_markers)
    required_present = len(required_seen) == len(required_markers)
    if stdout_path.exists() and not required_present:
        red_flags.append(f"required markers missing for {artifact_id}")

    return (
        {
            "artifact_id": artifact_id,
            "stdout_path": artifact_display_path(stdout_path),
            "summary_path": artifact_display_path(summary_path),
            "command_file": command_file,
            "required_markers_present": required_present,
            "required_markers_seen": required_seen,
            "strong_markers_seen": strong_seen,
            "weak_markers_seen": weak_seen,
            "summary_exists": summary_path.exists(),
        },
        red_flags,
    )


def render_markdown(payload: dict[str, Any]) -> str:
    response = payload["fill_after_run"]["artifacts"]["response_reacquire"]
    umpo = payload["fill_after_run"]["artifacts"]["umpo_message_reacquire"]
    review = payload["fill_after_run"]["review_outcome"]
    next_move = payload["fill_after_run"]["next_move"]

    lines = [
        "# PowerRequestOverride Reader-Binding Result Ledger",
        "",
        f"- Run ID: `{payload['fill_after_run']['run_id']}`",
        f"- Suggested outcome: `{review['chosen_outcome']}`",
        f"- Stop condition triggered: `{review['stop_condition_triggered']}`",
        "",
        "## Response Artifact",
        f"- Stdout: `{response['stdout_path']}`",
        f"- Summary: `{response['summary_path']}`",
        f"- Required markers present: `{response['required_markers_present']}`",
        f"- Strong markers: `{response['strong_markers_seen']}`",
        f"- Weak markers: `{response['weak_markers_seen']}`",
        "",
        "## UMPO Artifact",
        f"- Stdout: `{umpo['stdout_path']}`",
        f"- Summary: `{umpo['summary_path']}`",
        f"- Required markers present: `{umpo['required_markers_present']}`",
        f"- Strong markers: `{umpo['strong_markers_seen']}`",
        f"- Weak markers: `{umpo['weak_markers_seen']}`",
        "",
        "## Review",
        f"- Why: {review['why']}",
        f"- Red flags: `{review['red_flags']}`",
        "",
        "## Next Move",
        f"- Lane: `{next_move['lane']}`",
        f"- Exact target: `{next_move['exact_target']}`",
    ]
    return "\n".join(lines) + "\n"


def build_prefilled_payload(
    *,
    run_id: str,
    response_stdout: Path,
    response_summary: Path,
    umpo_stdout: Path,
    umpo_summary: Path,
) -> dict[str, Any]:
    rubric = load_json(RUBRIC_PATH)
    template = load_json(TEMPLATE_PATH)
    artifact_checks = {entry["artifact_id"]: entry for entry in (rubric.get("artifact_checks") or [])}

    response_review, response_red_flags = build_artifact_review(
        artifact_id="local-kd-powerrequest-response-reacquire",
        stdout_path=response_stdout,
        summary_path=response_summary,
        command_file="registry-research-framework/audit/power-request-override-response-reacquire-local-kd-20260419.txt",
        rubric_entry=artifact_checks["local-kd-powerrequest-response-reacquire"],
    )
    umpo_review, umpo_red_flags = build_artifact_review(
        artifact_id="local-kd-powerrequest-umpo-message-reacquire",
        stdout_path=umpo_stdout,
        summary_path=umpo_summary,
        command_file="registry-research-framework/audit/power-request-override-umpo-message-reacquire-local-kd-20260419.txt",
        rubric_entry=artifact_checks["local-kd-powerrequest-umpo-message-reacquire"],
    )

    red_flags = response_red_flags + umpo_red_flags
    chosen_outcome = infer_outcome(
        response_review["strong_markers_seen"],
        umpo_review["strong_markers_seen"],
        red_flags,
    )

    lane_map = {
        "direct-registry-read": ("kernel-side", "capture exact read/apply chain"),
        "consumer-semantics-without-read": ("kernel-side", "document consumer semantics and keep exact binding blocked"),
        "umpo-boundary-is-best-signal": ("umpo-boundary", "bounded power-service or powrprof follow-up"),
        "wrapper-only-path": ("kernel-side", "follow first non-wrapper callee only"),
    }
    lane, exact_target = lane_map[chosen_outcome]

    payload = template
    fill = payload["fill_after_run"]
    fill["run_id"] = run_id
    fill["artifacts"]["response_reacquire"] = response_review
    fill["artifacts"]["umpo_message_reacquire"] = umpo_review
    fill["review_outcome"]["chosen_outcome"] = chosen_outcome
    fill["review_outcome"]["why"] = (
        "Auto-suggested from retained marker hits; confirm against the reacquired stdout before promoting this result."
    )
    fill["review_outcome"]["red_flags"] = red_flags
    fill["review_outcome"]["stop_condition_triggered"] = bool(red_flags)
    fill["next_move"]["lane"] = lane
    fill["next_move"]["exact_target"] = exact_target
    payload["generated_utc"] = datetime.now(timezone.utc).isoformat()
    payload["generator"] = portable_path(Path(__file__))
    return payload


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate a prefilled PowerRequestOverride result ledger from reacquired KD artifacts.")
    parser.add_argument("--run-id", required=True)
    parser.add_argument("--response-stdout", type=Path, required=True)
    parser.add_argument("--response-summary", type=Path, required=True)
    parser.add_argument("--umpo-stdout", type=Path, required=True)
    parser.add_argument("--umpo-summary", type=Path, required=True)
    parser.add_argument("--output-json", type=Path, required=True)
    parser.add_argument("--output-md", type=Path, required=True)
    args = parser.parse_args()

    payload = build_prefilled_payload(
        run_id=args.run_id,
        response_stdout=args.response_stdout,
        response_summary=args.response_summary,
        umpo_stdout=args.umpo_stdout,
        umpo_summary=args.umpo_summary,
    )
    args.output_json.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    args.output_md.write_text(render_markdown(payload), encoding="utf-8")
    print(json.dumps({"output_json": portable_path(args.output_json), "output_md": portable_path(args.output_md)}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
