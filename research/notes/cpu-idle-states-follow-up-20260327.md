CPU idle states follow-up on Win25H2Clean

- Record: `power.disable-cpu-idle-states`
- Snapshot: `baseline-20260327-shell-stable`

What this pass added:

- a current-build Ghidra check on `C:/Windows/System32/ntoskrnl.exe`
- a v3.1 behavior-lane benchmark attempt for the raw bundle

## Ghidra follow-up

Two current-build string/xref passes were run on `ntoskrnl.exe`.

Registry-name probe:

- `evidence/files/ghidra/power.disable-cpu-idle-states/cpu-idle-registry-name-ghidra.md`

Internal-symbol probe:

- `evidence/files/ghidra/power.disable-cpu-idle-states/cpu-idle-internal-name-ghidra.md`

Result:

- no matching strings were found for `DisableIdleStatesAtBoot`
- no matching strings were found for `IdleStateTimeout`
- no matching strings were found for `ExitLatencyCheckEnabled`
- no matching strings were found for `PpmIdleDisableStatesAtBoot`
- no matching strings were found for `PpmExitLatencyCheckEnabled`
- no matching strings were found for `PopPepIdleStateTimeout`

So this pass did not produce a current-build kernel decompilation lead for the bundle.

## Rebooted benchmark attempt

Framework manifest:

- `evidence/records/power.disable-cpu-idle-states/behavior-lane.json`

Framework log:

- `evidence/records/power.disable-cpu-idle-states/behavior-lane.log`

What happened:

- the v3.1 behavior lane invoked `scripts/vm/run-cpu-idle-states-benchmark.ps1`
- the bundle write lane started from `baseline-20260327-shell-stable`
- the benchmark pass requested a reboot before any CPU or memory workload ran
- `Explorer` did not come back in time
- the guest later dropped to `not-running`
- recovery required a snapshot revert and a clean VM start
- the host staging folder for `cpu-idle-states-20260327-024350` only contained setup scripts because the run failed before the workload phase

Incident log:

- `research/vm-incidents.json`

This means the first rebooted benchmark attempt did not reach the `WinSAT CPU + WPR` or `WinSAT mem + WPR` workload phases. The useful result from this pass is not a benchmark score. It is the incident itself: on this VM, the raw CPU idle-state bundle is risky enough that the first rebooted benchmark lane broke shell availability and required a snapshot recovery.

## V3.1 runtime lane

Framework manifest:

- `evidence/records/power.disable-cpu-idle-states/runtime-lane.json`

Repo-tracked runtime summary:

- `evidence/files/vm-tooling-staging/cpu-idle-runtime-20260327-072057/summary.json`
- `evidence/files/vm-tooling-staging/cpu-idle-runtime-20260327-072057/cpu-idle-runtime.etl.md`

What happened:

- the v3.1 faz1 runtime lane reverted to `baseline-20260327-shell-stable`
- baseline state was captured with all three bundle values still missing
- the lane started a `WPR GeneralProfile` capture
- the candidate write did not complete; guest execution returned non-zero before any post-boot state was captured
- the ETW placeholder was still written, but the trace did not reach a clean stop/result stage
- the VM was recovered by snapshot revert
- post-recovery shell health was clean again

So the runtime lane improved the evidence shape because it now produced a repo-tracked summary and ETW placeholder under the canonical evidence root, but it still did not produce a positive kernel read or a successful candidate/post-boot capture. The useful result from this pass is again the failure mode itself: on this VM, this raw power bundle is unstable early enough that even the runtime-lane write/reboot sequence can fail before the candidate state is confirmed after reboot.
