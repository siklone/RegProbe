# Kernel/Power Next-Gate Ghidra Review

Date: 2026-03-28

Source queue:

- `registry-research-framework/audit/kernel-power-existing-next-gate-20260328.json`

Imported evidence:

- `evidence/files/ghidra/kernel-power-nextgate-ntoskrnl/ghidra-matches.md`
- `evidence/files/ghidra/kernel-power-nextgate-ntoskrnl/evidence.json`
- `evidence/files/ghidra/kernel-power-nextgate-ntoskrnl/ghidra-run.log`

Updated script source:

- `scripts/vm/ghidra/ExportStringXrefs.java`

## Why this pass exists

The previous ntoskrnl batch established that four promoted candidates had exact string xrefs, but the fallback ended in `// no disassembly available in range` for all unresolved blocks.

This pass improved the fallback so the repo now captures:

- a nearby instruction anchor if one exists
- a forced function boundary when Ghidra still has no natural function
- decompilation or a lower-level fallback instead of a dead-end placeholder

## What improved

The refreshed fallback no longer stops at an empty unresolved block. All four next-gate addresses now produce a structured artifact in `evidence.json` and a decompile section in `ghidra-matches.md`.

That is an improvement in tooling quality, but not a semantic breakthrough yet.

## Candidate split

### Stronger pair: watchdog values

- Suggested lane label: `power.session-watchdog-timeouts`
- `power.session-watchdog-resume-timeout`
- `power.session-watchdog-sleep-timeout`

Why they stay on top:

- exact string hit in `ntoskrnl.exe`
- stable xref addresses
- family already has a related repo-side pseudocode lead through `PoFxInitPowerManagement`

Current limitation:

- the decompilation comes from forced boundaries and is not trustworthy enough to claim exact semantics yet

Practical reading:

- keep these two together
- they are the best candidates for the next static and runtime-prep lane

### Weaker pair: Executive worker-thread values

- Suggested lane label: `system.executive-additional-worker-threads`
- `system.executive-additional-critical-worker-threads`
- `system.executive-additional-delayed-worker-threads`

Why they stay below the watchdog pair:

- exact string hit in `ntoskrnl.exe`
- stable xref addresses
- but the forced-boundary decompilation is still garbage-level and dominated by bad instruction warnings

Practical reading:

- keep these two together
- do not move them into a runtime lane yet
- they still need a better static-only pass first

## Conclusion

The new Ghidra fallback is worth keeping. It turns dead ends into reviewable artifacts.

However, the actual research decision from this pass is still conservative:

1. `WatchdogResumeTimeout` and `WatchdogSleepTimeout` remain the strongest next queue
2. `AdditionalCriticalWorkerThreads` and `AdditionalDelayedWorkerThreads` remain second-tier until static quality improves

Machine-readable summary:

- `registry-research-framework/audit/kernel-power-nextgate-ghidra-review-20260328.json`
