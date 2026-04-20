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


class VmKvmAppPublishDeploySmokeTests(unittest.TestCase):
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
                return_value=(1, {"status": "error", "error_kind": "guest-app-smoke-failed"}),
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = app_publish_deploy_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "guest-app-deploy-smoke-failed")
        self.assertEqual(payload["recovery_action"], "inspect-deploy-smoke-step")
        self.assertEqual(payload["transport_blocker"], "guest-app-deploy-smoke")

