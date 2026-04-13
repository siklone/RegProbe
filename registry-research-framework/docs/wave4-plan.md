# Wave 4 — Planned Extensions

## Motor 1: ETW Call Stack Capture
- Add STACK_WALKING flag to ETW trace
- Parse call stack frames from ETL
- Symbol resolution via Microsoft Symbol Server
- New bundle field: caller_stack

Status: schema and parser support for `caller_stack` is in place, and the KVM WPR runner can now fail stack-expected runs when no frames are present. The remaining capture work is the provider-specific WPR/xperf profile that actually emits stack frames.

## Motor 2: Ghidra Auto-Decompiler Pipeline
- Ghidra job queue: ghidra-job-queue.jsonl
- Caller-stack auto-trigger seeds: ghidra-autotrigger-seeds.jsonl
- Auto-trigger on unresolved caller_stack frames
- Decompile -> C pseudocode -> enrichment cache
- New bundle field: decompiled_context

Status: queue skeleton, dispatch manifest, dry-run dispatch runner, caller-stack seed generation, dispatch enrichment from those seeds, and a one-shot refresh command from fresh bundles are now in place for the active blocked `ghidra` lane. The runner now prioritizes caller-stack-pivot jobs; the remaining gap is hands-free auto-trigger and real headless execution on a host with `pwsh` + Ghidra available.

## Motor 3: AI Fuzzer
- Local Ollama (Qwen2.5-Coder-7B) integration
- Context: Ghidra output + ReactOS cache + corpus
- Output: ai-fuzzer discovery candidates
- Triage contract same as gap analysis

## Motor 4: DPC/ISR Latency Bench (bare metal only)
- xperf/WPR based
- placebo_threshold_pct: 2.0
- VM: not applicable (hypervisor interference)
- New blocker: dpc-bench-pending, dpc-bench-placebo

## Motor 5: WinDbg TTD / Minidump Pipeline
- Trigger: safety bench boot_success=false
- cdb -z dump.dmp -c "!analyze -v; q"
- New bundle field: crash_analysis

## Priority
P0: ETW call stack flag
P1: ETL parser frame extraction
P2: Symbol resolution
P3: Ghidra job queue + auto-trigger
P4: Ghidra headless job runner
P5: AI Fuzzer (Ollama)
P6: DPC/ISR bench
P7: WinDbg TTD
