# Undocumented Key Runtime Validation

Use this note for keys that remain undocumented after the normal documentation-first pass.

This is a later-phase workflow. It does not replace primary-source research.

## Purpose

When Microsoft Learn, ADMX, ADML, CSP, or another primary Microsoft source does not define the full key semantics, runtime testing can still help us answer narrower questions:

- Is the key real on a tested build?
- Which process writes or reads it?
- Do tested values such as `missing`, `0`, `1`, or `2` produce different observed behavior?
- Does the key look ignored, mirrored, or actively consumed?

## Escalation Order For Enum And Range Values

Do not jump directly to reverse engineering for every non-binary value.

Use this escalation order:

1. ADMX enum or `valueList`
2. ADML or Microsoft Learn behavior text
3. WPR or ETW behavioral diff
4. Ghidra or other reverse engineering

Rule of thumb:

- If ADMX explicitly lists the values, that is usually enough to define the documented enum meanings.
- If ADMX does not define the meanings clearly, WPR or ETW is the next runtime tool for comparing value-driven behavior.
- Use Ghidra only after documentation and runtime behavioral tracing have failed to explain the value.

Suggested mapping:

| Situation | Preferred Tool |
| --- | --- |
| Binary on/off with explicit ADMX mapping | ADMX and optional Procmon |
| Enum with ADMX `valueList` | ADMX |
| Enum with no ADMX mapping and no Microsoft Learn explanation | WPR or ETW |
| Fully undocumented value with no useful source | Ghidra |

## What Runtime Testing Can Prove

Runtime testing can support:

- exact runtime writes and reads on a specific build
- observed behavior for tested values
- whether `missing` behaves like one tested value or another
- whether a value appears ignored
- which process or service consumes the setting

## What Runtime Testing Cannot Prove By Itself

Runtime testing does not prove:

- the complete allowed range
- that untested values are invalid
- that behavior is stable across all Windows versions
- that a runtime key is the official management surface
- that the observed result is the only side effect

Treat runtime testing as observed semantics, not exhaustive semantics.

## When To Use This Workflow

Use it when all of the following are true:

- the normal documentation pass is already exhausted or blocked
- the key still matters to a real tweak or backlog decision
- we can test safely in a VM or otherwise reversible environment

Do not use this workflow to skip obvious documentation work.

## Minimum Test Matrix

At minimum, test:

- `missing`
- `0`
- `1`

If the key appears enum-like or multi-state, also test:

- `2`
- `3`
- any value already seen in the app, a VM, Procmon, or reverse engineering notes

If a value causes instability, stop and record that outcome instead of broadening the range blindly.

## Suggested Workflow

1. Capture the baseline.
   Record the current build, edition, key presence, related services, and visible behavior before changing anything.
2. Test the `missing` state first.
   Confirm whether the key is absent and document the visible baseline behavior.
3. Apply one value at a time.
   Change only one candidate value per test run.
4. Observe runtime activity.
   Use Procmon where possible to capture `RegSetValue`, `RegQueryValue`, `RegOpenKey`, and `RegCreateKey`.
5. Observe user-visible behavior.
   Check UI state, feature behavior, logs, service reactions, or command output.
6. Reboot or restart the relevant process if needed.
   Some keys only take effect after Explorer restart, service restart, sign-out, or reboot.
7. Roll back to baseline between trials.
   Avoid carrying state forward from one tested value into the next.

## Recommended Evidence Stack

For undocumented or partially documented keys, prefer stacking evidence in this order:

1. Procmon trace
2. UI or behavior diff
3. Event Log, ETW, or service log if available
4. Reverse engineering or binary references
5. Community reports only as weak supporting context

## Recording Template

Use a simple table like this while testing:

| Build | Key State | Process Read/Write | Observed Effect | Confidence |
| --- | --- | --- | --- | --- |
| `10.0.x` | `missing` | `Explorer.exe RegQueryValue` | Baseline behavior | `low` |
| `10.0.x` | `0` | `svchost.exe RegQueryValue` | Feature off observed | `medium` |
| `10.0.x` | `1` | `svchost.exe RegQueryValue` | Feature on observed | `medium` |

## How To Write The Record

If the key is still undocumented after runtime testing:

- keep only tested values in `allowed_values`
- use plain language such as `Observed off on tested build` instead of over-claiming
- keep `decision.confidence` at `low` or `medium` unless multiple strong runtime layers agree
- keep `decision.needs_vm_validation = true` unless the testing is already clean and repeatable
- state clearly that the range is runtime-observed, not exhaustively proven

Suggested wording:

- `Observed on tested build`
- `Appeared ignored on tested build`
- `Matched missing state on tested build`
- `Runtime-observed only; exhaustive range not proven`

## Publication Rule

Do not promote an undocumented key to a normal user-facing apply path just because `0` and `1` appeared to work once.

Runtime-only keys should usually remain:

- `review-required`
- `docs-first`
- or `validated` with conservative confidence and explicit caveats

Only move further when the evidence stack is strong enough for the risk level of the setting.
