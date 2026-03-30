# Power-control lightweight runtime follow-up (2026-03-30)

This follow-up reran the remaining docs-first power-control candidates on the tools-hardened baseline `RegProbe-Baseline-ToolsHardened-20260330`.

## Baseline changes used

- VMware Tools service recovery was enabled with automatic restart policy.
- `vmtoolsd.exe` priority was raised to `AboveNormal` on the baseline.
- The runtime lane no longer depended on large trace copy-back. The guest wrote compact phase summaries and the host copied only those summary files when guest variables did not return.
- The trigger profile avoided switching to `SCHEME_MIN` and `SCHEME_BALANCED`; it refreshed `SCHEME_CURRENT` with targeted registry writes and processor EPP nudges.

## Candidate results

- `power.control.class1-initial-unpark-count`: exact runtime read captured on the short trigger ETW lane.
- `power.control.perf-calculate-actual-utilization`: exact runtime read captured on the short trigger ETW lane.

## Notes

- `vmtoolsd --cmd info-set guestinfo.*` still returned `Two and exactly two arguments expected` on this VMware Tools build, so guestVar publication remained unreliable.
- The hardened baseline still solved the original blocking issue: the guest stayed running, shell health stayed good, and the host could recover the compact phase summaries without losing the runtime result.
