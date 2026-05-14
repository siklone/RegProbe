#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path
from typing import Any
from zipfile import ZIP_DEFLATED, ZipFile


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_TEST_OUTPUT = Path("tests/bin/Release/net8.0-windows")
DEFAULT_GUEST_REPO_ROOT = r"C:\RegProbe"
DEFAULT_GUEST_ZIP_PATH = r"C:\Tools\Inbound\regprobe-vm-test-stage.zip"
DEFAULT_GUEST_DOTNET = r"C:\Tools\DotNetSDK\8.0.416\dotnet.exe"


def resolve_dotnet_path(explicit_path: str | None) -> str:
    if explicit_path:
        return explicit_path
    repo_local = REPO_ROOT / ".tools" / "dotnet" / "dotnet"
    if repo_local.exists():
        return str(repo_local)
    bundled = Path.home() / ".dotnet" / "dotnet"
    if bundled.exists():
        return str(bundled)
    discovered = shutil.which("dotnet")
    return discovered or "dotnet"


def run_host_build(repo_root: Path, *, dotnet_path: str, configuration: str) -> tuple[int, dict[str, Any]]:
    cmd = [
        dotnet_path,
        "build",
        str(repo_root / "tests" / "tests.csproj"),
        "-c",
        configuration,
        "-p:EnableWindowsTargeting=true",
    ]
    completed = subprocess.run(cmd, cwd=repo_root, capture_output=True, text=True)
    return completed.returncode, {
        "cmd": cmd,
        "stdout": completed.stdout.strip(),
        "stderr": completed.stderr.strip(),
    }


def required_stage_paths(repo_root: Path, test_output_dir: Path) -> list[Path]:
    return [
        test_output_dir,
        repo_root / "Docs",
        repo_root / "research" / "records",
        repo_root / "research" / "promotion-gates.json",
    ]


def create_stage_zip(repo_root: Path, *, test_output_dir: Path, stage_zip_path: Path) -> dict[str, Any]:
    missing = [path for path in required_stage_paths(repo_root, test_output_dir) if not path.exists()]
    if missing:
        return {
            "status": "error",
            "error_kind": "missing-stage-input",
            "missing": [str(path) for path in missing],
        }

    stage_zip_path.parent.mkdir(parents=True, exist_ok=True)
    file_count = 0
    with ZipFile(stage_zip_path, "w", compression=ZIP_DEFLATED) as archive:
        for root in required_stage_paths(repo_root, test_output_dir):
            if root.is_file():
                archive.write(root, root.relative_to(repo_root).as_posix())
                file_count += 1
                continue
            for path in sorted(root.rglob("*")):
                if path.is_file():
                    archive.write(path, path.relative_to(repo_root).as_posix())
                    file_count += 1

    return {
        "status": "ok",
        "stage_zip_path": str(stage_zip_path),
        "file_count": file_count,
        "size_bytes": stage_zip_path.stat().st_size,
        "test_assembly": str(test_output_dir / "RegProbe.Tests.dll"),
    }


def write_guest_runner_script(path: Path) -> None:
    path.write_text(
        r"""param(
    [Parameter(Mandatory = $true)]
    [string]$StageZipPath,
    [Parameter(Mandatory = $true)]
    [string]$GuestRepoRoot,
    [Parameter(Mandatory = $true)]
    [string]$TestAssemblyRelativePath,
    [Parameter(Mandatory = $true)]
    [string]$ResultsRelativePath,
    [Parameter(Mandatory = $true)]
    [string]$DotnetPath,
    [string]$Filter = ''
)

$ErrorActionPreference = 'Continue'
if (-not (Test-Path -LiteralPath $DotnetPath)) {
    $DotnetPath = 'dotnet'
}

Remove-Item -LiteralPath $GuestRepoRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $GuestRepoRoot -Force | Out-Null
Expand-Archive -LiteralPath $StageZipPath -DestinationPath $GuestRepoRoot -Force

$testAssembly = Join-Path $GuestRepoRoot $TestAssemblyRelativePath
$results = Join-Path $GuestRepoRoot $ResultsRelativePath
New-Item -ItemType Directory -Path $results -Force | Out-Null
if (-not (Test-Path -LiteralPath $testAssembly)) {
    throw "Missing test assembly: $testAssembly"
}

$testArgs = @(
    'test',
    $testAssembly,
    '--logger',
    'trx;LogFileName=regprobe-tests.trx',
    '--results-directory',
    $results
)
if (-not [string]::IsNullOrWhiteSpace($Filter)) {
    $testArgs += @('--filter', $Filter)
}

$testOutput = & $DotnetPath @testArgs 2>&1
$exitCode = $LASTEXITCODE
$trx = Get-ChildItem -LiteralPath $results -Filter *.trx -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1
$failed = @()
$countersObj = $null
if ($trx) {
    [xml]$xml = Get-Content -LiteralPath $trx.FullName -Raw
    $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    $counters = $xml.SelectSingleNode('//t:Counters', $ns)
    if ($counters) {
        $countersObj = [pscustomobject]@{
            total = $counters.total
            executed = $counters.executed
            passed = $counters.passed
            failed = $counters.failed
            error = $counters.error
            timeout = $counters.timeout
            aborted = $counters.aborted
            notExecuted = $counters.notExecuted
        }
    }
    foreach ($unit in $xml.SelectNodes('//t:UnitTestResult[@outcome="Failed"]', $ns)) {
        $msgNode = $unit.SelectSingleNode('t:Output/t:ErrorInfo/t:Message', $ns)
        $failed += [pscustomobject]@{
            TestName = $unit.testName
            Message = if ($msgNode) { $msgNode.InnerText } else { '' }
        }
    }
}

[pscustomobject]@{
    Status = if ($exitCode -eq 0) { 'PASS' } else { 'FAIL' }
    ExitCode = $exitCode
    GuestRepoRoot = $GuestRepoRoot
    Dotnet = $DotnetPath
    TestAssembly = $testAssembly
    Filter = $Filter
    Counters = $countersObj
    Failed = $failed
    OutputTail = (($testOutput | Select-Object -Last 80) -join "`n")
    TrxPath = if ($trx) { $trx.FullName } else { $null }
} | ConvertTo-Json -Depth 8
exit $exitCode
""",
        encoding="utf-8",
    )


def run_json_command(cmd: list[str], *, cwd: Path) -> tuple[int, dict[str, Any]]:
    completed = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True)
    try:
        payload = json.loads(completed.stdout)
        if not isinstance(payload, dict):
            payload = {"status": "error", "stdout_parse_error": "stdout JSON payload is not an object"}
    except json.JSONDecodeError as exc:
        payload = {"status": "error", "stdout_parse_error": str(exc), "stdout": completed.stdout}
    if completed.stderr.strip():
        payload["stderr"] = completed.stderr.strip()
    return completed.returncode, payload


def parse_guest_test_result(qga_payload: dict[str, Any]) -> dict[str, Any]:
    execution = qga_payload.get("execution") if isinstance(qga_payload, dict) else None
    stdout = execution.get("stdout") if isinstance(execution, dict) else None
    if not isinstance(stdout, str) or not stdout.strip():
        return {
            "status": "error",
            "error_kind": "missing-guest-test-json",
            "qga_payload_status": qga_payload.get("status") if isinstance(qga_payload, dict) else None,
            "execution_exit": execution.get("exitcode") if isinstance(execution, dict) else None,
        }
    try:
        payload = json.loads(stdout)
    except json.JSONDecodeError as exc:
        return {
            "status": "error",
            "error_kind": "guest-test-json-parse-error",
            "parse_error": str(exc),
            "raw_stdout": stdout,
        }
    if not isinstance(payload, dict):
        return {
            "status": "error",
            "error_kind": "guest-test-json-not-object",
            "raw_stdout": stdout,
        }
    return payload


def main() -> int:
    parser = argparse.ArgumentParser(description="Stage RegProbe test output plus repo data into the KVM guest and run .NET tests there.")
    parser.add_argument("--repo-root", default=str(REPO_ROOT))
    parser.add_argument("--configuration", default="Release")
    parser.add_argument("--test-output-dir", default=str(DEFAULT_TEST_OUTPUT))
    parser.add_argument("--dotnet-path", help="Host dotnet path used only when --build is set.")
    parser.add_argument("--build", action="store_true", help="Build tests on the host before staging them.")
    parser.add_argument("--guest-repo-root", default=DEFAULT_GUEST_REPO_ROOT)
    parser.add_argument("--guest-zip-path", default=DEFAULT_GUEST_ZIP_PATH)
    parser.add_argument("--guest-dotnet-path", default=DEFAULT_GUEST_DOTNET)
    parser.add_argument("--guest-dir", default=r"C:\Tools\ValidationController\dotnet-tests")
    parser.add_argument("--filter", default="", help="Optional dotnet test filter.")
    parser.add_argument("--keep-artifacts", action="store_true")
    parser.add_argument("--wait-timeout", type=int, default=1800)
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    test_output_dir = (repo_root / args.test_output_dir).resolve()
    host_dotnet_path = resolve_dotnet_path(args.dotnet_path)

    summary: dict[str, Any] = {
        "summary_source": "guest-dotnet-tests",
        "repo_root": str(repo_root),
        "test_output_dir": str(test_output_dir),
        "guest_repo_root": args.guest_repo_root,
        "guest_zip_path": args.guest_zip_path,
        "guest_dotnet_path": args.guest_dotnet_path,
        "filter": args.filter,
        "status": "error",
    }

    temp_context: tempfile.TemporaryDirectory[str] | None = None
    work_root: Path
    if args.keep_artifacts:
        work_root = Path(tempfile.mkdtemp(prefix="regprobe-vm-dotnet-tests-")).resolve()
    else:
        temp_context = tempfile.TemporaryDirectory(prefix="regprobe-vm-dotnet-tests-")
        work_root = Path(temp_context.name).resolve()

    try:
        if args.build:
            build_returncode, build_payload = run_host_build(repo_root, dotnet_path=host_dotnet_path, configuration=args.configuration)
            summary["build_returncode"] = build_returncode
            summary["build_payload"] = build_payload
            if build_returncode != 0:
                summary.update({"status": "error", "error_kind": "host-test-build-failed"})
                print(json.dumps(summary, indent=2))
                return 1

        stage_zip_path = work_root / "regprobe-vm-test-stage.zip"
        stage_payload = create_stage_zip(repo_root, test_output_dir=test_output_dir, stage_zip_path=stage_zip_path)
        summary["stage_payload"] = stage_payload
        if stage_payload.get("status") != "ok":
            summary.update({"status": "error", "error_kind": stage_payload.get("error_kind") or "stage-zip-failed"})
            print(json.dumps(summary, indent=2))
            return 1

        put_file = repo_root / "scripts" / "vm-kvm" / "qga-put-file.py"
        upload_returncode, upload_payload = run_json_command(
            [
                sys.executable,
                str(put_file),
                "--source",
                str(stage_zip_path),
                "--destination",
                args.guest_zip_path,
                "--timeout",
                "30",
                "--chunk-size",
                "65536",
            ],
            cwd=repo_root,
        )
        summary["upload_returncode"] = upload_returncode
        summary["upload_payload"] = upload_payload
        if upload_returncode != 0 or upload_payload.get("status") != "uploaded":
            summary.update({"status": "error", "error_kind": "guest-upload-failed"})
            print(json.dumps(summary, indent=2))
            return 1

        guest_script = work_root / "run-guest-dotnet-tests.ps1"
        write_guest_runner_script(guest_script)
        qga_runner = repo_root / "scripts" / "vm-kvm" / "qga-run-powershell.py"
        test_assembly_relative = str((test_output_dir / "RegProbe.Tests.dll").relative_to(repo_root)).replace("/", "\\")
        test_returncode, test_payload = run_json_command(
            [
                sys.executable,
                str(qga_runner),
                "--script",
                str(guest_script),
                "--guest-dir",
                args.guest_dir,
                "--ps-arg",
                args.guest_zip_path,
                "--ps-arg",
                args.guest_repo_root,
                "--ps-arg",
                test_assembly_relative,
                "--ps-arg",
                "test-results",
                "--ps-arg",
                args.guest_dotnet_path,
                "--ps-arg",
                args.filter,
                "--wait-timeout",
                str(args.wait_timeout),
            ],
            cwd=repo_root,
        )
        guest_test_result = parse_guest_test_result(test_payload)
        summary["test_returncode"] = test_returncode
        summary["test_payload_status"] = test_payload.get("status")
        summary["guest_test_result"] = guest_test_result
        summary["status"] = "PASS" if guest_test_result.get("Status") == "PASS" else "FAIL"
        print(json.dumps(summary, indent=2))
        return 0 if summary["status"] == "PASS" else 1
    finally:
        if temp_context is not None:
            temp_context.cleanup()


if __name__ == "__main__":
    raise SystemExit(main())
