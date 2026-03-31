# Policy System EnableVirtualization Path-Aware Follow-up

Date: 2026-03-31
Candidate: `policy.system.enable-virtualization`

## Objective
- replay the path-aware runtime lane on the secondary VM profile
- confirm whether the previous `runtime_no_read + path_context_unclear` blocker was specific to the primary lane or reproducible across profiles

## Result
- secondary profile: `Win25H2Clean-B`
- runtime lane: shell-safe and no-hit
- exact runtime reads: `0`
- exact line hits: `0`
- path-only hits: `0`
- collision-path hits: `0`
- shell before and after stayed healthy
- canonical artifact: `evidence/files/path-aware/secondary/path-aware-runtime-secondary-20260331-110610/policy-system-enable-virtualization/summary.json`

## Verdict
- keep as `Class B`
- reason: `runtime_no_read + path_context_unclear`
- the new secondary run reproduces the same no-hit result, which makes the blocker cross-profile rather than a one-off transport issue
