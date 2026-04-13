# Wave 4 — Planned Extensions

## Motor 1: ETW Call Stack Capture
- Add STACK_WALKING flag to ETW trace
- Parse call stack frames from ETL
- Symbol resolution via Microsoft Symbol Server
- New bundle field: caller_stack

Status: schema and parser support for `caller_stack` is in place, and the KVM WPR runner can now fail stack-expected runs when no frames are present. The first xperf registry stackwalk profile is now captured in `registry-research-framework/config/etw-stackwalk-profiles.json`, and `generate_etw_stackwalk_capture_plan.py` renders the elevated Windows commands plus repo parse handoff. The matching guest helper lives at `scripts/vm/guest-tools/run-etw-registry-stackwalk-capture.ps1`, with `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` as the KVM host wrapper for bridge launch/upload. That wrapper can now also ingest the uploaded ETL/XML into `evidence/files/etw-stackwalk/<run-id>/`, build a `normalized-registry-bundle.json`, and refresh the Ghidra autotrigger lane in one pass. ETL discovery candidates now preserve `caller_stack` and `caller_stack_frame_count` when the parser finds stack frames, so the signal survives into queue/scoring views. The remaining capture work is to run that helper on a Windows host/guest with Windows Performance Toolkit and land the first real stack-bearing bundle.

## Motor 2: Ghidra Auto-Decompiler Pipeline
- Ghidra job queue: ghidra-job-queue.jsonl
- Bundle discovery manifest: ghidra-autotrigger-inputs.json
- Caller-stack auto-trigger seeds: ghidra-autotrigger-seeds.jsonl
- Auto-trigger on unresolved caller_stack frames
- Decompile -> C pseudocode -> enrichment cache
- New bundle field: decompiled_context

Status: queue skeleton, bundle discovery manifest, caller-stack seed generation, a symbol-resolution queue for unresolved frames, a symbolized-probe batch for those frames, a handoff surface for prepared symbol jobs, dispatch manifest, dry-run dispatch runner, dispatch enrichment from those seeds, refresh commands from one or many fresh bundles, a lane health summary, a one-shot sync command, and a synthetic smoke harness are now in place for the active blocked `ghidra` lane. The discovery manifest now ranks bundles by queued-candidate match, the runner prioritizes caller-stack-pivot jobs, the symbol-resolution queue makes unresolved offsets explicit, the handoff surface packages runnable work for an operator, refresh and sync now keep that handoff surface current automatically, and the smoke harness proves the lane can leave `idle` even before the next real stack-bearing bundle arrives; the remaining gap is hands-free auto-trigger and real headless execution on a host with `pwsh` + Ghidra available.

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
