# TweakPromotionGateCatalog static review - 2026-04-09

Bu review hostta `dotnet` bulunmadigi icin compile/test yerine static code review olarak yapildi.

## Reviewed surface

- [TweakPromotionGateCatalogService.cs](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/app/Services/TweakPromotionGateCatalogService.cs)

## Static review result

- Apply precondition mantigi beklenen contract ile hizali gorunuyor.
- `legacy-curated` tweak'ler fallback olarak gecmeye devam ediyor.
- `research-derived` tweak'ler yalniz `promotion_state == promoted` ise dogrudan geciyor.
- Contributor/debug override yalniz `overrideRequested && contributorMode && DebugOverrideAllowed` oldugunda devreye giriyor.
- Rollback path'i apply gate ustunden geciyor ve sonra `rollback_declared`, `rollback_executed`, `rollback_verified` durumlarini warning/deny seviyesinde ayristiriyor.

## No blocking finding

- Static review sirasinda apply precondition mantiginda blocker seviyesinde bir bug tespit edilmedi.

## Residual risk

- [TweakPromotionGateCatalogService.cs](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/app/Services/TweakPromotionGateCatalogService.cs):388
  `AppendMutationAuditLog` path'i hala best-effort, ama artik failure tamamen sessiz kalmiyor:
  hata `LastMutationAuditError` icinde tutuluyor ve `Debug.WriteLine(...)` ile debug channel'a yaziliyor.
  Buna ragmen hostta audit write failure'i UI'ya veya CLI surface'ine henuz aktif olarak tasinmiyor.
- Hostta `dotnet` olmadigi icin bu turda compile-time veya runtime C# proof alinmadi.

## Practical conclusion

- Mevcut Python/JSON surface testleri gate contract'i disardan dogruluyor.
- C# tarafinda bir sonraki en anlamli adim, `dotnet test` bulunan ortamda `TweakPromotionGateCatalogServiceTests` ve CLI command wiring'i canli kosmak.
