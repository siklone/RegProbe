# Power-Control Docs-First Trigger ETW Follow-Up (2026-03-29)

- Goal: try the cheaper trigger-based ETW path before escalating the remaining docs-first power-control candidates to WinDbg.
- Snapshot: `RegProbe-Baseline-Clean-20260329`
- Tool: `registry-research-framework/tools/run-power-control-docs-first-trigger-etw-capture.ps1`
- Scope in this turn: the five remaining `Class C` records
  - `Class1InitialUnparkCount`
  - `HibernateEnabledDefault`
  - `MfBufferingThreshold`
  - `PerfCalculateActualUtilization`
  - `TimerRebaseThresholdOnDripsExit`

## What Landed

- A dedicated guest-processed ETW trigger runner now exists for this family.
- The runner uses candidate-specific triggers and keeps only guest-processed summaries and filtered ETW text on-repo.
- Multiple hardening passes were needed before the host/guest orchestration stopped failing immediately:
  - empty-summary JSON write fix
  - robust null-safe stdout/stderr capture
  - guest command readiness probe switched to PowerShell
  - `vmrun list` added to the ready-state gate so a powered-off VM does not look healthy
  - `runProgramInGuest` failure made non-fatal so guest-side artifacts can still be copied if the payload completed

## Useful Partial Result

The first materially useful probe root from this ETW family is:

- `evidence/files/vm-tooling-staging/power-control-docs-first-trigger-etw-20260329-184522`

### `Class1InitialUnparkCount`

- Shell before capture was healthy.
- The guest invocation then lost VMware Tools before the copy-back stage.
- No guest summary or filtered ETW artifacts were recovered to the repo.
- Current outcome: `copy-incomplete`, not a usable runtime read result.

### `HibernateEnabledDefault`

- Shell before and after capture stayed healthy.
- The trigger itself immediately showed an environment limit:
  - `powercfg /hibernate on` returned `The request is not supported.`
  - firmware on the current VMware baseline does not support hibernation
- The filtered ETW artifact that did copy back contained only the `EventTrace` header line and no exact registry value activity.
- Current outcome: no exact runtime read on this baseline, with the extra finding that the intended hibernation trigger is not supported here.

## What Did Not Become Repo-Truth Yet

- The full 5-key ETW trigger batch never stabilized enough to produce one clean batch-level summary covering every remaining candidate.
- A later single-candidate pass for `PerfCalculateActualUtilization` timed out before a useful repo-tracked summary materialized.
- Because of that, the remaining 5 records are not updated in classification this turn.

## Interpretation

- The cheap trigger-based ETW path is still the right order before WinDbg, but on the current VMware baseline it is not yet promotion-grade.
- It already produced one meaningful negative result:
  - `HibernateEnabledDefault` does not get a real hibernation trigger on this baseline because hibernation itself is unsupported.
- It also exposed a separate orchestration problem:
  - long guest ETW runs can still lose VMware Tools during or right after capture, which breaks copy-back even when the trigger path itself was valid.

## Next Step

The next justified move is one of:

1. tighten the ETW runner again so guest summary copy-back survives long traces, then rerun the remaining candidates individually; or
2. escalate the remaining five candidates to a WinDbg/kernel-breakpoint lane if we want decisive early-boot or condition-bound reads instead of more orchestration work.
