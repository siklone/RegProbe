from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from research_path_lib import RESEARCH_ROOT, repo_relative_path

IMPORTED_CANDIDATE_BACKLOG_PATH = RESEARCH_ROOT / "imported-candidate-backlog.json"


def load_imported_candidate_backlog(path: Path | None = None) -> dict[str, Any] | None:
    backlog_path = path or IMPORTED_CANDIDATE_BACKLOG_PATH
    if not backlog_path.exists():
        return None
    with backlog_path.open("r", encoding="utf-8-sig") as handle:
        payload = json.load(handle)
    return payload if isinstance(payload, dict) else None


def build_imported_candidate_backlog_summary(path: Path | None = None) -> dict[str, Any]:
    backlog_path = path or IMPORTED_CANDIDATE_BACKLOG_PATH
    payload = load_imported_candidate_backlog(backlog_path)
    if not payload:
        return {
            "path": repo_relative_path(backlog_path),
            "exists": False,
            "generated_utc": None,
            "candidate_count": 0,
            "import_count": 0,
            "source_run_count": 0,
            "blocked_candidate_count": 0,
            "counts_by_source_tool": {},
            "counts_by_confidence": {},
            "counts_by_promotion_state": {},
        }

    return {
        "path": repo_relative_path(backlog_path),
        "exists": True,
        "generated_utc": payload.get("generated_utc"),
        "candidate_count": int(payload.get("candidate_count") or 0),
        "import_count": int(payload.get("import_count") or 0),
        "source_run_count": int(payload.get("source_run_count") or 0),
        "blocked_candidate_count": int(payload.get("blocked_candidate_count") or 0),
        "counts_by_source_tool": payload.get("counts_by_source_tool") or {},
        "counts_by_confidence": payload.get("counts_by_confidence") or {},
        "counts_by_promotion_state": payload.get("counts_by_promotion_state") or {},
    }
