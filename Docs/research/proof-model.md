# Proof Model And Visual Grammar

RegProbe already tracks a lot of evidence state. This page exists to make that model easier to skim.

## The Default Record Card

When a record is shown to humans, it should be possible to compress it into this shape:

| Field | What it should answer |
|------|------------------------|
| Setting name | What is this? |
| Registry path / value | Where does it live? |
| Plain-English effect | What changes if I apply it? |
| Confidence | How strong is the current proof? |
| Evidence badges | Which proof layers are present? |
| Risk badge | How sensitive is the setting? |
| Windows build coverage | Which builds were actually checked? |
| Rollback status | Can it be undone cleanly? |
| Apply allowed? | Is this safe to surface in the app? |

The repo does not need every page to look identical, but it should teach readers to expect those answers every time.

## Evidence Badges

These badges are meant to compress the repo's actual model, not replace it.

| Badge | Meaning |
|------|---------|
| `Docs` | Official Microsoft or equivalent primary documentation exists |
| `Policy` | ADMX/CSP or policy mapping exists |
| `VM` | Runtime behavior was validated in a VM |
| `Trace` | Procmon, ETW, WPR, or similar trace exists |
| `RE` | Reverse engineering materially contributed |
| `No-hit` | Negative evidence only |
| `Rollback` | Explicit rollback path was tested |
| `Experimental` | Not safe for default app exposure |

## Proof Ladder

Use this to show which kinds of proof are present for a record:

`Docs -> Policy -> VM diff -> Registry observation -> Trace -> RE -> Inference`

Not every setting needs every rung. What matters is that the missing rungs are visible, not hidden.

## Status Colors

| Color | Meaning |
|------|---------|
| `Green` | Executed proof with physical artifacts |
| `Yellow` | Legitimate control surface, runtime not fully mapped |
| `Orange` | Research in progress or missing capture |
| `Gray` | Archived negative evidence or no-hit |
| `Red` | Risky, blocked, or intentionally held |

## Negative Evidence Should Be Legible

Blocked or archived records should not just say "insufficient evidence." They should explain why.

Good blocked summaries usually answer:

- is the registry path real?
- is there any official semantic mapping?
- was the behavior reproduced in VM?
- is there a trace showing a live read or write path?
- is reverse engineering strong enough to justify an app-safe decision?

## Two-Layer Explanation Style

Every important page should prefer this order:

1. one-sentence plain-English summary
2. technical justification

That keeps the repo honest and readable at the same time. The policy and taxonomy still matter, but they land better after the reader already knows the decision.
