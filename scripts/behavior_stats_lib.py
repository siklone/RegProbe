from __future__ import annotations

import math
import statistics
from typing import Any


def _clean_numeric_samples(values: Any) -> list[float]:
    if not isinstance(values, list):
        return []
    result: list[float] = []
    for item in values:
        if isinstance(item, (int, float)) and not isinstance(item, bool):
            result.append(float(item))
    return result


def _mean(values: list[float]) -> float | None:
    return statistics.fmean(values) if values else None


def _stdev(values: list[float]) -> float | None:
    return statistics.stdev(values) if len(values) >= 2 else 0.0 if values else None


def _confidence_interval_95(mean_value: float | None, stdev_value: float | None, sample_count: int) -> list[float] | None:
    if mean_value is None or stdev_value is None or sample_count <= 0:
        return None
    if sample_count == 1:
        return [mean_value, mean_value]
    margin = 1.96 * (stdev_value / math.sqrt(sample_count))
    return [mean_value - margin, mean_value + margin]


def _normal_p_value(z_score: float) -> float:
    cdf = 0.5 * (1.0 + math.erf(abs(z_score) / math.sqrt(2.0)))
    return max(0.0, min(1.0, 2.0 * (1.0 - cdf)))


def summarize_before_after(before_samples: Any, after_samples: Any) -> dict[str, Any]:
    before = _clean_numeric_samples(before_samples)
    after = _clean_numeric_samples(after_samples)
    if not before or not after:
        return {
            "before_mean": None,
            "before_stdev": None,
            "after_mean": None,
            "after_stdev": None,
            "sample_count": 0,
            "p_value": None,
            "significant": None,
            "effect_size": None,
            "confidence_interval_95": None,
        }

    before_mean = _mean(before)
    after_mean = _mean(after)
    before_stdev = _stdev(before)
    after_stdev = _stdev(after)
    sample_count = min(len(before), len(after))
    pooled_variance = ((before_stdev or 0.0) ** 2 / len(before)) + ((after_stdev or 0.0) ** 2 / len(after))
    if pooled_variance <= 0:
        p_value = 0.0 if before_mean != after_mean else 1.0
    else:
        z_score = (before_mean - after_mean) / math.sqrt(pooled_variance)
        p_value = _normal_p_value(z_score)

    pooled_stdev = math.sqrt((((len(before) - 1) * (before_stdev or 0.0) ** 2) + ((len(after) - 1) * (after_stdev or 0.0) ** 2)) / max(1, (len(before) + len(after) - 2)))
    if pooled_stdev <= 0:
        effect_size = "none" if before_mean == after_mean else "large"
    else:
        d_value = abs((after_mean or 0.0) - (before_mean or 0.0)) / pooled_stdev
        if d_value < 0.2:
            effect_size = "none"
        elif d_value < 0.5:
            effect_size = "small"
        elif d_value < 0.8:
            effect_size = "medium"
        else:
            effect_size = "large"

    significant = p_value <= 0.05
    return {
        "before_mean": before_mean,
        "before_stdev": before_stdev,
        "after_mean": after_mean,
        "after_stdev": after_stdev,
        "sample_count": sample_count,
        "p_value": p_value,
        "significant": significant,
        "effect_size": effect_size,
        "confidence_interval_95": {
            "before": _confidence_interval_95(before_mean, before_stdev, len(before)),
            "after": _confidence_interval_95(after_mean, after_stdev, len(after)),
        },
    }
