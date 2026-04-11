# VM Validation Controller

This document defines the controller/agent validation loop for runtime registry experiments in the `Win25H2Clean` VM.

For the Linux/KVM runtime family, the same research intent applies but the transport is different:

- host transport: `scripts/vm-kvm/serve-guest-bridge.py`
- host admin-shell recovery: `scripts/vm-kvm/ensure-guest-admin-shell.py`
- guest command injection: `scripts/vm-kvm/type-to-guest.py`
- host-side quoted Procmon replay runner: `scripts/vm-kvm/run-guest-registry-policy-probe.py`
- guest bootstrap payload: `scripts/vm-kvm/build-research-bootstrap-iso.sh`
- host health audit: `scripts/vm-kvm/validate-research-lane.py`

The current KVM lane does not depend on the VMware shared-folder controller loop. It stages guest scripts through the bootstrap ISO and uses the bridge for short command delivery, copy-back, and health evidence upload.
Current host runners also reopen an elevated guest PowerShell session on demand before they stage guest helpers, so the visible console no longer needs to be kept manually in an admin state between runs.
KVM host runners now treat `error_kind`, `recovery_action`, `transport_blocker`, and `guest_health` as mandatory summary fields even for synthesized timeout or stage-fallback summaries.

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
- On KVM, keep the guest visible and prefer short, restartable guest commands; long monolithic typed payloads are more fragile than ISO-staged helpers plus bridge uploads.
