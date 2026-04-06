#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
payload_dir="${1:-$repo_root/dist/kvm-bootstrap-payload}"
output_iso="${2:-$repo_root/dist/regprobe-kvm-bootstrap.iso}"
windows_sdk_installer_url="https://go.microsoft.com/fwlink/?linkid=2357925"

rm -rf "$payload_dir"
mkdir -p "$payload_dir/guest-tools" "$payload_dir/repo-scripts/ghidra" "$(dirname "$output_iso")"

cp "$repo_root/scripts/vm-kvm/bootstrap-research-lane.ps1" "$payload_dir/"
cp "$repo_root/scripts/vm/guest-tools/"* "$payload_dir/guest-tools/"
cp "$repo_root/scripts/vm/apply-defender-tooling-exclusions.ps1" "$payload_dir/repo-scripts/"
cp "$repo_root/scripts/vm/tool-health-smoke.ps1" "$payload_dir/repo-scripts/"
cp "$repo_root/scripts/vm/install-dotnet-desktop-runtime.ps1" "$payload_dir/repo-scripts/"
cp "$repo_root/registry-research-framework/tools/pdb-download.ps1" "$payload_dir/repo-scripts/"
cp "$repo_root/scripts/vm/ghidra/"*.java "$payload_dir/repo-scripts/ghidra/"
curl -L --fail --output "$payload_dir/winsdksetup.exe" "$windows_sdk_installer_url" >/dev/null

cat >"$payload_dir/launch-bootstrap.ps1" <<'EOF'
$payloadRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$scriptPath = Join-Path $payloadRoot 'bootstrap-research-lane.ps1'

Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList @(
    '-NoExit',
    '-NoProfile',
    '-ExecutionPolicy', 'Bypass',
    '-File', $scriptPath,
    '-PayloadRoot', $payloadRoot,
    '-SkipGhidra',
    '-StatusWebhook', 'http://10.0.2.2:8787/regprobe-bootstrap'
)
EOF

cat >"$payload_dir/BOOT.cmd" <<'EOF'
@echo off
setlocal
powershell -NoExit -NoProfile -ExecutionPolicy Bypass -File "%~dp0bootstrap-research-lane.ps1" -PayloadRoot "%~dp0." -SkipGhidra -StatusWebhook "http://10.0.2.2:8787/regprobe-bootstrap"
EOF

cat >"$payload_dir/B.cmd" <<'EOF'
@echo off
setlocal
powershell -NoExit -NoProfile -ExecutionPolicy Bypass -File "%~dp0bootstrap-research-lane.ps1" -PayloadRoot "%~dp0." -SkipGhidra -StatusWebhook "http://10.0.2.2:8787/regprobe-bootstrap"
EOF

cat >"$payload_dir/README.txt" <<'EOF'
RegProbe KVM bootstrap media

Run B.cmd or BOOT.cmd from an elevated prompt or double-click one and accept the UAC prompt.
The bootstrap writes its summary to:
  C:\RegProbe-Diag\bootstrap\summary.json
EOF

xorriso -as mkisofs \
  -iso-level 3 \
  -J \
  -joliet-long \
  -V RPBOOT \
  -o "$output_iso" \
  "$payload_dir" >/dev/null

printf '%s\n' "$output_iso"
