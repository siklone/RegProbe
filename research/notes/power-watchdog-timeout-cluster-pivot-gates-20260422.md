# PowerWatchdog Timeout Cluster Pivot Gates - 2026-04-22

Bu note, `PowerWatchdog*TimeoutMsec` family icin hangi sinyal gelmeden lane'i yeniden isitmamamiz gerektigini sabitler.

## Family

- Registry path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Values:
  - `PowerWatchdogDrvSetMonitorTimeoutMsec`
  - `PowerWatchdogDwmSyncFlushTimeoutMsec`
  - `PowerWatchdogPoCalloutTimeoutMsec`
  - `PowerWatchdogPowerOnGdiTimeoutMsec`
  - `PowerWatchdogRequestQueueTimeoutMsec`

## What Is Actually Proved

- repo docs defaults retained:
  - `10000`
  - `30000`
  - `10000`
  - `30000`
  - `30000`
- clean baseline retained:
  - parent key exists
  - all five values absent
- current-build broad string batch clean no-hit
- enrichment already routes the family toward low-priority `power-request-simulation` / `windbg`

## Why The Lane Is Still On Hold

Eksik olan sey "daha cok runtime" degil; eksik olan sey path-aware pivot.

Su an yok:

- exact symbol
- exact global
- exact caller
- exact current-build string hit

Bunlardan biri olmadan lane'i yeniden acmak, ayni broad no-hit ciktilarini tekrar uretme riski tasiyor.

## Valid Pivot Gates

Lane'i ancak su sinyallerden biri gelirse yeniden ac:

### Gate 1 - exact current-build symbol/global

Ornek:

- bir watchdog leaf'i dogrudan isimleyen current-build symbol
- leaf timeout global'ini gosteren local-KD anchor

### Gate 2 - exact Ghidra string/caller

Ornek:

- watchdog leaf name'ini tasiyan new string hit
- string'den reader/helper/caller'a giden bounded callee chain

### Gate 3 - runtime trigger already tied to watchdog semantics

Ornek:

- generic power noise degil
- watchdog family ile dogrudan iliskili live trigger family

### Gate 4 - crossover clue from another retained lane

Ornek:

- `Win32kCalloutWatchdogTimeoutSeconds` gibi yakindan iliskili bir lane'den watchdog leaf'lere gecis veren exact helper/caller

## Invalid Reasons To Reopen

- "uzun zamandir bakmadik"
- "bir de Procmon deneyelim"
- "belki broad string batch bu sefer farkli cikar"
- "docs'ta geciyor, o zaman runtime'da da gorunmeli"

Bunlar pivot gate degil.

## If A Gate Opens

Ilk hamle maksimum dar kalmali:

1. exact symbol/global/caller'i sabitle
2. tek leaf uzerinden bounded command pack hazirla
3. family-wide runtime sweep'e hemen cikma

## Meaning

Bu family "iptal" degil.

Ama su an dogru durum su:

- retained defaults lane'i
- path-aware pivot bekleyen docs-first hold
- stronger clue gelmeden intentionally parked
