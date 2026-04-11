# KVM Local KD Breakpoint Limit Follow-up

Date: 2026-04-06
Transport: `regprobe-win11-25h2-session` local KD on KVM

## Objective
- test whether the new KVM local-KD lane can graduate from inspect-only symbol queries to breakpoint-and-go arbiter work on the live guest
- combine live local-KD with a UUID trigger burst to see whether `ExpUuidLoadSequenceNumber` or `ExpUuidSaveSequenceNumber` can be trapped directly

## Result
- the host-driven local-KD helper attached successfully and the trigger command executed with exit code `0`
- the current local-KD transport still rejected both `bp nt!ExpUuidLoadSequenceNumber ...` and `bp nt!ExpUuidSaveSequenceNumber ...`
- the debugger returned `Operation not supported by current debuggee` for both breakpoint commands
- a subsequent `g` command returned `No runnable debuggees`

## Artifacts
- `evidence/files/vm-tooling-staging/local-kd-uuid-breakpoint-20260406a/local-kd-uuid-breakpoint-20260406a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-uuid-breakpoint-20260406a/local-kd-uuid-breakpoint-20260406a.log`
- `evidence/files/vm-tooling-staging/local-kd-uuid-breakpoint-20260406a/local-kd-uuid-breakpoint-20260406a.stdout.txt`
- `evidence/files/vm-tooling-staging/local-kd-uuid-breakpoint-20260406a/local-kd-uuid-breakpoint-20260406a.trigger.stdout.txt`

## Short Take
- KVM local KD is currently good for inspect-only symbol, disassembly, and memory-string work
- it is not yet a breakpoint-capable or run-control-capable arbiter transport on this guest
- keep `Hyper-V debug family` as the planned long-term breakpoint lane; treat current KVM local KD as an inspection transport, not a full debugger-first replacement
