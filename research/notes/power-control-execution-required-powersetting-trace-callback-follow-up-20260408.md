# Execution-Required Power-Setting Trace Callback Follow-Up

Date: 2026-04-08

## Scope

Narrow the retained generic power-setting setter path for the execution-required pair.

## Artifacts

- `registry-research-framework/audit/execution-required-powersetting-trace-callback-20260408.json`
- `registry-research-framework/audit/execution-required-powersetting-trace-callback-20260408.md`
- `evidence/files/vm-tooling-staging/local-kd-powersetting-query-20260408a/stdout.txt`

## Findings

1. The retained `PopSetPowerSettingValue` disassembly still shows the generic power-setting layer doing in-memory/configuration work:
   - `PopFindPowerSettingConfiguration`
   - `PopSetNotificationWork`
2. The visible callback registration site in that same retained disassembly is:
   - `lea r8,[nt!PopTracePowerSettingChange]`
   - `call nt!PoRegisterPowerSettingCallback`
3. No retained generic setter evidence in this lane exposes an exact `AllowSystemRequiredPowerRequests` or `AllowAudioToEnableExecutionRequiredPowerRequests` binding site.

## Interpretation

This keeps the execution-required pair out of the visible generic setter path. The retained current-build `PopSetPowerSettingValue` evidence still looks like in-memory configuration plus notification/trace plumbing, not pair-specific binding. That pushes the unresolved part earlier: seeding or exact registration of the pair still has to happen before, outside, or deeper than the visible generic setter surface.
