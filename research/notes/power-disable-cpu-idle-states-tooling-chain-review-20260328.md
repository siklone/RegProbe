# power.disable-cpu-idle-states tooling chain review - 2026-03-28

## Summary

- A minimal VMware guest tooling smoke was added under `C:\RegProbe-Diag` to test only three things:
  - guest-side script execution
  - guest-side file creation
  - host-side copy-back into the repo evidence root
- A second minimal diagnostic then tested only the raw CPU idle-state registry bundle:
  - write with the PowerShell registry provider
  - write with `reg.exe`
  - read the bundle back
  - restore the bundle to missing
- Both minimal diagnostics succeeded on `baseline-20260327-regprobe-visible-shell-stable`.

## Source artifacts

- Minimal VMware tooling smoke:
  - `evidence/files/vm-tooling-staging/vm-tooling-minimal-diagnostic-20260328-200634/summary.json`
  - `evidence/files/vm-tooling-staging/vm-tooling-minimal-diagnostic-20260328-200634/environment.json`
  - `evidence/files/vm-tooling-staging/vm-tooling-minimal-diagnostic-20260328-200634/script-result.json`
  - `evidence/files/vm-tooling-staging/vm-tooling-minimal-diagnostic-20260328-200634/write-test.txt`
- Minimal direct registry diagnostic:
  - `evidence/files/vm-tooling-staging/cpu-idle-minimal-regwrite-20260328-201526/summary.json`
  - `evidence/files/vm-tooling-staging/cpu-idle-minimal-regwrite-20260328-201526/cpu-idle-minimal-regwrite-result.json`
- Runners:
  - `scripts/vm/run-vm-tooling-minimal-diagnostic.ps1`
  - `scripts/vm/run-cpu-idle-states-minimal-regwrite-diagnostic.ps1`

## Result

- The generic VMware guest execution and host copy-back chain is healthy on this baseline.
- `C:\RegProbe-Diag` is writable from the guest execution primitive used by the minimal smoke.
- The raw CPU idle-state bundle can be written and read back with both:
  - the PowerShell registry provider
  - `reg.exe`
- The direct minimal diagnostic also restored the bundle back to the observed baseline of all three values missing.

## Why this matters

This does not promote `power.disable-cpu-idle-states` and it does not remove the final `Class B` decision gate.

It does close one ambiguity. The remaining problem is no longer "the VM cannot run guest scripts" and it is no longer "the raw bundle cannot be written." The unresolved failure is now narrower:

- the heavier ValidationController-style write diagnostics did not emit guest-side results
- the v3.1 runtime lane still failed during `set-candidate` before reboot
- the corrected benchmark lane still failed in the reboot or shell-return path before workload start

That means the next meaningful escalation is the heavier orchestration itself: WPR, reboot, snapshot-restore timing, or the ValidationController guest wrapper path. Another identical broad "guest execution is broken" hypothesis is no longer supported by the evidence.
