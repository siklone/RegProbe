# Power Request Override Subtree Runtime Proof - 2026-04-18

## Target

- Record: `power.control.power-request-override-subtree`
- Registry root: `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride`
- VM: `Win25H2Clean` / KVM `regprobe-win11-25h2-session`

## Retained audit artifact

- [power-request-override-runtime-proof-20260418.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/power-request-override-runtime-proof-20260418.json)

## Why This Target

The existing record already had subtree presence, adjacent runtime access, local-KD override-family symbols, and bounded Ghidra/local-KD context. The remaining gap was narrower: whether the public `powercfg /requestsoverride` control surface materializes stable leaf state and whether that state can be removed cleanly.

## Official Control Surface

Microsoft Learn documents `powercfg /requestsoverride [caller_type name request]` as the command surface for setting Power Request overrides for a process, service, or driver. That documentation does not name the backing registry subtree, so this sprint treats it as control-surface evidence that still needs VM-backed registry proof.

Source: `https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options`

## Runtime Plan

The runtime order stayed evidence-first:

1. Run ETW stackwalk against the subtree root to prove current-build registry activity with physical ETL output.
2. Escalate to a command-state powercfg proof because ETW root reads alone do not prove leaf materialization or restore behavior.
3. Attempt Procmon for richer process-level registry evidence, but do not treat a failed SaveAs as proof.

## ETW Stackwalk Result

Run:

```text
power-request-override-subtree-etw-stackwalk-20260418
```

Artifacts:

- `evidence/raw/etw-stackwalk/power-request-override-subtree-etw-stackwalk-20260418/power-request-override-subtree-etw-stackwalk-20260418-summary.json`
- `evidence/raw/etw-stackwalk/power-request-override-subtree-etw-stackwalk-20260418/power-request-override-subtree-etw-stackwalk-20260418.etl`
- `evidence/raw/etw-stackwalk/power-request-override-subtree-etw-stackwalk-20260418/normalized-registry-bundle.json`

Observed:

- Runner status: `ok`
- Launch transport: `qga`
- Event count: `5497`
- Caller-stack event count: `1650`
- Stack field hit count: `402714`
- PowerRequestOverride text hits: `8`
- Stack-bearing operations included root `RegOpenKey`, `RegCloseKey`, and `RegQueryValue` reads for `RuleCount`, `DISABLED`, and `ENABLED`.

Interpretation:

This is direct runtime evidence that the subtree and control values are read on the checked-in build. It is not enough by itself to prove powercfg leaf add/remove semantics.

## Powercfg Command-State Proof

Artifact:

- `evidence/files/vm-tooling-staging/power-request-override-powercfg-proof-20260418/host-summary.json`

Command sequence:

```text
powercfg /requestsoverride
powercfg /requestsoverride PROCESS RegProbeOverrideProof.exe DISPLAY SYSTEM AWAYMODE
powercfg /requestsoverride
powercfg /requestsoverride PROCESS RegProbeOverrideProof.exe
powercfg /requestsoverride
```

Observed:

- Before add, the `PROCESS` section was empty.
- Add returned exit code `0`.
- After add, powercfg listed `RegProbeOverrideProof.exe DISPLAY SYSTEM AWAYMODE`.
- Registry snapshot after add showed `PowerRequestOverride\Process :: RegProbeOverrideProof.exe=7`.
- Remove returned exit code `0`.
- After remove, the `PROCESS` section was empty again.
- The test process value was absent after cleanup.
- `direct_cleanup_applied` was `false`, so direct registry deletion was not needed.

Interpretation:

This proves a reversible powercfg-managed Process leaf cycle on Win25H2Clean. Treat `7` as the observed all-request bitmask for this synthetic process proof, not as a complete Driver/Service leaf model.

## Procmon Escalation Result

A Procmon-backed policy probe was attempted with the same powercfg trigger. The trigger reached guest execution, but `Procmon SaveAs` timed out after `180` seconds and produced no CSV or normalized bundle. The large failed output also consumed enough guest disk that a cleanup command was required before QGA script execution could continue.

Interpretation:

This is an environment/tooling failure, not negative proof of the subtree. It should not be used as behavior evidence.

## Classification

Result: `research-only draft / intentional hold`.

Why:

- The Process leaf restore story is now materially stronger.
- The subtree still lacks complete Driver and Service leaf semantics.
- The exact current-build kernel reader binding remains adjacent rather than direct.
- The app does not expose this as a supported tweak surface.

## Next Follow-Up

Do not rerun broad Procmon first. The next step is a narrower static/debugger pass against the checked-in override reader path, or a dedicated Driver/Service leaf proof if a safe synthetic caller exists.
