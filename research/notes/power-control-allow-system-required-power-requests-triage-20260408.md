# power.control.allow-system-required-power-requests triage - 2026-04-08

## Summary

- `AllowSystemRequiredPowerRequests` is now a canonical `Control\Power` draft candidate.
- Repo docs assign it an explicit default of `1` and tie it to `PopPowerRequestConvertSystemToExecution`.
- The observed clean baseline confirms that the parent `Control\Power` path exists while the registry value is absent.
- The broad current-build string batch found an exact Unicode hit for `AllowSystemRequiredPowerRequests` in `ntoskrnl.exe`.
- Source-enrichment kept the value in the canonical queue and suggested a concrete next trigger family:
  - `power-request-simulation`
  - `PowerCreateRequest(SystemRequired)`
  - `PowerSetRequest(DisplayRequired)`
  - audio playback session
- A later repo-native follow-up exposed that same trigger family as a named harness in both the generic Procmon guest tool and the mega-trigger runtime surface.
- A later KVM local-KD pass resolved live current-build `nt!PopPowerRequestConvertSystemToExecution = 1`.
- The same wildcard KD sweep also surfaced the sibling symbol `nt!PopPowerRequestActiveAudioEnablesExecutionRequired`, but did not read its value.
- A later current-build KVM local-KD disassembly pass then showed direct reads of `nt!PopPowerRequestConvertSystemToExecution` inside `nt!PopPowerRequestHandleExecutionEnablementUpdate` and `nt!PopPowerRequestCallbackExecutionRequired`.
- A later wildcard lineage pass then showed that the current build exposes only one `*PowerRequest*Setting*` symbol, `nt!PopPowerRequestExecutionRequiredSettingCallback`, alongside `PopPowerRequestInitialize`, `PopPowerRequestOverrideInitialize`, and the `PopExecutionRequiredTimeout` family.

## Source artifacts

- `Docs/power/power.md`
- `research/notes/kernel-power-96-key-routing-20260327.md`
- `evidence/files/vm-tooling-staging/registry-batch-existence-96-live-20260329-100629/results.json`
- `evidence/files/vm-tooling-staging/targeted-string-batch-primary-20260331-135356/results.json`
- `registry-research-framework/enrichment/outputs/source-enrichment-20260403-044821/per-key/power.control.allow-system-required-power-requests.json`
- `scripts/vm/guest-tools/run-registry-policy-probe.ps1`
- `scripts/vm/run-power-control-batch-mega-trigger-runtime.guest.ps1`
- `evidence/files/vm-tooling-staging/local-kd-allowsystemrequired-20260408a/local-kd-allowsystemrequired-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-allowsystemrequired-20260408a/local-kd-allowsystemrequired-20260408a.log`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-reader-20260408a/local-kd-powerrequest-reader-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-reader-20260408a/local-kd-powerrequest-reader-20260408a.log`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-settinglineage-20260408a/local-kd-powerrequest-settinglineage-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-settinglineage-20260408a/local-kd-powerrequest-settinglineage-20260408a.log`

## Interpretation

- current evidence shape:
  - explicit repo-doc default
  - observed baseline-missing registry state
  - exact current-build kernel string hit
  - concrete enrichment-recommended trigger family
  - repo-native `power-request-simulation` harness
  - live KVM local-KD value `PopPowerRequestConvertSystemToExecution = 1`
  - direct current-build consumer reads in `PopPowerRequestHandleExecutionEnablementUpdate` and `PopPowerRequestCallbackExecutionRequired`
  - current-build callback/init lineage anchored by `PopPowerRequestExecutionRequiredSettingCallback`
- narrowed conclusion:
  - `AllowSystemRequiredPowerRequests` is stronger than a pure docs-first backlog item
  - the value belongs in the canonical research set as a power-request draft
- still unresolved:
  - a registry seeding caller from `Control\Power`
  - whether the named power-request harness surfaces an exact-read lane
- next proof path:
  - pivot from the now-confirmed callback/init family to a registry seeding path for `PopPowerRequestConvertSystemToExecution`
  - take a narrow `power-request-simulation` replay instead of a broad family batch
