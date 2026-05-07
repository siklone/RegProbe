# Blocked Worklist

Generated: `2026-05-07T23:27:59.972902Z`

Blocked candidates: `3`

## Actionability

- `active`: 3

## Lane Summary

- `runtime-trace`: 3 | first: `peripheral.audio-disable-enhancements` | `winopt research list-blocked --worklist --lane runtime-trace --top 5` | Redesign the current app mutation path before more evidence collection; audio enhancements likely need per-device or permission-aware handling, then rerun VM app QA.

## Top Actionable Candidates

- `peripheral.audio-disable-enhancements` (`runtime-trace`, score=29, blockers=1)
- `power.disable-hibernation` (`runtime-trace`, score=29, blockers=1)
- `power.disable-superfetch` (`runtime-trace`, score=29, blockers=1)

## Candidates

### `peripheral.audio-disable-enhancements`

- Lane: `runtime-trace`
- Actionability: `active`
- Priority score: `29`
- Feature area: `Audio Enhancement Flags`
- Key path: `HKCU\Software\Microsoft\Windows\CurrentVersion\Audio + HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render`
- Value name: `EnhancementBundle`
- Blockers: `app-qa-access-denied`
- Suggested command: `winopt research show-blocked peripheral.audio-disable-enhancements --json`
- Next action hint: Redesign the current app mutation path before more evidence collection; audio enhancements likely need per-device or permission-aware handling, then rerun VM app QA.

### `power.disable-hibernation`

- Lane: `runtime-trace`
- Actionability: `active`
- Priority score: `29`
- Feature area: `PowerCfg Hibernation`
- Key path: `powercfg.exe /hibernate`
- Value name: `Mode`
- Blockers: `vm-firmware-hibernation-unsupported`
- Suggested command: `winopt research show-blocked power.disable-hibernation --json`
- Next action hint: Move this to a hibernation-capable VM or bare-metal validation lane; the current VM correctly proves not-applicable handling only.

### `power.disable-superfetch`

- Lane: `runtime-trace`
- Actionability: `active`
- Priority score: `29`
- Feature area: `SysMain Service Stop Command`
- Key path: `sc.exe SysMain`
- Value name: `ServiceState`
- Blockers: `app-qa-clean-baseline-needed`
- Suggested command: `winopt research show-blocked power.disable-superfetch --json`
- Next action hint: Provision a clean baseline where the target service is enabled/running, then rerun VM app QA to prove apply and rollback mutation.
