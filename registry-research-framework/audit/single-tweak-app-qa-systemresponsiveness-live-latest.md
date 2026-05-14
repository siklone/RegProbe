# Single Tweak App QA: SystemResponsiveness

- Generated UTC: `2026-05-14T10:20:44.9203191+00:00`
- Tweak: `power.disable-network-power-saving.policy`
- Status: `ok`
- Success: `True`
- Summary: Apply/verify path completed and rollback restored the tweak.
- Card: `Network Power and Multimedia Responsiveness` / `Power`
- Claim boundary: `True`
- Proof lanes: `docs, runtime, source, rollback`
- Contract: `ok` - QA card snapshot contract passed.

## Value Story

- `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\SystemResponsiveness`: current `20`, target `10`, verified after apply.
- `HKLM\System\CurrentControlSet\Services\TCPIP\Parameters\DisableTaskOffload`: current `missing`, target set by the card, verified after apply.
- Rollback restored the previous registry state after verification.

## Stages

- `detect-before`: `Detect - Success` - Detected 1 of 2 values (matches: 0, missing: 1).
- `apply`: `Apply - Success` - Run completed.
- `rollback`: `Rollback - Rolled Back` - Rolled back registry values.
- `detect-after`: `Detect - Success` - Detected 1 of 2 values (matches: 0, missing: 1).
