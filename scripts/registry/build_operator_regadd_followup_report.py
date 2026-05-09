#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
import subprocess
import xml.etree.ElementTree as ET
from datetime import UTC, datetime
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]


def now_utc() -> str:
    return datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def normalize_registry_path(value: str) -> str:
    cleaned = value.replace("/", "\\").strip("\\")
    cleaned = re.sub(r"^HKEY_LOCAL_MACHINE\\", lambda _: "HKLM\\", cleaned, flags=re.IGNORECASE)
    cleaned = re.sub(r"^HKLM\\", lambda _: "HKLM\\", cleaned, flags=re.IGNORECASE)
    cleaned = re.sub(r"^HKEY_CURRENT_USER\\", lambda _: "HKCU\\", cleaned, flags=re.IGNORECASE)
    cleaned = re.sub(r"^HKCU\\", lambda _: "HKCU\\", cleaned, flags=re.IGNORECASE)
    return cleaned.lower()


def target_key(path: str, value_name: str) -> tuple[str, str]:
    return normalize_registry_path(path), value_name.lower()


def load_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def load_records(path: Path) -> list[dict[str, Any]]:
    payload = load_json(path)
    records = payload.get("records") if isinstance(payload, dict) else payload
    if not isinstance(records, list):
        raise ValueError(f"Expected records[] in {path}")
    return [record for record in records if isinstance(record, dict)]


def load_inventory_entries(path: Path) -> list[dict[str, Any]]:
    payload = load_json(path)
    entries = payload.get("entries") if isinstance(payload, dict) else payload
    if not isinstance(entries, list):
        raise ValueError(f"Expected entries[] in {path}")
    return [entry for entry in entries if isinstance(entry, dict)]


def local_rg(pattern: str, roots: list[str], *, max_lines: int = 40) -> list[str]:
    cmd = ["rg", "-n", "--fixed-strings", pattern, *roots]
    completed = subprocess.run(cmd, cwd=str(REPO_ROOT), capture_output=True, text=True, check=False)
    lines = [line for line in completed.stdout.splitlines() if line.strip()]
    return lines[:max_lines]


def xml_local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def decimal_child_value(element: ET.Element, child_name: str) -> str | None:
    for child in element:
        if xml_local_name(child.tag) != child_name:
            continue
        for grandchild in child:
            if xml_local_name(grandchild.tag) == "decimal":
                return grandchild.attrib.get("value")
    return None


def parse_admx_policy_map(paths: list[Path]) -> dict[tuple[str, str], list[dict[str, Any]]]:
    result: dict[tuple[str, str], list[dict[str, Any]]] = {}
    for path in paths:
        if not path.exists():
            continue
        try:
            root = ET.parse(path).getroot()
        except ET.ParseError:
            continue
        for policy in root.iter():
            if xml_local_name(policy.tag) != "policy":
                continue
            key = policy.attrib.get("key")
            value_name = policy.attrib.get("valueName")
            if not key or not value_name:
                continue
            normalized_path = key
            if not re.match(r"^(HKLM|HKCU|HKEY_)", normalized_path, flags=re.IGNORECASE):
                normalized_path = "HKLM\\" + normalized_path
            try:
                source_file = str(path.relative_to(REPO_ROOT))
            except ValueError:
                source_file = str(path)
            entry = {
                "policy_name": policy.attrib.get("name"),
                "class": policy.attrib.get("class"),
                "source_file": source_file,
                "registry_path": normalized_path,
                "value_name": value_name,
                "enabled_value": decimal_child_value(policy, "enabledValue"),
                "disabled_value": decimal_child_value(policy, "disabledValue"),
                "display_name_ref": policy.attrib.get("displayName"),
                "explain_text_ref": policy.attrib.get("explainText"),
            }
            result.setdefault(target_key(normalized_path, value_name), []).append(entry)
    return result


def source_hit_summary(pattern: str) -> dict[str, Any]:
    roots = [
        "research",
        "Docs",
        "evidence/captures",
        "evidence/raw/etw-stackwalk",
        "evidence/raw/ghidra",
        "evidence/static",
        "evidence/files/external",
        "registry-research-framework/audit",
    ]
    hits = local_rg(pattern, roots, max_lines=80)
    return {
        "pattern": pattern,
        "hit_count_sampled": len(hits),
        "sample": hits,
    }


def classify_key_missing(
    record: dict[str, Any],
    inventory_entry: dict[str, Any] | None,
    admx_map: dict[tuple[str, str], list[dict[str, Any]]],
    wave_summary: dict[str, Any],
) -> dict[str, Any]:
    path = str(record.get("path") or "")
    value_name = str(record.get("value_name") or "")
    key = target_key(path, value_name)
    local_sources = source_hit_summary(value_name)
    admx_hits = admx_map.get(key, [])
    mutation = None
    for item in wave_summary.get("records", []):
        if isinstance(item, dict) and str(item.get("target", "")).lower().endswith(f"{path}\\{value_name}".lower()):
            mutation = item
            break

    external: list[dict[str, Any]] = []
    verdict = "no-authoritative-evidence-for-25h2"
    confidence = "medium"
    default_interpretation = "absent-on-clean-25h2"
    recommendation = "do-not-promote-without-consumer-proof"

    if value_name.lower() == "powerthrottlingoff":
        external.append(
            {
                "source": "Microsoft Learn ADMX_Power Policy CSP",
                "url": "https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-power#powerthrottlingturnoff",
                "summary": "Documents PowerThrottlingTurnOff as ADMX-backed and maps it to System\\CurrentControlSet\\Control\\Power\\PowerThrottling\\PowerThrottlingOff for Windows 11.",
            }
        )
        verdict = "source-backed-policy-default-absent"
        confidence = "high"
        default_interpretation = "not-configured/unset; ADMX enabled=1 disabled=0"
        recommendation = "keep-as-policy-backed; distinguish clean-default-absent from configured state"
    elif value_name.lower() == "policy" and "forcehibernatedisabled" in path.lower():
        external.extend(
            [
                {
                    "source": "ReactOS/web search",
                    "url": "https://reactos.org/archives/public/ros-diffs/2020-March/072845.html",
                    "summary": "ReactOS hibernation references found around power capability checks, but no exact ForceHibernateDisabled\\Policy registry path evidence was found in the retained local corpus.",
                },
                {
                    "source": "Microsoft Learn search",
                    "url": "https://learn.microsoft.com/en-us/search/?terms=ForceHibernateDisabled",
                    "summary": "No official Microsoft Learn exact-path evidence was found during this pass.",
                },
            ]
        )

    return {
        "index": record.get("index"),
        "registry_path": path,
        "value_name": value_name,
        "requested_data": record.get("requested_data"),
        "vm_25h2_status": {
            "key_exists": record.get("key_exists"),
            "value_exists": record.get("value_exists"),
            "status": record.get("status"),
            "baseline": "registry-research-framework/audit/operator-regadd-vm-baseline-20260509T081911Z.json",
        },
        "repo_inventory": {
            "repo_exact_target_match_count": record.get("repo_exact_target_match_count"),
            "repo_text_hit_count": record.get("repo_text_hit_count"),
            "app_surface_text_hit": record.get("app_surface_text_hit"),
            "repo_exact_target_matches": (inventory_entry or {}).get("repo_exact_target_matches", []),
            "source_hit_files_sample": (inventory_entry or {}).get("source_hit_files_sample", []),
        },
        "local_admx_hits": admx_hits,
        "local_source_hits": local_sources,
        "external_sources_checked": external,
        "mutation_smoke": mutation,
        "verdict": verdict,
        "confidence": confidence,
        "default_interpretation": default_interpretation,
        "recommendation": recommendation,
    }


def recommended_values(record: dict[str, Any]) -> list[int]:
    requested_raw = str(record.get("requested_data") or "").strip()
    values: list[int] = []
    try:
        requested = int(requested_raw, 0)
        values.append(requested)
    except ValueError:
        requested = 0
    for candidate in (0, 1):
        if candidate not in values:
            values.append(candidate)
    if requested not in {0, 1} and requested not in values:
        values.insert(0, requested)
    return values[:4]


def build_default_rows(
    records: list[dict[str, Any]],
    inventory: dict[tuple[str, str], dict[str, Any]],
    admx_map: dict[tuple[str, str], list[dict[str, Any]]],
) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    for record in records:
        path = str(record.get("path") or "")
        value_name = str(record.get("value_name") or "")
        key = target_key(path, value_name)
        status = str(record.get("status") or "")
        if status not in {"value-present", "value-missing"}:
            continue
        inv = inventory.get(key, {})
        default_kind = "observed-present" if record.get("value_exists") else "observed-absent"
        default_value = record.get("value") if record.get("value_exists") else None
        source_quality = "vm-observed"
        default_notes = (
            "Clean Windows 11 25H2 VM has this value present."
            if record.get("value_exists")
            else "Clean Windows 11 25H2 VM has the containing key but the value is absent; absent is the observed default state."
        )
        if admx_map.get(key):
            source_quality = "vm-observed-plus-admx"
            default_notes += " Local ADMX policy mapping also exists; disabled/not configured semantics must be kept separate from explicit writes."
        rows.append(
            {
                "index": record.get("index"),
                "registry_path": path,
                "value_name": value_name,
                "requested_data": record.get("requested_data"),
                "vm_status": status,
                "default_kind": default_kind,
                "default_value": default_value,
                "default_source": "operator-regadd-vm-baseline-20260509T081911Z",
                "source_quality": source_quality,
                "default_notes": default_notes,
                "repo_exact_target_match_count": record.get("repo_exact_target_match_count"),
                "repo_text_hit_count": record.get("repo_text_hit_count"),
                "repo_exact_target_matches": inv.get("repo_exact_target_matches", []),
                "admx_hits": admx_map.get(key, []),
                "risk_flags": record.get("risk_flags", []),
                "recommended_test_values": recommended_values(record),
            }
        )
    return rows


def md_cell(value: object) -> str:
    if value is None:
        text = ""
    elif isinstance(value, (dict, list)):
        text = json.dumps(value, ensure_ascii=False, sort_keys=True)
    else:
        text = str(value)
    return text.replace("|", "\\|").replace("\n", " ")


def write_markdown(payload: dict[str, Any], path: Path) -> None:
    lines = [
        "# Operator Reg Add Follow-up Report",
        "",
        f"- Generated UTC: `{payload['generated_utc']}`",
        f"- Baseline: `{payload['inputs']['baseline']}`",
        f"- Key-missing records: `{len(payload['key_missing_audit'])}`",
        f"- Default rows: `{len(payload['default_value_matrix'])}`",
        "",
        "## Key-Missing Audit",
        "",
        "| Target | VM 25H2 | Verdict | Default interpretation | Recommendation |",
        "|---|---|---|---|---|",
    ]
    for item in payload["key_missing_audit"]:
        target = f"{item['registry_path']}\\{item['value_name']}"
        vm_status = item["vm_25h2_status"]["status"]
        lines.append(
            "| "
            + " | ".join(
                [
                    f"`{md_cell(target)}`",
                    f"`{md_cell(vm_status)}`",
                    f"`{md_cell(item['verdict'])}`",
                    md_cell(item["default_interpretation"]),
                    md_cell(item["recommendation"]),
                ]
            )
            + " |"
        )
    lines.extend(["", "## Default Value Matrix", ""])
    lines.extend(
        [
            "| # | Target | VM status | Default | Source quality | Test values |",
            "|---:|---|---|---|---|---|",
        ]
    )
    for row in payload["default_value_matrix"]:
        target = f"{row['registry_path']}\\{row['value_name']}"
        default = "absent" if row["default_kind"] == "observed-absent" else row["default_value"]
        lines.append(
            "| "
            + " | ".join(
                [
                    md_cell(row["index"]),
                    f"`{md_cell(target)}`",
                    f"`{md_cell(row['vm_status'])}`",
                    f"`{md_cell(default)}`",
                    f"`{md_cell(row['source_quality'])}`",
                    f"`{md_cell(row['recommended_test_values'])}`",
                ]
            )
            + " |"
        )
    lines.extend(
        [
            "",
            "## External References",
            "",
            "- Microsoft Learn ADMX_Power Policy CSP: https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-power#powerthrottlingturnoff",
            "- ReactOS hibernation capability diff reviewed as a non-exact source: https://reactos.org/archives/public/ros-diffs/2020-March/072845.html",
        ]
    )
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Build follow-up source/default report for the operator reg-add VM campaign.")
    parser.add_argument(
        "--baseline",
        default=str(REPO_ROOT / "registry-research-framework" / "audit" / "operator-regadd-vm-baseline-20260509T081911Z.json"),
    )
    parser.add_argument(
        "--inventory",
        default=str(REPO_ROOT / "registry-research-framework" / "audit" / "operator-regadd-inventory-20260508-repo.json"),
    )
    parser.add_argument(
        "--wave-summary",
        default=str(REPO_ROOT / "registry-research-framework" / "audit" / "operator-regadd-vm-wave-20260509.json"),
    )
    parser.add_argument(
        "--output",
        default=str(REPO_ROOT / "registry-research-framework" / "audit" / "operator-regadd-followup-source-default-report-20260509.json"),
    )
    parser.add_argument("--markdown-output", default="")
    args = parser.parse_args()

    baseline_path = Path(args.baseline).resolve()
    inventory_path = Path(args.inventory).resolve()
    wave_path = Path(args.wave_summary).resolve()
    output_path = Path(args.output).resolve()
    markdown_path = Path(args.markdown_output).resolve() if args.markdown_output else output_path.with_suffix(".md")

    records = load_records(baseline_path)
    inventory_entries = load_inventory_entries(inventory_path)
    inventory_by_key = {
        target_key(str(entry.get("path") or ""), str(entry.get("value_name") or "")): entry
        for entry in inventory_entries
    }
    admx_map = parse_admx_policy_map(list((REPO_ROOT / "evidence" / "files" / "external").glob("**/*.admx")))
    wave_summary = load_json(wave_path)

    key_missing = [record for record in records if record.get("status") == "key-missing"]
    key_missing_audit = [
        classify_key_missing(record, inventory_by_key.get(target_key(str(record.get("path") or ""), str(record.get("value_name") or ""))), admx_map, wave_summary)
        for record in key_missing
    ]
    default_rows = build_default_rows(records, inventory_by_key, admx_map)

    payload = {
        "generated_utc": now_utc(),
        "status": "ok",
        "inputs": {
            "baseline": str(baseline_path.relative_to(REPO_ROOT)),
            "inventory": str(inventory_path.relative_to(REPO_ROOT)),
            "wave_summary": str(wave_path.relative_to(REPO_ROOT)),
        },
        "key_missing_audit": key_missing_audit,
        "default_value_matrix": default_rows,
        "counts": {
            "key_missing": len(key_missing_audit),
            "observed_present_defaults": sum(1 for row in default_rows if row["default_kind"] == "observed-present"),
            "observed_absent_defaults": sum(1 for row in default_rows if row["default_kind"] == "observed-absent"),
            "admx_mapped_defaults": sum(1 for row in default_rows if row["admx_hits"]),
        },
        "next_steps": [
            "Harden Procmon bootlog runner for QGA-first launch before using it as a high-volume discovery lane.",
            "Install or stage Windows Performance Toolkit/xperf in the clean 25H2 VM before ETW stackwalk discovery waves.",
            "Run per-value mutation waves with user-session Store/Settings smoke and CPU/IO benchmark deltas.",
        ],
    }
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    write_markdown(payload, markdown_path)
    print(
        json.dumps(
            {
                "status": payload["status"],
                "json": str(output_path),
                "markdown": str(markdown_path),
                "counts": payload["counts"],
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
