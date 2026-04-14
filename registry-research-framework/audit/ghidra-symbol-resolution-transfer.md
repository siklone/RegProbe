# Ghidra Symbol Resolution Transfer

- Transfer status: `ready`
- Operator blocker: `transfer-pack-ready`
- Next action: `Copy the listed repo files and use the exported commands on the destination KVM-capable host.`
- Selected jobs: `16`
- Candidate count: `1`
- Required repo files: `9`
- Missing repo files: `0`

## Transfer Jobs

- `ghidra-symbol-01-ntoskrnl-exe-0x7ff74fcf128b` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-01-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-02-ntoskrnl-exe-0x7ff74fcf379d` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-02-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-03-ntoskrnl-exe-0x7ff74fcf65c6` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-03-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-04-ntoskrnl-exe-0x7ff74fcf6775` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-04-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-05-ntoskrnl-exe-0x7ff9a412e436` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-05-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-06-ntoskrnl-exe-0x7ff9a412edab` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-06-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-07-ntoskrnl-exe-0x7ff9a4130aad` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-07-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-08-ntoskrnl-exe-0x7ff9a5ebe8d7` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-08-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-09-ntoskrnl-exe-0x7ff9a772c48c` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-09-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-10-ntoskrnl-exe-0x7ff9a7801d74` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-10-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-11-ntoskrnl-exe-0xfffff803c3f27b4d` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-11-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-12-ntoskrnl-exe-0xfffff803c3fed794` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-12-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-13-ntoskrnl-exe-0xfffff803c3fedd84` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-13-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-14-ntoskrnl-exe-0xfffff803c42be358` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-14-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-15-ntoskrnl-exe-0xfffff803c447108c` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-15-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-16-ntoskrnl-exe-0xfffff803c46e49f6` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-16-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`

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
