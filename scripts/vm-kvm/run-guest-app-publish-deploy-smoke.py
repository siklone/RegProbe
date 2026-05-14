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

from command_json_lib import parse_command_json
from summary_contract_lib import apply_summary_contract


def resolve_dotnet_path(explicit_path: str | None) -> str:
    if explicit_path:
        return explicit_path
    bundled = Path.home() / ".dotnet" / "dotnet"
    if bundled.exists():
        return str(bundled)
    discovered = shutil.which("dotnet")
    return discovered or "dotnet"


def run_json_command(cmd: list[str], *, cwd: Path) -> tuple[int, dict[str, Any]]:
    completed = subprocess.run(cmd, cwd=str(cwd), capture_output=True, text=True)
    return completed.returncode, parse_command_json(completed.stdout, stderr=completed.stderr)


def run_dotnet_publish(
    repo_root: Path,
    *,
    dotnet_path: str,
    project_path: Path,
    configuration: str,
    runtime: str,
    self_contained: bool,
    publish_dir: Path,
) -> tuple[int, dict[str, Any]]:
    cmd = [
        dotnet_path,
        "publish",
        str(project_path),
        "-c",
        configuration,
        "-r",
        runtime,
        "--self-contained",
        "true" if self_contained else "false",
        "-p:EnableWindowsTargeting=true",
        "-o",
        str(publish_dir),
    ]
    completed = subprocess.run(cmd, cwd=str(repo_root), capture_output=True, text=True)
    payload: dict[str, Any] = {
        "cmd": cmd,
        "project_path": str(project_path),
        "publish_dir": str(publish_dir),
        "self_contained": self_contained,
        "stdout": completed.stdout.strip(),
        "stderr": completed.stderr.strip(),
    }
    if completed.returncode == 0:
        files = [path for path in publish_dir.rglob("*") if path.is_file()]
        payload["published_file_count"] = len(files)
        payload["app_exe_exists"] = (publish_dir / "RegProbe.App.exe").exists()
    return completed.returncode, payload


def create_publish_zip(
    publish_dir: Path,
    *,
    publish_zip_path: Path,
) -> tuple[int, dict[str, Any]]:
    if not publish_dir.exists():
        return 1, {
            "status": "error",
            "error": f"Publish directory does not exist: {publish_dir}",
            "publish_dir": str(publish_dir),
            "publish_zip_path": str(publish_zip_path),
        }
    files = sorted(path for path in publish_dir.rglob("*") if path.is_file())
    publish_zip_path.parent.mkdir(parents=True, exist_ok=True)
    with ZipFile(publish_zip_path, "w", compression=ZIP_DEFLATED) as archive:
        for path in files:
            archive.write(path, path.relative_to(publish_dir))
    return 0, {
        "status": "ok",
        "publish_dir": str(publish_dir),
        "publish_zip_path": str(publish_zip_path),
        "archived_file_count": len(files),
        "app_exe_archived": any(path.name == "RegProbe.App.exe" for path in files),
    }


def run_app_deploy_smoke(
    repo_root: Path,
    *,
    publish_zip_path: Path,
    launch_wait_timeout: int,
    linger_seconds: int,
    leave_running: bool,
    guest_publish_zip_path: str,
    guest_app_root: str,
    guest_app_exe: str,
) -> tuple[int, dict[str, Any]]:
    cmd = [
        sys.executable,
        str(repo_root / "scripts" / "vm-kvm" / "run-guest-app-deploy-smoke.py"),
        "--publish-zip",
        str(publish_zip_path),
        "--launch-wait-timeout",
        str(launch_wait_timeout),
        "--linger-seconds",
        str(linger_seconds),
        "--guest-publish-zip-path",
        guest_publish_zip_path,
        "--guest-app-root",
        guest_app_root,
        "--guest-app-exe",
        guest_app_exe,
    ]
    if leave_running:
        cmd.append("--leave-running")
    return run_json_command(cmd, cwd=repo_root)


def build_deploy_smoke_command(
    repo_root: Path,
    *,
    publish_zip_path: Path,
    launch_wait_timeout: int,
    linger_seconds: int,
    leave_running: bool,
    guest_publish_zip_path: str,
    guest_app_root: str,
    guest_app_exe: str,
) -> list[str]:
    cmd = [
        sys.executable,
        str(repo_root / "scripts" / "vm-kvm" / "run-guest-app-deploy-smoke.py"),
        "--publish-zip",
        str(publish_zip_path),
        "--launch-wait-timeout",
        str(launch_wait_timeout),
        "--linger-seconds",
        str(linger_seconds),
        "--guest-publish-zip-path",
        guest_publish_zip_path,
        "--guest-app-root",
        guest_app_root,
        "--guest-app-exe",
        guest_app_exe,
    ]
    if leave_running:
        cmd.append("--leave-running")
    return cmd


def build_dry_run_payload(
    *,
    repo_root: Path,
    project_path: Path,
    dotnet_path: str,
    configuration: str,
    runtime: str,
    work_root: Path,
    publish_dir: Path,
    publish_zip_path: Path,
    launch_wait_timeout: int,
    linger_seconds: int,
    leave_running: bool,
    guest_publish_zip_path: str,
    guest_app_root: str,
    guest_app_exe: str,
    artifact_retention: str,
    self_contained: bool,
) -> dict[str, Any]:
    publish_cmd = [
        dotnet_path,
        "publish",
        str(project_path),
        "-c",
        configuration,
        "-r",
        runtime,
        "--self-contained",
        "true" if self_contained else "false",
        "-p:EnableWindowsTargeting=true",
        "-o",
        str(publish_dir),
    ]
    deploy_smoke_cmd = build_deploy_smoke_command(
        repo_root,
        publish_zip_path=publish_zip_path,
        launch_wait_timeout=launch_wait_timeout,
        linger_seconds=linger_seconds,
        leave_running=leave_running,
        guest_publish_zip_path=guest_publish_zip_path,
        guest_app_root=guest_app_root,
        guest_app_exe=guest_app_exe,
    )
    return apply_summary_contract(
        {
            "summary_source": "guest-app-publish-deploy-smoke",
            "status": "ok",
            "mode": "dry-run",
            "repo_root": str(repo_root),
            "project_path": str(project_path),
            "configuration": configuration,
            "runtime": runtime,
            "self_contained": self_contained,
            "dotnet_path": dotnet_path,
            "work_root": str(work_root),
            "publish_dir": str(publish_dir),
            "publish_zip_path": str(publish_zip_path),
            "launch_wait_timeout": launch_wait_timeout,
            "linger_seconds": linger_seconds,
            "artifact_retention": artifact_retention,
            "publish_command": publish_cmd,
            "zip_preview": {
                "source_dir": str(publish_dir),
                "zip_path": str(publish_zip_path),
            },
            "deploy_smoke_command": deploy_smoke_cmd,
            "guest_paths": {
                "publish_zip_path": guest_publish_zip_path,
                "app_root": guest_app_root,
                "app_exe": guest_app_exe,
            },
        }
    )


def build_verify_only_payload(
    *,
    repo_root: Path,
    project_path: Path,
    dotnet_path: str,
    configuration: str,
    runtime: str,
    work_root: Path,
    publish_dir: Path,
    publish_zip_path: Path,
    launch_wait_timeout: int,
    linger_seconds: int,
    leave_running: bool,
    guest_publish_zip_path: str,
    guest_app_root: str,
    guest_app_exe: str,
    artifact_retention: str,
    self_contained: bool,
) -> dict[str, Any]:
    blockers: list[str] = []
    if not repo_root.exists():
        blockers.append(f"repo-root-missing:{repo_root}")
    if not project_path.exists():
        blockers.append(f"project-missing:{project_path}")

    dotnet_candidate = Path(dotnet_path)
    if (dotnet_candidate.is_absolute() or "/" in dotnet_path) and not dotnet_candidate.exists():
        blockers.append(f"dotnet-missing:{dotnet_path}")
    elif not (dotnet_candidate.is_absolute() or "/" in dotnet_path):
        if shutil.which(dotnet_path) is None:
            blockers.append(f"dotnet-unresolvable:{dotnet_path}")

    deploy_smoke_script = repo_root / "scripts" / "vm-kvm" / "run-guest-app-deploy-smoke.py"
    if not deploy_smoke_script.exists():
        blockers.append(f"deploy-smoke-script-missing:{deploy_smoke_script}")

    dry_run_payload = build_dry_run_payload(
        repo_root=repo_root,
        project_path=project_path,
        dotnet_path=dotnet_path,
        configuration=configuration,
        runtime=runtime,
        work_root=work_root,
        publish_dir=publish_dir,
        publish_zip_path=publish_zip_path,
        launch_wait_timeout=launch_wait_timeout,
        linger_seconds=linger_seconds,
        leave_running=leave_running,
        guest_publish_zip_path=guest_publish_zip_path,
        guest_app_root=guest_app_root,
        guest_app_exe=guest_app_exe,
        artifact_retention=artifact_retention,
        self_contained=self_contained,
    )
    recommended_execute_command = [
        sys.executable,
        str(repo_root / "scripts" / "vm-kvm" / "run-guest-app-publish-deploy-smoke.py"),
        "--linger-seconds",
        str(linger_seconds),
    ]
    if leave_running:
        recommended_execute_command.append("--leave-running")
    if self_contained:
        recommended_execute_command.append("--self-contained")
    if artifact_retention == "kept":
        recommended_execute_command.append("--keep-artifacts")
    operator_checklist = [
        f"Confirm dotnet is reachable at {dotnet_path}.",
        f"Confirm the publish target exists: {project_path}.",
        f"Review the guest destination root: {guest_app_root}.",
        f"Review the guest executable path: {guest_app_exe}.",
        "Run the recommended execute command once the blockers list is empty.",
    ]
    return apply_summary_contract(
        {
            **dry_run_payload,
            "mode": "verify-only",
            "ready_for_execute": not blockers,
            "blockers": blockers,
            "next_step": dry_run_payload["deploy_smoke_command"] if not blockers else None,
            "recommended_execute_command": recommended_execute_command if not blockers else None,
            "operator_checklist": operator_checklist,
            "status": "ok",
        }
    )


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Publish the app, zip it, deploy it into the KVM guest, and run launch smoke."
    )
    parser.add_argument("--repo-root", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--project-path", default="app/app.csproj")
    parser.add_argument("--configuration", default="Release")
    parser.add_argument("--runtime", default="win-x64")
    parser.add_argument(
        "--self-contained",
        action="store_true",
        help="Publish with the Windows runtime included. Use this for VMs without dotnet/Microsoft.WindowsDesktop.App installed.",
    )
    parser.add_argument("--dotnet-path")
    parser.add_argument("--work-root")
    parser.add_argument("--keep-artifacts", action="store_true")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--verify-only", action="store_true")
    parser.add_argument("--launch-wait-timeout", type=int, default=20)
    parser.add_argument("--linger-seconds", type=int, default=5)
    parser.add_argument("--leave-running", action="store_true")
    parser.add_argument("--guest-publish-zip-path", default=r"C:\Tools\Inbound\app-publish-current-branch.zip")
    parser.add_argument("--guest-app-root", default=r"C:\Tools\AppSmoke")
    parser.add_argument("--guest-app-exe", default=r"C:\Tools\AppSmoke\RegProbe.App.exe")
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    project_path = (repo_root / args.project_path).resolve()
    dotnet_path = resolve_dotnet_path(args.dotnet_path)

    temp_context: tempfile.TemporaryDirectory[str] | None = None
    cleanup_work_root = False
    if args.work_root:
        work_root = Path(args.work_root).resolve()
        work_root.mkdir(parents=True, exist_ok=True)
    elif args.keep_artifacts:
        work_root = Path(tempfile.mkdtemp(prefix="regprobe-app-publish-smoke-")).resolve()
    else:
        temp_context = tempfile.TemporaryDirectory(prefix="regprobe-app-publish-smoke-")
        work_root = Path(temp_context.name).resolve()
        cleanup_work_root = True

    publish_dir = work_root / "publish"
    publish_zip_path = work_root / "RegProbe.App.publish.zip"

    summary: dict[str, Any] = {
        "summary_source": "guest-app-publish-deploy-smoke",
        "repo_root": str(repo_root),
        "project_path": str(project_path),
        "configuration": args.configuration,
        "runtime": args.runtime,
        "self_contained": args.self_contained,
        "dotnet_path": dotnet_path,
        "work_root": str(work_root),
        "publish_dir": str(publish_dir),
        "publish_zip_path": str(publish_zip_path),
        "launch_wait_timeout": args.launch_wait_timeout,
        "linger_seconds": args.linger_seconds,
        "artifact_retention": "kept" if args.keep_artifacts or args.work_root else "ephemeral",
    }

    try:
        if args.verify_only:
            payload = build_verify_only_payload(
                repo_root=repo_root,
                project_path=project_path,
                dotnet_path=dotnet_path,
                configuration=args.configuration,
                runtime=args.runtime,
                work_root=work_root,
                publish_dir=publish_dir,
                publish_zip_path=publish_zip_path,
                launch_wait_timeout=args.launch_wait_timeout,
                linger_seconds=args.linger_seconds,
                leave_running=args.leave_running,
                guest_publish_zip_path=args.guest_publish_zip_path,
                guest_app_root=args.guest_app_root,
                guest_app_exe=args.guest_app_exe,
                artifact_retention=summary["artifact_retention"],
                self_contained=args.self_contained,
            )
            print(json.dumps(payload, indent=2))
            return 0

        if args.dry_run:
            payload = build_dry_run_payload(
                repo_root=repo_root,
                project_path=project_path,
                dotnet_path=dotnet_path,
                configuration=args.configuration,
                runtime=args.runtime,
                work_root=work_root,
                publish_dir=publish_dir,
                publish_zip_path=publish_zip_path,
                launch_wait_timeout=args.launch_wait_timeout,
                linger_seconds=args.linger_seconds,
                leave_running=args.leave_running,
                guest_publish_zip_path=args.guest_publish_zip_path,
                guest_app_root=args.guest_app_root,
                guest_app_exe=args.guest_app_exe,
                artifact_retention=summary["artifact_retention"],
                self_contained=args.self_contained,
            )
            print(json.dumps(payload, indent=2))
            return 0

        publish_returncode, publish_payload = run_dotnet_publish(
            repo_root,
            dotnet_path=dotnet_path,
            project_path=project_path,
            configuration=args.configuration,
            runtime=args.runtime,
            self_contained=args.self_contained,
            publish_dir=publish_dir,
        )
        summary["publish_returncode"] = publish_returncode
        summary["publish_payload"] = publish_payload
        if publish_returncode != 0:
            summary.update(
                {
                    "status": "error",
                    "error_kind": "app-publish-failed",
                    "error": "dotnet publish did not complete successfully.",
                }
            )
            payload = apply_summary_contract(
                summary,
                default_error_kind="app-publish-failed",
                default_recovery_action="inspect-publish-step",
                default_transport_blocker="dotnet-publish",
                default_guest_health="stable",
            )
            print(json.dumps(payload, indent=2))
            return 1

        zip_returncode, zip_payload = create_publish_zip(
            publish_dir,
            publish_zip_path=publish_zip_path,
        )
        summary["zip_returncode"] = zip_returncode
        summary["zip_payload"] = zip_payload
        if zip_returncode != 0:
            summary.update(
                {
                    "status": "error",
                    "error_kind": "app-publish-zip-failed",
                    "error": "Failed to create a publish zip from the app publish output.",
                }
            )
            payload = apply_summary_contract(
                summary,
                default_error_kind="app-publish-zip-failed",
                default_recovery_action="inspect-zip-step",
                default_transport_blocker="publish-zip",
                default_guest_health="stable",
            )
            print(json.dumps(payload, indent=2))
            return 1

        deploy_smoke_returncode, deploy_smoke_payload = run_app_deploy_smoke(
            repo_root,
            publish_zip_path=publish_zip_path,
            launch_wait_timeout=args.launch_wait_timeout,
            linger_seconds=args.linger_seconds,
            leave_running=args.leave_running,
            guest_publish_zip_path=args.guest_publish_zip_path,
            guest_app_root=args.guest_app_root,
            guest_app_exe=args.guest_app_exe,
        )
        summary["deploy_smoke_returncode"] = deploy_smoke_returncode
        summary["deploy_smoke_payload"] = deploy_smoke_payload
        if deploy_smoke_returncode != 0 or deploy_smoke_payload.get("status") != "ok":
            summary.update(
                {
                    "status": "error",
                    "error_kind": str(deploy_smoke_payload.get("error_kind") or "guest-app-deploy-smoke-failed"),
                    "error": str(
                        deploy_smoke_payload.get("error")
                        or "The guest deploy plus launch smoke runner did not complete successfully."
                    ),
                }
            )
            payload = apply_summary_contract(
                summary,
                default_error_kind=str(summary["error_kind"]),
                default_recovery_action=str(deploy_smoke_payload.get("recovery_action") or "inspect-deploy-smoke-step"),
                default_transport_blocker=str(
                    deploy_smoke_payload.get("transport_blocker") or "guest-app-deploy-smoke"
                ),
                default_guest_health=str(deploy_smoke_payload.get("guest_health") or "degraded"),
            )
            print(json.dumps(payload, indent=2))
            return 1

        summary["status"] = "ok"
        payload = apply_summary_contract(summary)
        print(json.dumps(payload, indent=2))
        return 0
    finally:
        if temp_context is not None:
            if cleanup_work_root:
                temp_context.cleanup()


if __name__ == "__main__":
    raise SystemExit(main())
