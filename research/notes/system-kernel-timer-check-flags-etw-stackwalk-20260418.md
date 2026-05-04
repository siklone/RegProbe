# system.kernel.timer-check-flags ETW stackwalk sprint - 2026-04-18

## Target

- Candidate: `system.kernel.timer-check-flags`
- Registry path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `TimerCheckFlags`
- VM target: `Win25H2Clean`
- Runtime lane: KVM/QGA ETW registry stackwalk

## Why this sprint

`TimerCheckFlags` already has unusually strong static and live-state evidence for an undocumented kernel setting: repo docs map it to `KeTimerCheckFlags`, the current-build kernel contains the value-name string, source-enrichment retains WRK-backed `KeTimerCheckFlags` semantics, local KD reads `nt!KeTimerCheckFlags = 1`, and the current-build INIT descriptor binds the value name to the same global RVA.

The unresolved question is narrower: can a live current-build runtime trace retain an exact registry read for `TimerCheckFlags` under `Session Manager\Kernel`?

## Static refresh

No primary Microsoft documentation was found for `TimerCheckFlags` as a modern registry contract. Microsoft documents adjacent debugging and tracing registry surfaces under `Session Manager` and `Session Manager\Kernel`, but not this value or its bit semantics. The static confidence therefore still comes from repo docs, current-build binary/string evidence, WRK source-enrichment, the INIT descriptor row, and live KD state rather than a first-party policy or Learn page.

## Runtime plan

Lane order for this sprint:

1. ETW registry stackwalk first, because the missing proof is an exact runtime read.
2. Reuse the already-completed WPR/QGA boot-registry no-hit as the stronger boot/runtime negative lane.
3. Do not escalate to WinDbg unless a new exact-read caller pivot appears.

This follows the repo contract: ETW first, no broad replay without a better trigger, and no debugger escalation when the cheaper lanes still produce only no-hit evidence.

## Runtime result

The ETW stackwalk run completed through QGA and produced physical artifacts.

- Run id: `timer-check-flags-etw-stackwalk-20260418`
- Capture status: `ok`
- Duration: `45` seconds
- Normalized registry events: `5461`
- Caller-stack events: `1750`
- Stack field hit count: `406613`
- `value_name = TimerCheckFlags` hits: `0`
- exact `Session Manager\Kernel` key-path hits: `0`
- `TimerCheckFlags` text hits: `1`

The single retained `TimerCheckFlags` text hit was not target behavior. It was the helper-side `reg.exe` command line:

```text
reg.exe query HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel /v TimerCheckFlags
```

The helper probe itself reported `ERROR: Invalid syntax.` because the space-containing key path was not quoted correctly by the guest helper. That means the run is a real post-boot ETW capture, but the helper query is not a valid target read and must not be counted as proof.

## Interpretation

This sprint adds a narrow negative/weak ETW result, not a promotion signal.

What improved:

- The repo now has a fresh QGA-launched ETW stackwalk bundle for the candidate.
- The normalized bundle was inspected at field level, not by string count alone.
- The result explicitly distinguishes helper-command-line text from a real `TimerCheckFlags` value read.

What did not improve:

- There is still no retained exact runtime read for `TimerCheckFlags`.
- The current-build bit semantics remain undocumented.
- The previous QGA/WPR boot-registry no-hit remains the leading runtime lane.

## Artifact set

- `registry-research-framework/audit/system-kernel-timer-check-flags-etw-stackwalk-20260418.json`
- `evidence/raw/etw-stackwalk/timer-check-flags-etw-stackwalk-20260418/timer-check-flags-etw-stackwalk-20260418-summary.json`
- `evidence/raw/etw-stackwalk/timer-check-flags-etw-stackwalk-20260418/timer-check-flags-etw-stackwalk-20260418.etl`
- `evidence/raw/etw-stackwalk/timer-check-flags-etw-stackwalk-20260418/normalized-registry-bundle.json`

## Classification

Keep `system.kernel.timer-check-flags` as `blocked pending stronger runtime proof`.

Do not promote this to a shipped tweak, and do not repeat broad idle ETW/WPR capture without a better trigger or caller pivot. The next proof step is either a fixed exact-query helper used only as instrumentation validation, a narrower boot/init descriptor consumer trace, or authoritative current-build documentation for the bit contract.
