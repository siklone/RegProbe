# Single Tweak App QA: SystemResponsiveness

- Generated UTC: `2026-05-12T01:31:04.0535335+00:00`
- Tweak: `power.disable-network-power-saving.policy`
- Status: `ok`
- Success: `True`
- Summary: Apply/verify path completed and rollback restored the tweak.
- Card: `Network Power and Multimedia Responsiveness` / `Power`
- Claim boundary: `True`
- Proof lanes: `docs, runtime, source, rollback`
- Contract: `ok` - QA card snapshot contract passed.

## Stages

- `detect-before`: `Detect - Success` - Detected 1 of 2 values (matches: 0, missing: 1).
- `apply`: `Apply - Success` - Run completed.
- `rollback`: `Rollback - Rolled Back` - Rolled back registry values.
- `detect-after`: `Detect - Success` - Detected 1 of 2 values (matches: 0, missing: 1).
