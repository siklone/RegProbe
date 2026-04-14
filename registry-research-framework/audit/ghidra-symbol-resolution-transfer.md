# Ghidra Symbol Resolution Transfer

- Transfer status: `ready`
- Operator blocker: `transfer-pack-ready`
- Next action: `Copy the listed repo files and use the exported commands on the destination KVM-capable host.`
- Selected jobs: `5`
- Candidate count: `1`
- Required repo files: `9`
- Missing repo files: `0`

## Transfer Jobs

- `ghidra-symbol-01-kernelbase-dll-0x2e436` -> `KernelBase.dll` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\KernelBase.dll --output-name ghidra-symbolized-kernelbase-dll-0x2e436-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests --module-offset KernelBase.dll+0x2E436 --module-offset KernelBase.dll+0x2EDAB --module-offset KernelBase.dll+0x30AAD`
- `ghidra-symbol-04-kernel32-dll-0x2e8d7` -> `kernel32.dll` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\kernel32.dll --output-name ghidra-symbolized-kernel32-dll-0x2e8d7-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests --module-offset kernel32.dll+0x2E8D7`
- `ghidra-symbol-05-ntdll-dll-0x161d74` -> `ntdll.dll` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntdll.dll --output-name ghidra-symbolized-ntdll-dll-0x161d74-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests --module-offset ntdll.dll+0x161D74 --module-offset ntdll.dll+0x8C48C`
- `ghidra-symbol-07-ntoskrnl-exe-0x327b4d` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-ntoskrnl-exe-0x327b4d-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests --module-offset ntoskrnl.exe+0x327B4D --module-offset ntoskrnl.exe+0x3ED794 --module-offset ntoskrnl.exe+0x3EDD84 --module-offset ntoskrnl.exe+0x6BE358 --module-offset ntoskrnl.exe+0x87108C --module-offset ntoskrnl.exe+0xAE49F6`
- `ghidra-symbol-13-reg-exe-0x128b` -> `reg.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\reg.exe --output-name ghidra-symbolized-reg-exe-0x128b-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests --module-offset reg.exe+0x128B --module-offset reg.exe+0x379D --module-offset reg.exe+0x65C6 --module-offset reg.exe+0x6775`

## Required Repo Paths

- `scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py`
- `scripts/vm-kvm/ensure-guest-admin-shell.py`
- `scripts/vm-kvm/type-to-guest.py`
- `scripts/vm-kvm/guest_bridge.py`
- `scripts/vm-kvm/summary_contract_lib.py`
- `scripts/vm/guest-tools/run-ghidra-symbolized-probe.ps1`
- `scripts/vm/guest-tools/ghidra-headless.cmd`
- `scripts/vm/ghidra/ExportBranchAnalysis.java`
- `scripts/vm/ghidra/SetPdbSymbolRepository.java`

## Missing Repo Paths

- none
