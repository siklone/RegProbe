# Power / Kernel Hold Lanes Board - 2026-04-22

Bu board, aktif sprintte hemen kosulmayan ama unutulmamasini istedigimiz iki lane'i sabitler:

- `system.kernel.global-timer-resolution-requests`
- `power.control.power-watchdog-timeout-cluster`

Amac, bu lane'leri "backlog karanligi"na birakmadan, neyin kanitlandigini ve hangi sinyal gelmeden tekrar genisletilmemesi gerektigini netlestirmek.

## 1) `system.kernel.global-timer-resolution-requests`

- Registry path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `GlobalTimerResolutionRequests`
- Current posture: narrow symbol reacquire lane hazir, broad runtime replay kapali

### What is proved

- exact retained global: `nt!KiGlobalTimerResolutionRequests`
- narrow local-KD pass hazir:
  - `registry-research-framework/audit/global-timer-resolution-reader-reacquire-local-kd-20260422.txt`
  - `scripts/vm-kvm/run-power-kernel-symbol-hunt.py`
- current sprint icin dogru tekrar deneme sekli broad replay degil, narrow symbol reacquire

### What failed already

- broad runtime sprint yeni exact reader vermedi
- onceki WPR/QGA timeout lane actionable reader clue acmadi

### What should unblock it

- `KiGlobalTimerResolutionRequests` etrafinda exact nearby reader/helper shortlist
- timer-resolution family icinde broad fog yerine path-aware symbol cluster

### Do not repeat

- broad timer-resolution replay
- generic `*TimerResolution*` widening without retained `KiGlobalTimerResolutionRequests` anchor

## 2) `power.control.power-watchdog-timeout-cluster`

- Registry path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Values:
  - `PowerWatchdogDrvSetMonitorTimeoutMsec`
  - `PowerWatchdogDwmSyncFlushTimeoutMsec`
  - `PowerWatchdogPoCalloutTimeoutMsec`
  - `PowerWatchdogPowerOnGdiTimeoutMsec`
  - `PowerWatchdogRequestQueueTimeoutMsec`
- Current posture: docs-first hold

### What is proved

- repo docs defaults retained:
  - `PowerWatchdogDrvSetMonitorTimeoutMsec = 10000`
  - `PowerWatchdogDwmSyncFlushTimeoutMsec = 30000`
  - `PowerWatchdogPoCalloutTimeoutMsec = 10000`
  - `PowerWatchdogPowerOnGdiTimeoutMsec = 30000`
  - `PowerWatchdogRequestQueueTimeoutMsec = 30000`
- clean baseline retained:
  - parent `Control\\Power` path exists
  - values absent on clean baseline

### What failed already

- current-build broad string batch clean no-hit
- stronger symbol/global/caller pivot henuz cikmadi

### What should unblock it

- current-build symbol/global that names one of the watchdog leaves
- Ghidra side exact caller that binds one timeout leaf to a path-aware routine
- live runtime lane that is already tied to a watchdog family operation, not generic power noise

### Do not repeat

- same broad string batch
- generic Procmon replay
- value-name existence check'ini tekrar ayni seviyede yapmak

## Fast Routing Rule

- `GlobalTimerResolutionRequests` icin next action:
  - narrow symbol reacquire tamam
  - sonra path-aware shortlist var mi bak
- `PowerWatchdog*TimeoutMsec` icin next action:
  - stronger pivot gelene kadar hold

## Meaning

Bu board'in anlami su:

- `GlobalTimerResolutionRequests` aktif ama narrow
- `PowerWatchdog*TimeoutMsec` retained ama intentionally parked

Ikisini ayni "belki sonra bakariz" torbasina atma; biri active-narrow, digeri docs-first hold.
