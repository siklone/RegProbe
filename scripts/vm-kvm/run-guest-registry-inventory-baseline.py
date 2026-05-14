#!/usr/bin/env python3
from __future__ import annotations

import argparse
import base64
import json
import subprocess
import sys
from datetime import UTC, datetime
from pathlib import Path
from typing import Any

from vm_env import vm_connect, vm_domain


REPO_ROOT = Path(__file__).resolve().parents[2]


def now_utc() -> str:
    return datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def load_inventory(path: Path, *, limit: int = 0, include_parse_errors: bool = False) -> list[dict[str, Any]]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    entries = payload.get("entries") if isinstance(payload, dict) else payload
    if not isinstance(entries, list):
        raise ValueError(f"Inventory must be a JSON list or object with entries[]: {path}")

    selected: list[dict[str, Any]] = []
    for entry in entries:
        if not isinstance(entry, dict):
            continue
        if not include_parse_errors and entry.get("parse_status", "ok") != "ok":
            continue
        if not entry.get("path") or not entry.get("value_name"):
            continue
        selected.append(entry)
        if limit and len(selected) >= limit:
            break
    return selected


def build_guest_script(entries: list[dict[str, Any]]) -> str:
    entries_json = json.dumps(entries, ensure_ascii=False, separators=(",", ":"))
    entries_b64 = base64.b64encode(entries_json.encode("utf-8")).decode("ascii")
    return rf'''
$ErrorActionPreference = 'Stop'
$entriesJson = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String('{entries_b64}'))
$entries = $entriesJson | ConvertFrom-Json

function Convert-ToPsRegistryPath {{
    param([Parameter(Mandatory = $true)][string]$Path)
    $normalized = $Path.Trim()
    $normalized = $normalized -replace '^HKEY_LOCAL_MACHINE\\', 'HKLM:\'
    $normalized = $normalized -replace '^HKLM\\', 'HKLM:\'
    $normalized = $normalized -replace '^HKEY_CURRENT_USER\\', 'HKCU:\'
    $normalized = $normalized -replace '^HKCU\\', 'HKCU:\'
    $normalized = $normalized -replace '^HKEY_CLASSES_ROOT\\', 'HKCR:\'
    $normalized = $normalized -replace '^HKCR\\', 'HKCR:\'
    $normalized = $normalized -replace '^HKEY_USERS\\', 'Registry::HKEY_USERS\'
    $normalized = $normalized -replace '^HKU\\', 'Registry::HKEY_USERS\'
    $normalized = $normalized -replace '^HKEY_CURRENT_CONFIG\\', 'Registry::HKEY_CURRENT_CONFIG\'
    $normalized = $normalized -replace '^HKCC\\', 'Registry::HKEY_CURRENT_CONFIG\'
    return $normalized
}}

function Convert-RegistryValue {{
    param($Value)
    if ($null -eq $Value) {{
        return $null
    }}
    if ($Value -is [byte[]]) {{
        return [ordered]@{{
            shape = 'byte-array'
            length = $Value.Length
            hex = (($Value | ForEach-Object {{ $_.ToString('x2') }}) -join '')
        }}
    }}
    if ($Value -is [array]) {{
        return @($Value)
    }}
    return $Value
}}

function Convert-ValueHex {{
    param($Value)
    if ($null -eq $Value) {{
        return $null
    }}
    if ($Value -is [byte] -or $Value -is [int16] -or $Value -is [uint16] -or $Value -is [int] -or $Value -is [uint32] -or $Value -is [long] -or $Value -is [uint64]) {{
        try {{
            return ('0x{{0:x}}' -f ([int64]$Value))
        }}
        catch {{
            return $null
        }}
    }}
    return $null
}}

function Get-AclSummary {{
    param([Parameter(Mandatory = $true)][string]$PsPath)
    try {{
        $acl = Get-Acl -LiteralPath $PsPath -ErrorAction Stop
        $writePrincipals = @(
            $acl.Access |
                Where-Object {{
                    $_.AccessControlType -eq 'Allow' -and
                    ($_.RegistryRights.ToString() -match 'SetValue|WriteKey|FullControl|CreateSubKey')
                }} |
                Select-Object -First 12 |
                ForEach-Object {{
                    [ordered]@{{
                        identity = $_.IdentityReference.ToString()
                        rights = $_.RegistryRights.ToString()
                        inherited = [bool]$_.IsInherited
                    }}
                }}
        )
        return [ordered]@{{
            status = 'ok'
            owner = $acl.Owner
            write_principals_sample = $writePrincipals
        }}
    }}
    catch {{
        return [ordered]@{{
            status = 'error'
            error = $_.Exception.Message
        }}
    }}
}}

function Read-RegistryEntry {{
    param($Entry)

    $path = [string]$Entry.path
    $valueName = [string]$Entry.value_name
    $psPath = Convert-ToPsRegistryPath -Path $path
    $row = [ordered]@{{
        index = $Entry.index
        path = $path
        value_name = $valueName
        requested_type = $Entry.type
        requested_data = $Entry.requested_data
        parse_status = $Entry.parse_status
        repo_exact_target_match_count = $Entry.repo_exact_target_match_count
        repo_text_hit_count = $Entry.repo_text_hit_count
        app_surface_text_hit = $Entry.app_surface_text_hit
        risk_flags = @($Entry.risk_flags)
        ps_path = $psPath
        key_exists = $false
        value_exists = $false
        status = 'not-read'
        value = $null
        value_kind = $null
        value_hex = $null
        sibling_value_count = $null
        sibling_values_sample = @()
        acl = $null
        error = $null
    }}

    try {{
        if (-not (Test-Path -LiteralPath $psPath)) {{
            $row.status = 'key-missing'
            return [pscustomobject]$row
        }}

        $row.key_exists = $true
        $key = Get-Item -LiteralPath $psPath -ErrorAction Stop
        $names = @($key.GetValueNames() | Sort-Object)
        $row.sibling_value_count = $names.Count
        $row.sibling_values_sample = @($names | Select-Object -First 80)
        $row.acl = Get-AclSummary -PsPath $psPath

        if ($names -notcontains $valueName) {{
            $row.status = 'value-missing'
            return [pscustomobject]$row
        }}

        $rawValue = $key.GetValue($valueName, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
        $row.value_exists = $true
        $row.value = Convert-RegistryValue -Value $rawValue
        $row.value_kind = $key.GetValueKind($valueName).ToString()
        $row.value_hex = Convert-ValueHex -Value $rawValue
        $row.status = 'value-present'
        return [pscustomobject]$row
    }}
    catch {{
        $row.status = 'error'
        $row.error = $_.Exception.Message
        return [pscustomobject]$row
    }}
}}

$records = @()
foreach ($entry in $entries) {{
    $records += Read-RegistryEntry -Entry $entry
}}

$summary = [ordered]@{{
    generated_utc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    status = 'ok'
    total_entries = $records.Count
    key_present_count = @($records | Where-Object {{ $_.key_exists }}).Count
    key_missing_count = @($records | Where-Object {{ -not $_.key_exists }}).Count
    value_present_count = @($records | Where-Object {{ $_.value_exists }}).Count
    value_missing_count = @($records | Where-Object {{ $_.key_exists -and -not $_.value_exists }}).Count
    error_count = @($records | Where-Object {{ $_.status -eq 'error' }}).Count
    records = $records
}}

$summary | ConvertTo-Json -Depth 14
'''.lstrip()


def run(cmd: list[str], *, timeout: int | None = None) -> subprocess.CompletedProcess[str]:
    return subprocess.run(cmd, cwd=str(REPO_ROOT), capture_output=True, text=True, timeout=timeout)


def run_guest_script(
    script: Path,
    *,
    domain: str,
    connect: str,
    wait_timeout: int,
) -> dict[str, Any]:
    cmd = [
        sys.executable,
        str(REPO_ROOT / "scripts" / "vm-kvm" / "qga-run-powershell.py"),
        "--domain",
        domain,
        "--connect",
        connect,
        "--script",
        str(script),
        "--guest-dir",
        r"C:\RegProbe-Diag\registry-inventory-baseline",
        "--wait-timeout",
        str(wait_timeout),
        "--keep",
    ]
    completed = run(cmd, timeout=wait_timeout + 60)
    try:
        qga_payload = json.loads(completed.stdout)
    except json.JSONDecodeError as error:
        return {
            "status": "error",
            "error": "qga-output-json-parse-failed",
            "message": str(error),
            "returncode": completed.returncode,
            "stdout": completed.stdout,
            "stderr": completed.stderr,
        }

    stdout = ((qga_payload.get("execution") or {}).get("stdout") if isinstance(qga_payload.get("execution"), dict) else None) or ""
    if completed.returncode != 0:
        return {
            "status": "error",
            "error": "qga-run-powershell-failed",
            "returncode": completed.returncode,
            "qga": qga_payload,
            "stderr": completed.stderr,
        }
    try:
        baseline = json.loads(stdout)
    except json.JSONDecodeError as error:
        return {
            "status": "error",
            "error": "guest-baseline-json-parse-failed",
            "message": str(error),
            "qga": qga_payload,
            "guest_stdout": stdout,
            "stderr": completed.stderr,
        }
    baseline["qga"] = {
        "status": qga_payload.get("status"),
        "domain": qga_payload.get("domain"),
        "guest_script_path": qga_payload.get("guest_script_path"),
    }
    return baseline


def summarize_status(records: list[dict[str, Any]]) -> dict[str, int]:
    counts = {
        "total_entries": len(records),
        "key_present_count": 0,
        "key_missing_count": 0,
        "value_present_count": 0,
        "value_missing_count": 0,
        "error_count": 0,
        "repo_exact_target_match_count": 0,
    }
    for record in records:
        if record.get("key_exists"):
            counts["key_present_count"] += 1
        else:
            counts["key_missing_count"] += 1
        if record.get("value_exists"):
            counts["value_present_count"] += 1
        elif record.get("key_exists"):
            counts["value_missing_count"] += 1
        if record.get("status") == "error":
            counts["error_count"] += 1
        if int(record.get("repo_exact_target_match_count") or 0) > 0:
            counts["repo_exact_target_match_count"] += 1
    return counts


def md_cell(value: object) -> str:
    text = "" if value is None else str(value)
    text = text.replace("|", "\\|").replace("\n", " ")
    return text


def write_markdown(payload: dict[str, Any], path: Path) -> None:
    records = payload.get("records") if isinstance(payload.get("records"), list) else []
    counts = summarize_status([record for record in records if isinstance(record, dict)])
    lines = [
        "# Operator Reg Add VM Baseline",
        "",
        f"- Generated UTC: `{payload.get('generated_utc')}`",
        f"- Status: **{payload.get('status')}**",
        f"- Total entries: `{counts['total_entries']}`",
        f"- Key present/missing: `{counts['key_present_count']}` / `{counts['key_missing_count']}`",
        f"- Value present/missing: `{counts['value_present_count']}` / `{counts['value_missing_count']}`",
        f"- Error count: `{counts['error_count']}`",
        f"- Repo exact target matches: `{counts['repo_exact_target_match_count']}`",
        "",
        "## Records",
        "",
        "| # | Target | Requested | VM status | Current value | Kind | Sibling count | Repo hits | Risk flags |",
        "|---:|---|---:|---|---|---|---:|---:|---|",
    ]
    for record in records:
        if not isinstance(record, dict):
            continue
        target = f"{record.get('path')}\\{record.get('value_name')}"
        current = record.get("value")
        if isinstance(current, dict):
            current = json.dumps(current, sort_keys=True)
        risk_flags = ", ".join(str(item) for item in record.get("risk_flags", []) if item)
        lines.append(
            "| "
            + " | ".join(
                [
                    md_cell(record.get("index")),
                    f"`{md_cell(target)}`",
                    f"`{md_cell(record.get('requested_data'))}`",
                    f"`{md_cell(record.get('status'))}`",
                    f"`{md_cell(current)}`",
                    f"`{md_cell(record.get('value_kind'))}`",
                    md_cell(record.get("sibling_value_count")),
                    md_cell(record.get("repo_exact_target_match_count")),
                    md_cell(risk_flags),
                ]
            )
            + " |"
        )
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Read an operator reg-add inventory from the Windows guest without mutating registry state.")
    parser.add_argument(
        "--inventory",
        default=str(REPO_ROOT / "registry-research-framework" / "audit" / "operator-regadd-inventory-20260508-repo.json"),
    )
    parser.add_argument("--domain", default=vm_domain("regprobe-win11-25h2-session"))
    parser.add_argument("--connect", default=vm_connect("qemu:///session"))
    parser.add_argument("--output", default="")
    parser.add_argument("--markdown-output", default="")
    parser.add_argument("--limit", type=int, default=0)
    parser.add_argument("--include-parse-errors", action="store_true")
    parser.add_argument("--wait-timeout", type=int, default=240)
    args = parser.parse_args()

    inventory_path = Path(args.inventory).resolve()
    generated_utc = now_utc()
    timestamp = generated_utc.replace(":", "").replace("-", "").replace("Z", "Z")
    output_path = Path(args.output) if args.output else REPO_ROOT / "registry-research-framework" / "audit" / f"operator-regadd-vm-baseline-{timestamp}.json"
    markdown_path = Path(args.markdown_output) if args.markdown_output else output_path.with_suffix(".md")

    entries = load_inventory(inventory_path, limit=args.limit, include_parse_errors=args.include_parse_errors)
    script_dir = REPO_ROOT / "dist" / "kvm-generated"
    script_dir.mkdir(parents=True, exist_ok=True)
    script_path = script_dir / f"operator-regadd-vm-baseline-{timestamp}.ps1"
    script_path.write_text(build_guest_script(entries), encoding="utf-8")

    payload = run_guest_script(script_path, domain=args.domain, connect=args.connect, wait_timeout=args.wait_timeout)
    payload.setdefault("generated_utc", generated_utc)
    payload["input_inventory"] = str(inventory_path)
    payload["host_script"] = str(script_path)
    payload["domain"] = args.domain
    payload["connect"] = args.connect

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    write_markdown(payload, markdown_path)

    print(
        json.dumps(
            {
                "status": payload.get("status"),
                "json": str(output_path),
                "markdown": str(markdown_path),
                "summary": summarize_status(payload.get("records") if isinstance(payload.get("records"), list) else []),
                "error": payload.get("error"),
            },
            indent=2,
        )
    )
    return 0 if payload.get("status") == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
