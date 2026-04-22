# Power / Kernel Priority Run Order - 2026-04-22

Bu run order, aktif power/kernel sprint icin operator'in ilk kosmasi gereken dar bundle'lari sabitler.

## Step 1 - Reacquire execution-required + global timer narrow symbols

Command:

```bash
python3 scripts/vm-kvm/run-power-kernel-symbol-hunt.py --dry-run
python3 scripts/vm-kvm/run-power-kernel-symbol-hunt.py
```

Why first:

- `AllowSystemRequiredPowerRequests` ve `AllowAudioToEnableExecutionRequiredPowerRequests` icin eldeki en iyi exact current-build narrowing burada.
- `GlobalTimerResolutionRequests` lane'i tekrar genis runtime sprint'e donmeden once symbol surface'i burada daraltmali.

Stop if:

- `execution-required-init-walker` pass'i `0x140C48AB8` veya retained globals'i kaybederse
- `execution-required-setting-callback` pass'i timeout-only branch'i net gostermezse
- `global-timer-resolution-reader` pass'i sadece ayni broad fog'u tekrar ederse

## Step 2 - Classify execution-required timeout branch vs boolean seed branch

Read together:

- `research/notes/power-kernel-exact-experiment-sheet-20260422.md`
- `research/notes/power-kernel-symbol-hunt-targets-20260422.md`

Decision:

- `PopPowerRequestExecutionRequiredSettingCallback` timeout-only semantics veriyorsa bunu `Allow*ExecutionRequired*` boolean seed proof'u gibi yorumlama.
- Boolean seed lane icin primary retained discriminator yine `INIT` walker + wrapper split olarak kalir.

## Step 3 - Run PowerRequestOverride boundary discriminator

Command:

```bash
python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py --verify-only
python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py --dry-run
python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py
```

Why second:

- Override subtree lane'inde sorulacak soru artik "var mi" degil, "response-side mi yoksa UMPO boundary'de mi" sorusu.
- Bu lane execution-required lane'den farkli olarak reader-binding discriminator istiyor.

Stop if:

- `--verify-only` bundle blocker dondururse
- response-side pass sadece wrapper/plumbing ise
- UMPO pass yeni boundary sinyali vermeden generic message fog'a donerse

## Step 4 - Keep watchdog family on hold unless a stronger pivot appears

Hold:

- `PowerWatchdogDrvSetMonitorTimeoutMsec`
- `PowerWatchdogDwmSyncFlushTimeoutMsec`
- `PowerWatchdogPoCalloutTimeoutMsec`
- `PowerWatchdogPowerOnGdiTimeoutMsec`
- `PowerWatchdogRequestQueueTimeoutMsec`

Reason:

- defaults retained, baseline retained, ama current-build path-aware symbol/global pivot henuz yok

Do not:

- broad string batch'i aynen tekrar etme
- generic Procmon replay ile bu aileyi yeniden isitma

## Finish State

Good finish for this sprint su ikisinden en az birini vermeli:

- execution-required pair icin retained current-build seeding discriminator
- PowerRequestOverride icin response-vs-UMPO boundary karari

Bu ikisi yoksa lane'i genisletme; sadece onceki dar passes'ten ne eksik kaldigini not et.
