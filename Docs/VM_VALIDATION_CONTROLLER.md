# VM Validation Controller

This document defines the controller/agent validation loop for runtime registry experiments in the `Win25H2Clean` VM.

## Purpose

Use the VM as a safe discovery environment for:

- single-value registry experiments
- reboot-sensitive validation
- benchmark runs with structured feedback
- reversible test cycles

The controller runs on the host.
The agent runs in the guest.

## Files

- Host controller:
  - `scripts/vm/host-validation-controller.ps1`
- Guest agent:
  - `scripts/vm/guest-validation-agent.ps1`
- Guest installer:
  - `scripts/vm/install-guest-validation-agent.ps1`

## Feedback Model

The guest agent writes structured phase changes to the shared-folder controller workspace:

- `BOOT_START`
- `BASELINE_CAPTURED`
- `VALUE_APPLIED`
- `RESTART_AFTER_APPLY`
- `POST_REBOOT_AFTER_APPLY`
- `IDLE_REACHED`
- `BENCH_START`
- `BENCH_DONE`
- `RESTORE_DONE`
- `RESTART_AFTER_RESTORE`
- `POST_REBOOT_AFTER_RESTORE`
- `COMPLETE`
- `ERROR`

The host controller polls `status.json` and prints short live feedback:

- `started`
- `live`
- `done`
- `blocked`
- `next`

## Artifacts

Each test writes to:

- `config.json`
- `status.json`
- `result.json`
- `agent.log`
- `artifacts/benchmark-run-XX.stdout.txt`
- `artifacts/benchmark-run-XX.stderr.txt`
- `artifacts/benchmark-run-XX.perf.csv`

## Baseline Cycle

Each test is independent:

1. capture baseline
2. apply one candidate value
3. reboot if required
4. wait for system idle
5. run warmup and measured benchmark passes
6. restore baseline
7. reboot again if required

Do not chain values cumulatively.
Every candidate should start from a clean baseline.

## Install The Guest Agent

Run from the host:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\vm\install-guest-validation-agent.ps1
```

This:

- copies the guest agent to `C:\Tools\Scripts\guest-validation-agent.ps1`
- registers the `RegProbeValidationAgent` startup task

## KVM Manual Bootstrap

When VMware `vmrun` guest control is unavailable, build the KVM bootstrap ISO on the host:

```bash
python3 scripts/vm/build-kvm-bootstrap-iso.py
```

This restores:

- `dist/regprobe-kvm-bootstrap.iso`

Inside the Windows guest, mount the ISO and run:

```powershell
powershell -ExecutionPolicy Bypass -File .\install-guest-validation-agent-local.ps1
```

That guest-local installer:

- copies `guest-validation-agent.ps1` and `request-guest-restart.ps1` into `C:\Tools\Scripts`
- prepares `C:\Tools\ValidationController`
- creates a local launch helper
- does **not** register the startup task unless you pass `-RegisterStartupTask`

If you also have a Windows qemu guest agent installer on the host, include it when building:

```bash
python3 scripts/vm/build-kvm-bootstrap-iso.py --qga-installer /absolute/path/to/qemu-ga-x86_64.msi
```

Then inside the guest you can run:

```powershell
powershell -ExecutionPolicy Bypass -File .\install-guest-validation-agent-local.ps1 -InstallQemuGuestAgent
```

From the Linux host you can also verify that the libvirt domain exposes the qemu guest-agent channel:

```bash
python3 scripts/vm/ensure-kvm-qga-channel.py --emit-json
```

If that reports `present_after=true` but `guest_agent_connected=false`, the channel exists and the remaining gap is the Windows guest-agent install/service inside the guest.

Use this as a manual recovery/bootstrap surface for KVM. The repo controller itself still assumes `vmrun` and the shared-folder model.

## Run A Test

Example:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\vm\host-validation-controller.ps1 `
  -TestId 'example.case' `
  -RegistryPath 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel' `
  -ValueName 'ExampleValue' `
  -ValueType 'DWord' `
  -CandidateValue 1 `
  -BenchmarkCommand 'winsat mem' `
  -RestartMode reboot
```

## Notes

- The controller is responsible for orchestration and feedback.
- The guest agent is responsible for applying the value, waiting for idle, benchmarking, and restoring the baseline.
- VM results are a discovery signal, not final truth for hardware-sensitive settings.
- Promising candidates should still be rechecked on bare metal.
