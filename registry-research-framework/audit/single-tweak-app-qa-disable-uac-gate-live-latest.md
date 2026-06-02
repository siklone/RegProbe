# Disable UAC App Gate QA

- Tweak: `security.disable-uac`
- VM: `regprobe-win11-25h2-session`
- Result: `not-app-card-ready`
- Success flag: `false`
- Stages: `0`
- Mutating path: blocked before detect/apply/rollback
- Summary: This tweak exists in the catalog, but it is not a shipped end-user app card. Use Contributor Lab or the explicit QA-only gated mutation override for research review.

## Proof Lane Snapshot

| Lane | State | Note |
| --- | --- | --- |
| Docs | `ready` | Repo security docs identify the legacy full-UAC-disable command. |
| Runtime | `partial` | Runtime proof is still missing; no app-card-ready runtime claim is made. |
| Source | `partial` | Source lane is contributor context only; catalog/source context is not treated as value-behavior proof. |
| Rollback | `ready` | Record restore story is present, but promotion remains rejected because the action is high-risk. |

## Acceptance

- Normal app QA does not run this record as a shipped card.
- Startup/open-tweak navigation requires `IsEndUserAppCardAllowed`.
- Contributor/VM research can still use the explicit gated override when the operator intentionally accepts that risk.
