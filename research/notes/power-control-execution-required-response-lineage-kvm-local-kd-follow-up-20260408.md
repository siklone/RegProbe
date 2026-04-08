## Summary
- A retained KVM local-KD follow-up tightened the visible current-build execution-required power-request family below the existing callback/init/timeout/UMPO evidence.
- `x nt!*PowerRequest*Reg*` returned no visible current-build symbols.
- `nt!PopPowerRequestHandleRequestOverrideQueryResponse` updates request state, calls `PopPowerRequestEvaluatePendingRequestStatus`, and queues `PopPowerRequestUpdateWorkItem`.
- `nt!PopPowerRequestCallbackWorker` drains the update queue and routes changed bits through `PopPowerRequestStatsSetActive` and `PopPowerRequestHandleRequestUpdate`.
- `nt!PopPowerRequestCallbackExecutionRequired` consults `PoPdcCallbacks`, updates `PopPowerRequestPdcNotifiedExecutionRequired` or `PopPowerRequestPdcNotifiedSystemRequired`, tests `PopPowerRequestConvertSystemToExecution`, and writes `PopSIdle` state before `PopCheckResiliencyScenarios`.

## Evidence
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-resplineage-20260408a/local-kd-powerrequest-resplineage-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-resplineage-20260408a/local-kd-powerrequest-resplineage-20260408a.log`

## Interpretation
- The visible current-build execution-required family is now callback/init/timeout/override-response/update-work-item/PDC driven.
- The wildcard miss on `*PowerRequest*Reg*` matters because it weakens the idea of an obvious current-build registration or registry-seeding helper inside the exposed symbol family.
- `PopPowerRequestHandleRequestOverrideQueryResponse` and `PopPowerRequestCallbackWorker` both operate on in-memory request state and queued work rather than exposing a registry read.
- `PopPowerRequestCallbackExecutionRequired` narrows the system-required branch further: it gates one branch on `PopPowerRequestConvertSystemToExecution`, but still does so inside a PDC callback plus policy-state flow rather than a demonstrated `Control\\Power` reader.

## Next Questions
- Does a separate hidden callback-registration or policy-initialization path still seed `AllowSystemRequiredPowerRequests` or `AllowAudioToEnableExecutionRequiredPowerRequests` from `Control\\Power` on modern builds?
- Or is the visible modern family entirely runtime callback, timer, worker, UMPO, and PDC driven?
