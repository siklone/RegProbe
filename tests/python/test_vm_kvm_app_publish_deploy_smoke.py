from __future__ import annotations

import importlib.util
import io
import json
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock
from zipfile import ZipFile


REPO_ROOT = Path(__file__).resolve().parents[2]
VM_KVM_SCRIPTS = REPO_ROOT / "scripts" / "vm-kvm"
if str(VM_KVM_SCRIPTS) not in sys.path:
    sys.path.insert(0, str(VM_KVM_SCRIPTS))


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


app_publish_deploy_smoke = load_module(
    "run_guest_app_publish_deploy_smoke_for_tests",
    VM_KVM_SCRIPTS / "run-guest-app-publish-deploy-smoke.py",
)
command_json_lib = load_module(
    "command_json_lib_for_publish_deploy_tests",
    VM_KVM_SCRIPTS / "command_json_lib.py",
)


class VmKvmAppPublishDeploySmokeTests(unittest.TestCase):
    def test_parse_command_json_reports_invalid_json(self) -> None:
        payload = command_json_lib.parse_command_json("{not-json", stderr="bad-json")
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["stderr"], "bad-json")
        self.assertIn("stdout_parse_error", payload)

    def test_run_app_deploy_smoke_returns_parse_error_payload_for_invalid_json(self) -> None:
        completed = mock.Mock(returncode=0, stdout="{not-json", stderr="bad-json")
        with mock.patch.object(app_publish_deploy_smoke.subprocess, "run", return_value=completed):
            exit_code, payload = app_publish_deploy_smoke.run_app_deploy_smoke(
                REPO_ROOT,
                publish_zip_path=Path("/tmp/publish.zip"),
                launch_wait_timeout=20,
                linger_seconds=5,
                leave_running=False,
                guest_publish_zip_path=r"C:\Tools\Inbound\app-publish-current-branch.zip",
                guest_app_root=r"C:\Tools\AppSmoke",
                guest_app_exe=r"C:\Tools\AppSmoke\RegProbe.App.exe",
            )

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["status"], "error")
        self.assertIn("stdout_parse_error", payload)
        self.assertEqual(payload["stderr"], "bad-json")

    def test_run_app_deploy_smoke_returns_parse_error_payload_for_non_object_json(self) -> None:
        completed = mock.Mock(returncode=0, stdout='["not","object"]', stderr="")
        with mock.patch.object(app_publish_deploy_smoke.subprocess, "run", return_value=completed):
            exit_code, payload = app_publish_deploy_smoke.run_app_deploy_smoke(
                REPO_ROOT,
                publish_zip_path=Path("/tmp/publish.zip"),
                launch_wait_timeout=20,
                linger_seconds=5,
                leave_running=False,
                guest_publish_zip_path=r"C:\Tools\Inbound\app-publish-current-branch.zip",
                guest_app_root=r"C:\Tools\AppSmoke",
                guest_app_exe=r"C:\Tools\AppSmoke\RegProbe.App.exe",
            )

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["stdout_parse_error"], "stdout JSON payload is not an object")

    def test_run_app_deploy_smoke_passes_launch_wait_timeout(self) -> None:
        completed = mock.Mock(returncode=0, stdout='{"status":"ok"}', stderr="")
        with mock.patch.object(app_publish_deploy_smoke.subprocess, "run", return_value=completed) as run_mock:
            exit_code, payload = app_publish_deploy_smoke.run_app_deploy_smoke(
                REPO_ROOT,
                publish_zip_path=Path("/tmp/publish.zip"),
                launch_wait_timeout=41,
                linger_seconds=5,
                leave_running=False,
                guest_publish_zip_path=r"C:\Tools\Inbound\app-publish-current-branch.zip",
                guest_app_root=r"C:\Tools\AppSmoke",
                guest_app_exe=r"C:\Tools\AppSmoke\RegProbe.App.exe",
            )

        cmd = run_mock.call_args.args[0]
        self.assertIn("--launch-wait-timeout", cmd)
        self.assertIn("41", cmd)
        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["status"], "ok")

    def test_verify_only_returns_ready_payload_with_next_step(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            work_root = Path(temp_root)
            argv = [
                "run-guest-app-publish-deploy-smoke.py",
                "--work-root",
                str(work_root),
                "--verify-only",
                "--linger-seconds",
                "7",
            ]

            with mock.patch.object(sys, "argv", argv), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = app_publish_deploy_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["status"], "ok")
        self.assertEqual(payload["mode"], "verify-only")
        self.assertTrue(payload["ready_for_execute"])
        self.assertEqual(payload["blockers"], [])
        self.assertIn("run-guest-app-deploy-smoke.py", payload["next_step"][1])
        self.assertIn("run-guest-app-publish-deploy-smoke.py", payload["recommended_execute_command"][1])
        self.assertIn("--launch-wait-timeout", payload["next_step"])
        self.assertIn("--linger-seconds", payload["recommended_execute_command"])
        self.assertEqual(len(payload["operator_checklist"]), 5)

    def test_verify_only_surfaces_blockers_when_project_and_dotnet_are_missing(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            work_root = Path(temp_root)
            argv = [
                "run-guest-app-publish-deploy-smoke.py",
                "--work-root",
                str(work_root),
                "--verify-only",
                "--project-path",
                "app/does-not-exist.csproj",
                "--dotnet-path",
                "/tmp/definitely-missing-dotnet",
            ]

            with mock.patch.object(sys, "argv", argv), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = app_publish_deploy_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["mode"], "verify-only")
        self.assertFalse(payload["ready_for_execute"])
        self.assertTrue(any(item.startswith("project-missing:") for item in payload["blockers"]))
        self.assertTrue(any(item.startswith("dotnet-missing:") for item in payload["blockers"]))
        self.assertIsNone(payload["next_step"])
        self.assertIsNone(payload["recommended_execute_command"])
        self.assertEqual(len(payload["operator_checklist"]), 5)

    def test_dry_run_returns_planned_publish_zip_and_deploy_commands(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            work_root = Path(temp_root)
            argv = [
                "run-guest-app-publish-deploy-smoke.py",
                "--work-root",
                str(work_root),
                "--dry-run",
                "--linger-seconds",
                "7",
                "--leave-running",
            ]

            with mock.patch.object(sys, "argv", argv), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = app_publish_deploy_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["status"], "ok")
        self.assertEqual(payload["mode"], "dry-run")
        self.assertIn("publish_command", payload)
        self.assertEqual(payload["publish_command"][1:3], ["publish", str(REPO_ROOT / "app" / "app.csproj")])
        self.assertIn("-p:EnableWindowsTargeting=true", payload["publish_command"])
        self.assertEqual(payload["zip_preview"]["source_dir"], str(work_root / "publish"))
        self.assertEqual(payload["zip_preview"]["zip_path"], str(work_root / "RegProbe.App.publish.zip"))
        self.assertIn("run-guest-app-deploy-smoke.py", payload["deploy_smoke_command"][1])
        self.assertIn("--launch-wait-timeout", payload["deploy_smoke_command"])
        self.assertIn("--leave-running", payload["deploy_smoke_command"])
        self.assertEqual(payload["guest_paths"]["app_root"], r"C:\Tools\AppSmoke")
        self.assertEqual(payload["recovery_action"], "none")

    def test_run_dotnet_publish_sets_enable_windows_targeting(self) -> None:
        completed = mock.Mock(returncode=0, stdout="publish ok", stderr="")
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            publish_dir = Path(temp_root) / "publish"
            publish_dir.mkdir()
            (publish_dir / "RegProbe.App.exe").write_text("exe", encoding="utf-8")
            with mock.patch.object(app_publish_deploy_smoke.subprocess, "run", return_value=completed) as run_mock:
                exit_code, _payload = app_publish_deploy_smoke.run_dotnet_publish(
                    REPO_ROOT,
                    dotnet_path="/tmp/dotnet",
                    project_path=REPO_ROOT / "app" / "app.csproj",
                    configuration="Release",
                    runtime="win-x64",
                    self_contained=False,
                    publish_dir=publish_dir,
                )

        self.assertEqual(exit_code, 0)
        cmd = run_mock.call_args.args[0]
        self.assertIn("-p:EnableWindowsTargeting=true", cmd)
        self.assertEqual(cmd[cmd.index("--self-contained") + 1], "false")

    def test_run_dotnet_publish_can_include_runtime_for_guest_without_dotnet(self) -> None:
        completed = mock.Mock(returncode=0, stdout="publish ok", stderr="")
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            publish_dir = Path(temp_root) / "publish"
            publish_dir.mkdir()
            (publish_dir / "RegProbe.App.exe").write_text("exe", encoding="utf-8")
            with mock.patch.object(app_publish_deploy_smoke.subprocess, "run", return_value=completed) as run_mock:
                exit_code, payload = app_publish_deploy_smoke.run_dotnet_publish(
                    REPO_ROOT,
                    dotnet_path="/tmp/dotnet",
                    project_path=REPO_ROOT / "app" / "app.csproj",
                    configuration="Release",
                    runtime="win-x64",
                    self_contained=True,
                    publish_dir=publish_dir,
                )

        self.assertEqual(exit_code, 0)
        self.assertTrue(payload["self_contained"])
        cmd = run_mock.call_args.args[0]
        self.assertEqual(cmd[cmd.index("--self-contained") + 1], "true")

    def test_create_publish_zip_archives_publish_contents_without_parent_prefix(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            publish_dir = Path(temp_root) / "publish"
            publish_dir.mkdir()
            (publish_dir / "RegProbe.App.exe").write_text("exe", encoding="utf-8")
            nested_dir = publish_dir / "runtimes" / "win"
            nested_dir.mkdir(parents=True)
            (nested_dir / "runtime.json").write_text("{}", encoding="utf-8")
            zip_path = Path(temp_root) / "publish.zip"

            exit_code, payload = app_publish_deploy_smoke.create_publish_zip(
                publish_dir,
                publish_zip_path=zip_path,
            )

            self.assertEqual(exit_code, 0)
            self.assertEqual(payload["archived_file_count"], 2)
            with ZipFile(zip_path) as archive:
                members = sorted(archive.namelist())
            self.assertEqual(members, ["RegProbe.App.exe", "runtimes/win/runtime.json"])

    def test_main_returns_ok_when_publish_zip_and_deploy_smoke_succeed(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            work_root = Path(temp_root)
            argv = [
                "run-guest-app-publish-deploy-smoke.py",
                "--work-root",
                str(work_root),
                "--linger-seconds",
                "1",
            ]

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                app_publish_deploy_smoke,
                "run_dotnet_publish",
                return_value=(0, {"published_file_count": 3, "app_exe_exists": True}),
            ), mock.patch.object(
                app_publish_deploy_smoke,
                "create_publish_zip",
                return_value=(0, {"status": "ok", "archived_file_count": 3}),
            ), mock.patch.object(
                app_publish_deploy_smoke,
                "run_app_deploy_smoke",
                return_value=(0, {"status": "ok", "smoke_returncode": 0}),
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = app_publish_deploy_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["status"], "ok")
        self.assertEqual(payload["recovery_action"], "none")
        self.assertEqual(payload["artifact_retention"], "kept")

    def test_main_returns_error_when_publish_fails(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            argv = [
                "run-guest-app-publish-deploy-smoke.py",
                "--work-root",
                temp_root,
            ]

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                app_publish_deploy_smoke,
                "run_dotnet_publish",
                return_value=(1, {"stderr": "publish failed"}),
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = app_publish_deploy_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "app-publish-failed")
        self.assertEqual(payload["recovery_action"], "inspect-publish-step")
        self.assertEqual(payload["transport_blocker"], "dotnet-publish")

    def test_main_returns_error_when_zip_creation_fails(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            argv = [
                "run-guest-app-publish-deploy-smoke.py",
                "--work-root",
                temp_root,
            ]

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                app_publish_deploy_smoke,
                "run_dotnet_publish",
                return_value=(0, {"published_file_count": 3}),
            ), mock.patch.object(
                app_publish_deploy_smoke,
                "create_publish_zip",
                return_value=(1, {"status": "error", "error": "zip failed"}),
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = app_publish_deploy_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "app-publish-zip-failed")
        self.assertEqual(payload["recovery_action"], "inspect-zip-step")
        self.assertEqual(payload["transport_blocker"], "publish-zip")

    def test_main_returns_error_when_deploy_smoke_fails(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            argv = [
                "run-guest-app-publish-deploy-smoke.py",
                "--work-root",
                temp_root,
            ]

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                app_publish_deploy_smoke,
                "run_dotnet_publish",
                return_value=(0, {"published_file_count": 3}),
            ), mock.patch.object(
                app_publish_deploy_smoke,
                "create_publish_zip",
                return_value=(0, {"status": "ok", "archived_file_count": 3}),
            ), mock.patch.object(
                app_publish_deploy_smoke,
                "run_app_deploy_smoke",
                return_value=(
                    1,
                    {
                        "status": "error",
                        "error_kind": "app-startup-crash",
                        "error": "A new crash log was written during the launch smoke window.",
                        "recovery_action": "inspect-app-crash-logs",
                        "transport_blocker": "app-startup-crash",
                    },
                ),
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = app_publish_deploy_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "app-startup-crash")
        self.assertEqual(payload["recovery_action"], "inspect-app-crash-logs")
        self.assertEqual(payload["transport_blocker"], "app-startup-crash")

    def test_main_returns_error_when_deploy_smoke_stdout_is_not_json(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            argv = [
                "run-guest-app-publish-deploy-smoke.py",
                "--work-root",
                temp_root,
            ]

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                app_publish_deploy_smoke,
                "run_dotnet_publish",
                return_value=(0, {"published_file_count": 3}),
            ), mock.patch.object(
                app_publish_deploy_smoke,
                "create_publish_zip",
                return_value=(0, {"status": "ok", "archived_file_count": 3}),
            ), mock.patch.object(
                app_publish_deploy_smoke,
                "run_app_deploy_smoke",
                return_value=(0, {"status": "error", "stdout_parse_error": "invalid json"}),
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = app_publish_deploy_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "guest-app-deploy-smoke-failed")
        self.assertEqual(payload["recovery_action"], "inspect-deploy-smoke-step")
        self.assertEqual(payload["transport_blocker"], "guest-app-deploy-smoke")
        self.assertEqual(payload["deploy_smoke_payload"]["stdout_parse_error"], "invalid json")
