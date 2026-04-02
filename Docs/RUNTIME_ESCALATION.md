# Runtime Escalation

RegProbe now uses an escalation model for hard runtime cases instead of treating every key as `write -> reboot -> idle -> inspect`.

## Current Order

1. `ETW` or targeted runtime trace
2. `safe mega-trigger runtime v2`
3. `WinDbg boot registry trace`
4. source-enrichment cross-reference

Use the cheaper lane first and move to the more expensive lane only when the earlier one stays inconclusive.

## Safe Mega-Trigger Runtime v2

The current power-control runtime pilot is a recovery-first orchestration lane:

- host runner:
  - `registry-research-framework/tools/run-power-control-batch-mega-trigger-runtime.ps1`
- guest payload:
  - `scripts/vm/run-power-control-batch-mega-trigger-runtime.guest.ps1`

Key properties:

- stale probe detection before new work
- `-RecoverOnly` mode for forced snapshot recovery
- explicit `arm -> reboot -> run` state machine
- shell-health and guest-command boot gates
- safe pilot trigger profile only
- host polls compact guest JSON instead of pulling ETL/CSV

Current pilot scope:

- `AllowAudioToEnableExecutionRequiredPowerRequests`
- `AllowSystemRequiredPowerRequests`
- `AlwaysComputeQosHints`
- `CoalescingFlushInterval`
- `IdleProcessorsRequireQosManagement`

Current trigger profile:

- `cpu_stress`
- `power_plan_and_requests`
- `multi_thread_burst`
- `disk_io_burst`
- `process_spawn_burst`
- `foreground_background_switch`
- `timer_resolution_change`
- `network_activity`

Status today:

- recovery and restore behavior is working
- the guest gets through all eight pilot triggers
- the remaining blocker is ETL parsing after trace stop
- until that parser issue is closed, treat this lane as `pilot / follow-up`, not final classification

## WinDbg Dead-Flag Arbiter

Use WinDbg only after runtime tracing leaves a strong no-hit queue.

Primary scripts:

- `scripts/vm/configure-kernel-debug-baseline.ps1`
- `scripts/vm/new-windbg-registry-watch-script.ps1`
- `registry-research-framework/tools/run-windbg-boot-registry-trace.ps1`

This lane exists to answer the question ETW cannot always answer:

- is the key read in very early boot before ETW is ready?
- or is it really a dead flag?

Working rule:

- `ETW no-hit + WinDbg hit` -> early boot read, keep alive
- `ETW no-hit + WinDbg no-hit` -> strongest dead-flag signal

## Source Enrichment

Static reverse engineering should be cross-referenced against source-style or structured references whenever possible.

Primary scripts:

- `registry-research-framework/config/source-enrichment-sources.json`
- `registry-research-framework/tools/run-source-enrichment-scan.ps1`
- `scripts/source_enrichment_scan.py`

Planned source families:

- `ReactOS`
- `WRK`
- `System Informer`
- `Sandboxie`
- `Wine`
- local `ADMX`
- local `WDK / SDK headers`

This lane is enrichment, not proof by itself. Use it to:

- discover likely function names or feature surfaces
- raise or lower priority for runtime testing
- cross-check Ghidra evidence
- separate `widely referenced` keys from `zero-reference` dead-flag candidates

## Static Evidence Guardrails

Nohuto-driven guardrails remain mandatory:

- PDB-backed Ghidra first
- bounded branch output only
- no `FUN_` / `DAT_` committed evidence
- no long decompile walls
- if unclear, write `unclear`
- use source-enrichment references as cross-checks, not as a replacement for branch-backed Windows evidence

## IDA Status

`IDA` is currently an optional lane, not a required gate.

- `IDA Free` is present as a manual helper
- automation-capable headless parity is still blocked
- `Ghidra + PDB` remains the canonical static lane until a working IDA automation build exists
