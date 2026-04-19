# Power Request Override Subtree Driver/Service Proof - 2026-04-18

## Target

- Record: `power.control.power-request-override-subtree`
- Registry root: `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride`
- VM: `Win25H2Clean` / KVM `regprobe-win11-25h2-session`

## Why This Follow-Up

The previous sprint already proved a reversible `Process` leaf cycle through the documented `powercfg /requestsoverride` surface. The most useful remaining runtime gap was narrower: whether the same public control surface materializes and removes `Service` and `Driver` leaves with the same observed bitmask model.

## Runtime Plan

This follow-up stayed in the VM lane and kept the scope tight:

1. Start from empty `Service` and `Driver` leaves under `PowerRequestOverride`.
2. Use the documented `powercfg /requestsoverride` surface to add one known service caller and one known driver caller with `DISPLAY SYSTEM AWAYMODE`.
3. Capture both the `powercfg /requestsoverride` listing and the leaf-value state before add, after add, and after remove.
4. Attempt an ETW follow-up around the same root, but do not treat a stuck host wrapper as behavior proof.

## Chosen Callers

- Service caller: `Audiosrv`
- Driver caller: `ACPI`

These are current-build caller names that already exist on the guest, so the proof does not depend on synthetic leaf names for this pass.

## Powercfg Command-State Proof

Artifact:

- `evidence/files/vm-tooling-staging/power-request-override-driver-service-powercfg-proof-20260418/host-summary.json`

Command sequence:

```text
powercfg /requestsoverride
powercfg /requestsoverride SERVICE Audiosrv DISPLAY SYSTEM AWAYMODE
powercfg /requestsoverride DRIVER ACPI DISPLAY SYSTEM AWAYMODE
powercfg /requestsoverride
powercfg /requestsoverride SERVICE Audiosrv
powercfg /requestsoverride DRIVER ACPI
powercfg /requestsoverride
```

Observed:

- Before add, `Service` and `Driver` leaf values were empty.
- `powercfg /requestsoverride SERVICE Audiosrv DISPLAY SYSTEM AWAYMODE` returned exit code `0`.
- `powercfg /requestsoverride DRIVER ACPI DISPLAY SYSTEM AWAYMODE` returned exit code `0`.
- After add, `powercfg /requestsoverride` listed:
  - `Audiosrv DISPLAY SYSTEM AWAYMODE`
  - `ACPI DISPLAY SYSTEM AWAYMODE`
- After add, the registry leaf snapshots showed:
  - `PowerRequestOverride\Service :: Audiosrv=7`
  - `PowerRequestOverride\Driver :: ACPI=7`
- Both remove commands returned exit code `0`.
- After remove, `powercfg /requestsoverride` returned to empty `SERVICE` and `DRIVER` sections.
- After remove, both `Service` and `Driver` leaves remained present but empty.
- `direct_cleanup_applied` stayed `false`, so direct registry deletion was not required.

## Interpretation

This is direct runtime proof that the documented powercfg surface can materialize and remove `Service` and `Driver` leaf values under `PowerRequestOverride`, and that the observed storage model for these tested callers matches the earlier `Process` proof: `DISPLAY SYSTEM AWAYMODE` produces an observed value of `7`.

What this does **not** prove:

- the exact current-build component that reads those leaves
- that the live reader/consumer path is fully identical to the powercfg-managed storage model
- that the subtree is ready for an app-facing tweak surface

## ETW Follow-Up Note

An ETW follow-up was launched against the same subtree during this run, but the host wrapper did not finish ingesting before manual cleanup. No new ETW artifact from that attempt is treated as published proof in this sprint.

Interpretation:

This is tooling/environment context only. The sprint result stands on the command-state proof artifact above, plus the previously retained ETW root-read artifact from the prior sprint.

## Classification

Result: `research-only draft / intentional hold`.

Why:

- The cross-leaf powercfg storage model is now materially stronger.
- Rollback/removal behavior is now proven for `Process`, `Service`, and `Driver` leaves through the public command surface.
- The exact live reader binding and consumer semantics are still unresolved.

