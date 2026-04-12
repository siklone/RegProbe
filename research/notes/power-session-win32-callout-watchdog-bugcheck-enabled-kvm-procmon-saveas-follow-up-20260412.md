# Power Session Win32CalloutWatchdogBugcheckEnabled KVM Procmon SaveAs Follow-up

Date: 2026-04-12
Candidate: `power.session-win32-callout-watchdog-bugcheck-enabled`
Guest: `regprobe-win11-25h2-session`

## Objective
- try the missing Procmon runtime lane for `Win32CalloutWatchdogBugcheckEnabled`
- reuse the dedicated `watchdog-power-burst` trigger family instead of another broad watchdog pass
- separate “we never tried a bounded Procmon runtime attempt” from “the current KVM Procmon export path still fails before CSV review”

## Result
- the dedicated KVM runtime replay reached live guest execution and uploaded a probe-stage artifact
- the last uploaded probe stage stalled at `procmon-saveas`
- the guest-side result text recorded the terminal error `Procmon SaveAs timed out after 60 second(s).`
- no CSV and no hits CSV were produced
- the result text still reported `RESTORED={\"path_exists\":false,\"value_exists\":false,...}`, so the lane completed its restore step even though the Procmon export failed

## Artifacts
- `evidence/files/vm-tooling-staging/win32-callout-watchdog-bugcheck-procmon-kvm-20260412b/win32-callout-watchdog-bugcheck-procmon-kvm-20260412b-summary.json`
- `evidence/files/vm-tooling-staging/win32-callout-watchdog-bugcheck-procmon-kvm-20260412b/win32-callout-watchdog-bugcheck-procmon-kvm-20260412b-probe-stage.json`
- `evidence/files/vm-tooling-staging/win32-callout-watchdog-bugcheck-procmon-kvm-20260412b/win32-callout-watchdog-bugcheck-procmon-kvm-20260412b.txt`
- `evidence/files/vm-tooling-staging/win32-callout-watchdog-bugcheck-procmon-kvm-20260412b/host-review.json`

## Short Take
- the candidate is no longer missing an attempted bounded Procmon runtime lane
- on this KVM guest, the new blocker is explicit Procmon export reliability rather than trigger identity
- the runtime-read gap remains open, but the next attempt should branch away from blindly repeating the same `Procmon SaveAs` transport
