# Ghidra Transfer Pack Execution Run

- Execution run status: `ready`
- Mode: `dry-run`
- Operator blocker: `execution-run-ready`
- Next action: `Review the dry-run commands, then rerun with --execute on a KVM-capable host.`
- Import root: `registry-research-framework/audit/ghidra-autotrigger-smoke/ghidra-symbol-resolution-transfer-pack-import`
- Planned jobs: `4`
- Ready jobs: `4`
- Blocked jobs: `0`
- Executed jobs: `0`

## Jobs

- `ghidra-symbol-01-ntoskrnl-exe-syntheticpower-control-allow-audio-to-enable-execution-required-power-requestsresolver`
  cwd: `registry-research-framework/audit/ghidra-autotrigger-smoke/ghidra-symbol-resolution-transfer-pack-import`
  command: `python3 repo/scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path 'C:\Windows\System32\ntoskrnl.exe' --output-name ghidra-symbolized-01-power-control-allow-audio-to-enable-execution-required-power-requests --pattern AllowAudioToEnableExecutionRequiredPowerRequests`
- `ghidra-symbol-02-ntoskrnl-exe-0x1920`
  cwd: `registry-research-framework/audit/ghidra-autotrigger-smoke/ghidra-symbol-resolution-transfer-pack-import`
  command: `python3 repo/scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path 'C:\Windows\System32\ntoskrnl.exe' --output-name ghidra-symbolized-02-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-03-ntoskrnl-exe-0x2c80`
  cwd: `registry-research-framework/audit/ghidra-autotrigger-smoke/ghidra-symbol-resolution-transfer-pack-import`
  command: `python3 repo/scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path 'C:\Windows\System32\ntoskrnl.exe' --output-name ghidra-symbolized-03-system-kernel-dpc-watchdog-profile-cluster --pattern DpcWatchdogProfileBufferSizeBytes`
- `ghidra-symbol-04-ntoskrnl-exe-0xfffff80512340002`
  cwd: `registry-research-framework/audit/ghidra-autotrigger-smoke/ghidra-symbol-resolution-transfer-pack-import`
  command: `python3 repo/scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path 'C:\Windows\System32\ntoskrnl.exe' --output-name ghidra-symbolized-04-power-session-watchdog-timeouts --pattern WatchdogResumeTimeout`

## Blockers

- none
