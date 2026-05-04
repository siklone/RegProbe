# system.kernel.global-timer-resolution-requests runtime sprint - 2026-04-18

## Target

- Candidate: `system.kernel.global-timer-resolution-requests`
- Registry path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `GlobalTimerResolutionRequests`
- VM target: `Win25H2Clean`
- Trigger family: `power-request-simulation`

## Why this sprint

This record already had repo docs, baseline state, current-build static binding, and a live KVM local-KD read of `nt!KiGlobalTimerResolutionRequests = 0`, but it still lacked a clean current-build runtime trace result. The previous 2026-04-13 WPR/QGA lane ended as timeout-salvaged negative evidence, so the remaining question was whether the no-hit result reflected a real current-build absence of exact registry reads or only the wrapper timeout.

## Static refresh

The sprint reused the existing strongest static chain:

- repo docs map `GlobalTimerResolutionRequests = 0` to `KiGlobalTimerResolutionRequests`
- the clean baseline keeps the parent key but not the value
- current-build `ntoskrnl.exe` contains an exact `GlobalTimerResolutionRequests` string hit
- the current-build INIT descriptor scan binds the value name to the same RVA as `nt!KiGlobalTimerResolutionRequests`
- source-enrichment still recommends the `power-request-simulation` family as the cheapest runtime trigger

That is enough to justify a narrow runtime pass, but not enough to claim an active current-build registry read by itself.

## Runtime plan

Lane order for this sprint:

1. ETW stackwalk first with a power-request trigger
2. Procmon support lane only if ETW stayed weak
3. WPR boot-registry escalation only after the cheaper lanes failed to produce an exact read

That order matches the repo contract: ETW first, Procmon as supporting evidence, WPR only when the cheaper runtime lanes remain weak or ambiguous.

## Runtime results

### 1. ETW stackwalk

The ETW lane produced real guest artifacts, including `.etl`, `.raw.etl`, and `.xml`, but the retained signal was resolver-only.

- `summary_exists = false`
- `etl_exists = true`
- `xml_exists = true`
- `exact_value_hit_count = 2`
- `regquery_hit_count = 50`
- `stack_field_hit_count = 100`

The retained sample lines were only the helper-side `reg.exe query` command for the target value. This is a weak signal, not proof of target behavior.

### 2. Procmon support lane

The Procmon power-request follow-up did not produce a usable CSV or normalized bundle.

- `status = error`
- `error_kind = probe-stage-error`
- `probe_stage = exception`
- `probe_stage_message = Procmon SaveAs timed out after 120 second(s).`

This is an environment-sensitive export failure, not proof that the target is inactive.

### 3. WPR boot-registry escalation

The WPR boot-registry rerun completed cleanly on the current guest.

- `status = ok`
- `reboot_observed = true`
- `etl_exists = true`
- `csv_exists = true`
- `normalized_bundle_exists = true`
- `normalization_status = ok`
- `hit_line_count = 101`
- `fragment_hit_counts["Session Manager\\Kernel"] = 101`
- `fragment_hit_counts["GlobalTimerResolutionRequests"] = 0`
- `event_count = 0`

This is stronger than the older timeout-salvaged lane because the run completed end-to-end and the normalizer wrote a real output bundle. The retained hit set shows repeated `Session Manager\Kernel` key opens and key-control operations during boot, but still no exact `GlobalTimerResolutionRequests` value read.

## Interpretation

This sprint improves the record in two concrete ways:

1. It removes the old ambiguity around the WPR lane. The current QGA/WPR rerun is no longer a timeout-salvaged guess. It is a clean current-build boot-registry result.
2. It upgrades the runtime story from "generic no-hit" to "supporting subtree activity with zero exact value hits." The trace now shows repeated `Session Manager\Kernel` boot activity while still failing to retain a direct `GlobalTimerResolutionRequests` read.

That is still not enough to close `runtime_no_read`. The classification remains: blocked pending stronger runtime proof.

## Artifact set

- `registry-research-framework/audit/system-kernel-global-timer-resolution-requests-runtime-sprint-20260418.json`
- `evidence/files/vm-tooling-staging/global-timer-resolution-requests-runtime-sprint-20260418/global-timer-resolution-requests-procmon-power-request-20260418a-summary.json`
- `evidence/files/vm-tooling-staging/global-timer-resolution-requests-runtime-sprint-20260418/global-timer-resolution-requests-procmon-power-request-20260418a-probe-stage.json`
- `evidence/files/vm-tooling-staging/global-timer-resolution-requests-runtime-sprint-20260418/global-timer-resolution-requests-wpr-qga-20260418b-summary-arm.json`
- `evidence/files/vm-tooling-staging/global-timer-resolution-requests-runtime-sprint-20260418/global-timer-resolution-requests-wpr-qga-20260418b-summary.json`
- `evidence/files/vm-tooling-staging/global-timer-resolution-requests-runtime-sprint-20260418/global-timer-resolution-requests-wpr-qga-20260418b.hits.txt`
- `evidence/files/vm-tooling-staging/global-timer-resolution-requests-runtime-sprint-20260418/global-timer-resolution-requests-wpr-qga-20260418b.normalized.json`

## Next narrow proof step

Do not reopen broad ETL-vs-timeout work for this candidate. If this record is revisited, the next justified step is a narrower exact-read runtime lane focused on the Session Manager Kernel descriptor consumer or another current-build trace path that can retain a value-level read, not just subtree activity.
