# System Executive Additional Worker Threads KVM Local KD Symbol Follow-up

Date: 2026-04-06
Candidate: `system.executive-additional-worker-threads`
Guest: `regprobe-win11-25h2-session`

## Objective
- verify that the host-driven KVM local-KD lane can resolve the exact live kernel data symbols that match the Executive worker-thread pair
- check whether the supporting ReactOS variable names line up with the current-build guest kernel and not just earlier string-only or fallback evidence

## Result
- the host-driven local-KD helper attached successfully to the live guest kernel and completed without timing out
- `symchk.exe` returned `0`, the debugger query completed, and `x nt!*Exp*Additional*Worker*` resolved both tracked data symbols in the running guest
- the live kernel reported `fffff800\`97afa4a8 nt!ExpAdditionalCriticalWorkerThreads = <no type information>`
- the live kernel reported `fffff800\`97afa4a4 nt!ExpAdditionalDelayedWorkerThreads = <no type information>`

## Artifacts
- `evidence/files/vm-tooling-staging/local-kd-executive-worker-wildcard-20260406a/local-kd-executive-worker-wildcard-20260406a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-executive-worker-wildcard-20260406a/local-kd-executive-worker-wildcard-20260406a.log`
- `evidence/files/vm-tooling-staging/local-kd-executive-worker-wildcard-20260406a/local-kd-executive-worker-wildcard-20260406a.stdout.txt`
- `evidence/files/vm-tooling-staging/local-kd-executive-worker-wildcard-20260406a/local-kd-executive-worker-wildcard-20260406a.txt`

## Short Take
- KVM local KD now confirms the exact `ExpAdditionalCriticalWorkerThreads` and `ExpAdditionalDelayedWorkerThreads` data-symbol pair in the live guest kernel
- this tightens the current-build symbol story behind the earlier ReactOS-adjacent hypothesis and string-only evidence
- it does not replace the already decisive lightweight ETW read, and it does not by itself create a new registry-read proof
