# Power / Kernel Exact Experiment Sheet - 2026-04-22

Bu sheet, aktif power/kernel research lane'i icin "hangi key/value", "ne denendi", "neyin no-hit oldugu", ve "siradaki dar komut ne" sorularini tek yerde toplar.

## 1) `power.control.allow-system-required-power-requests`

- Registry path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `AllowSystemRequiredPowerRequests`
- Current posture: static seeding path guclu, live current-build read henuz yok
- Proved:
  - static `INIT` walker lane `0x140C48AB8` icinde retained
  - descriptor row: `Power` + `AllowSystemRequiredPowerRequests` -> `0x140FD7114`
  - target global: `nt!PopPowerRequestConvertSystemToExecution`
- Failed / no-hit:
  - broad runtime replay bu value icin exact read call'i vermedi
  - timeout callback lane bunun boolean seed okuyucusu degil
- Next narrow command:

```bash
python3 scripts/vm-kvm/run-power-kernel-symbol-hunt.py --dry-run
python3 scripts/vm-kvm/run-power-kernel-symbol-hunt.py
```

- Pass to watch: `execution-required-init-walker`
- Success marker:
  - `PopPowerRequestConvertSystemToExecution`
  - `0x140C48AB8`
  - wrapper split `0x140C483EF` / `0x140C48414`

## 2) `power.control.allow-audio-to-enable-execution-required-power-requests`

- Registry path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `AllowAudioToEnableExecutionRequiredPowerRequests`
- Current posture: system pair ile ayni `INIT` walker tablosunda, live read hala unresolved
- Proved:
  - descriptor row: `Power` + `AllowAudioToEnableExecutionRequiredPowerRequests` -> `0x140FD71A0`
  - target global: `nt!PopPowerRequestActiveAudioEnablesExecutionRequired`
  - wrapper selector farki retained: `r8b=0` vs `r8b=1`
- Failed / no-hit:
  - broad symbol hunt bunu tek basina yeni bir reader'a tasimadi
  - callback lane timeout branch'i gosterdi, audio/system boolean seed'i degil
- Next narrow command:

```bash
python3 scripts/vm-kvm/run-power-kernel-symbol-hunt.py
```

- Passes to watch:
  - `execution-required-init-walker`
  - `execution-required-setting-callback`
- Success marker:
  - `PopPowerRequestActiveAudioEnablesExecutionRequired`
  - `GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT` ayni bundle icinde timeout-only branch'i ayirir

## 3) `power.control.power-request-override-subtree`

- Registry path:
  - `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride\Process`
  - `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride\Service`
  - `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride\Driver`
- Observed storage model:
  - `powercfg /requestsoverride` materialization'i retained
  - request bitmask `7` consumer-side comparison icin en guclu retained clue
- Proved:
  - response-side boundary target: `nt!PopPowerRequestHandleRequestOverrideQueryResponse`
  - transport-side target: `nt!PopUmpoSendPowerMessage`
  - query-side target: `nt!PopUmpoSendPowerRequestOverrideQuery`
- Failed / no-hit:
  - generic `*PowerRequest*Reg*` sweep lane'i kapandi
  - broad subtree replay yeni semantic proof getirmedi
- Next narrow command:

```bash
python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py --verify-only
python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py --dry-run
python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py
```

- Success marker:
  - response pass'te exact registry read
  - ya da UMPO message pass'te acik transport boundary

## 4) `system.kernel.global-timer-resolution-requests`

- Registry path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `GlobalTimerResolutionRequests`
- Current posture: docs/runtime hold degil; symbol reacquire ile reader shortlist daraltma asamasinda
- Proved:
  - target global retained: `nt!KiGlobalTimerResolutionRequests`
  - separate timer-resolution symbol surface re-open icin narrow pass hazir
- Failed / no-hit:
  - onceki broad runtime sprint yeni reader vermedi
  - WPR/QGA timeout sprinti actionable path acmadi
- Next narrow command:

```bash
python3 scripts/vm-kvm/run-power-kernel-symbol-hunt.py
```

- Pass to watch: `global-timer-resolution-reader`
- Success marker:
  - `KiGlobalTimerResolutionRequests`
  - exact nearby symbol shortlist

## 5) `power.control.power-watchdog-timeout-cluster`

- Registry path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value family:
  - `PowerWatchdogDrvSetMonitorTimeoutMsec`
  - `PowerWatchdogDwmSyncFlushTimeoutMsec`
  - `PowerWatchdogPoCalloutTimeoutMsec`
  - `PowerWatchdogPowerOnGdiTimeoutMsec`
  - `PowerWatchdogRequestQueueTimeoutMsec`
- Current posture: docs-first hold, but not forgotten
- Proved:
  - repo defaults retained:
    - `10000`, `30000`, `10000`, `30000`, `30000`
  - clean baseline: parent key var, values absent
- Failed / no-hit:
  - current-build broad string batch clean no-hit
  - stronger symbol/global pivot henuz yok
- Next narrow move:
  - yeni path-aware clue gelmeden genis runtime lane acma
  - sadece stronger symbol/global clue cikarsa local-KD pass ekle

## Fast Read

- Hemen kosulacak bundle: `scripts/vm-kvm/run-power-kernel-symbol-hunt.py`
- Override boundary icin kosulacak bundle: `scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py`
- Su an bilincli olarak bekleyen docs-first lane: `PowerWatchdog*TimeoutMsec`
- Su an en iyi exact proof adayi:
  - execution-required pair icin retained `INIT` walker
  - override subtree icin response-vs-UMPO discriminator
