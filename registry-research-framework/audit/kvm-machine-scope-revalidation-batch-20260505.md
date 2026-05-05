# KVM Machine-Scope Revalidation Batch

Date: 2026-05-05T13:08:51.8557437Z
Domain: `regprobe-win11-25h2-session`

This batch re-read selected machine-scope service and registry targets on the live KVM guest to refresh age-based validation state without changing the guest configuration.

## Machine

- Computer: `DESKTOP-AHPV0FV`
- CurrentBuildNumber: `26200`
- UBR: `8246`
- BuildLabEx: `26100.1.amd64fre.ge_release.240331-1435`

## Observations

### `system.services.disable-connected-user-experiences`

- Service pattern: `DiagTrack`
- Match count: `1`
- `DiagTrack`: start `Auto`, state `Running`

### `system.services.disable-print-notifications`

- Service pattern: `PrintNotify`
- Match count: `1`
- `PrintNotify`: start `Manual`, state `Stopped`

### `system.services.disable-print-spooler`

- Service pattern: `Spooler`
- Match count: `1`
- `Spooler`: start `Auto`, state `Running`

### `system.services.disable-bluetooth-support`

- Service pattern: `bthserv`
- Match count: `1`
- `bthserv`: start `Manual`, state `Stopped`

### `system.services.disable-bluetooth-user-service`

- Service pattern: `BluetoothUserService_*`
- Match count: `1`
- `BluetoothUserService_410af`: start `Manual`, state `Stopped`

### `system.services.disable-bluetooth-audio-gateway`

- Service pattern: `BTAGService`
- Match count: `1`
- `BTAGService`: start `Manual`, state `Stopped`

### `system.wait-to-kill-service-timeout`

- Path: `HKLM\SYSTEM\CurrentControlSet\Control`
- Value: `WaitToKillServiceTimeout`
- Path exists: `True`
- Value exists: `True`
- Observed value: `5000`

### `system.priority-control`

- Path: `HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl`
- Value: `Win32PrioritySeparation`
- Path exists: `True`
- Value exists: `True`
- Observed value: `2`

### `power.control.hibernate-enabled`

- Path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value: `HibernateEnabled`
- Path exists: `True`
- Value exists: `True`
- Observed value: `0`

