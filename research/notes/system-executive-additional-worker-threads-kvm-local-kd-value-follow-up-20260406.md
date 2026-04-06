# System Executive Additional Worker Threads KVM Local KD Value Follow-up

Date: 2026-04-06
Candidate: `system.executive-additional-worker-threads`
Guest: `regprobe-win11-25h2-session`

## Objective
- verify the live values behind the already-resolved `ExpAdditionalCriticalWorkerThreads` and `ExpAdditionalDelayedWorkerThreads` data symbols
- check whether the running KVM guest still carries the same zero baseline seen in the earlier clean export

## Result
- the host-driven local-KD helper attached successfully to the live guest kernel and completed without timing out
- `symchk.exe` returned `0`, the debugger query completed, and `x nt!*Exp*Additional*Worker*` again resolved both tracked data symbols in the running guest
- `dd nt!ExpAdditionalDelayedWorkerThreads L2` then returned `fffff800\`97afa4a4  00000000 00000000`
- that live dump matches the adjacent delayed and critical worker-thread globals, so the current working guest still carries `0 / 0` at the exact data-symbol pair

## Artifacts
- `evidence/files/vm-tooling-staging/local-kd-executive-values-20260406a/local-kd-executive-values-20260406a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-executive-values-20260406a/local-kd-executive-values-20260406a.log`
- `evidence/files/vm-tooling-staging/local-kd-executive-values-20260406a/local-kd-executive-values-20260406a.stdout.txt`
- `evidence/files/vm-tooling-staging/local-kd-executive-values-20260406a/local-kd-executive-values-20260406a.txt`

## Short Take
- KVM local KD now confirms that the running guest still holds the Executive worker-thread pair at `0 / 0` at the exact live `ExpAdditional*` globals
- this lines up with the earlier clean baseline export and reduces doubt that the KVM working guest drifted away from the documented baseline
- it does not replace the already decisive ETW runtime-read proof for promotion
