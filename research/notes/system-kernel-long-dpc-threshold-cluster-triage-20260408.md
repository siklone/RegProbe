# system.kernel-long-dpc-threshold-cluster triage - 2026-04-08

## Summary

- `LongDpcQueueThreshold` and `LongDpcRuntimeThreshold` now have a canonical schema-backed draft cluster.
- Repo docs explicitly list:
  - `LongDpcQueueThreshold = 3 // KiLongDpcQueueThreshold`
  - `LongDpcRuntimeThreshold = 100 // KiLongDpcRuntimeThreshold`
- The observed clean baseline confirms that `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel` exists while both values are absent.
- The broad current-build string batch gave both values exact Unicode hits in `ntoskrnl.exe`.
- The lightweight `session-manager-kernel` runtime replay later armed both values from missing to `1`, rebooted once, and still kept both candidates in the residual `no-hit` hold.
- Existing source-enrichment converges on the same next lane for both values: `timer-dpc-stress`, low priority, `windbg` queue.

## Source artifacts

- `Docs/system/system.md`
- `research/notes/kernel-power-96-key-routing-20260327.md`
- `research/notes/session-manager-kernel-batch-lightweight-runtime-20260331.md`
- `evidence/files/vm-tooling-staging/session-manager-kernel-batch-lightweight-runtime-primary-20260331-171654/state.json`
- `evidence/files/vm-tooling-staging/registry-batch-existence-96-live-20260329-100629/results.json`
- `evidence/files/vm-tooling-staging/targeted-string-batch-primary-20260331-135356/results.json`
- `evidence/files/vm-tooling-staging/session-manager-kernel-batch-lightweight-runtime-primary-20260331-171654/results.json`
- `registry-research-framework/enrichment/outputs/source-enrichment-20260403-044821/per-key/system.kernel.long-dpc-queue-threshold.json`
- `registry-research-framework/enrichment/outputs/source-enrichment-20260403-044821/per-key/system.kernel.long-dpc-runtime-threshold.json`

## Interpretation

- new proof gained:
  - both values have explicit repo-doc defaults and kernel-global mappings, not just token-only mentions
  - both values have a clean observed-baseline state: parent path exists, values missing
  - both values have exact checked-in-build string hits in `ntoskrnl.exe`
  - both values already survived a broad runtime replay that explicitly armed them from missing to `1` and still stayed `no-hit`
- narrowed conclusion:
  - this is a real docs-first DPC-threshold cluster, but not yet a checked-in-build path-aware kernel lane
  - the evidence is stronger than raw backlog because the names survive into the checked-in build, but still weaker than a real caller, symbolized global, or live query/set path
- next proof path:
  - revisit only when a stronger path-aware clue appears, ideally from a checked-in-build symbol/global, a Ghidra xref, or a targeted `timer-dpc-stress` runtime lane
