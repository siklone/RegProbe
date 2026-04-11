# system.kernel.global-timer-resolution-requests triage - 2026-04-08

## Summary

- `GlobalTimerResolutionRequests` is now a canonical `Session Manager\Kernel` draft candidate.
- Repo docs assign it an explicit default of `0` and tie it to `KiGlobalTimerResolutionRequests`.
- The observed clean baseline confirms that the parent `Session Manager\Kernel` path exists while the registry value is absent.
- The broad current-build string batch found an exact Unicode hit for `GlobalTimerResolutionRequests` in `ntoskrnl.exe`.
- A later KVM local-KD pass resolved `nt!KiGlobalTimerResolutionRequests` and returned `0`, matching the repo-doc default.
- Source-enrichment kept the value in the canonical queue and suggested a concrete next trigger family:
  - `power-request-simulation`
  - `PowerCreateRequest(SystemRequired)`
  - `PowerSetRequest(DisplayRequired)`
  - audio playback session
- A later repo-native follow-up added that trigger family as a named harness in both the generic Procmon guest tool and the mega-trigger runtime surface.
- The earlier tools-hardened lightweight runtime batch wrote the value as `1`, rebooted once, preserved shell health, and still produced `no-hit` runtime output for this specific value.

## Source artifacts

- `Docs/system/system.md`
- `research/notes/kernel-power-96-key-routing-20260327.md`
- `evidence/files/vm-tooling-staging/registry-batch-existence-96-live-20260329-100629/results.json`
- `evidence/files/vm-tooling-staging/targeted-string-batch-primary-20260331-135356/results.json`
- `evidence/files/vm-tooling-staging/local-kd-globaltimerres-20260408a/local-kd-globaltimerres-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-globaltimerres-20260408a/local-kd-globaltimerres-20260408a.log`
- `evidence/files/vm-tooling-staging/session-manager-kernel-batch-lightweight-runtime-primary-20260331-171654/state.json`
- `evidence/files/vm-tooling-staging/session-manager-kernel-batch-lightweight-runtime-primary-20260331-171654/results.json`
- `registry-research-framework/enrichment/outputs/source-enrichment-20260403-044821/per-key/system.kernel.global-timer-resolution-requests.json`
- `scripts/vm/guest-tools/run-registry-policy-probe.ps1`
- `scripts/vm/run-power-control-batch-mega-trigger-runtime.guest.ps1`

## Interpretation

- current evidence shape:
  - explicit repo-doc default
  - observed baseline-missing registry state
  - exact current-build kernel string hit
  - live current-build KD state `KiGlobalTimerResolutionRequests = 0`
  - concrete enrichment-recommended trigger family
  - repo-native `power-request-simulation` harness
  - runtime write-and-reboot lane with no exact read
- narrowed conclusion:
  - `GlobalTimerResolutionRequests` is stronger than a pure docs-first backlog item
  - the value belongs in the canonical research set as a live-state-confirmed timer-resolution kernel draft
- still unresolved:
  - a current-build reader or seeding caller
  - whether the named `power-request-simulation` harness can surface a narrower exact-read lane
- next proof path:
  - pivot from the now-resolved live state to a narrow `power-request-simulation` runtime lane using the new named harness
  - isolate a current-build reader or seeding caller for `KiGlobalTimerResolutionRequests`
