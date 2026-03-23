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
- registers the `WindowsOptimizerValidationAgent` startup task

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
