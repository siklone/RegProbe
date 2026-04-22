# GlobalTimerResolutionRequests Exact Lane Card - 2026-04-22

Bu lane card, `system.kernel.global-timer-resolution-requests` icin "simdi ne biliyoruz", "neyi tekrar etmeyecegiz", ve "bir sonraki dar lane tam olarak ne" sorularini kapatir.

## Target

- Registry path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `GlobalTimerResolutionRequests`
- Current kernel anchor: `nt!KiGlobalTimerResolutionRequests`
- Current posture: `active-narrow`

## What Is Already Strong

- repo docs explicit default veriyor: `0`
- clean baseline parent key'i koruyor, value absent
- exact current-build kernel string hit retained
- local-KD state retained: `KiGlobalTimerResolutionRequests = 0`
- exact narrow command pack hazir:
  - `registry-research-framework/audit/global-timer-resolution-reader-reacquire-local-kd-20260422.txt`
  - `scripts/vm-kvm/run-power-kernel-symbol-hunt.py`

## What Is Already Weak Or Closed

- broad ETW lane exact read vermedi
- Procmon support lane environment timeout ile dustu
- clean WPR/QGA rerun subtree activity verdi ama `GlobalTimerResolutionRequests` value hit'i vermedi
- bu yuzden "bir daha broad runtime replay yapalim" fikri bilgi kazanci dusuk bir tekrar olur

## Exact Question

Bir sonraki soru su:

- `KiGlobalTimerResolutionRequests` etrafinda exact reader/helper shortlist var mi
- yoksa lane current-build tarafinda sadece retained state/global seviyesinde mi kalacak

## Exact Next Commands

Preview:

```bash
python3 scripts/vm-kvm/run-power-kernel-symbol-hunt.py --dry-run
```

Execute:

```bash
python3 scripts/vm-kvm/run-power-kernel-symbol-hunt.py
```

Focus only on this pass:

- `global-timer-resolution-reader`

## Good Outcome

- `KiGlobalTimerResolutionRequests` hala clean resolve olur
- nearby timer-resolution surface broad fog yerine narrower shortlist verir
- shortlist, sonraki static follow-up icin ilk anlamli helper/caller setini cikarir

## Acceptable But Not Closing Outcome

- `KiGlobalTimerResolutionRequests` resolve olur
- ama yanindaki symbol surface hala generic kalir

Bu durumda lane kapanmaz; sadece `active-narrow` olarak kalir.

## Bad Outcome

- exact retained anchor kaybolur
- pass sadece generic `*TimerResolution*` listing'e doner
- yeni artifact onceki broad replay sonucundan daha iyi bir discriminator vermez

Bu durumda lane'i genisletme; retained anchor geri gelmeden yeni runtime sprint acma.

## Do Not Repeat

- broad WPR/QGA timeout lane
- subtree-presence yorumunu exact value-read gibi anlatmak
- generic `*TimerResolution*` widening without `KiGlobalTimerResolutionRequests`

## Meaning

Bu candidate artik docs-only degil, ama exact reader proof'u da degil.

Dogru anlatim:

- retained current-build state var
- retained global anchor var
- exact reader lane hala unresolved
- bir sonraki hamle narrow symbol reacquire ve helper shortlist
