# Ghidra Branch Review

- Program: `ntoskrnl.exe`
- Probe: `kernel-power-nextgate-ntoskrnl-branch-template-refresh`
- Timestamp: `2026-04-01T04:02:22.383724100Z`
- PDB source: `C:\Tools\Symbols\kernel-power-nextgate-ntoskrnl-branch-template-refresh`
- Patterns: `WatchdogResumeTimeout`, `WatchdogSleepTimeout`, `AdditionalCriticalWorkerThreads`, `AdditionalDelayedWorkerThreads`

## `WatchdogResumeTimeout`

### String @ `140c6a618`

`WatchdogResumeTimeout`

- Function: `<no function>`
- Function source: `unresolved`
- Function confidence: `string_only_review`
- Address: `140c63608`
- Register focus: `RAX`
- Flag focus: `unclear`
- Compare: `unclear`
- Jump: `unclear`
- Value mapping: `unclear`
- Branch effect: `trap/fault-adjacent block detected; control-flow may be misleading.`
- Stack note: `no obvious stack-relative access in the bounded context.`
- Exception gate: `trap-or-fault-adjacent instructions present; control-flow may be misleading.`
- Heuristic score: `0`
- Heuristic reasons: `exception/trap gate forced review-only`
- Effect: unclear - exception-adjacent control flow needs manual review before any semantic claim.
- Unclear: `true`

```asm
; context_before
140c618b1  HLT
140c619a8  ADD byte ptr CS:[RAX],DH
140c619ab  ADD byte ptr [RAX],AL
140c619ad  ADD byte ptr [RAX],AL
140c619af  ADD byte ptr [RAX],DL
; branch_snippet
140c619b1  INT1
; context_after
```

## `WatchdogSleepTimeout`

### String @ `140c6a648`

`WatchdogSleepTimeout`

- Function: `<no function>`
- Function source: `unresolved`
- Function confidence: `string_only_review`
- Address: `140c635d8`
- Register focus: `RAX`
- Flag focus: `unclear`
- Compare: `unclear`
- Jump: `unclear`
- Value mapping: `unclear`
- Branch effect: `trap/fault-adjacent block detected; control-flow may be misleading.`
- Stack note: `no obvious stack-relative access in the bounded context.`
- Exception gate: `trap-or-fault-adjacent instructions present; control-flow may be misleading.`
- Heuristic score: `0`
- Heuristic reasons: `exception/trap gate forced review-only`
- Effect: unclear - exception-adjacent control flow needs manual review before any semantic claim.
- Unclear: `true`

```asm
; context_before
140c618b1  HLT
140c619a8  ADD byte ptr CS:[RAX],DH
140c619ab  ADD byte ptr [RAX],AL
140c619ad  ADD byte ptr [RAX],AL
140c619af  ADD byte ptr [RAX],DL
; branch_snippet
140c619b1  INT1
; context_after
```

## `AdditionalCriticalWorkerThreads`

### String @ `140c6a210`

`AdditionalCriticalWorkerThreads`

- Function: `<no function>`
- Function source: `unresolved`
- Function confidence: `string_only_review`
- Address: `140c62b88`
- Register focus: `RAX`
- Flag focus: `unclear`
- Compare: `unclear`
- Jump: `unclear`
- Value mapping: `unclear`
- Branch effect: `trap/fault-adjacent block detected; control-flow may be misleading.`
- Stack note: `no obvious stack-relative access in the bounded context.`
- Exception gate: `trap-or-fault-adjacent instructions present; control-flow may be misleading.`
- Heuristic score: `0`
- Heuristic reasons: `exception/trap gate forced review-only`
- Effect: unclear - exception-adjacent control flow needs manual review before any semantic claim.
- Unclear: `true`

```asm
; context_before
140c618b1  HLT
140c619a8  ADD byte ptr CS:[RAX],DH
140c619ab  ADD byte ptr [RAX],AL
140c619ad  ADD byte ptr [RAX],AL
140c619af  ADD byte ptr [RAX],DL
; branch_snippet
140c619b1  INT1
; context_after
```

## `AdditionalDelayedWorkerThreads`

### String @ `140c6a1c8`

`AdditionalDelayedWorkerThreads`

- Function: `<no function>`
- Function source: `unresolved`
- Function confidence: `string_only_review`
- Address: `140c62bb8`
- Register focus: `RAX`
- Flag focus: `unclear`
- Compare: `unclear`
- Jump: `unclear`
- Value mapping: `unclear`
- Branch effect: `trap/fault-adjacent block detected; control-flow may be misleading.`
- Stack note: `no obvious stack-relative access in the bounded context.`
- Exception gate: `trap-or-fault-adjacent instructions present; control-flow may be misleading.`
- Heuristic score: `0`
- Heuristic reasons: `exception/trap gate forced review-only`
- Effect: unclear - exception-adjacent control flow needs manual review before any semantic claim.
- Unclear: `true`

```asm
; context_before
140c618b1  HLT
140c619a8  ADD byte ptr CS:[RAX],DH
140c619ab  ADD byte ptr [RAX],AL
140c619ad  ADD byte ptr [RAX],AL
140c619af  ADD byte ptr [RAX],DL
; branch_snippet
140c619b1  INT1
; context_after
```

