from __future__ import annotations

import hashlib
import os
import re
import shutil
from pathlib import Path
from typing import Iterable

REDACTED_USER = "<USER>"
REPO_ROOT = Path(__file__).resolve().parents[1]
RESEARCH_ROOT = REPO_ROOT / "research"
EVIDENCE_FILES_ROOT = RESEARCH_ROOT / "evidence-files"

_HOME = Path.home()
_ALT_REPO_ROOTS = {
    REPO_ROOT,
    REPO_ROOT.resolve(),
    REPO_ROOT.parent / "Open-Trace-Project",
}
KNOWN_REPO_ROOTS = [root for root in _ALT_REPO_ROOTS if root.exists()]

TEXT_IMPORT_SUFFIXES = {
    ".adml",
    ".admx",
    ".bat",
    ".c",
    ".cmd",
    ".cpp",
    ".cs",
    ".csv",
    ".etl.txt",
    ".htm",
    ".html",
    ".ini",
    ".json",
    ".log",
    ".md",
    ".ps1",
    ".reg",
    ".txt",
    ".xml",
    ".yaml",
    ".yml",
}

WINDOWS_PATH_RE = re.compile(r"(?P<path>(?<![A-Za-z0-9])[A-Za-z]:[\\/][^\"\r\n]+?\.[A-Za-z0-9._-]+)")
REPO_PATH_RE = re.compile(r"(?P<path>(?:Docs|research)[\\/][^\"\r\n;|<>\s]+?\.[A-Za-z0-9._-]+)")
BROKEN_WEB_PLACEHOLDER_RE = re.compile(
    r"(?P<path>(?:httpresearch|research)/evidence-files/missing/(?P<domain>[^/\s]+?\.md)/(?P<tail>[^\s)\"'>]+))",
    re.IGNORECASE,
)
URL_RE = re.compile(r"https?://[^\s)\"'>]+")

OLD_PREFIXES = (
    "Docs/tweaks/research/",
    "Docs\\tweaks\\research\\",
    "Docs/tweaks/_source-mirrors/",
    "Docs\\tweaks\\_source-mirrors\\",
    "Docs/research/",
    "Docs\\research\\",
)


def _normalize_slashes(value: str) -> str:
    return value.replace("\\", "/")


def _strip_repo_root_prefix(value: str) -> str:
    normalized = _normalize_slashes(value).strip()
    for root in KNOWN_REPO_ROOTS:
        prefix = _normalize_slashes(str(root)).rstrip("/")
        if normalized.lower().startswith(prefix.lower() + "/"):
            return normalized[len(prefix) + 1 :]
        if normalized.lower() == prefix.lower():
            return ""
    return normalized


def is_web_url(value: str | None) -> bool:
    if not value:
        return False
    lowered = value.strip().lower()
    return lowered.startswith("http://") or lowered.startswith("https://")


def restore_web_placeholder_url(value: str | None) -> str:
    if not value:
        return ""

    text = value.strip().replace("\\", "/")
    match = BROKEN_WEB_PLACEHOLDER_RE.fullmatch(text)
    if not match:
        return text

    domain_token = match.group("domain")
    if domain_token.lower().endswith(".md"):
        domain_token = domain_token[:-3]
    domain = domain_token.replace("-", ".")
    tail = match.group("tail").lstrip("/")
    if not tail:
        return f"https://{domain}"
    return f"https://{domain}/{tail}"


def normalize_repo_relative_path(value: str | None) -> str:
    if not value:
        return ""

    normalized = _strip_repo_root_prefix(value)
    normalized = normalized.strip().replace("\\", "/")

    if normalized.startswith("./"):
        normalized = normalized[2:]

    replacements = {
        "Docs/tweaks/research/": "research/",
        "Docs/tweaks/_source-mirrors/": "research/_source-mirrors/",
        "Docs/research/": "research/",
    }
    for old, new in replacements.items():
        if normalized.lower().startswith(old.lower()):
            normalized = new + normalized[len(old) :]
            break

    return normalized


def repo_relative_path(path: Path) -> str:
    try:
        return normalize_repo_relative_path(str(path.relative_to(REPO_ROOT)))
    except ValueError:
        return normalize_repo_relative_path(str(path))


def resolve_repo_path(value: str | None) -> Path | None:
    if not value:
        return None

    raw = value.strip()
    if re.match(r"^[A-Za-z]:[\\/]", raw):
        absolute_candidate = _actual_local_candidate(raw)
        if not absolute_candidate:
            return None
        for root in KNOWN_REPO_ROOTS:
            try:
                absolute_candidate.relative_to(root)
                return absolute_candidate
            except ValueError:
                continue
        return None

    normalized = normalize_repo_relative_path(value)
    if not normalized:
        return None

    candidate_path = Path(normalized.replace("/", os.sep))
    candidate = candidate_path if candidate_path.is_absolute() else (REPO_ROOT / candidate_path)
    if not candidate.exists():
        return None

    for root in KNOWN_REPO_ROOTS:
        try:
            candidate.relative_to(root)
            return candidate
        except ValueError:
            continue
    return None


def _actual_local_candidate(raw_path: str) -> Path | None:
    expanded = raw_path.replace(REDACTED_USER, _HOME.name).replace("/", "\\")
    candidate = Path(expanded)
    if candidate.exists():
        return candidate
    return None


def _external_relative_path(source_path: Path) -> Path:
    parts = list(source_path.parts)
    lowered = [part.lower() for part in parts]

    if "vm-tooling-staging" in lowered:
        index = lowered.index("vm-tooling-staging")
        tail = parts[index:]
        return Path(*tail)

    if "temp" in lowered:
        index = len(lowered) - 1 - lowered[::-1].index("temp")
        tail = parts[index + 1 :] or [source_path.name]
        stem = "local-temp" if "appdata" in lowered else "host-temp"
        return Path(stem, *tail)

    tail = parts[-3:] if len(parts) >= 3 else parts
    drive = (source_path.drive or "external").replace(":", "").lower()
    return Path("external", drive, *tail)


def _slug(text: str) -> str:
    cleaned = re.sub(r"[^a-z0-9]+", "-", text.lower()).strip("-")
    return cleaned or "artifact"


def _write_placeholder(target_base: Path, title: str, source_name: str, note: str) -> Path:
    target = target_base.with_suffix(target_base.suffix + ".md") if target_base.suffix else target_base.with_suffix(".md")
    target.parent.mkdir(parents=True, exist_ok=True)
    body = [
        "# External Evidence Placeholder",
        "",
        f"Title: {title or 'Captured evidence'}",
        "",
        f"File: `{source_name}`",
        "",
        note,
        "",
        "This placeholder keeps the evidence trail repo-friendly without leaking host-local paths.",
        "",
    ]
    target.write_text("\n".join(body), encoding="utf-8", newline="\n")
    return target


def import_external_artifact(value: str, title: str = "") -> str:
    source_path = _actual_local_candidate(value)
    source_name = Path(value.replace(REDACTED_USER, _HOME.name)).name or Path(value).name or _slug(title or "artifact")
    relative_base = _external_relative_path(source_path) if source_path else Path("missing", _slug(source_name))
    target_base = EVIDENCE_FILES_ROOT / relative_base

    if not source_path or not source_path.exists():
        target = _write_placeholder(
            target_base,
            title,
            source_name,
            "The local capture is not present in this workspace anymore.",
        )
        return repo_relative_path(target)

    suffix = source_path.suffix.lower()
    if suffix in TEXT_IMPORT_SUFFIXES:
        target = target_base
        target.parent.mkdir(parents=True, exist_ok=True)
        if not target.exists() or source_path.stat().st_mtime > target.stat().st_mtime or source_path.stat().st_size != target.stat().st_size:
            shutil.copy2(source_path, target)
        return repo_relative_path(target)

    target = _write_placeholder(
        target_base,
        title,
        source_path.name,
        f"The original capture is `{suffix or 'binary'}` and is not checked in directly. Keep the linked note and the paired text exports instead.",
    )
    return repo_relative_path(target)


def normalize_reference(value: str | None, title: str = "") -> str:
    if not value:
        return ""

    stripped = value.strip()
    if not stripped:
        return ""

    restored = restore_web_placeholder_url(stripped)
    if is_web_url(restored):
        return restored

    if is_web_url(stripped):
        return stripped

    repo_path = resolve_repo_path(stripped)
    if repo_path:
        return repo_relative_path(repo_path)

    normalized = normalize_repo_relative_path(stripped)
    if normalized.startswith("research/") or normalized.startswith("Docs/"):
        return normalized

    if re.match(r"^[A-Za-z]:[\\/]", stripped):
        return import_external_artifact(stripped, title=title)

    return normalize_repo_relative_path(stripped)


def normalize_reference_text(value: str | None, title: str = "") -> str:
    if not value:
        return ""

    text = str(value)

    def replace_repo(match: re.Match[str]) -> str:
        return normalize_reference(match.group("path"), title=title)

    def replace_windows(match: re.Match[str]) -> str:
        return normalize_reference(match.group("path"), title=title)

    def replace_broken_web_placeholder(match: re.Match[str]) -> str:
        return restore_web_placeholder_url(match.group("path"))

    text = BROKEN_WEB_PLACEHOLDER_RE.sub(replace_broken_web_placeholder, text)
    text = REPO_PATH_RE.sub(replace_repo, text)
    text = WINDOWS_PATH_RE.sub(replace_windows, text)

    return text.replace("\\", "/")


def _relative_link_target(doc_path: Path, target: str) -> str:
    if is_web_url(target):
        return target
    absolute_target = REPO_ROOT / target.replace("/", os.sep)
    relative = os.path.relpath(absolute_target, doc_path.parent)
    return relative.replace("\\", "/")


def display_reference_label(target: str) -> str:
    if not target:
        return ""
    if is_web_url(target):
        return target
    return target.replace("\\", "/")


def linkify_reference_text(value: str | None, doc_path: Path) -> str:
    if not value:
        return ""

    text = str(value).replace("|", "\\|").replace("\r", "").replace("\n", "<br>")

    def replace_url(match: re.Match[str]) -> str:
        target = match.group(0)
        return f"[{target}]({target})"

    def replace_repo(match: re.Match[str]) -> str:
        target = normalize_reference(match.group("path"))
        if not target:
            return match.group("path")
        label = display_reference_label(target)
        return f"[{label}]({_relative_link_target(doc_path, target)})"

    def replace_broken_web_placeholder(match: re.Match[str]) -> str:
        return restore_web_placeholder_url(match.group("path"))

    text = BROKEN_WEB_PLACEHOLDER_RE.sub(replace_broken_web_placeholder, text)
    text = URL_RE.sub(replace_url, text)
    text = REPO_PATH_RE.sub(replace_repo, text)
    return text


def file_sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(65536), b""):
            digest.update(chunk)
    return digest.hexdigest()


def existing_repo_paths(paths: Iterable[str]) -> list[str]:
    result: list[str] = []
    for value in paths:
        normalized = normalize_reference(value)
        if not normalized:
            continue
        absolute = REPO_ROOT / normalized.replace("/", os.sep)
        if absolute.exists():
            result.append(normalized)
    return result
