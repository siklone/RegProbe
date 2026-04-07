# System Executive UuidSequenceNumber KVM WPR Addboot Follow-up

Date: 2026-04-07
Candidate: `system.executive-uuid-sequence-number`
Guest: `regprobe-win11-25h2-session`

## Objective
- branch away from the Procmon `SaveAs` export wall with a boot-time WPR registry capture lane
- determine whether the remaining UUID exact-read gap can be closed with WPR boot tracing on the working KVM guest

## Result
- the KVM guest admin-shell recovery path was shortened from `Invoke-WebRequest ...` to `iwr ...`, which removed the previous `Request` truncation failure and restored deterministic ready-marker uploads
- the WPR boot-registry guest helper now emits stage uploads and bounded process results for `wpr` and `tracerpt`
- a first instrumented rerun (`uuidsequence-wpr-boot-kvm-20260407c`) reached arm, uploaded `stage.json`, and failed before reboot with `status = error`, `error_kind = wpr-addboot-nonzero-exit`, and `error = wpr -addboot exited with code <null>.`
- the arm retry already used the broader `Power + Registry` boot profile family instead of `Registry` alone, so the failure is not explained by the earlier narrower profile choice
- a second rerun (`uuidsequence-wpr-boot-kvm-20260407d`) reproduced the same arm-stage blocker on the same guest with the same `wpr-addboot-nonzero-exit` surface and no stderr detail
- the host runner now stops immediately when `summary-arm.json` reports `status = error`, so failed WPR arm phases no longer trigger blind guest reboots

## Artifacts
- `evidence/files/vm-tooling-staging/uuidsequence-wpr-addboot-kvm-20260407cd/c-summary-arm.json`
- `evidence/files/vm-tooling-staging/uuidsequence-wpr-addboot-kvm-20260407cd/c-stage.json`
- `evidence/files/vm-tooling-staging/uuidsequence-wpr-addboot-kvm-20260407cd/d-summary-arm.json`
- `evidence/files/vm-tooling-staging/uuidsequence-wpr-addboot-kvm-20260407cd/d-stage.json`
- `evidence/files/vm-tooling-staging/uuidsequence-wpr-addboot-kvm-20260407cd/host-review.json`

## Short Take
- the remaining UUID exact-read gap is no longer “try WPR boot registry next”
- on this working KVM guest, the WPR boot-registry lane currently fails before reboot at `wpr -addboot`, even after switching to the broader `Power + Registry` boot profile family
- Procmon exact-read is still blocked at `SaveAs`, and WPR boot capture is now blocked earlier at bootstrap
- the next winning branch should move away from guest-side typed capture commands and toward a transport that does not depend on Procmon export or WPR boot arming
