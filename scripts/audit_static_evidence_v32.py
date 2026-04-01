from __future__ import annotations

import argparse
import json
import re
import ssl
from collections import defaultdict
from dataclasses import dataclass
from datetime import datetime, timezone
from html.parser import HTMLParser
from pathlib import Path
from typing import Any
from urllib.error import HTTPError, URLError
from urllib.parse import urlparse
from urllib.request import Request, urlopen

REPO_ROOT = Path(__file__).resolve().parents[1]
AUDIT_ROOT = REPO_ROOT / "registry-research-framework" / "audit"
CONFIG_ROOT = REPO_ROOT / "registry-research-framework" / "config"
RECORDS_ROOT = REPO_ROOT / "research" / "records"
EVIDENCE_RECORDS_ROOT = REPO_ROOT / "evidence" / "records"
RESEARCH_NOTES_ROOT = REPO_ROOT / "research" / "notes"
AUDIT_NOTES_ROOT = REPO_ROOT / "registry-research-framework" / "audit"
GHIDRA_ROOT = REPO_ROOT / "evidence" / "files" / "ghidra"
LINK_REVIEW_OVERRIDES_PATH = CONFIG_ROOT / "link-context-overrides.json"

SCAN_TARGETS = (
    RECORDS_ROOT,
    EVIDENCE_RECORDS_ROOT,
    RESEARCH_NOTES_ROOT,
    AUDIT_NOTES_ROOT,
)

URL_RE = re.compile(r"https?://[^\s\"'<>`]+", re.IGNORECASE)
CAMEL_CASE_RE = re.compile(r"(?<=[a-z0-9])(?=[A-Z])")
MICROSOFT_HOSTS = {
    "learn.microsoft.com",
    "support.microsoft.com",
    "techcommunity.microsoft.com",
}
GENERATED_AUDIT_PREFIXES = (
    "static-evidence-v32-",
    "nohuto-priority-queue-",
    "nohuto-priority-v32-reaudit-",
)

PRIORITY_RECORDS: dict[str, dict[str, Any]] = {
    "system.priority-control": {
        "priority": 1,
        "reason": "Nohuto flagged Win32PrioritySeparation for overstated semantics and static-proof quality.",
    },
    "power.disable-network-power-saving.policy": {
        "priority": 1,
        "reason": "Nohuto flagged SystemResponsiveness doc interpretation inside the network/MMCSS child record.",
    },
}


class HtmlSummaryParser(HTMLParser):
    def __init__(self) -> None:
        super().__init__()
        self._current: list[str] = []
        self.title = ""
        self.h1 = ""
        self.text_parts: list[str] = []
        self._capture = False

    def handle_starttag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        lowered = tag.lower()
        self._current.append(lowered)
        self._capture = lowered in {"title", "h1", "p"}

    def handle_endtag(self, tag: str) -> None:
        lowered = tag.lower()
        while self._current:
            top = self._current.pop()
            if top == lowered:
                break
        self._capture = bool(self._current and self._current[-1] in {"title", "h1", "p"})

    def handle_data(self, data: str) -> None:
        text = " ".join(data.split())
        if not text:
            return
        current = self._current[-1] if self._current else ""
        if current == "title" and not self.title:
            self.title = text
        elif current == "h1" and not self.h1:
            self.h1 = text
        elif self._capture and len(" ".join(self.text_parts)) < 1200:
            self.text_parts.append(text)


@dataclass
class UrlReference:
    source_file: str
    source_kind: str
    tweak_id: str | None
    value_name: str | None
    registry_path: str | None
    url: str
    expected_tokens: list[str]


@dataclass
class LinkReviewOverride:
    source_file: str
    source_kind: str
    url: str
    review_status: str
    reviewed_utc: str
    notes: str


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(payload, handle, ensure_ascii=False, indent=2)
        handle.write("\n")


def write_text(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content.rstrip() + "\n", encoding="utf-8", newline="\n")


def repo_relative(path: Path) -> str:
    return str(path.relative_to(REPO_ROOT)).replace("\\", "/")


def load_link_review_overrides() -> dict[tuple[str, str, str], LinkReviewOverride]:
    if not LINK_REVIEW_OVERRIDES_PATH.exists():
        return {}
    try:
        payload = json.loads(read_text(LINK_REVIEW_OVERRIDES_PATH))
    except Exception:
        return {}

    overrides: dict[tuple[str, str, str], LinkReviewOverride] = {}
    for item in payload.get("entries") or []:
        if not isinstance(item, dict):
            continue
        source_file = str(item.get("source_file") or "").strip()
        source_kind = str(item.get("source_kind") or "").strip()
        url = str(item.get("url") or "").strip()
        review_status = str(item.get("review_status") or "").strip()
        reviewed_utc = str(item.get("reviewed_utc") or "").strip()
        notes = str(item.get("notes") or "").strip()
        if not source_file or not source_kind or not url or not review_status:
            continue
        overrides[(source_file, source_kind, url)] = LinkReviewOverride(
            source_file=source_file,
            source_kind=source_kind,
            url=url,
            review_status=review_status,
            reviewed_utc=reviewed_utc,
            notes=notes,
        )
    return overrides


def normalize_token_text(value: str) -> str:
    return CAMEL_CASE_RE.sub(" ", value)


def merge_tokens(*groups: list[str]) -> list[str]:
    deduped: list[str] = []
    seen: set[str] = set()
    for group in groups:
        for token in group:
            lowered = token.lower()
            if lowered in seen:
                continue
            seen.add(lowered)
            deduped.append(token)
    return deduped


def tokenize_text(*parts: str | None) -> list[str]:
    tokens: list[str] = []
    for part in parts:
        text = normalize_token_text(str(part or "").strip())
        if not text:
            continue
        pieces = re.split(r"[^A-Za-z0-9]+", text)
        tokens.extend(piece for piece in pieces if len(piece) >= 4)
    return merge_tokens(tokens)


def tokenize_expected(value_name: str | None, registry_path: str | None) -> list[str]:
    tokens: list[str] = []
    if value_name:
        tokens.append(value_name)
        pieces = re.split(r"[^A-Za-z0-9]+", normalize_token_text(value_name))
        tokens.extend(piece for piece in pieces if len(piece) >= 4)
    if registry_path:
        tokens.append(registry_path)
        tail = registry_path.split("\\")[-1]
        tokens.append(tail)
        pieces = re.split(r"[^A-Za-z0-9]+", normalize_token_text(tail))
        tokens.extend(piece for piece in pieces if len(piece) >= 4)
    return merge_tokens(tokens)


def choose_target_context(targets: list[dict[str, Any]], *parts: str | None) -> tuple[str | None, str | None]:
    probe_text = " ".join(normalize_token_text(str(part or "")) for part in parts).lower()
    if not probe_text:
        if len(targets) == 1 and isinstance(targets[0], dict):
            return targets[0].get("value_name"), targets[0].get("path")
        return None, None

    for target in targets:
        if not isinstance(target, dict):
            continue
        value_name = target.get("value_name")
        registry_path = target.get("path")
        expected_tokens = tokenize_expected(value_name, registry_path)
        if any(token.lower() in probe_text for token in expected_tokens if len(token) >= 4):
            return value_name, registry_path

    if len(targets) == 1 and isinstance(targets[0], dict):
        return targets[0].get("value_name"), targets[0].get("path")
    return None, None


def should_skip_url_source(path: Path) -> bool:
    try:
        path.relative_to(AUDIT_ROOT)
    except ValueError:
        return False
    return any(path.name.startswith(prefix) for prefix in GENERATED_AUDIT_PREFIXES)


def extract_url_refs_from_json(path: Path) -> list[UrlReference]:
    refs: list[UrlReference] = []
    payload = json.loads(read_text(path))
    tweak_id = str(payload.get("tweak_id") or payload.get("record_id") or "")
    targets = (payload.get("setting") or {}).get("targets") or []
    record_tokens = merge_tokens(
        *[
            tokenize_expected(target.get("value_name"), target.get("path"))
            for target in targets
            if isinstance(target, dict)
        ]
    )

    proof = payload.get("validation_proof") or {}
    source_url = str(proof.get("source_url") or "").strip()
    if source_url.startswith("http"):
        proof_value_name, proof_registry_path = choose_target_context(targets, str(proof.get("exact_quote_or_path") or ""))
        if not proof_value_name and not proof_registry_path:
            proof_value_name, proof_registry_path = choose_target_context(
                targets,
                str(proof.get("notes") or ""),
                source_url,
            )
        proof_tokens = merge_tokens(
            tokenize_expected(proof_value_name, proof_registry_path),
            tokenize_text(str(proof.get("exact_quote_or_path") or "")),
        )
        if not proof_tokens:
            proof_tokens = record_tokens
        refs.append(
            UrlReference(
                source_file=repo_relative(path),
                source_kind="validation_proof",
                tweak_id=tweak_id or None,
                value_name=proof_value_name,
                registry_path=proof_registry_path,
                url=source_url,
                expected_tokens=proof_tokens,
            )
        )

    for item in payload.get("evidence") or []:
        if not isinstance(item, dict):
            continue
        url = str(item.get("location") or item.get("url") or "").strip()
        if not url.startswith("http"):
            continue
        item_value_name, item_registry_path = choose_target_context(
            targets,
            str(item.get("title") or ""),
            str(item.get("summary") or ""),
            url,
        )
        item_tokens = merge_tokens(
            tokenize_expected(item_value_name, item_registry_path),
            tokenize_text(str(item.get("title") or "")),
        )
        if not item_tokens:
            item_tokens = record_tokens
        refs.append(
            UrlReference(
                source_file=repo_relative(path),
                source_kind=str(item.get("kind") or "evidence"),
                tweak_id=tweak_id or None,
                value_name=item_value_name,
                registry_path=item_registry_path,
                url=url,
                expected_tokens=item_tokens,
            )
        )
    return refs


def extract_url_refs_from_text(path: Path) -> list[UrlReference]:
    refs: list[UrlReference] = []
    text = read_text(path)
    for match in URL_RE.finditer(text):
        refs.append(
            UrlReference(
                source_file=repo_relative(path),
                source_kind="text-reference",
                tweak_id=None,
                value_name=None,
                registry_path=None,
                url=match.group(0),
                expected_tokens=[],
            )
        )
    return refs


def scan_ghidra_outputs() -> dict[str, Any]:
    ghidra_files = sorted(GHIDRA_ROOT.rglob("ghidra-matches.md"))
    pdb_missing: list[dict[str, Any]] = []
    bloat: list[dict[str, Any]] = []
    branch_template_missing: list[dict[str, Any]] = []
    required_fields = {
        "function_confidence",
        "register_focus",
        "flag_focus",
        "compare_condition",
        "jump_condition",
        "branch_effect",
        "stack_summary",
        "exception_review_required",
        "heuristic_score",
        "heuristic_reasons",
    }

    for file_path in ghidra_files:
        text = read_text(file_path)
        line_count = text.count("\n") + 1 if text else 0
        evidence_path = file_path.with_name("evidence.json")
        evidence_payload: dict[str, Any] = {}
        if evidence_path.exists():
            try:
                evidence_payload = json.loads(read_text(evidence_path))
            except Exception:
                evidence_payload = {}

        contains_fun = "FUN_" in text
        contains_no_function = "<no function>" in text
        pdb_loaded = bool(evidence_payload.get("pdb_loaded"))
        if contains_fun or contains_no_function or not pdb_loaded:
            pdb_missing.append(
                {
                    "artifact": repo_relative(file_path),
                    "evidence": repo_relative(evidence_path) if evidence_path.exists() else None,
                    "line_count": line_count,
                    "contains_fun": contains_fun,
                    "contains_no_function": contains_no_function,
                    "pdb_loaded": pdb_loaded,
                    "reason": "FUN_ or missing PDB-backed symbol proof",
                }
            )

        if line_count > 200:
            bloat.append(
                {
                    "artifact": repo_relative(file_path),
                    "line_count": line_count,
                    "reason": "Committed Ghidra output exceeds the bounded branch-review format.",
                }
            )

        matches = evidence_payload.get("matches") or []
        if matches:
            first = matches[0] if isinstance(matches[0], dict) else {}
            missing = sorted(field for field in required_fields if field not in first)
            if missing:
                branch_template_missing.append(
                    {
                        "artifact": repo_relative(file_path),
                        "evidence": repo_relative(evidence_path) if evidence_path.exists() else None,
                        "missing_fields": missing,
                        "reason": "Committed evidence does not satisfy the bounded branch template.",
                    }
                )

    return {
        "generated_utc": now_utc(),
        "ghidra_artifact_count": len(ghidra_files),
        "pdb_missing_count": len(pdb_missing),
        "ghidra_bloat_count": len(bloat),
        "branch_template_missing_count": len(branch_template_missing),
        "pdb_missing": pdb_missing,
        "ghidra_bloat": bloat,
        "branch_template_missing": branch_template_missing,
    }


def gather_url_references() -> list[UrlReference]:
    refs: list[UrlReference] = []
    for root in SCAN_TARGETS:
        for path in sorted(root.rglob("*")):
            if not path.is_file():
                continue
            if should_skip_url_source(path):
                continue
            if path.suffix.lower() == ".json":
                try:
                    refs.extend(extract_url_refs_from_json(path))
                except Exception:
                    continue
            elif path.suffix.lower() in {".md", ".txt"}:
                try:
                    refs.extend(extract_url_refs_from_text(path))
                except Exception:
                    continue
    return refs


def fetch_url(url: str, timeout: int = 20) -> dict[str, Any]:
    request = Request(url, headers={"User-Agent": "RegProbe-LinkAudit/1.0"})
    ssl_context = ssl.create_default_context()
    try:
        with urlopen(request, timeout=timeout, context=ssl_context) as response:
            content_type = response.headers.get("Content-Type", "")
            raw = response.read(128000)
            body = raw.decode("utf-8", errors="ignore")
            parser = HtmlSummaryParser()
            if "html" in content_type.lower() or "<html" in body.lower():
                parser.feed(body)
            return {
                "ok": True,
                "status_code": getattr(response, "status", 200),
                "final_url": response.geturl(),
                "content_type": content_type,
                "title": parser.title,
                "h1": parser.h1,
                "snippet": " ".join(parser.text_parts)[:500],
                "error": None,
            }
    except HTTPError as exc:
        return {
            "ok": False,
            "status_code": exc.code,
            "final_url": url,
            "content_type": "",
            "title": "",
            "h1": "",
            "snippet": "",
            "error": str(exc),
        }
    except URLError as exc:
        return {
            "ok": False,
            "status_code": None,
            "final_url": url,
            "content_type": "",
            "title": "",
            "h1": "",
            "snippet": "",
            "error": str(exc.reason),
        }
    except Exception as exc:  # pragma: no cover - safety net
        return {
            "ok": False,
            "status_code": None,
            "final_url": url,
            "content_type": "",
            "title": "",
            "h1": "",
            "snippet": "",
            "error": str(exc),
        }


def is_microsoft_url(url: str) -> bool:
    host = (urlparse(url).hostname or "").lower()
    return host in MICROSOFT_HOSTS


def classify_link(url: str, fetch_result: dict[str, Any], refs: list[UrlReference]) -> tuple[str, str]:
    if not fetch_result["ok"]:
        return "broken_url", "URL did not resolve cleanly."

    title_h1 = " ".join(
        part for part in [fetch_result.get("title") or "", fetch_result.get("h1") or "", fetch_result.get("snippet") or ""] if part
    ).lower()
    expected_tokens: list[str] = []
    for ref in refs:
        expected_tokens.extend(ref.expected_tokens)

    expected_tokens = [token for token in expected_tokens if len(token) >= 4]
    token_hits = [token for token in expected_tokens if token.lower() in title_h1]
    redirected = fetch_result.get("final_url") and fetch_result["final_url"] != url

    if is_microsoft_url(url):
        if expected_tokens and not token_hits:
            return "reachable_but_mismatch", "Microsoft page resolved but title/H1/snippet did not match the claimed key/path."
        if redirected:
            return "redirected_ok", "Microsoft link redirected successfully, but still needs claim review."
        return "reachable_manual_review", "Microsoft page resolved and needs claim-to-page review."

    if redirected:
        return "redirected_ok", "URL redirected successfully."
    return "reachable_manual_review", "URL resolved successfully."


def run_link_audit(refs: list[UrlReference]) -> dict[str, Any]:
    grouped: dict[str, list[UrlReference]] = defaultdict(list)
    for ref in refs:
        grouped[ref.url].append(ref)

    overrides = load_link_review_overrides()
    results: list[dict[str, Any]] = []
    broken: list[dict[str, Any]] = []
    context_review: list[dict[str, Any]] = []
    reviewed_fit: list[dict[str, Any]] = []
    reviewed_mismatch: list[dict[str, Any]] = []

    for url in sorted(grouped):
        fetch_result = fetch_url(url)
        raw_status, raw_reason = classify_link(url, fetch_result, grouped[url])
        status = raw_status
        reason = raw_reason
        review_matches: list[dict[str, Any]] = []
        unresolved_review_refs: list[dict[str, Any]] = []
        mismatch_review_refs: list[dict[str, Any]] = []

        if raw_status in {"reachable_but_mismatch", "reachable_manual_review"}:
            for ref in grouped[url]:
                override = overrides.get((ref.source_file, ref.source_kind, url))
                if not override:
                    unresolved_review_refs.append(
                        {
                            "source_file": ref.source_file,
                            "source_kind": ref.source_kind,
                            "tweak_id": ref.tweak_id,
                        }
                    )
                    continue
                review_entry = {
                    "source_file": ref.source_file,
                    "source_kind": ref.source_kind,
                    "tweak_id": ref.tweak_id,
                    "review_status": override.review_status,
                    "reviewed_utc": override.reviewed_utc,
                    "notes": override.notes,
                }
                review_matches.append(review_entry)
                if override.review_status == "reviewed_context_mismatch":
                    mismatch_review_refs.append(review_entry)

            if mismatch_review_refs:
                status = "reviewed_context_mismatch"
                reason = "Manual review confirmed at least one linked claim does not fit the page."
            elif review_matches and not unresolved_review_refs and len(review_matches) == len(grouped[url]):
                status = "reviewed_context_fit"
                reason = "All linked claims were manually reviewed and confirmed against the page."

        entry = {
            "url": url,
            "status": status,
            "reason": reason,
            "raw_status": raw_status,
            "raw_reason": raw_reason,
            "status_code": fetch_result.get("status_code"),
            "final_url": fetch_result.get("final_url"),
            "title": fetch_result.get("title"),
            "h1": fetch_result.get("h1"),
            "snippet": fetch_result.get("snippet"),
            "error": fetch_result.get("error"),
            "manual_reviews": review_matches,
            "unresolved_review_sources": unresolved_review_refs,
            "sources": [
                {
                    "source_file": ref.source_file,
                    "source_kind": ref.source_kind,
                    "tweak_id": ref.tweak_id,
                    "value_name": ref.value_name,
                    "registry_path": ref.registry_path,
                }
                for ref in grouped[url]
            ],
        }
        results.append(entry)
        if status == "broken_url":
            broken.append(entry)
        elif status in {"reachable_but_mismatch", "reachable_manual_review", "reviewed_context_mismatch"}:
            context_review.append(entry)
            if status == "reviewed_context_mismatch":
                reviewed_mismatch.append(entry)
        elif status == "reviewed_context_fit":
            reviewed_fit.append(entry)

    return {
        "generated_utc": now_utc(),
        "url_reference_count": len(refs),
        "unique_url_count": len(grouped),
        "broken_count": len(broken),
        "context_review_count": len(context_review),
        "reviewed_context_fit_count": len(reviewed_fit),
        "reviewed_context_mismatch_count": len(reviewed_mismatch),
        "results": results,
        "link_broken": broken,
        "link_context_review": context_review,
        "link_reviewed_context_fit": reviewed_fit,
        "link_reviewed_context_mismatch": reviewed_mismatch,
    }


def load_priority_record(tweak_id: str) -> dict[str, Any]:
    candidates = [
        RECORDS_ROOT / f"{tweak_id}.json",
        RECORDS_ROOT / f"{tweak_id}.review.json",
    ]
    for record_path in candidates:
        if not record_path.exists():
            continue
        try:
            return json.loads(read_text(record_path))
        except Exception:
            continue
    return {}


def build_priority_queue(ghidra_scan: dict[str, Any], link_audit: dict[str, Any]) -> dict[str, Any]:
    link_by_tweak: dict[str, list[str]] = defaultdict(list)
    for entry in link_audit.get("results") or []:
        for source in entry.get("sources") or []:
            tweak_id = source.get("tweak_id")
            if tweak_id:
                link_by_tweak[str(tweak_id)].append(str(entry.get("status") or "unknown"))

    priority_entries: list[dict[str, Any]] = []
    for tweak_id, details in PRIORITY_RECORDS.items():
        record_payload = load_priority_record(tweak_id)
        link_statuses = sorted(set(link_by_tweak.get(tweak_id, [])))
        static_ghidra = ((record_payload.get("static_analysis") or {}).get("ghidra") or {}) if record_payload else {}
        decision = record_payload.get("decision") or {}
        has_bad_link = any(status in {"broken_url", "reachable_but_mismatch"} for status in link_statuses)
        doc_source_present = bool(record_payload.get("doc_source"))
        needs_doc_reaudit = has_bad_link or not doc_source_present
        needs_pdb_reaudit = tweak_id == "system.priority-control" and static_ghidra.get("status") in {
            "blocked-pdb-required",
            "blocked-pdb-missing",
        }
        resolution_status = "resolved"
        if needs_doc_reaudit and needs_pdb_reaudit:
            resolution_status = "open"
        elif needs_doc_reaudit or needs_pdb_reaudit:
            resolution_status = "partial"
        remaining_actions: list[str] = []
        if needs_doc_reaudit:
            remaining_actions.append("doc_reaudit")
        if needs_pdb_reaudit:
            remaining_actions.append("pdb_static_reaudit")
        if decision.get("manual_review_required"):
            remaining_actions.append("manual_record_review")
        priority_entries.append(
            {
                "tweak_id": tweak_id,
                "priority": details["priority"],
                "reason": details["reason"],
                "queued_from": "manual-override",
                "link_statuses": link_statuses,
                "needs_pdb_reaudit": needs_pdb_reaudit,
                "needs_doc_reaudit": needs_doc_reaudit,
                "resolution_status": resolution_status,
                "remaining_actions": remaining_actions,
            }
        )
    return {
        "generated_utc": now_utc(),
        "entries": priority_entries,
    }


def render_markdown_summary(
    ghidra_scan: dict[str, Any],
    link_audit: dict[str, Any],
    priority_queue: dict[str, Any],
) -> str:
    lines = [
        "# Static Evidence v3.2 Audit",
        "",
        f"- Generated: {now_utc()}",
        f"- Ghidra artifacts scanned: {ghidra_scan['ghidra_artifact_count']}",
        f"- PDB-missing artifacts: {ghidra_scan['pdb_missing_count']}",
        f"- Ghidra bloat artifacts: {ghidra_scan['ghidra_bloat_count']}",
        f"- Branch-template-missing artifacts: {ghidra_scan['branch_template_missing_count']}",
        f"- URL references: {link_audit['url_reference_count']}",
        f"- Unique URLs: {link_audit['unique_url_count']}",
        f"- Broken URLs: {link_audit['broken_count']}",
        f"- Context review URLs: {link_audit['context_review_count']}",
        f"- Reviewed context-fit URLs: {link_audit['reviewed_context_fit_count']}",
        "",
        "## Priority queue",
        "",
        "| Tweak | Priority | Status | Reason | Link statuses |",
        "| --- | --- | --- | --- | --- |",
    ]
    for entry in priority_queue["entries"]:
        statuses = ", ".join(entry["link_statuses"]) if entry["link_statuses"] else "none"
        lines.append(
            f"| {entry['tweak_id']} | {entry['priority']} | {entry['resolution_status']} | {entry['reason']} | {statuses} |"
        )

    lines.extend(
        [
            "",
            "## PDB-missing sample",
            "",
        ]
    )
    for entry in ghidra_scan["pdb_missing"][:10]:
        lines.append(f"- `{entry['artifact']}`")

    lines.extend(
        [
            "",
            "## Link issues sample",
            "",
        ]
    )
    for entry in (link_audit["link_broken"] + link_audit["link_context_review"])[:10]:
        lines.append(f"- `{entry['status']}` {entry['url']}")
    return "\n".join(lines)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Audit RegProbe static evidence and links for v3.2 re-audit.")
    parser.add_argument("--stamp", default=datetime.now().strftime("%Y%m%d"))
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    ghidra_scan = scan_ghidra_outputs()
    refs = gather_url_references()
    link_audit = run_link_audit(refs)
    priority_queue = build_priority_queue(ghidra_scan, link_audit)

    summary = {
        "generated_utc": now_utc(),
        "ghidra_artifact_count": ghidra_scan["ghidra_artifact_count"],
        "pdb_missing_count": ghidra_scan["pdb_missing_count"],
        "ghidra_bloat_count": ghidra_scan["ghidra_bloat_count"],
        "branch_template_missing_count": ghidra_scan["branch_template_missing_count"],
        "url_reference_count": link_audit["url_reference_count"],
        "unique_url_count": link_audit["unique_url_count"],
        "broken_url_count": link_audit["broken_count"],
        "context_review_count": link_audit["context_review_count"],
        "reviewed_context_fit_count": link_audit["reviewed_context_fit_count"],
        "reviewed_context_mismatch_count": link_audit["reviewed_context_mismatch_count"],
        "priority_count": len(priority_queue["entries"]),
    }

    write_json(AUDIT_ROOT / f"static-evidence-v32-scan-{args.stamp}.json", summary)
    write_json(AUDIT_ROOT / f"static-evidence-v32-pdb-missing-{args.stamp}.json", {"entries": ghidra_scan["pdb_missing"]})
    write_json(AUDIT_ROOT / f"static-evidence-v32-ghidra-bloat-{args.stamp}.json", {"entries": ghidra_scan["ghidra_bloat"]})
    write_json(AUDIT_ROOT / f"static-evidence-v32-branch-template-missing-{args.stamp}.json", {"entries": ghidra_scan["branch_template_missing"]})
    write_json(AUDIT_ROOT / f"static-evidence-v32-link-audit-{args.stamp}.json", link_audit)
    write_json(AUDIT_ROOT / f"static-evidence-v32-link-broken-{args.stamp}.json", {"entries": link_audit["link_broken"]})
    write_json(AUDIT_ROOT / f"static-evidence-v32-link-context-review-{args.stamp}.json", {"entries": link_audit["link_context_review"]})
    write_json(AUDIT_ROOT / f"static-evidence-v32-link-reviewed-context-fit-{args.stamp}.json", {"entries": link_audit["link_reviewed_context_fit"]})
    write_json(AUDIT_ROOT / f"static-evidence-v32-link-reviewed-context-mismatch-{args.stamp}.json", {"entries": link_audit["link_reviewed_context_mismatch"]})
    write_json(AUDIT_ROOT / f"nohuto-priority-queue-{args.stamp}.json", priority_queue)
    write_text(AUDIT_ROOT / f"static-evidence-v32-summary-{args.stamp}.md", render_markdown_summary(ghidra_scan, link_audit, priority_queue))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
