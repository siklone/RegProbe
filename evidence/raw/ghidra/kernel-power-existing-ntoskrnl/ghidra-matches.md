# Ghidra Branch Review

- Program: `ntoskrnl.exe`
- Probe: `kernel-power-existing-ntoskrnl-branch-template-refresh-branch-template-refresh`
- Timestamp: `2026-04-01T03:47:48.265287100Z`
- PDB source: `C:\Tools\Symbols\kernel-power-existing-ntoskrnl-branch-template-refresh-branch-template-refresh`
- Patterns: `WatchdogResumeTimeout, WatchdogSleepTimeout, AdditionalCriticalWorkerThreads, AdditionalDelayedWorkerThreads, UuidSequenceNumber, AllowRemoteDASD`
- Raw matches: `6`
- Committed matches: `1`
- Omitted additional symbolized branch hits: `0`
- Omitted unresolved review hits: `4`
- Omitted lower-confidence review hits: `1`

## `WatchdogResumeTimeout`

_Review-only string hits existed for this pattern, but no committed branch block survived compaction. Raw unresolved output stays out of the committed surface._

## `WatchdogSleepTimeout`

_Review-only string hits existed for this pattern, but no committed branch block survived compaction. Raw unresolved output stays out of the committed surface._

## `AdditionalCriticalWorkerThreads`

_Review-only string hits existed for this pattern, but no committed branch block survived compaction. Raw unresolved output stays out of the committed surface._

## `AdditionalDelayedWorkerThreads`

_Review-only string hits existed for this pattern, but no committed branch block survived compaction. Raw unresolved output stays out of the committed surface._

## `UuidSequenceNumber`

_No matching strings found._

## `AllowRemoteDASD`

### Branch @ `1404cb6b0`

- Function: `IopAllowRemoteDASD`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Register focus: `EAX, RCX, RBP, R9, RDX, EBX`
- Flag focus: `ZF, SF, CF, OF`
- Compare: `1404cb6bc  TEST EAX,EAX`
- Jump: `1404cb6be  JS 0x1404cb6e3`
- Value mapping: `value=1 participates in this conditional block; opposite branch still needs explicit review.`
- Branch effect: `compare + conditional jump recovered in bounded context.`
- Stack note: `stack-relative access is visible in the bounded context; review local variables and home-space assumptions before claiming semantics.`
- Exception gate: `none`
- Heuristic score: `95`
- Heuristic reasons: `pdb-symbol present | compare+jump survived bounded symbolized review | compare/test anchor found | conditional jump found | value immediate found in bounded block | stack-relative context detected`
- Effect: PDB-backed function identity, compare/jump structure, and a bounded value map are present.
- Unclear: `False`

```asm
; context_before
1404cb6a1  TEST EAX,EAX
1404cb6a3  JS 0x1404cb6ec
1404cb6a5  MOV RCX,qword ptr [RBP + 0x10]
1404cb6a9  LEA R9,[RBP + 0x18]
1404cb6ad  XOR R8D,R8D
; branch_snippet
1404cb6b0  LEA RDX,[0x1406b8040]
1404cb6bc  TEST EAX,EAX
1404cb6be  JS 0x1404cb6e3
1404cb6c4  CMP dword ptr [RCX + 0xc],EBX
1404cb6a3  JS 0x1404cb6ec
1404cb6a1  TEST EAX,EAX
; context_after
1404cb6b7  CALL 0x1409b29dc
1404cb6bc  TEST EAX,EAX
1404cb6be  JS 0x1404cb6e3
1404cb6c0  MOV RCX,qword ptr [RBP + 0x18]
1404cb6c4  CMP dword ptr [RCX + 0xc],EBX
```

_Omitted 0 additional symbolized branch hit(s), 1 lower-confidence review hit(s), and 0 unresolved hit(s) from the committed surface._
