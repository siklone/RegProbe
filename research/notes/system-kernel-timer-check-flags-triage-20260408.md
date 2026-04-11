# system.kernel.timer-check-flags triage + KD follow-up - 2026-04-08

## Summary

- `TimerCheckFlags` is now a canonical `Session Manager\Kernel` draft candidate.
- Repo docs assign it an explicit default of `1` and tie it to `KeTimerCheckFlags`.
- The observed clean baseline confirms that the parent `Session Manager\Kernel` path exists while the registry value is absent.
- The broad current-build string batch found an exact Unicode hit for `TimerCheckFlags` in `ntoskrnl.exe`.
- Later WRK-backed source-enrichment retained:
  - `extern ULONG KeTimerCheckFlags;`
  - `ULONG KeTimerCheckFlags = KE_TIMER_CHECK_FREES;`
  - `if ((KeTimerCheckFlags & KE_TIMER_CHECK_FREES) == 0) {`
- The earlier tools-hardened lightweight runtime batch wrote the value as `1`, rebooted once, preserved shell health, and still produced `no-hit` runtime output for this specific value.
- A dedicated live KVM local-KD bundle now resolves `nt!KeTimerCheckFlags` and directly reads its current-build value as `1`.

## Source artifacts

- `Docs/system/system.md`
- `research/notes/kernel-power-96-key-routing-20260327.md`
- `evidence/files/vm-tooling-staging/registry-batch-existence-96-live-20260329-100629/results.json`
- `evidence/files/vm-tooling-staging/targeted-string-batch-primary-20260331-135356/results.json`
- `evidence/files/vm-tooling-staging/session-manager-kernel-batch-lightweight-runtime-primary-20260331-171654/state.json`
- `evidence/files/vm-tooling-staging/session-manager-kernel-batch-lightweight-runtime-primary-20260331-171654/results.json`
- `registry-research-framework/enrichment/outputs/source-enrichment-20260403-192135/master-enrichment.json`
- `evidence/files/vm-tooling-staging/local-kd-timercheckflags-20260408a/local-kd-timercheckflags-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-timercheckflags-20260408a/local-kd-timercheckflags-20260408a.log`

## Interpretation

- current evidence shape:
  - explicit repo-doc default
  - observed baseline-missing registry state
  - exact current-build kernel string hit
  - WRK semantic source retention for initializer and bit-test behavior
  - runtime write-and-reboot lane with no exact read
  - dedicated live current-build KD proof that `KeTimerCheckFlags = 1`
- narrowed conclusion:
  - `TimerCheckFlags` is stronger than a pure docs-first backlog item
  - the value belongs in the canonical research set as a live-state-confirmed timer-diagnostic kernel draft
- still unresolved:
  - a current-build reader or seeding caller
  - whether the WRK `KE_TIMER_CHECK_FREES` mapping is still semantically intact on modern builds
- next proof path:
  - isolate the direct current-build reader path before spending more time on broad runtime capture
  - decide whether a narrow timer-heavy live lane can surface an exact read without depending on the current Procmon export-blocked path
