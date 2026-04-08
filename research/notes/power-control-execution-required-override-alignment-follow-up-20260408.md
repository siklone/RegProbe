# Execution-Required Override Alignment Follow-Up

Date: 2026-04-08

## Scope

Connect the retained adjacent `PowerRequestOverride` runtime path hits to the already-proven current-build override lineage.

## Artifacts

- `registry-research-framework/audit/execution-required-override-alignment-20260408.json`
- `registry-research-framework/audit/execution-required-override-alignment-20260408.md`
- `evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/path-hits.csv`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-reglineage-20260408a/stdout.txt`
- `evidence/files/vm-tooling-staging/registry-dumps/power-control-root-20260324-210206/power-control-root.txt`

## Findings

1. The retained docs-first runtime trace shows 15 adjacent accesses under `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride`.
2. The retained wildcard KD lineage pass independently shows current-build override-family symbols:
   - `PopPowerRequestHandleRequestOverrideQueryResponse`
   - `PopPowerRequestOverrideInitialize`
   - `PopUmpoSendPowerRequestOverrideQuery`
   - `PopUmpoSendPowerRequestOverrideCleanup`
3. The retained power-control root dump also contains the `PowerRequestOverride` subtree.

## Interpretation

The adjacent `PowerRequestOverride` path hits are no longer just generic nearby noise. They line up with a visible current-build override lineage that is already present in retained KD evidence and with a persisted subtree in the retained power-control root dump. That makes the remaining gap even narrower: the execution-required pair still lacks an exact runtime read, while the adjacent override flow is now structurally and runtime-backed.
