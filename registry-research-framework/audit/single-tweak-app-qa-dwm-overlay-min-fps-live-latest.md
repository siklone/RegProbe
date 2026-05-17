# Single Tweak App QA: DWM Overlay Minimum FPS

Generated: `2026-05-17T17:45:50Z`

## Summary

| Field | Value |
|---|---|
| Tweak ID | `system.dwm-disable-overlay-min-fps` |
| Tweak name | DWM Overlay Minimum FPS Check |
| Status | `ok` |
| Success | `true` |
| Evidence class | `A` |
| Research status | `PROMOTED` |
| Rollback state | `ready` |

Apply/verify path completed and rollback restored the tweak.

## Stage Results

| Stage | Applied status | Status message | Current value |
|---|---|---|---|
| `detect-before` | `NotApplied` | Value not set. | `Not set` |
| `apply` | `Applied` | Run completed. | `0 (0x0)` |
| `rollback` | `NotApplied` | Removed value to restore default. | `0 (0x0)` |
| `detect-after` | `NotApplied` | Value not set. | `Not set` |

## Card Contract

| Check | Result |
|---|---|
| Claim boundary present | `true` |
| Proof lanes | `docs`, `runtime`, `source`, `rollback` |
| Missing card fields | none |

Source JSON: `registry-research-framework/audit/single-tweak-app-qa-dwm-overlay-min-fps-live-latest.json`
