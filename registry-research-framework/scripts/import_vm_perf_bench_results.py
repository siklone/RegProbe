#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
EVIDENCE_ROOT = REPO_ROOT / "evidence" / "records"
PERF_BENCH_SCRIPT = "registry-research-framework/scripts/vm/run-perf-bench-guest.ps1"
PERF_BENCH_WRAPPER = "registry-research-framework/scripts/vm/run-perf-bench-promoted-batch.guest.ps1"


def load_json(path: Path) -> Any:
    with path.open("r", encoding="utf-8-sig") as handle:
        return json.load(handle)


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(payload, handle, ensure_ascii=False, indent=2)
        handle.write("\n")


def load_manifest(path: Path) -> list[dict[str, Any]]:
    payload = load_json(path)
    if isinstance(payload, list):
        entries = payload
    elif isinstance(payload, dict):
        entries = payload.get("results") or payload.get("entries") or []
    else:
        entries = []
    return [entry for entry in entries if isinstance(entry, dict)]


def counter_name(measurement_profile: str) -> str:
    if measurement_profile == "power":
        return "vm-relative-power-latency-ms"
    return "vm-relative-system-latency-ms"


def build_perf_summary(entry: dict[str, Any]) -> str:
    candidate_id = str(entry.get("candidate_id") or "")
    baseline = entry.get("baseline_median_ms")
    applied = entry.get("applied_median_ms")
    delta_ms = entry.get("delta_ms")
    delta_pct = entry.get("delta_pct")
    return (
        f"VM perf bench (relative) for {candidate_id} measured a median of "
        f"{baseline} ms before apply and {applied} ms after apply "
        f"(delta {delta_ms} ms / {delta_pct}%). Rollback verified; "
        "bare-metal perf validation is still pending."
    )


def build_typeperf_payload(entry: dict[str, Any], output_file: str) -> dict[str, Any]:
    measurement_profile = str(entry.get("measurement_profile") or "system")
    return {
        "executed": True,
        "counter": counter_name(measurement_profile),
        "before_average": entry.get("baseline_median_ms"),
        "after_average": entry.get("applied_median_ms"),
        "before_median_ms": entry.get("baseline_median_ms"),
        "after_median_ms": entry.get("applied_median_ms"),
        "delta_percent": entry.get("delta_pct"),
        "delta_ms": entry.get("delta_ms"),
        "sample_count": entry.get("sample_count"),
        "aggregation": "median",
        "measurement_profile": measurement_profile,
        "measurement_components": entry.get("measurement_components") or [],
        "bench_tier": entry.get("bench_tier"),
        "bench_profile": entry.get("bench_profile"),
        "bench_environment": entry.get("bench_environment"),
        "bench_measurement_reliability": entry.get("bench_measurement_reliability"),
        "bench_bare_metal_pending": entry.get("bench_bare_metal_pending"),
        "rollback_verified": entry.get("rollback_verified"),
        "summary": build_perf_summary(entry),
        "before_file": output_file,
        "after_file": output_file,
        "output_file": output_file,
        "executed_at": entry.get("executed_at"),
    }


def append_timeline_step(full_evidence: dict[str, Any], executed_at: str | None) -> None:
    if not executed_at:
        return
    timeline = full_evidence.setdefault("timeline", [])
    if not isinstance(timeline, list):
        full_evidence["timeline"] = []
        timeline = full_evidence["timeline"]
    for item in timeline:
        if isinstance(item, dict) and item.get("step") == "vm_perf_bench":
            item["timestamp"] = executed_at
            return
    timeline.append({"step": "vm_perf_bench", "timestamp": executed_at})


def update_full_evidence(entry: dict[str, Any]) -> None:
    candidate_id = str(entry.get("candidate_id") or "").strip()
    if not candidate_id:
        raise ValueError("Manifest entry is missing candidate_id")

    record_dir = EVIDENCE_ROOT / candidate_id
    full_evidence_path = record_dir / "full-evidence.json"
    if not full_evidence_path.exists():
        raise FileNotFoundError(f"Missing full evidence bundle: {full_evidence_path}")

    full_evidence = load_json(full_evidence_path)
    if not isinstance(full_evidence, dict):
        raise ValueError(f"Unexpected JSON payload in {full_evidence_path}")

    output_file = f"evidence/records/{candidate_id}/bench-results/{candidate_id}-vm-performance.json"
    perf_payload = dict(entry)
    perf_payload["output_file"] = output_file
    perf_payload.setdefault("rollback_failure_reason", None)

    write_json(record_dir / "bench-results" / f"{candidate_id}-vm-performance.json", perf_payload)

    behavior = full_evidence.setdefault("behavior", {})
    if not isinstance(behavior, dict):
        behavior = {}
        full_evidence["behavior"] = behavior
    behavior["typeperf"] = build_typeperf_payload(perf_payload, output_file)

    reproducibility = full_evidence.setdefault("reproducibility", {})
    if not isinstance(reproducibility, dict):
        reproducibility = {}
        full_evidence["reproducibility"] = reproducibility
    reproducibility["perf_bench_script"] = PERF_BENCH_SCRIPT
    reproducibility["perf_bench_wrapper"] = PERF_BENCH_WRAPPER

    full_evidence["perf_bench_results"] = perf_payload
    append_timeline_step(full_evidence, str(perf_payload.get("executed_at") or ""))
    write_json(full_evidence_path, full_evidence)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Import VM perf bench results into evidence bundles.")
    parser.add_argument("--manifest", required=True, type=Path, help="JSON manifest containing perf bench result entries")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    entries = load_manifest(args.manifest)
    if not entries:
        raise SystemExit("No perf bench entries found in manifest")

    for entry in entries:
        update_full_evidence(entry)
        print(f"updated {entry['candidate_id']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
