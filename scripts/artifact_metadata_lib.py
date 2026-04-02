from __future__ import annotations

from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from research_path_lib import file_sha256


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def stat_collected_utc(path: Path) -> str:
    return datetime.fromtimestamp(path.stat().st_mtime, tz=timezone.utc).isoformat().replace("+00:00", "Z")


def build_artifact_metadata(
    repo_root: Path,
    repo_ref: str,
    *,
    collected_utc: str | None = None,
    extra: dict[str, Any] | None = None,
) -> dict[str, Any]:
    normalized_ref = repo_ref.replace("\\", "/").lstrip("/")
    repo_path = repo_root / Path(normalized_ref)
    payload: dict[str, Any] = {"path": normalized_ref}
    if extra:
        payload.update(extra)
    if repo_path.exists() and repo_path.is_file():
        payload["sha256"] = file_sha256(repo_path)
        payload["size"] = repo_path.stat().st_size
        payload["collected_utc"] = collected_utc or stat_collected_utc(repo_path)
        payload["exists"] = True
    else:
        payload["sha256"] = None
        payload["size"] = None
        payload["collected_utc"] = collected_utc
        payload["exists"] = False
    return payload
