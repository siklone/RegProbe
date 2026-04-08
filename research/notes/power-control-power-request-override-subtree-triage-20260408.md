# Power Request Override Subtree Triage - 2026-04-08

This slice splits `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride` out from the execution-required pair lane and treats it as its own subtree candidate.

Retained artifacts used:

- runtime path-hit trace: `evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/path-hits.csv`
- retained root dump: `evidence/files/vm-tooling-staging/registry-dumps/power-control-root-20260324-210206/power-control-root.txt`
- retained wildcard KD lineage: `evidence/files/vm-tooling-staging/local-kd-powerrequest-reglineage-20260408a/stdout.txt`
- canonical audit: `registry-research-framework/audit/power-request-override-runtime-audit-20260408.json`

Observed result:

- the retained root dump contains `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride`
- the retained current-build path trace shows `15` subtree hits, all from `svchost.exe`
- operation mix is `RegOpenKey=9`, `RegQueryKey=3`, `RegCloseKey=3`
- observed paths are the subtree root plus `Driver`, `Process`, and `Service`
- the leaf probes in the retained runtime capture land as `NAME NOT FOUND`
- the retained wildcard KD lineage exposes `PopPowerRequestHandleRequestOverrideQueryResponse`, `PopPowerRequestOverrideInitialize`, `PopUmpoSendPowerRequestOverrideQuery`, and `PopUmpoSendPowerRequestOverrideCleanup`

Conclusion: `PowerRequestOverride` is no longer just adjacent noise around the execution-required pair. It has its own persisted subtree, current-build runtime access, and visible override-family kernel lineage. The lane should be tracked as a standalone draft subtree candidate. The remaining blocker is narrower: exact leaf semantics and a bounded static path are still unresolved.
