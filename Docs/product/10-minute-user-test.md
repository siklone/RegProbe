# 10-Minute User Test

This is the lightweight product sanity test for a fresh RegProbe user. It is
not a research campaign and it does not require the user to understand VM
runners, ETW, Procmon, Ghidra, tranches, or host-noise gates.

## Goal

Watch whether a new user can answer four questions before changing anything:

1. What does this card do?
2. What is my current system value?
3. What value will RegProbe apply, and what is the known/default value when
   available?
4. How will I verify and roll back the change?

## Setup

- Use a normal Windows desktop or the standard test VM.
- Start with the WPF app only.
- Do not explain the research pipeline unless the user explicitly asks.
- Pick one low-risk, apply-allowed card with a clear rollback story.

## Script

1. Open RegProbe and ask the user to pick one card that looks understandable.
2. Ask them to describe the verdict, confidence, and risk badge in their own
   words.
3. Ask them to find the current value, known/default value when available, and
   target value.
4. Ask them what rollback will restore or delete.
5. Ask them to open the evidence drawer and explain what is known versus what
   RegProbe does not claim.
6. Ask them to run Preview or a dry/safe path before Apply.
7. If the environment is disposable, apply the card, verify it, then restore the
   previous state.

## What To Capture

Record short notes, not a transcript:

- Which label or button caused hesitation?
- Did the user see current/default/target values before Apply?
- Did the rollback story feel concrete?
- Did evidence details feel useful or too technical?
- Did any contributor-only words leak into the normal flow?
- Did the user trust the verdict more after opening the evidence drawer?

## Pass Criteria

- The user can find current, default or known baseline, target, and rollback
  before applying.
- The user can distinguish a verified claim from a bounded or missing claim.
- The user does not need VM, tranche, noise-gate, ETW, Procmon, or Ghidra
  vocabulary to make a normal app decision.
- Any hesitation is captured as a product issue or copy follow-up.
