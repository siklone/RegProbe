# Run Tiers

RegProbe accepts observations from many machines, but not every run has the
same proof value. This page defines the minimal tier model used by contributors,
automation, and future UI badges.

## Tier Summary

| Tier | Meaning | Can support reference claims? |
|---|---|---|
| `certified` | Standard environment, tight noise gate, healthy VM transport, complete artifacts. | Yes |
| `community` | Useful external observation from a non-standard or partially described environment. | No |
| `noisy` | Run completed under noisy, incomplete, or unknown conditions. | No |

Community and noisy runs can still help triage future work. They must not be
used as proof for performance claims, shipping verdicts, or default app-card
copy without a certified or otherwise local/runtime-backed lane.

## Certified Run Requirements

A certified run must satisfy all of these requirements:

- Windows 11 25H2 guest based on the standard RegProbe clean snapshot.
- VM transport is healthy before the run. For KVM this means
  `scripts/vm-kvm/vm-health-check.py --json` returns QGA health `ok`.
- Tight host-noise gate is enabled and reports `ok`.
- Campaign summary has `non_ok=[]` or equivalent zero-failure contract.
- Artifacts are complete and referenced by JSON paths that can be checked in or
  traced to a checked-in receipt.
- Apply, verify, reboot when required, and rollback stages report bounded,
  parseable results.
- The run records enough environment metadata to reproduce or reject it later.

The default certified profile for the current lane is
`win11-25h2-kvm-qga-tight-v1`.

## Community Run Requirements

A community run may come from KVM, Hyper-V, VMware, VirtualBox, bare metal, or a
different resource profile. It should still include environment and noise
metadata, but it is not a reference source unless it is re-run in a certified
profile.

Community runs are useful for:

- spotting values that deserve a certified rerun
- finding hardware- or hypervisor-specific behavior
- validating that rollback stories are understandable outside the maintainer VM
- discovering UI confusion in real usage

Community runs are not enough for:

- promoted app-card performance claims
- replacing Microsoft/local/static/runtime evidence
- marking a record as reference-verified
- loosening host-noise thresholds

## Noisy Run Rules

A noisy run is retained for debugging, but its data is not proof. Use `noisy`
when host-noise status is noisy, missing, malformed, or when artifacts are
incomplete enough that the result cannot be independently reviewed.

Noisy runs may be used to create a rerun plan. They must not close a blocker,
promote a card, or support a benchmark claim.

## Minimal JSON Contract

New campaign and experiment outputs should include these fields when practical:

```json
{
  "run_tier": "certified",
  "certification_profile": "win11-25h2-kvm-qga-tight-v1",
  "reference_eligible": true,
  "verification_badge": "verified",
  "environment": {
    "host_os": "linux",
    "host_cpu_logical_count": 16,
    "host_cpu_physical_count": 8,
    "host_ram_gib": 32,
    "vm_backend": "kvm",
    "vm_domain": "regprobe-win11-25h2-session",
    "vm_cpu_count": 4,
    "vm_ram_gib": 8,
    "guest_os": "Windows 11",
    "guest_build": "25H2",
    "snapshot_id": "clean-25h2-qga"
  },
  "noise": {
    "status": "ok",
    "threshold_profile": "tight-v1",
    "host_noise_meta": {}
  }
}
```

Allowed values:

- `run_tier`: `certified`, `community`, or `noisy`
- `reference_eligible`: `true` only when the run satisfies certified
  requirements
- `verification_badge`: `verified`, `community-observed`, or `noisy-debug`
- `noise.status`: `ok`, `noisy`, `skipped`, or `unknown`

## UI Badge Guidance

End-user cards should stay simple:

- `Verified`: certified/reference-eligible evidence exists for the shipped
  claim.
- `Community observed`: useful observation exists, but it is not reference
  proof. Show this only in evidence details or contributor/debug views.
- `Noisy/debug only`: do not show by default to normal users.

The WPF app should not expose tranche names, host-noise thresholds, QGA details,
ETW stage names, Procmon raw paths, or Ghidra internals unless the user opens an
advanced technical evidence surface.

## Contributor Rules

- Prefer certified reruns when changing app-card copy, promotion gates, or
  rollback claims.
- Store community observations with full environment metadata.
- Treat forum or community-sourced values as `community-hint` until a VM or
  local runtime lane validates them.
- Do not lower the noise gate to make a result pass.
- If the host is noisy, keep the artifact and generate a low-noise rerun plan.
