#!/usr/bin/env python3
from __future__ import annotations

import csv
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

REPO_ROOT = Path(__file__).resolve().parents[1]
DOCS_ROOT = REPO_ROOT / "Docs"
PROVIDER_ROOT = REPO_ROOT / "OpenTraceProject.App" / "Services" / "TweakProviders"
ENGINE_ROOT = REPO_ROOT / "OpenTraceProject.Engine" / "Tweaks"
OUTPUT_CSV = DOCS_ROOT / "tweaks" / "tweak-source-audit.csv"
OUTPUT_MD = DOCS_ROOT / "tweaks" / "tweak-source-audit.md"

DOC_EXTS = {".md", ".txt", ".c", ".ps1", ".py", ".json"}
EXCLUDED_DOC_PATTERNS = {
    "tweak-catalog",
    "tweak-details",
    "tweak-docs-",
}

DOC_FOLDER_MAP = {
    "privacy": "privacy",
    "security": "security",
    "network": "network",
    "power": "power",
    "system": "system",
    "visibility": "visibility",
    "peripheral": "peripheral",
    "audio": "peripheral",
    "misc": "misc",
    "cleanup": "cleanup",
    "explorer": "visibility",
    "notifications": "notifications",
    "performance": "performance",
}

EXTRA_DOCS_BY_PREFIX = {
    "system": [
        DOCS_ROOT / "tweaks" / "win-config" / "batch-01.md",
    ],
    "power": [
        DOCS_ROOT / "tweaks" / "win-config" / "batch-01.md",
    ],
    "visibility": [
        DOCS_ROOT / "visibility" / "use-case-guide.md",
    ],
    "security": [
        DOCS_ROOT / "security" / "use-case-guide.md",
    ],
}

CREATE_CALL_RE = re.compile(r"yield\s+return\s+(?P<call>Create[A-Za-z0-9_]+)\s*\(")
NEW_CALL_RE = re.compile(r"return\s+new\s+(?P<call>[A-Za-z0-9_]*Tweak)\s*\(")
BASE_CALL_RE = re.compile(r":\s*base\s*\(")

STRING_RE = re.compile(r'@?"([^"]*)"')


@dataclass
class TweakAudit:
    tweak_id: str
    name: str
    call: str
    source: str
    tokens: list[str]
    documented: bool
    missing_tokens: list[str]


def collect_docs_text(paths: Iterable[Path]) -> str:
    chunks: list[str] = []
    for path in paths:
        if not path.exists():
            continue
        if path.is_dir():
            for nested in path.rglob("*"):
                if not nested.is_file():
                    continue
                if nested.suffix.lower() not in DOC_EXTS:
                    continue
                normalized = str(nested.relative_to(DOCS_ROOT)).replace("\\", "/")
                if normalized.startswith("tweaks/") and any(pattern in normalized for pattern in EXCLUDED_DOC_PATTERNS):
                    continue
                try:
                    text = nested.read_text(encoding="utf-8", errors="ignore")
                except Exception:
                    continue
                chunks.append(text.lower())
        else:
            if path.suffix.lower() not in DOC_EXTS:
                continue
            normalized = str(path.relative_to(DOCS_ROOT)).replace("\\", "/")
            if normalized.startswith("tweaks/") and any(pattern in normalized for pattern in EXCLUDED_DOC_PATTERNS):
                continue
            try:
                text = path.read_text(encoding="utf-8", errors="ignore")
            except Exception:
                continue
            chunks.append(text.lower())
    return "\n".join(chunks)


def docs_text_for_prefix(prefix: str) -> str:
    folder_name = DOC_FOLDER_MAP.get(prefix, "tweaks")
    base_path = DOCS_ROOT / folder_name
    extra = EXTRA_DOCS_BY_PREFIX.get(prefix, [])
    return collect_docs_text([base_path, *extra])


def token_in_docs(token: str, docs_text: str) -> bool:
    if not token:
        return False
    normalized = token.lower()
    escaped = normalized.replace("\\", "\\\\")
    return normalized in docs_text or escaped in docs_text


def find_matching_paren(text: str, start: int) -> int:
    depth = 0
    in_string = False
    verbatim = False
    escape = False
    i = start
    while i < len(text):
        ch = text[i]
        if in_string:
            if verbatim:
                if ch == '"' and i + 1 < len(text) and text[i + 1] == '"':
                    i += 1
                elif ch == '"':
                    in_string = False
                    verbatim = False
            else:
                if escape:
                    escape = False
                elif ch == '\\':
                    escape = True
                elif ch == '"':
                    in_string = False
        else:
            if ch == '"':
                in_string = True
                verbatim = i > 0 and text[i - 1] == '@'
            elif ch in "([{":
                depth += 1
            elif ch in ")]}":
                depth -= 1
                if depth == 0:
                    return i
        i += 1
    return -1


def split_args(text: str) -> list[str]:
    args: list[str] = []
    current: list[str] = []
    depth = 0
    in_string = False
    verbatim = False
    escape = False
    i = 0
    while i < len(text):
        ch = text[i]
        if in_string:
            current.append(ch)
            if verbatim:
                if ch == '"' and i + 1 < len(text) and text[i + 1] == '"':
                    current.append(text[i + 1])
                    i += 1
                elif ch == '"':
                    in_string = False
                    verbatim = False
            else:
                if escape:
                    escape = False
                elif ch == '\\':
                    escape = True
                elif ch == '"':
                    in_string = False
        else:
            if ch == '"':
                in_string = True
                verbatim = i > 0 and text[i - 1] == '@'
                current.append(ch)
            elif ch in "([{":
                depth += 1
                current.append(ch)
            elif ch in ")]}":
                depth -= 1
                current.append(ch)
            elif ch == ',' and depth == 0:
                args.append("".join(current).strip())
                current = []
            else:
                current.append(ch)
        i += 1
    if current:
        args.append("".join(current).strip())
    return args


def extract_first_string(arg: str) -> str:
    match = STRING_RE.search(arg or "")
    return match.group(1) if match else ""


def extract_strings(arg: str) -> list[str]:
    return [match.group(1) for match in STRING_RE.finditer(arg or "")]


def extract_value_names_from_entries(arg: str, entry_type: str) -> list[str]:
    names: list[str] = []
    pattern = re.compile(rf"{entry_type}\\s*\\(.*?\\)", re.DOTALL)
    for match in pattern.finditer(arg):
        chunk = match.group(0)
        args = split_args(chunk[chunk.find('(') + 1 : chunk.rfind(')')])
        # RegistryValueSetEntry(name, kind, value)
        if entry_type == "RegistryValueSetEntry" and len(args) >= 1:
            name = extract_first_string(args[0])
            if name:
                names.append(name)
        # RegistryValueBatchEntry(hive, path, valueName, kind, value)
        if entry_type == "RegistryValueBatchEntry" and len(args) >= 3:
            name = extract_first_string(args[2])
            if name:
                names.append(name)
    return names


def extract_tokens(call: str, args: list[str], name: str, description: str) -> list[str]:
    tokens: list[str] = []
    if call == "CreateRegistryTweak" and len(args) >= 8:
        key_path = extract_first_string(args[6])
        value_name = extract_first_string(args[7])
        tokens.extend([key_path, value_name])
    elif call == "CreateRegistryValueSetTweak" and len(args) >= 8:
        key_path = extract_first_string(args[6])
        tokens.append(key_path)
        tokens.extend(extract_value_names_from_entries(args[7], "RegistryValueSetEntry"))
    elif call == "CreateRegistryValueBatchTweak" and len(args) >= 6:
        # entries are arg[5]
        tokens.extend(extract_value_names_from_entries(args[5], "RegistryValueBatchEntry"))
        tokens.extend(extract_strings(args[5]))
    elif call == "CreateServiceStartModeBatchTweak" and len(args) >= 6:
        tokens.extend(extract_strings(args[5]))
    elif call == "CreateScheduledTaskBatchTweak" and len(args) >= 6:
        tokens.extend(extract_strings(args[5]))
    elif call == "CreateFileRenameTweak" and len(args) >= 7:
        tokens.extend(extract_strings(args[5]))
        tokens.extend(extract_strings(args[6]))
    else:
        # Fallback: use name/description strings to find docs
        if len(args) >= 3:
            tokens.extend([extract_first_string(args[1]), extract_first_string(args[2])])

    tokens = [token for token in tokens if token]
    if not tokens:
        tokens = [token for token in [name, description] if token]
    return tokens


def extract_metadata(chunk: str) -> tuple[str, str, str]:
    strings = extract_strings(chunk)
    tweak_id = strings[0] if len(strings) >= 1 else ""
    name = strings[1] if len(strings) >= 2 else ""
    description = strings[2] if len(strings) >= 3 else ""
    return tweak_id, name, description


def extract_entries_from_file(path: Path, pattern: re.Pattern) -> Iterable[tuple[str, str, str, list[str]]]:
    text = path.read_text(encoding="utf-8")
    for match in pattern.finditer(text):
        call_name = match.group("call")
        open_paren = text.find("(", match.end() - 1)
        if open_paren == -1:
            continue
        close_paren = find_matching_paren(text, open_paren)
        if close_paren == -1:
            continue
        chunk = text[open_paren + 1 : close_paren]
        tweak_id, name, description = extract_metadata(chunk)
        if not tweak_id:
            continue
        args = split_args(chunk)
        tokens = extract_tokens(call_name, args, name, description)
        line = text.count("\n", 0, match.start()) + 1
        source = f"{path.relative_to(REPO_ROOT)}#L{line}"
        yield tweak_id, name, call_name, tokens, source


def extract_entries_from_base_call(path: Path) -> Iterable[tuple[str, str, str, list[str]]]:
    text = path.read_text(encoding="utf-8")
    for match in BASE_CALL_RE.finditer(text):
        open_paren = text.find("(", match.end() - 1)
        if open_paren == -1:
            continue
        close_paren = find_matching_paren(text, open_paren)
        if close_paren == -1:
            continue
        chunk = text[open_paren + 1 : close_paren]
        tweak_id, name, description = extract_metadata(chunk)
        if not tweak_id:
            continue
        args = split_args(chunk)
        tokens = extract_tokens("base", args, name, description)
        line = text.count("\n", 0, match.start()) + 1
        source = f"{path.relative_to(REPO_ROOT)}#L{line}"
        yield tweak_id, name, "base", tokens, source


def main() -> int:
    docs_cache: dict[str, str] = {}

    audits: list[TweakAudit] = []
    seen: set[str] = set()

    provider_paths = sorted(PROVIDER_ROOT.glob("*.cs"))
    legacy_path = PROVIDER_ROOT / "LegacyTweakProvider.cs"

    for path in provider_paths:
        if path == legacy_path:
            continue
        for tweak_id, name, call, tokens, source in extract_entries_from_file(path, CREATE_CALL_RE):
            key = tweak_id.lower()
            if key in seen:
                continue
            seen.add(key)
            prefix = tweak_id.split(".", 1)[0].lower() if "." in tweak_id else "tweaks"
            if prefix not in docs_cache:
                docs_cache[prefix] = docs_text_for_prefix(prefix)
            docs_text = docs_cache[prefix]
            found = [token for token in tokens if token_in_docs(token, docs_text)]
            missing = [token for token in tokens if not token_in_docs(token, docs_text)]
            documented = len(tokens) > 0 and len(found) > 0
            audits.append(TweakAudit(tweak_id, name, call, source, tokens, documented, missing))

    for path in sorted(ENGINE_ROOT.rglob("*.cs")):
        for tweak_id, name, call, tokens, source in extract_entries_from_file(path, NEW_CALL_RE):
            key = tweak_id.lower()
            if key in seen:
                continue
            seen.add(key)
            prefix = tweak_id.split(".", 1)[0].lower() if "." in tweak_id else "tweaks"
            if prefix not in docs_cache:
                docs_cache[prefix] = docs_text_for_prefix(prefix)
            docs_text = docs_cache[prefix]
            found = [token for token in tokens if token_in_docs(token, docs_text)]
            missing = [token for token in tokens if not token_in_docs(token, docs_text)]
            documented = len(tokens) > 0 and len(found) > 0
            audits.append(TweakAudit(tweak_id, name, call, source, tokens, documented, missing))
        for tweak_id, name, call, tokens, source in extract_entries_from_base_call(path):
            key = tweak_id.lower()
            if key in seen:
                continue
            seen.add(key)
            prefix = tweak_id.split(".", 1)[0].lower() if "." in tweak_id else "tweaks"
            if prefix not in docs_cache:
                docs_cache[prefix] = docs_text_for_prefix(prefix)
            docs_text = docs_cache[prefix]
            found = [token for token in tokens if token_in_docs(token, docs_text)]
            missing = [token for token in tokens if not token_in_docs(token, docs_text)]
            documented = len(tokens) > 0 and len(found) > 0
            audits.append(TweakAudit(tweak_id, name, call, source, tokens, documented, missing))

    if legacy_path.exists():
        for tweak_id, name, call, tokens, source in extract_entries_from_file(legacy_path, CREATE_CALL_RE):
            key = tweak_id.lower()
            if key in seen:
                continue
            seen.add(key)
            prefix = tweak_id.split(".", 1)[0].lower() if "." in tweak_id else "tweaks"
            if prefix not in docs_cache:
                docs_cache[prefix] = docs_text_for_prefix(prefix)
            docs_text = docs_cache[prefix]
            found = [token for token in tokens if token_in_docs(token, docs_text)]
            missing = [token for token in tokens if not token_in_docs(token, docs_text)]
            documented = len(tokens) > 0 and len(found) > 0
            audits.append(TweakAudit(tweak_id, name, call, source, tokens, documented, missing))

    audits.sort(key=lambda a: a.tweak_id)
    OUTPUT_CSV.parent.mkdir(parents=True, exist_ok=True)

    with OUTPUT_CSV.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(["id", "name", "call", "documented", "missing_tokens", "source", "tokens"])
        for audit in audits:
            writer.writerow([
                audit.tweak_id,
                audit.name,
                audit.call,
                "yes" if audit.documented else "no",
                ";".join(audit.missing_tokens),
                audit.source,
                ";".join(audit.tokens),
            ])

    missing = [audit for audit in audits if not audit.documented]
    with OUTPUT_MD.open("w", encoding="utf-8") as handle:
        handle.write("# Tweak Source Audit (Generated)\n\n")
        handle.write(f"Total tweaks: {len(audits)}\n\n")
        handle.write(f"Missing documentation: {len(missing)}\n\n")
        handle.write("| ID | Name | Call | Missing Tokens | Source |\n")
        handle.write("| --- | --- | --- | --- | --- |\n")
        for audit in missing:
            missing_tokens = ", ".join(audit.missing_tokens) if audit.missing_tokens else "(none)"
            handle.write(f"| `{audit.tweak_id}` | {audit.name} | {audit.call} | {missing_tokens} | `{audit.source}` |\n")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
