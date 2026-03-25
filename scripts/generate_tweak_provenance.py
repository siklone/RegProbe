#!/usr/bin/env python3
from __future__ import annotations

import csv
import json
import re
import subprocess
import time
import urllib.error
import urllib.request
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

REPO_ROOT = Path(__file__).resolve().parents[1]
DOCS_ROOT = REPO_ROOT / "Docs"
TWEAKS_ROOT = DOCS_ROOT / "tweaks"
CATALOG_CSV = TWEAKS_ROOT / "tweak-catalog.csv"
AUDIT_CSV = TWEAKS_ROOT / "tweak-source-audit.csv"
OUTPUT_JSON = TWEAKS_ROOT / "tweak-provenance.json"
OUTPUT_CSV = TWEAKS_ROOT / "tweak-provenance.csv"
OUTPUT_MD = TWEAKS_ROOT / "tweak-provenance.md"
OUTPUT_MISSING_CSV = TWEAKS_ROOT / "tweak-provenance-missing.csv"
OUTPUT_MISSING_MD = TWEAKS_ROOT / "tweak-provenance-missing.md"
OUTPUT_BIBLIOGRAPHY_MD = TWEAKS_ROOT / "windows-internals-bibliography.md"
OVERRIDES_JSON = TWEAKS_ROOT / "tweak-provenance-overrides.json"
LOCAL_MIRRORS_ROOT = TWEAKS_ROOT / "_source-mirrors"

USER_AGENT = "OpenTraceProject-ProvenanceGenerator"
TEXT_EXTENSIONS = {
    ".md",
    ".txt",
    ".ps1",
    ".cmd",
    ".bat",
    ".py",
    ".reg",
    ".json",
    ".xml",
    ".c",
    ".cpp",
    ".h",
    ".inf",
    ".iss",
}
GENERIC_TOKEN_PARTS = {
    "hklm",
    "hkcu",
    "currentcontrolset",
    "controlset001",
    "software",
    "system",
    "services",
    "control",
    "parameters",
    "windows",
    "microsoft",
    "default",
    "currentversion",
    "type",
    "data",
    "length",
    "reg_dword",
    "reg_sz",
    "reg_multi_sz",
}

REPO_DEFINITIONS = {
    "win-config": {
        "owner": "nohuto",
        "repo": "win-config",
        "branch": "main",
        "repository_url": "https://github.com/nohuto/win-config",
        "weight": 12,
    },
    "win-registry": {
        "owner": "nohuto",
        "repo": "win-registry",
        "branch": "main",
        "repository_url": "https://github.com/nohuto/win-registry",
        "weight": 10,
    },
    "regkit": {
        "owner": "nohuto",
        "repo": "regkit",
        "branch": "main",
        "repository_url": "https://github.com/nohuto/regkit",
        "weight": 5,
    },
    "decompiled-pseudocode": {
        "owner": "nohuto",
        "repo": "decompiled-pseudocode",
        "branch": "main",
        "repository_url": "https://github.com/nohuto/decompiled-pseudocode",
        "weight": 4,
    },
}

WIN_CONFIG_CATEGORY_FALLBACKS = {
    "audio": "peripheral/desc.md",
    "cleanup": "cleanup/desc.md",
    "explorer": "visibility/desc.md",
    "misc": "misc/desc.md",
    "network": "network/desc.md",
    "notifications": "system/desc.md",
    "performance": "affinities/desc.md",
    "peripheral": "peripheral/desc.md",
    "power": "power/desc.md",
    "privacy": "privacy/desc.md",
    "security": "security/desc.md",
    "system": "system/desc.md",
    "visibility": "visibility/desc.md",
}

INTERNALS_REFERENCES = [
    {
        "id": "windows-internals-general",
        "title": "Windows Internals resource page",
        "url": "https://learn.microsoft.com/en-us/sysinternals/resources/windows-internals",
        "summary": "Official Microsoft landing page for the Windows Internals books and companion material.",
        "patterns": ["system.*", "network.*", "security.*", "performance.*", "power.*", "peripheral.*"],
    },
    {
        "id": "priority-control-local-asset",
        "title": "Local asset: Win32PrioritySeparation",
        "url": "Docs/system/assets/Win32PrioritySeparation.pdf",
        "summary": "Local research asset for Win32PrioritySeparation and scheduler-related registry behavior.",
        "patterns": ["system.priority-control"],
    },
    {
        "id": "smb-remote-fsds-local-asset",
        "title": "Local asset: Windows Internals E7 Part 2",
        "url": "Docs/affinities/assets/E7-P2.pdf",
        "summary": "Local Windows Internals Part 2 asset used in the SMB / Remote FSD documentation chain.",
        "patterns": [
            "network.optimize-smb",
            "network.disable-default-shares",
            "network.disable-plaintext-smb-passwords",
            "network.require-ntlmv2-session-security",
            "network.smb-*",
        ],
    },
]

DECOMPILED_HINTS = [
    {
        "title": "decompiled-pseudocode / dxgkrnl",
        "url": "https://github.com/nohuto/decompiled-pseudocode/tree/main/dxgkrnl",
        "summary": "Graphics kernel pseudocode relevant to GraphicsDrivers / TDR / HAGS style registry work.",
        "patterns": ["system.enable-hags", "system.graphics-*", "security.graphics-*"],
        "token_markers": ["graphicsdrivers", "tdr", "hwschmode"],
    },
    {
        "title": "decompiled-pseudocode / mmcss",
        "url": "https://github.com/nohuto/decompiled-pseudocode/tree/main/mmcss",
        "summary": "MMCSS pseudocode relevant to SystemProfile scheduler values.",
        "patterns": ["power.*", "system.*"],
        "token_markers": ["systemresponsiveness", "networkthrottlingindex", "mmcss"],
    },
    {
        "title": "decompiled-pseudocode / stornvme",
        "url": "https://github.com/nohuto/decompiled-pseudocode/tree/main/stornvme",
        "summary": "StorNVMe pseudocode relevant to storage driver registry behavior.",
        "patterns": ["storage.*"],
        "token_markers": ["stornvme", "storport", "nvme"],
    },
    {
        "title": "decompiled-pseudocode / USBHUB3",
        "url": "https://github.com/nohuto/decompiled-pseudocode/tree/main/USBHUB3",
        "summary": "USB hub pseudocode relevant to USB and peripheral registry behavior.",
        "patterns": ["peripheral.*", "audio.*"],
        "token_markers": ["usb", "usbhub", "usbflags", "wisp", "touch"],
    },
    {
        "title": "decompiled-pseudocode / ntoskrnl",
        "url": "https://github.com/nohuto/decompiled-pseudocode/tree/main/ntoskrnl",
        "summary": "Kernel pseudocode relevant to Session Manager / PriorityControl / DPC paths.",
        "patterns": ["system.*", "performance.*"],
        "token_markers": ["prioritycontrol", "session manager", "threaddpcenable", "dpc", "kernel"],
    },
]


@dataclass
class TweakEntry:
    tweak_id: str
    name: str
    category: str
    risk: str
    source: str
    tokens: list[str]


@dataclass
class RepoDocument:
    repo_id: str
    path: str
    url: str
    title: str
    text: str
    lower_text: str


def api_json(url: str):
    request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT, "Accept": "application/vnd.github+json"})
    with urllib.request.urlopen(request) as response:
        return json.loads(response.read().decode("utf-8"))


def raw_text(url: str) -> str:
    request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    with urllib.request.urlopen(request) as response:
        return response.read().decode("utf-8", errors="ignore")


def safe_fetch_text(url: str) -> str:
    try:
        return raw_text(url)
    except urllib.error.HTTPError as exc:
        print(f"[warn] {url} -> {exc.code}")
        return ""
    except Exception as exc:  # pragma: no cover - best effort network fetch
        print(f"[warn] {url} -> {exc}")
        return ""


def repo_tree(repo_id: str) -> tuple[str, list[dict]]:
    local_root = LOCAL_MIRRORS_ROOT / repo_id
    if local_root.is_dir():
        sha = subprocess.check_output(
            ["git", "-C", str(local_root), "rev-parse", "HEAD"],
            text=True,
        ).strip()
        files = subprocess.check_output(
            ["git", "-C", str(local_root), "ls-files"],
            text=True,
        ).splitlines()
        tree: list[dict] = []
        for relative_path in files:
            file_path = local_root / relative_path
            if not file_path.is_file():
                continue
            tree.append(
                {
                    "type": "blob",
                    "path": relative_path.replace("\\", "/"),
                    "size": file_path.stat().st_size,
                    "local_path": str(file_path),
                }
            )
        return sha, tree

    repo = REPO_DEFINITIONS[repo_id]
    commit = api_json(f"https://api.github.com/repos/{repo['owner']}/{repo['repo']}/commits/{repo['branch']}")
    sha = commit["sha"]
    tree = api_json(f"https://api.github.com/repos/{repo['owner']}/{repo['repo']}/git/trees/{sha}?recursive=1")
    return sha, tree.get("tree", [])


def raw_url(repo_id: str, path: str) -> str:
    repo = REPO_DEFINITIONS[repo_id]
    return f"https://raw.githubusercontent.com/{repo['owner']}/{repo['repo']}/{repo['branch']}/{path}"


def blob_url(repo_id: str, path: str) -> str:
    repo = REPO_DEFINITIONS[repo_id]
    return f"https://github.com/{repo['owner']}/{repo['repo']}/blob/{repo['branch']}/{path}"


def include_document(repo_id: str, path: str, size: int | None) -> bool:
    extension = Path(path).suffix.lower()
    if extension not in TEXT_EXTENSIONS:
        return False
    if size and size > 600_000:
        return False

    if repo_id == "regkit":
        return (
            path == "README.md"
            or path.startswith("src/")
            or path.startswith("include/")
            or path.startswith("installer/")
        )

    if repo_id == "decompiled-pseudocode":
        return False

    return True


def load_repo_documents(repo_id: str, tree: list[dict]) -> list[RepoDocument]:
    documents: list[RepoDocument] = []
    for node in tree:
        if node.get("type") != "blob":
            continue
        path = node.get("path", "")
        if not include_document(repo_id, path, node.get("size")):
            continue

        local_path = node.get("local_path")
        if local_path:
            text = Path(local_path).read_text(encoding="utf-8", errors="ignore")
        else:
            text = safe_fetch_text(raw_url(repo_id, path))
        if not text.strip():
            continue

        documents.append(
            RepoDocument(
                repo_id=repo_id,
                path=path,
                url=blob_url(repo_id, path),
                title=f"{repo_id} / {path}",
                text=text,
                lower_text=text.lower(),
            )
        )
        time.sleep(0.02)

    return documents


def read_catalog() -> dict[str, dict[str, str]]:
    entries: dict[str, dict[str, str]] = {}
    with CATALOG_CSV.open("r", encoding="utf-8", newline="") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            tweak_id = (row.get("id") or "").strip()
            if not tweak_id:
                continue
            entries[tweak_id.lower()] = {
                "id": tweak_id,
                "name": (row.get("name") or "").strip(),
                "category": (row.get("category") or "").strip(),
                "risk": (row.get("risk") or "").strip(),
                "source": (row.get("source") or "").strip(),
            }
    return entries


def load_manual_overrides() -> list[dict]:
    if not OVERRIDES_JSON.exists():
        return []

    try:
        payload = json.loads(OVERRIDES_JSON.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        raise SystemExit(f"[error] failed to parse {OVERRIDES_JSON}: {exc}") from exc

    entries = payload.get("Entries", [])
    if not isinstance(entries, list):
        raise SystemExit(f"[error] {OVERRIDES_JSON} must contain an Entries array")

    normalized: list[dict] = []
    for entry in entries:
        if not isinstance(entry, dict):
            continue
        normalized.append(entry)
    return normalized


def normalize_text(value: str) -> str:
    normalized = value.lower().replace("\\", "/").replace("_", " ")
    normalized = re.sub(r"\s+", " ", normalized)
    return normalized.strip()


def token_variants(token: str) -> list[str]:
    token = token.strip()
    if not token:
        return []

    normalized = normalize_text(token)
    variants = [normalized]
    if "\\" in token or "/" in token:
        for part in re.split(r"[\\/{}().,\s\-]+", token):
            part = normalize_text(part)
            if len(part) >= 4 and part not in GENERIC_TOKEN_PARTS:
                variants.append(part)
    elif len(normalized) >= 4 and normalized not in GENERIC_TOKEN_PARTS:
        variants.append(normalized)

    unique: list[str] = []
    for variant in variants:
        if variant and variant not in unique:
            unique.append(variant)
    return unique


def build_tweaks() -> list[TweakEntry]:
    catalog = read_catalog()
    tweaks: list[TweakEntry] = []

    with AUDIT_CSV.open("r", encoding="utf-8", newline="") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            tweak_id = (row.get("id") or "").strip()
            if not tweak_id:
                continue
            catalog_entry = catalog.get(tweak_id.lower(), {})
            tokens_raw = [token.strip() for token in (row.get("tokens") or "").split(";") if token.strip()]
            tokens: list[str] = []
            for raw_token in tokens_raw:
                for variant in token_variants(raw_token):
                    if variant not in tokens:
                        tokens.append(variant)

            if not tokens:
                for variant in token_variants(catalog_entry.get("name", "")):
                    if variant not in tokens:
                        tokens.append(variant)

            tweaks.append(
                TweakEntry(
                    tweak_id=tweak_id,
                    name=catalog_entry.get("name", (row.get("name") or "").strip()),
                    category=(catalog_entry.get("category") or tweak_id.split(".", 1)[0]).strip(),
                    risk=(catalog_entry.get("risk") or "").strip(),
                    source=(catalog_entry.get("source") or (row.get("source") or "").strip()),
                    tokens=tokens,
                )
            )

    tweaks.sort(key=lambda item: item.tweak_id.lower())
    return tweaks


def category_prefix(tweak: TweakEntry) -> str:
    return tweak.tweak_id.split(".", 1)[0].lower() if "." in tweak.tweak_id else tweak.category.lower()


def match_score(tweak: TweakEntry, document: RepoDocument) -> tuple[int, list[str]]:
    matched: list[str] = []
    for token in tweak.tokens:
        if token and token in document.lower_text:
            matched.append(token)

    if not matched:
        return 0, []

    score = len(matched) * 14
    prefix = category_prefix(tweak)
    if document.repo_id == "win-config" and document.path.startswith(f"{prefix}/"):
        score += 18

    if document.path.endswith("/desc.md"):
        score += 8
    elif document.path.endswith("README.md"):
        score += 6

    score += REPO_DEFINITIONS[document.repo_id]["weight"]
    return score, matched


def fallback_reference(tweak: TweakEntry, repo_documents: dict[str, list[RepoDocument]]) -> dict | None:
    prefix = category_prefix(tweak)
    fallback_path = WIN_CONFIG_CATEGORY_FALLBACKS.get(prefix)
    if not fallback_path:
        return None

    for document in repo_documents.get("win-config", []):
        if document.path == fallback_path:
            return {
                "repo": "win-config",
                "title": document.title,
                "url": document.url,
                "summary": "Category-level upstream win-config lineage fallback. Still needs a stronger tweak-specific match before it can speak for value semantics.",
                "score": 5,
                "matched_tokens": [],
            }

    return None


def matches_pattern(value: str, pattern: str) -> bool:
    escaped = re.escape(pattern).replace(r"\*", ".*")
    return re.fullmatch(escaped, value, re.IGNORECASE) is not None


def tweak_matches_override(tweak: TweakEntry, override: dict) -> bool:
    override_id = (override.get("Id") or "").strip()
    if override_id and tweak.tweak_id.lower() == override_id.lower():
        return True

    pattern = (override.get("Pattern") or "").strip()
    if pattern and matches_pattern(tweak.tweak_id, pattern):
        return True

    return False


def internals_references_for(tweak: TweakEntry) -> list[dict]:
    references: list[dict] = []
    for entry in INTERNALS_REFERENCES:
        if any(matches_pattern(tweak.tweak_id, pattern) for pattern in entry["patterns"]):
            references.append(
                {
                    "kind": "internals",
                    "title": entry["title"],
                    "url": entry["url"],
                    "summary": entry["summary"],
                }
            )
    return references


def decompiled_reference_for(tweak: TweakEntry) -> dict | None:
    tweak_tokens = " ".join(tweak.tokens)
    for hint in DECOMPILED_HINTS:
        if any(matches_pattern(tweak.tweak_id, pattern) for pattern in hint["patterns"]):
            return {
                "kind": "nohuto",
                "title": hint["title"],
                "url": hint["url"],
                "summary": hint["summary"],
            }
        if any(marker in tweak_tokens for marker in hint["token_markers"]):
            return {
                "kind": "nohuto",
                "title": hint["title"],
                "url": hint["url"],
                "summary": hint["summary"],
            }
    return None


def build_entry(tweak: TweakEntry, repo_documents: dict[str, list[RepoDocument]]) -> dict:
    hits: list[dict] = []
    for repo_id, documents in repo_documents.items():
        for document in documents:
            score, matched_tokens = match_score(tweak, document)
            if score <= 0:
                continue
            hits.append(
                {
                    "repo": repo_id,
                    "title": document.title,
                    "url": document.url,
                    "summary": f"Matched {len(matched_tokens)} audit token(s) in {repo_id}.",
                    "score": score,
                    "matched_tokens": matched_tokens,
                }
            )

    hits.sort(
        key=lambda item: (
            item["score"],
            len(item["matched_tokens"]),
            item["repo"] == "win-config",
            item["repo"] == "win-registry",
        ),
        reverse=True,
    )

    deduped_hits: list[dict] = []
    seen_urls: set[str] = set()
    for hit in hits:
        if hit["url"] in seen_urls:
            continue
        seen_urls.add(hit["url"])
        deduped_hits.append(hit)
        if len(deduped_hits) >= 3:
            break

    coverage_state = "unmapped"
    has_nohuto_evidence = False
    needs_review = True
    references: list[dict] = []
    source_repositories: list[str] = []
    matched_tokens: list[str] = []

    if deduped_hits:
        coverage_state = "repo-backed"
        has_nohuto_evidence = True
        needs_review = False
        for hit in deduped_hits:
            references.append(
                {
                    "kind": "nohuto",
                    "title": hit["title"],
                    "url": hit["url"],
                    "summary": hit["summary"],
                }
            )
            if hit["repo"] not in source_repositories:
                source_repositories.append(hit["repo"])
            for token in hit["matched_tokens"]:
                if token not in matched_tokens:
                    matched_tokens.append(token)
    else:
        fallback = fallback_reference(tweak, repo_documents)
        if fallback:
            coverage_state = "category-fallback"
            has_nohuto_evidence = True
            needs_review = True
            references.append(
                {
                    "kind": "nohuto",
                    "title": fallback["title"],
                    "url": fallback["url"],
                    "summary": fallback["summary"],
                }
            )
            source_repositories.append("win-config")

    decompiled_ref = decompiled_reference_for(tweak)
    if decompiled_ref and not any(ref["url"] == decompiled_ref["url"] for ref in references):
        references.append(decompiled_ref)
        if "decompiled-pseudocode" not in source_repositories:
            source_repositories.append("decompiled-pseudocode")

    internals_refs = internals_references_for(tweak)
    for ref in internals_refs:
        if not any(existing["url"] == ref["url"] for existing in references):
            references.append(ref)

    has_windows_internals_context = any(ref["kind"] == "internals" for ref in references)

    if coverage_state == "repo-backed":
        summary = (
            f"Matched {len(matched_tokens)} audit token(s) across "
            f"{', '.join(source_repositories)}. These links show where the name came from. Value semantics still come from the research record."
        )
    elif coverage_state == "category-fallback":
        summary = (
            "Linked to the closest upstream win-config category docs, but this tweak still needs "
            "a stronger tweak-specific upstream match before being considered fully curated."
        )
    elif has_windows_internals_context:
        summary = "Only Windows Internals notes are linked right now. Upstream dump or pseudocode sources are still missing."
    else:
        summary = "No upstream source match found yet. Keep this tweak in review-only state."

    return {
        "Id": tweak.tweak_id,
        "Name": tweak.name,
        "Category": tweak.category,
        "Risk": tweak.risk,
        "Source": tweak.source,
        "HasNohutoEvidence": has_nohuto_evidence,
        "HasWindowsInternalsContext": has_windows_internals_context,
        "NeedsReview": needs_review,
        "CoverageState": coverage_state,
        "Summary": summary,
        "SourceRepositories": source_repositories,
        "MatchedTokens": matched_tokens,
        "References": [
            {
                "Kind": ref["kind"],
                "Title": ref["title"],
                "Url": ref["url"],
                "Summary": ref["summary"],
            }
            for ref in references
        ],
    }


def apply_manual_overrides(entry: dict, tweak: TweakEntry, manual_overrides: list[dict]) -> dict:
    applicable = [override for override in manual_overrides if tweak_matches_override(tweak, override)]
    if not applicable:
        return entry

    references = list(entry["References"])
    source_repositories = list(entry["SourceRepositories"])
    matched_tokens = list(entry["MatchedTokens"])
    coverage_state = entry["CoverageState"]
    needs_review = entry["NeedsReview"]
    summary = entry["Summary"]

    for override in applicable:
        if override.get("DropCategoryFallback"):
            references = [
                reference for reference in references
                if "Category-level upstream win-config lineage fallback." not in reference.get("Summary", "")
            ]

        for repo_id in override.get("SourceRepositories", []):
            repo_name = str(repo_id).strip()
            if repo_name and repo_name not in source_repositories:
                source_repositories.append(repo_name)

        for token in override.get("MatchedTokens", []):
            token_text = str(token).strip()
            if token_text and token_text not in matched_tokens:
                matched_tokens.append(token_text)

        for reference in override.get("References", []):
            if not isinstance(reference, dict):
                continue

            url = str(reference.get("Url") or "").strip()
            if not url:
                continue
            if any(existing.get("Url") == url for existing in references):
                continue

            references.append(
                {
                    "Kind": str(reference.get("Kind") or "research").strip(),
                    "Title": str(reference.get("Title") or url).strip(),
                    "Url": url,
                    "Summary": str(reference.get("Summary") or "").strip(),
                }
            )

        if override.get("ClearReview"):
            needs_review = False

        coverage_override = str(override.get("CoverageState") or "").strip()
        if coverage_override:
            coverage_state = coverage_override

        summary_override = str(override.get("Summary") or "").strip()
        if summary_override:
            summary = summary_override

    has_nohuto_evidence = entry["HasNohutoEvidence"] or any(
        reference.get("Kind") == "nohuto" for reference in references
    ) or any(repo_id in REPO_DEFINITIONS for repo_id in source_repositories)
    has_windows_internals_context = any(reference.get("Kind") == "internals" for reference in references)

    if not coverage_state and has_nohuto_evidence:
        coverage_state = "repo-backed"
    elif coverage_state in {"unmapped", "category-fallback"} and has_nohuto_evidence and not needs_review:
        coverage_state = "repo-backed"

    entry["HasNohutoEvidence"] = has_nohuto_evidence
    entry["HasWindowsInternalsContext"] = has_windows_internals_context
    entry["NeedsReview"] = needs_review
    entry["CoverageState"] = coverage_state
    entry["Summary"] = summary
    entry["SourceRepositories"] = source_repositories
    entry["MatchedTokens"] = matched_tokens
    entry["References"] = references
    return entry


def build_bibliography_markdown() -> str:
    lines = [
        "# Windows Internals Bibliography",
        "",
        "Generated by `scripts/generate_tweak_provenance.py`.",
        "",
        "This file is intentionally conservative: it lists Windows Internals references used as subsystem context, not as a license to auto-apply undocumented tweaks.",
        "",
    ]

    for entry in INTERNALS_REFERENCES:
        lines.append(f"## <a id=\"{entry['id']}\"></a> {entry['title']}")
        lines.append("")
        lines.append(f"- Link: {entry['url']}")
        lines.append(f"- Why it matters: {entry['summary']}")
        lines.append(f"- Applies to: {', '.join(entry['patterns'])}")
        lines.append("")

    return "\n".join(lines) + "\n"


def write_outputs(payload: dict) -> None:
    TWEAKS_ROOT.mkdir(parents=True, exist_ok=True)
    OUTPUT_JSON.write_text(json.dumps(payload, indent=2), encoding="utf-8")

    entries = payload["Entries"]
    with OUTPUT_CSV.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow([
            "id",
            "name",
            "category",
            "risk",
            "coverage_state",
            "has_nohuto_evidence",
            "has_windows_internals_context",
            "needs_review",
            "source_repositories",
            "matched_tokens",
            "summary",
        ])
        for entry in entries:
            writer.writerow([
                entry["Id"],
                entry["Name"],
                entry["Category"],
                entry["Risk"],
                entry["CoverageState"],
                "yes" if entry["HasNohutoEvidence"] else "no",
                "yes" if entry["HasWindowsInternalsContext"] else "no",
                "yes" if entry["NeedsReview"] else "no",
                ";".join(entry["SourceRepositories"]),
                ";".join(entry["MatchedTokens"]),
                entry["Summary"],
            ])

    missing_entries = [entry for entry in entries if entry["NeedsReview"]]
    with OUTPUT_MISSING_CSV.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(["id", "name", "category", "coverage_state", "source_repositories", "summary"])
        for entry in missing_entries:
            writer.writerow([
                entry["Id"],
                entry["Name"],
                entry["Category"],
                entry["CoverageState"],
                ";".join(entry["SourceRepositories"]),
                entry["Summary"],
            ])

    lines = [
        "# Tweak Source Report",
        "",
        "Generated by `scripts/generate_tweak_provenance.py`.",
        "",
        f"- Summary: {payload['Summary']}",
        f"- Generated: {payload['GeneratedAtUtc']}",
        "",
        "## Tracked Sources",
        "",
    ]

    for source in payload["Sources"]:
        lines.append(f"- `{source['Repository']}` @ `{source['CommitSha'][:12]}` - {source['RepositoryUrl']}")

    lines.extend([
        "",
        "## Coverage",
        "",
        "| ID | Coverage | Repos | Internals | Review | Notes |",
        "| --- | --- | --- | --- | --- | --- |",
    ])

    for entry in entries:
        lines.append(
            f"| `{entry['Id']}` | {entry['CoverageState']} | "
            f"`{', '.join(entry['SourceRepositories']) or '-'}` | "
            f"{'yes' if entry['HasWindowsInternalsContext'] else 'no'} | "
            f"{'yes' if entry['NeedsReview'] else 'no'} | {entry['Summary']} |"
        )

    OUTPUT_MD.write_text("\n".join(lines) + "\n", encoding="utf-8")

    missing_lines = [
        "# Tweak Source Missing / Review Report",
        "",
        f"- Missing or review-needed entries: {len(missing_entries)}",
        "",
        "| ID | Coverage | Repos | Notes |",
        "| --- | --- | --- | --- |",
    ]
    for entry in missing_entries:
        missing_lines.append(
            f"| `{entry['Id']}` | {entry['CoverageState']} | `{', '.join(entry['SourceRepositories']) or '-'}` | {entry['Summary']} |"
        )
    OUTPUT_MISSING_MD.write_text("\n".join(missing_lines) + "\n", encoding="utf-8")

    OUTPUT_BIBLIOGRAPHY_MD.write_text(build_bibliography_markdown(), encoding="utf-8")


def main() -> int:
    tweaks = build_tweaks()
    manual_overrides = load_manual_overrides()

    source_states: list[dict] = []
    repo_documents: dict[str, list[RepoDocument]] = {}

    for repo_id in ("win-config", "win-registry", "regkit"):
        sha, tree = repo_tree(repo_id)
        repo_documents[repo_id] = load_repo_documents(repo_id, tree)
        source_states.append(
            {
                "Repository": repo_id,
                "CommitSha": sha,
                "RepositoryUrl": REPO_DEFINITIONS[repo_id]["repository_url"],
            }
        )
        print(f"[info] {repo_id}: {len(repo_documents[repo_id])} searchable documents")

    decompiled_sha, _ = repo_tree("decompiled-pseudocode")
    source_states.append(
        {
            "Repository": "decompiled-pseudocode",
            "CommitSha": decompiled_sha,
            "RepositoryUrl": REPO_DEFINITIONS["decompiled-pseudocode"]["repository_url"],
        }
    )

    entries = [apply_manual_overrides(build_entry(tweak, repo_documents), tweak, manual_overrides) for tweak in tweaks]
    total = len(entries)
    repo_backed = sum(1 for entry in entries if entry["HasNohutoEvidence"])
    internals_backed = sum(1 for entry in entries if entry["HasWindowsInternalsContext"])
    review_needed = sum(1 for entry in entries if entry["NeedsReview"])

    payload = {
        "GeneratedAtUtc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
        "Summary": f"{repo_backed}/{total} tweaks have upstream dump or pseudocode links, {internals_backed} have Windows Internals notes, {review_needed} still need review.",
        "TotalTweaks": total,
        "RepoBackedTweaks": repo_backed,
        "InternalsBackedTweaks": internals_backed,
        "ReviewNeededTweaks": review_needed,
        "Sources": source_states,
        "Entries": entries,
    }

    write_outputs(payload)
    print(f"[done] wrote {OUTPUT_JSON}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
