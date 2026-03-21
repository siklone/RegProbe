# Single-Value Validation Protocol

This note defines the validation workflow for settings that expose a single registry value name or a single obvious control surface.

The goal is to prove three things:

1. What the value means.
2. Which process or surface reads or writes it at runtime.
3. That the record can be restored to the original state after testing.

## When To Use This Workflow

Use this workflow when a record has one dominant value target and the semantics are not already fully obvious from a primary Microsoft source.

Typical cases:

- one registry value under one key path
- a single user preference or policy toggle
- a raw numeric setting that is not clearly documented as a full enum list

## Evidence Order

Prefer the cheapest proof that still answers the question honestly:

1. Official Microsoft documentation, ADMX, ADML, or Policy CSP
2. Local UI or settings diff in the Win25H2Clean VM
3. Procmon runtime read/write proof
4. WPR or WPA when the behavior is performance-related
5. Ghidra or decompiled pseudocode for undocumented internal behavior

Do not jump to Ghidra if the registry meaning is already clear from docs or a policy mapping.

## Required Test Cycle

Every runtime-backed single-value validation should include a reversible cycle:

- baseline state
- apply candidate state
- verify the observed effect
- restore the original state

If the setting is behavior-sensitive, keep the capture isolated in the VM and avoid host-side validation.

## Choosing The Second State

Pick the nearest safe alternate state:

- binary value: test the opposite state
- enum value: test the documented adjacent or contrasting state
- raw bitmask or scheduler value: test the most defensible nearby value, then explain why it was chosen

If there is no safe alternate state, stop and keep the record as research-only.

## What To Record

Write the proof in machine-readable form and keep the human summary short.

Capture:

- exact registry path
- exact value name
- baseline value
- candidate value
- observed process or UI trigger
- observed read or write path
- restore result
- artifact file path
- confidence level

## Nohuto Rule

Nohuto can be used for lineage, upstream naming, or historical context.
It must not be presented as the source of value semantics unless the record explicitly says so.

If a record cites nohuto, state exactly which file or dump path provided the lineage.

## Output Style

Validation notes should be concise and explicit.

Recommended structure:

```text
Target: <record id>
Path: <registry path>
Value: <name>
Baseline: <state>
Candidate: <state>
Observed: <what changed>
Restore: <how it was restored>
Artifacts: <files>
```

If WPR is used, include the ETL path and the metric or event that changed.
If Ghidra is used, include the binary or pseudocode path and the branch or comparison that explains the value.
