CPU idle states follow-up on Win25H2Clean

- Record: `power.disable-cpu-idle-states`
- Snapshot: `baseline-20260325-shell-stable`

What this pass added:

- a current-build Ghidra check on `C:/Windows/System32/ntoskrnl.exe`
- a rebooted benchmark attempt for the raw bundle

## Ghidra follow-up

Two current-build string/xref passes were run on `ntoskrnl.exe`.

Registry-name probe:

- `research/evidence-files/ghidra/power.disable-cpu-idle-states/cpu-idle-registry-name-ghidra.md`

Internal-symbol probe:

- `research/evidence-files/ghidra/power.disable-cpu-idle-states/cpu-idle-internal-name-ghidra.md`

Result:

- no matching strings were found for `DisableIdleStatesAtBoot`
- no matching strings were found for `IdleStateTimeout`
- no matching strings were found for `ExitLatencyCheckEnabled`
- no matching strings were found for `PpmIdleDisableStatesAtBoot`
- no matching strings were found for `PpmExitLatencyCheckEnabled`
- no matching strings were found for `PopPepIdleStateTimeout`

So this pass did not produce a current-build kernel decompilation lead for the bundle.

## Rebooted benchmark attempt

Script:

- `scripts/vm/run-cpu-idle-states-benchmark.ps1`

Host artifact root:

- `research/evidence-files/vm-tooling-staging/cpu-idle-states-20260327-002948`

What happened:

- the bundle write lane started from `baseline-20260325-shell-stable`
- the benchmark pass requested a reboot before any CPU or memory workload ran
- `Explorer` did not come back in time
- the guest later dropped to `not-running`
- recovery required a snapshot revert and a clean VM start

Incident log:

- `research/vm-incidents.json`

This means the first rebooted benchmark attempt did not reach the `WinSAT CPU + WPR` or `WinSAT mem + WPR` workload phases. The useful result from this pass is not a benchmark score. It is the incident itself: on this VM, the raw CPU idle-state bundle is risky enough that the first rebooted benchmark lane broke shell availability and required a snapshot recovery.
