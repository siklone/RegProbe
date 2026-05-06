Execution-required pair için 2026-04-11 statik follow-up:

- Guest-side minimal UTF-16 tarama [string-offsets.json](../../evidence/files/vm-tooling-staging/executionrequired-xref-20260411/string-offsets.json) ile current-build `ntoskrnl.exe` içinde iki exact string hit verdi:
  - `AllowAudioToEnableExecutionRequiredPowerRequests` -> file offset `0xBF6530`
  - `AllowSystemRequiredPowerRequests` -> file offset `0xBF65E0`
- Her iki string de `INIT` section içinde yer alıyor; host-side PE eşlemesi bunları sırasıyla yaklaşık `0x140C7D530` ve `0x140C7D5E0` RVA/VA kümesine yerleştiriyor.
- String kümesinin çevresindeki ham veri, tekil string değil bir descriptor table gösteriyor. İlgili slice, `Power` anahtarı altında değer adı ve hedef global eşlemesi tutuyor:
  - `Power` + `AllowSystemRequiredPowerRequests` -> `0x140FD7114`
  - `Power` + `AllowAudioToEnableExecutionRequiredPowerRequests` -> `0x140FD71A0`
- Bu iki hedef adres, retained local-KD evidence ile isimlenen current-build global’lere denk geliyor:
  - `0x140FD7114` <-> `nt!PopPowerRequestConvertSystemToExecution`
  - `0x140FD71A0` <-> `nt!PopPowerRequestActiveAudioEnablesExecutionRequired`
- Aynı table slice içinde komşu `Power` değerleri de görünüyor (`TtmEnabled`, `DeepIoCoalescingEnabled`, `CheckPowerSourceAfterRtcWakeTime`, `Power\\ModernSleep`, `Power\\PowerThrottling`). Bu, execution-required pair’in izole bir docs hit değil, daha geniş bir power-init descriptor bloğunun parçası olduğunu güçlendiriyor.

Çıkarım:

- Current-build static lane artık sadece “string exists” seviyesinde değil. `AllowSystemRequiredPowerRequests` ve `AllowAudioToEnableExecutionRequiredPowerRequests` için `Control\\Power` altındaki registry value adı -> exact kernel global eşlemesi gösteren bir `INIT` descriptor table mevcut.
- Bu, eski blocker’daki “exact current-build binding site yok” ifadesini daraltıyor: direct registry read/caller fonksiyonu hâlâ bulunmadı, ama value-name to global binding artık current-build binary içinden görülebiliyor.
- Retained local-KD address exports aynı current-build host-side `objdump` görünümleriyle de hizalanıyor:
  - `0x140A27260` -> `nt!PopPowerRequestCallbackExecutionRequired`, burada PAGE disassembly `0x140FD7114` / `nt!PopPowerRequestConvertSystemToExecution` okumasını koruyor
  - `0x140A3BCEC` -> `nt!PopPowerRequestHandleExecutionEnablementUpdate`, burada PAGE disassembly `0x140FD7114` okumasının hemen ardından `0x140A3BD2C` çağrısını yapıyor
  - `0x140A3BD2C` -> `nt!PopPowerRequestEvaluateExecutionRequiredStatus`, burada PAGE disassembly `0x140FD71A0` / `nt!PopPowerRequestActiveAudioEnablesExecutionRequired` ile `0x140FD70B0` / `nt!PopExecutionRequiredTimeout` okumalarını gösteriyor
- Bu address alignment, yeni INIT-table bulgusunun repo’da zaten retained olan current-build reader/callback lineage ile aynı binary içinde oturduğunu güçlendiriyor; kapanmayan boşluk registry-backed seeding/init path olmaya devam ediyor.

Kapanmayan gap:

- Hafif host-side RIP-relative tarama ile table başlangıcına ya da iki hedef global’e giden açık `lea`/`mov` init xref’i henüz bulunamadı.
- Yani henüz kanıtlanan şey exact reader/caller değil; current-build init-time binding table.

En mantıklı sonraki adım:

- Bu descriptor region’ı tüketen init routine’i bulmak için ya symbol-seeded Ghidra xref lane’i tekrar açılmalı ya da bu table’a komşu `INIT` function range’i daha güçlü bir disassembler ile çözülmeli.
