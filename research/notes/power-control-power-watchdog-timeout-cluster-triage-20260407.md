# power.control.power-watchdog-timeout-cluster triage - 2026-04-07

## Summary

- The `Control\Power` `PowerWatchdog*TimeoutMsec` cluster remains a docs-first hold, but it is no longer an unstructured backlog.
- Repo docs explicitly list five default values under `InitializePowerWatchdogTimeoutDefaults`:
  - `PowerWatchdogDrvSetMonitorTimeoutMsec = 10000`
  - `PowerWatchdogDwmSyncFlushTimeoutMsec = 30000`
  - `PowerWatchdogPoCalloutTimeoutMsec = 10000`
  - `PowerWatchdogPowerOnGdiTimeoutMsec = 30000`
  - `PowerWatchdogRequestQueueTimeoutMsec = 30000`
- The observed clean baseline confirms that `HKLM\SYSTEM\CurrentControlSet\Control\Power` exists while all five registry values are absent.
- The broad current-build string batch gave all five values a clean `no-hit` result, so this family still lacks a current-build path-aware kernel pivot.
- Existing enrichment already points them to the same next runtime family: `power-request-simulation`, low priority, `windbg` queue.

## Source artifacts

- `Docs/power/power.md`
- `research/notes/kernel-power-96-key-routing-20260327.md`
- `research/notes/kernel-power-96-broad-targeted-string-follow-up-20260331.md`
- `evidence/files/vm-tooling-staging/registry-batch-existence-96-live-20260329-100629/results.json`
- `evidence/files/vm-tooling-staging/targeted-string-batch-primary-20260331-135356/results.json`
- `registry-research-framework/enrichment/outputs/source-enrichment-20260403-044821/per-key/power.control.power-watchdog-*.json`

## Interpretation

- new proof gained:
  - the family has explicit repo-doc defaults, not just name-only mentions
  - the family has a clean observed-baseline state: parent path exists, values missing
  - the family has a clean current-build no-hit structural result, not a mixed or ambiguous string story
- narrowed conclusion:
  - this is a real docs-first watchdog-default cluster, but it is still below the line for aggressive RE because the current build has not yet provided a symbol/string/caller pivot comparable to `Win32kCalloutWatchdogTimeoutSeconds`
- next proof path:
  - only revisit when a stronger path-aware clue appears, ideally from a current-build symbol/global, a Ghidra string hit, or a live runtime/read lane tied to `power-request-simulation`
