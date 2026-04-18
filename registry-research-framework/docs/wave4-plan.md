# Wave 4 — Planned Extensions

## Motor 1: ETW Call Stack Capture
ETW trace'e STACK_WALKING flag ekle (0x4).
Her registry event için call stack frame'lerini capture et.
Symbol resolution: Microsoft Symbol Server.
New bundle field: caller_stack[]

## Motor 2: Ghidra Auto-Decompiler Pipeline
Trigger: caller_stack içinde resolved=false frame.
Job queue: ghidra-job-queue.jsonl
Ghidra headless -> C pseudocode -> enrichment cache.
New bundle field: decompiled_context

## Motor 3: Evidence Triage Queue
Aktif yon runtime research ve kanit guclendirme.
Context: Ghidra output + ReactOS cache + mevcut corpus.
Output: discovery_source=triage-queue candidates.
Triage contract aynı.

## Motor 4: DPC/ISR Latency Bench
Bare metal tier only — VM'de hypervisor interference var.
Tool: xperf / WPR.
placebo_threshold_pct: 2.0
New blockers: dpc-bench-pending, dpc-bench-placebo

## Motor 5: WinDbg TTD / Minidump Pipeline
Trigger: safety bench boot_success=false.
cdb -z dump.dmp -c "!analyze -v; q"
New bundle field: crash_analysis

## Dependency
Motor 1 -> Motor 2 -> Motor 3
Motor 4 bağımsız (bare metal)
Motor 5 bağımsız (safety bench failure)

## Priority
P0: ETW call stack flag
P1: ETL parser frame extraction
P2: Symbol resolution + caller_stack bundle field
P3: Ghidra job queue + auto-trigger
P4: Ghidra headless job runner
P5: Evidence triage queue
P6: DPC/ISR bench
P7: WinDbg TTD
