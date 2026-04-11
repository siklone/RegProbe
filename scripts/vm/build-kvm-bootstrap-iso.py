#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import shutil
import subprocess
from datetime import datetime, timezone
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
DIST_DIR = REPO_ROOT / "dist"
DEFAULT_OUTPUT = DIST_DIR / "regprobe-kvm-bootstrap.iso"
STAGING_DIR = DIST_DIR / "regprobe-kvm-bootstrap-staging"
LOCAL_INSTALLER = REPO_ROOT / "scripts" / "vm" / "install-guest-validation-agent-local.ps1"
GUEST_AGENT = REPO_ROOT / "scripts" / "vm" / "guest-validation-agent.ps1"
RESTART_HELPER = REPO_ROOT / "scripts" / "vm" / "request-guest-restart.ps1"


README_TEXT = """RegProbe KVM Bootstrap ISO
=========================

Purpose
- Provide a repo-native CD-ROM payload for the libvirt/KVM Windows guest.
- Restore a valid ISO at dist/regprobe-kvm-bootstrap.iso.
- Allow manual in-guest installation of the validation agent files even when vmrun/shared-folder tooling is unavailable.

Contents
- install-guest-validation-agent-local.ps1
- guest-validation-agent.ps1
- request-guest-restart.ps1
- manifest.json

Manual guest steps
1. Open an elevated PowerShell window inside the Windows guest.
2. Change into the mounted CD-ROM drive that contains this ISO.
3. Run:
   powershell -ExecutionPolicy Bypass -File .\\install-guest-validation-agent-local.ps1
4. This installs files under C:\\Tools\\Scripts and prepares C:\\Tools\\ValidationController.
5. It does not register the startup task by default. Add -RegisterStartupTask only when a config workflow exists.
6. If the ISO also contains a qemu guest agent installer, run:
   powershell -ExecutionPolicy Bypass -File .\\install-guest-validation-agent-local.ps1 -InstallQemuGuestAgent

Notes
- A qemu guest agent installer is optional and can be injected at build time.
- The current repo controller stack still assumes vmrun/shared-folder orchestration.
"""


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Build the RegProbe KVM bootstrap ISO.")
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT, help="Output ISO path.")
    parser.add_argument(
        "--qga-installer",
        type=Path,
        default=None,
        help="Optional Windows qemu guest agent installer to include on the ISO.",
    )
    parser.add_argument(
        "--extra",
        action="append",
        default=[],
        help="Optional extra host file to include under extras/ inside the ISO.",
    )
    return parser.parse_args()


def require_file(path: Path) -> None:
    if not path.exists():
        raise FileNotFoundError(f"Required file missing: {path}")


def write_text(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content.rstrip() + "\n", encoding="utf-8")


def copy_file(src: Path, dst: Path) -> None:
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src, dst)


def main() -> int:
    args = parse_args()
    xorriso = shutil.which("xorriso")
    if not xorriso:
        raise RuntimeError("xorriso is required to build the bootstrap ISO.")

    for path in (LOCAL_INSTALLER, GUEST_AGENT, RESTART_HELPER):
        require_file(path)

    if STAGING_DIR.exists():
        shutil.rmtree(STAGING_DIR)
    STAGING_DIR.mkdir(parents=True, exist_ok=True)

    copy_file(LOCAL_INSTALLER, STAGING_DIR / LOCAL_INSTALLER.name)
    copy_file(GUEST_AGENT, STAGING_DIR / GUEST_AGENT.name)
    copy_file(RESTART_HELPER, STAGING_DIR / RESTART_HELPER.name)
    write_text(STAGING_DIR / "README.txt", README_TEXT)

    extras: list[str] = []
    qga_installer_rel = None
    if args.qga_installer:
        qga_path = args.qga_installer.expanduser()
        if not qga_path.exists():
            raise FileNotFoundError(f"QEMU guest agent installer not found: {qga_path}")
        qga_target = STAGING_DIR / "extras" / qga_path.name
        copy_file(qga_path, qga_target)
        qga_installer_rel = qga_target.relative_to(STAGING_DIR).as_posix()
        extras.append(qga_installer_rel)

    for raw in args.extra:
        extra_path = Path(raw).expanduser()
        if not extra_path.exists():
            continue
        target = STAGING_DIR / "extras" / extra_path.name
        copy_file(extra_path, target)
        extras.append(target.relative_to(STAGING_DIR).as_posix())

    manifest = {
        "generated_utc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "output_iso": str(args.output),
        "files": sorted(
            str(path.relative_to(STAGING_DIR)).replace("\\", "/")
            for path in STAGING_DIR.rglob("*")
            if path.is_file()
        ),
        "qga_installer_included": qga_installer_rel,
        "extras_included": extras,
    }
    write_text(STAGING_DIR / "manifest.json", json.dumps(manifest, ensure_ascii=False, indent=2))

    args.output.parent.mkdir(parents=True, exist_ok=True)
    if args.output.exists():
        args.output.unlink()

    subprocess.run(
        [
            xorriso,
            "-as",
            "mkisofs",
            "-quiet",
            "-iso-level",
            "3",
            "-full-iso9660-filenames",
            "-volid",
            "REGPROBE_KVM_BOOTSTRAP",
            "-output",
            str(args.output),
            str(STAGING_DIR),
        ],
        check=True,
    )

    print(args.output)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
