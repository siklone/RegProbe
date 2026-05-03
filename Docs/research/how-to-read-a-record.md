# How To Read A Record

RegProbe records are easiest to understand when you read them in two layers:

1. plain-English verdict
2. technical justification

The repo is rigorous on purpose, but the fastest reading path is not to start with taxonomy. Start with the decision, then the proof behind it.

## The Four Questions

Every setting or evidence record should answer these four questions clearly:

- What is this setting?
- How strong is the proof?
- What happens if I apply it?
- How do I undo it?

If one of those answers is missing, the record is not done yet.

## Recommended Reading Order

Read a record in this order:

1. `Verdict`
   This tells you whether the setting is safe to expose, still blocked, intentionally held, or archived.
2. `Plain-English explanation`
   One sentence describing what the setting appears to do and why the current decision was made.
3. `Proof snapshot`
   Look for whether the record has `Docs`, `Policy`, `VM`, `Trace`, `RE`, `No-hit`, and `Rollback`.
4. `Risk snapshot`
   This should explain whether the setting is machine-wide, boot-sensitive, driver-sensitive, or otherwise risky.
5. `Artifacts`
   Physical artifacts matter. Captures, hashes, and timestamps are what turn claims into evidence.

## What The Proof Layers Mean

### Control Surface Proof

This is where the repo proves that Windows exposes a legitimate setting surface:

- official docs
- ADMX/CSP or policy mapping
- current app/provider mapping

This is valuable, but it does not automatically prove runtime behavior.

### Runtime Proof

This is where the repo shows live behavior:

- VM state diffs
- Procmon
- ETW
- WPR
- boot traces

If a record talks about kernel, boot, power, or driver behavior, runtime proof matters more than a nice doc page.

### Interpretation

This is the judgment layer:

- should the app expose it?
- is apply allowed?
- what is still unknown?
- what is the rollback path?

RegProbe tries hard not to blur this layer with the evidence itself.

## Status Legend

- `Green`: executed proof with physical artifacts
- `Yellow`: legitimate control surface, runtime not fully mapped
- `Orange`: research in progress or missing capture
- `Gray`: archived negative evidence or no-hit
- `Red`: risky, blocked, or intentionally held from apply

## A Good Record Summary

The most useful records can be summarized like this:

### Verdict

Blocked from Apply pending runtime validation.

### Plain-English explanation

Windows is observed as exposing the setting as a real control surface, but the repo does not yet have enough live proof to say the behavior is honored consistently on current builds.

### Proof snapshot

`Docs ✓  Policy ✓  VM ✗  Trace ✗  RE partial`

### Risk snapshot

System-policy sensitive, rollback path defined, not recommended for host-first testing.

### Artifacts

`2 files, SHA-256 recorded, captured on 2026-04-xx`

That structure is intentionally simple. It helps contributors avoid overclaiming, and it helps readers understand why a record is shippable, blocked, or still research-only.
