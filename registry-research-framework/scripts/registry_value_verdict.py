#!/usr/bin/env python3
from __future__ import annotations

from dataclasses import dataclass
from typing import Any


NO_EFFECT_PCT = 3.0
LOW_CONFIDENCE_PCT = 7.0
STABLE_SPREAD_PCT = 20.0

SAFETY_PRIORITY = {
    "boot_failure": 0,
    "rollback_failure": 1,
    "app_breakage": 2,
}

PERF_PRIORITY = {
    "harmful": 10,
    "noisy": 20,
    "cpu_gain": 30,
    "io_gain": 40,
    "low_confidence": 50,
    "no_effect": 60,
}


@dataclass(frozen=True)
class MetricSpec:
    name: str
    category: str
    baseline_key: str
    applied_key: str
    higher_is_better: bool


METRICS = (
    MetricSpec("cpu_single_seconds", "cpu", "cpu_single_seconds", "cpu_single_seconds", False),
    MetricSpec("cpu_multi_seconds", "cpu", "cpu_multi_seconds", "cpu_multi_seconds", False),
    MetricSpec(
        "cpu_single_iterations_per_second",
        "cpu",
        "cpu_single_iterations_per_second",
        "cpu_single_iterations_per_second",
        True,
    ),
    MetricSpec(
        "cpu_multi_iterations_per_second",
        "cpu",
        "cpu_multi_iterations_per_second",
        "cpu_multi_iterations_per_second",
        True,
    ),
    MetricSpec("io_write_read_mib_per_second", "io", "io_write_read_mib_per_second", "io_write_read_mib_per_second", True),
    MetricSpec("io_write_mib_per_second", "io", "io_write_mib_per_second", "io_write_mib_per_second", True),
    MetricSpec("io_read_mib_per_second", "io", "io_read_mib_per_second", "io_read_mib_per_second", True),
)


def numeric(value: Any) -> float | None:
    try:
        if value is None:
            return None
        return float(value)
    except (TypeError, ValueError):
        return None


def stage_result(payload: dict[str, Any], stage: str) -> dict[str, Any]:
    stages = payload.get("stages")
    if not isinstance(stages, dict):
        return {}
    stage_payload = stages.get(stage)
    if not isinstance(stage_payload, dict):
        return {}
    result = stage_payload.get("result")
    return result if isinstance(result, dict) else {}


def stage_wrapper(payload: dict[str, Any], stage: str) -> dict[str, Any]:
    stages = payload.get("stages")
    if not isinstance(stages, dict):
        return {}
    stage_payload = stages.get(stage)
    return stage_payload if isinstance(stage_payload, dict) else {}


def smoke_from(stage_payload: dict[str, Any], key: str = "smoke") -> dict[str, Any]:
    smoke = stage_payload.get(key)
    return smoke if isinstance(smoke, dict) else {}


def benchmarks_from(stage_payload: dict[str, Any], key: str = "smoke") -> dict[str, Any]:
    benchmarks = smoke_from(stage_payload, key).get("benchmarks")
    return benchmarks if isinstance(benchmarks, dict) else {}


def _noise_meta(payload: dict[str, Any], stage: str) -> dict[str, Any]:
    wrapper = stage_wrapper(payload, stage)
    meta = wrapper.get("host_noise_meta") or wrapper.get("noise_meta")
    return meta if isinstance(meta, dict) else {"noise_status": "unknown", "reason": "legacy-artifact-missing-host-noise-meta"}


def _noise_statuses(payload: dict[str, Any]) -> list[str]:
    return [str(_noise_meta(payload, stage).get("noise_status") or "unknown") for stage in ("apply", "post_reboot_rollback", "post_rollback")]


def _host_noise_status(payload: dict[str, Any]) -> str:
    statuses = _noise_statuses(payload)
    if any(status == "noisy" for status in statuses):
        return "noisy"
    if any(status == "unknown" for status in statuses):
        return "unknown"
    if any(status not in {"ok", "skipped"} for status in statuses):
        return "unknown"
    return "ok"


def _delta_pct(baseline: float, applied: float, *, higher_is_better: bool) -> float | None:
    if baseline == 0:
        return None
    raw = ((applied - baseline) / abs(baseline)) * 100.0
    if not higher_is_better:
        raw = -raw
    return round(raw, 3)


def _spread_for(bench: dict[str, Any], key: str) -> float | None:
    spreads = bench.get("spreads")
    if isinstance(spreads, dict):
        value = numeric(spreads.get(key))
        if value is not None:
            return value

    spread_key = f"{key}_spread_pct"
    return numeric(bench.get(spread_key))


def _metric_verdict(delta_pct: float, *, category: str, confidence: str) -> str:
    if delta_pct <= -LOW_CONFIDENCE_PCT:
        return "harmful"
    if abs(delta_pct) < NO_EFFECT_PCT:
        return "no_effect"
    if abs(delta_pct) < LOW_CONFIDENCE_PCT:
        return "low_confidence"
    if confidence == "low":
        return "low_confidence"
    return "cpu_gain" if category == "cpu" else "io_gain"


def _confidence(payload: dict[str, Any], baseline_bench: dict[str, Any], applied_bench: dict[str, Any], spec: MetricSpec) -> str:
    noise_status = _host_noise_status(payload)
    if noise_status == "noisy":
        return "low"

    baseline_spread = _spread_for(baseline_bench, spec.baseline_key)
    applied_spread = _spread_for(applied_bench, spec.applied_key)
    if baseline_spread is not None and applied_spread is not None:
        if max(baseline_spread, applied_spread) > STABLE_SPREAD_PCT:
            return "low"
        if max(baseline_spread, applied_spread) > 10.0:
            return "medium"
        return "high" if noise_status == "ok" else "medium"

    return "medium" if noise_status == "ok" else "low"


def _hard_failure_count(smoke: dict[str, Any]) -> int | None:
    value = smoke.get("hard_failure_count")
    if isinstance(value, int):
        return value
    parsed = numeric(value)
    return int(parsed) if parsed is not None else None


def _interactive_failure_count(smoke: dict[str, Any]) -> int | None:
    interactive = smoke.get("interactive_user_smoke")
    if not isinstance(interactive, dict):
        return None
    value = interactive.get("failure_count")
    if isinstance(value, int):
        return value
    parsed = numeric(value)
    return int(parsed) if parsed is not None else None


def _state_equals(left: Any, right: Any) -> bool:
    if not isinstance(left, dict) or not isinstance(right, dict):
        return False
    fields = ("key_exists", "value_exists", "value", "value_kind", "status")
    return all(left.get(field) == right.get(field) for field in fields if field in left or field in right)


def _safety_findings(payload: dict[str, Any]) -> list[dict[str, Any]]:
    findings: list[dict[str, Any]] = []
    status = str(payload.get("status") or "")
    error = str(payload.get("error") or "")
    outcome = str(payload.get("outcome") or "")

    if error.startswith("guest-did-not-return") or outcome == "boot-failure-recovered":
        findings.append(
            {
                "verdict": "boot_failure",
                "confidence": "high",
                "reason": error or "guest required snapshot recovery after reboot",
                "metrics_used": ["health_checks", "recovery"],
            }
        )
    elif "rollback" in error or "rollback" in outcome:
        findings.append(
            {
                "verdict": "rollback_failure",
                "confidence": "high",
                "reason": error or "rollback stage required snapshot recovery",
                "metrics_used": ["stages", "recovery"],
            }
        )
    elif payload.get("recovery"):
        findings.append(
            {
                "verdict": "app_breakage",
                "confidence": "high",
                "reason": error or "stage failure required snapshot recovery",
                "metrics_used": ["stages", "recovery"],
            }
        )
    elif status not in {"ok", "running", ""}:
        findings.append(
            {
                "verdict": "boot_failure" if "reboot" in error or "boot" in error else "app_breakage",
                "confidence": "medium",
                "reason": error or f"experiment status is {status}",
                "metrics_used": ["status", "error"],
            }
        )

    apply_stage = stage_result(payload, "apply")
    post_reboot = stage_result(payload, "post_reboot_rollback")
    post_rollback = stage_result(payload, "post_rollback")

    baseline_smoke = smoke_from(apply_stage, "baseline_smoke")
    for stage_name, stage in (("apply", apply_stage), ("post_reboot_rollback", post_reboot), ("post_rollback", post_rollback)):
        smoke = smoke_from(stage)
        baseline_hard = _hard_failure_count(baseline_smoke) or 0
        stage_hard = _hard_failure_count(smoke)
        if stage_hard is not None and stage_hard > baseline_hard:
            findings.append(
                {
                    "verdict": "app_breakage",
                    "confidence": "high",
                    "reason": f"{stage_name} hard smoke failures increased from {baseline_hard} to {stage_hard}",
                    "metrics_used": [f"{stage_name}.smoke.hard_failure_count"],
                }
            )
            break

        baseline_interactive = _interactive_failure_count(baseline_smoke) or 0
        stage_interactive = _interactive_failure_count(smoke)
        if stage_interactive is not None and stage_interactive > baseline_interactive:
            findings.append(
                {
                    "verdict": "app_breakage",
                    "confidence": "medium",
                    "reason": f"{stage_name} interactive smoke failures increased from {baseline_interactive} to {stage_interactive}",
                    "metrics_used": [f"{stage_name}.smoke.interactive_user_smoke.failure_count"],
                }
            )
            break

    original = apply_stage.get("original")
    after_restore = post_reboot.get("after_restore")
    final = post_rollback.get("final")
    restore_action = str(post_reboot.get("restore_action") or "")
    if post_reboot and (not restore_action or restore_action.startswith("restore-noop")):
        findings.append(
            {
                "verdict": "rollback_failure",
                "confidence": "medium",
                "reason": f"rollback restore action was {restore_action or 'missing'}",
                "metrics_used": ["post_reboot_rollback.restore_action"],
            }
        )
    elif original and after_restore and not _state_equals(original, after_restore):
        findings.append(
            {
                "verdict": "rollback_failure",
                "confidence": "high",
                "reason": "post-reboot restore state does not match original registry state",
                "metrics_used": ["apply.original", "post_reboot_rollback.after_restore"],
            }
        )
    elif original and final and not _state_equals(original, final):
        findings.append(
            {
                "verdict": "rollback_failure",
                "confidence": "high",
                "reason": "post-rollback final state does not match original registry state",
                "metrics_used": ["apply.original", "post_rollback.final"],
            }
        )

    return findings


def _performance_findings(payload: dict[str, Any]) -> list[dict[str, Any]]:
    findings: list[dict[str, Any]] = []
    apply_stage = stage_result(payload, "apply")
    post_reboot = stage_result(payload, "post_reboot_rollback")
    baseline_bench = benchmarks_from(apply_stage, "baseline_smoke")
    candidate_benches = (
        ("apply", benchmarks_from(apply_stage)),
        ("post_reboot", benchmarks_from(post_reboot)),
    )

    if not baseline_bench:
        return findings

    if _host_noise_status(payload) == "noisy":
        return [
            {
                "verdict": "noisy",
                "confidence": "low",
                "delta_pct": None,
                "reason": "host preflight marked one or more stages noisy",
                "metrics_used": ["host_noise_meta"],
                "stage": None,
            }
        ]

    for stage_name, applied_bench in candidate_benches:
        if not applied_bench or applied_bench.get("status") == "error":
            continue
        for spec in METRICS:
            baseline_value = numeric(baseline_bench.get(spec.baseline_key))
            applied_value = numeric(applied_bench.get(spec.applied_key))
            if baseline_value is None or applied_value is None:
                continue
            delta = _delta_pct(baseline_value, applied_value, higher_is_better=spec.higher_is_better)
            if delta is None:
                continue
            confidence = _confidence(payload, baseline_bench, applied_bench, spec)
            verdict = _metric_verdict(delta, category=spec.category, confidence=confidence)
            findings.append(
                {
                    "verdict": verdict,
                    "confidence": confidence,
                    "delta_pct": delta,
                    "reason": f"{stage_name} {spec.name} changed by {delta:+.2f}%",
                    "metrics_used": [spec.name],
                    "stage": stage_name,
                    "baseline": baseline_value,
                    "applied": applied_value,
                }
            )

    return findings


def _best_performance_finding(findings: list[dict[str, Any]]) -> dict[str, Any] | None:
    if not findings:
        return None

    def sort_key(item: dict[str, Any]) -> tuple[int, float]:
        verdict = str(item.get("verdict") or "no_effect")
        delta = numeric(item.get("delta_pct"))
        magnitude = abs(delta) if delta is not None else 0.0
        return (PERF_PRIORITY.get(verdict, 99), -magnitude)

    return sorted(findings, key=sort_key)[0]


def compute_registry_value_verdict(payload: dict[str, Any]) -> dict[str, Any]:
    safety = _safety_findings(payload)
    if safety:
        top = sorted(safety, key=lambda item: SAFETY_PRIORITY.get(str(item.get("verdict")), 99))[0]
        return {
            "overall": top["verdict"],
            "confidence": top["confidence"],
            "delta_pct": top.get("delta_pct"),
            "reason": top["reason"],
            "metrics_used": top.get("metrics_used", []),
            "host_noise": _host_noise_status(payload),
            "safety_findings": safety,
            "performance_findings": _performance_findings(payload),
        }

    performance = _performance_findings(payload)
    top_perf = _best_performance_finding(performance)
    if top_perf is None:
        top_perf = {
            "verdict": "no_effect",
            "confidence": "low" if _host_noise_status(payload) == "unknown" else "medium",
            "delta_pct": None,
            "reason": "no comparable benchmark metrics found",
            "metrics_used": [],
        }

    return {
        "overall": top_perf["verdict"],
        "confidence": top_perf["confidence"],
        "delta_pct": top_perf.get("delta_pct"),
        "reason": top_perf["reason"],
        "metrics_used": top_perf.get("metrics_used", []),
        "host_noise": _host_noise_status(payload),
        "safety_findings": safety,
        "performance_findings": performance,
    }
