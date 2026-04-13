# Ghidra Transfer Pack Execution Plan

- Execution plan status: `ready`
- Operator blocker: `execution-plan-ready`
- Next action: `Run the destination_command values from the imported pack root on the KVM-capable host.`
- Import root: `registry-research-framework/audit/ghidra-autotrigger-smoke/ghidra-symbol-resolution-transfer-pack-import`
- Ready jobs: `4`
- Blocked jobs: `0`
- Candidate count: `4`

## Ready Jobs

- `ghidra-symbol-01-ntoskrnl-exe-syntheticpower-control-allow-audio-to-enable-execution-required-power-requestsresolver`
  command: `python3 repo/scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path 'C:\Windows\System32\ntoskrnl.exe' --output-name ghidra-symbolized-01-power-control-allow-audio-to-enable-execution-required-power-requests --pattern AllowAudioToEnableExecutionRequiredPowerRequests`
- `ghidra-symbol-02-ntoskrnl-exe-0x1920`
  command: `python3 repo/scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path 'C:\Windows\System32\ntoskrnl.exe' --output-name ghidra-symbolized-02-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-03-ntoskrnl-exe-0x2c80`
  command: `python3 repo/scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path 'C:\Windows\System32\ntoskrnl.exe' --output-name ghidra-symbolized-03-system-kernel-dpc-watchdog-profile-cluster --pattern DpcWatchdogProfileBufferSizeBytes`
- `ghidra-symbol-04-ntoskrnl-exe-0xfffff80512340002`
  command: `python3 repo/scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path 'C:\Windows\System32\ntoskrnl.exe' --output-name ghidra-symbolized-04-power-session-watchdog-timeouts --pattern WatchdogResumeTimeout`

## Errors

- none
