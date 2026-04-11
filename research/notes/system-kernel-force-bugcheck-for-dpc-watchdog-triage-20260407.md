# system.kernel.force-bugcheck-for-dpc-watchdog triage + KD follow-up - 2026-04-07

## Summary

- `ForceBugcheckForDpcWatchdog` is a strong `Session Manager\Kernel` draft candidate.
- Repo docs assign it an explicit default of `0` and tie it to `KiForceBugcheckForDpcWatchdog`.
- The observed clean baseline confirms that the parent `Session Manager\Kernel` path exists while the registry value is absent.
- The broad current-build string batch found an exact Unicode hit for `ForceBugcheckForDpcWatchdog` in `ntoskrnl.exe`.
- The earlier tools-hardened lightweight runtime batch wrote the value as `1`, rebooted once, preserved shell health, and still produced `no-hit` runtime trace output for this specific value.
- A dedicated live KVM local-KD bundle now resolves `nt!KiForceBugcheckForDpcWatchdog` and directly reads its current-build live value as `0`.

## Source artifacts

- `Docs/system/system.md`
- `research/notes/kernel-power-96-key-routing-20260327.md`
- `research/notes/kernel-power-96-broad-targeted-string-follow-up-20260331.md`
- `research/notes/session-manager-kernel-batch-lightweight-runtime-20260331.md`
- `evidence/files/vm-tooling-staging/registry-batch-existence-96-live-20260329-100629/results.json`
- `evidence/files/vm-tooling-staging/targeted-string-batch-primary-20260331-135356/results.json`
- `evidence/files/vm-tooling-staging/session-manager-kernel-batch-lightweight-runtime-primary-20260331-171654/state.json`
- `evidence/files/vm-tooling-staging/session-manager-kernel-batch-lightweight-runtime-primary-20260331-171654/results.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-force-bugcheck-kd-20260407a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-force-bugcheck-kd-20260407a/dpc-watchdog-force-bugcheck-kd-20260407a.stdout.txt`
- `evidence/files/vm-tooling-staging/dpc-watchdog-force-bugcheck-kd-20260407a/host-review.json`

## Interpretation

- current evidence shape:
  - explicit repo-doc default
  - observed baseline-missing registry state
  - exact current-build kernel string hit
  - runtime write-and-reboot lane with no exact read
  - dedicated live current-build kernel symbol and value proof (`KiForceBugcheckForDpcWatchdog = 0`)
- next proof path:
  - identify a current-build reader or seeding caller for `KiForceBugcheckForDpcWatchdog`
  - decide whether the registry value is an active override or only a documented default-mapping clue for a currently static global
