Execution-required pair icin 2026-04-11 static init-walker follow-up:

- Host-side `objdump` follow-up, `0x140C48AB8` adresinde labelsiz bir `INIT` routine buldu.
- Bu routine `0x140C48AF1` noktasinda `0x140C72E30` tablosunu yukluyor; taranan executable section'larda bu tabloya giden baska bir `lea` gorunmedi.
- Iki local wrapper bu routine'i cagiriyor:
  - `0x140C483EF` with `r8b=0`
  - `0x140C48414` with `r8b=1`
- Loop icinde gorunen zincir:
  - `mov r8, [rbx]` ardindan `call 0x1407E334C`
  - `0x1407E334C`, row key string'den `UNICODE_STRING` olusturup `0x1407E3394` child-entry arama helper'ina iletiyor; bu, subkey lookup icin guclu bir isaret.
  - `mov rdx, [rbx+0x8]` ardindan `call 0x14086A794`
  - `0x14086A794`, located key object'e `+0x24` ekleyip `0x14086C510`'a gidiyor.
  - `0x14086C510`, istenen UTF-16 value name'i entry strings ile case-insensitive karsilastiriyor ve miss durumunda `STATUS_OBJECT_NAME_NOT_FOUND` / out index `-1` donduruyor; bu da value-name lookup davraniyisi veriyor.
  - `mov r8, [rbx+0x10]` ardindan `call 0x140C4CB20`
  - `0x140C4CB20`, bulunan value blob'unu row target pointer'ina kopyaliyor.
- Execution-required pair ayni tablo icinde:
  - `0x140C76250`: `Power` + `AllowSystemRequiredPowerRequests` -> `0x140FD7114` -> `nt!PopPowerRequestConvertSystemToExecution`
  - `0x140C76280`: `Power` + `AllowAudioToEnableExecutionRequiredPowerRequests` -> `0x140FD71A0` -> `nt!PopPowerRequestActiveAudioEnablesExecutionRequired`

Inference:

- Bu labelsiz `INIT` walker, execution-required pair icin current-build static registry seeding path olarak okunmali.

Kalan gap:

- Routine henuz symbol-resolved degil.
- Primary Microsoft doc destegi yok.
- Exact runtime registry read henuz yakalanmadi.
