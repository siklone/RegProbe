# Tweak DokÃ¼mantasyon Åžablonu

Bu ÅŸablon, yeni tweak ekleme veya mevcut tweak'leri belgeleme iÃ§in kullanÄ±lÄ±r.

---

## Tweak Bilgi KartÄ±

```yaml
id: tweak.category.name
name: Ä°nsan Okunabilir Ä°sim
description: |
  Tweak'in ne yaptÄ±ÄŸÄ±nÄ±n aÃ§Ä±klamasÄ±.
  Bir cÃ¼mlede Ã¶zet.
risk: Safe | Advanced | Risky
category: Privacy | System | Network | Power | etc.
area: Registry | Service | Task | Command | Composite
requires_elevation: true | false
reversible: true | false
windows_versions:
  - Windows 10 (22H2+)
  - Windows 11
```

---

## DetaylÄ± AÃ§Ä±klama

### Ne Yapar?
Tweak'in teknik aÃ§Ä±klamasÄ±. Hangi registry anahtarlarÄ±nÄ± deÄŸiÅŸtiriyor, hangi servisleri durduruyor, vb.

### Neden KullanÄ±lÄ±r?
KullanÄ±m senaryolarÄ± ve faydalarÄ±.

### Potansiyel Yan Etkiler
- Liste halinde olasÄ± yan etkiler
- Hangi uygulamalar etkilenebilir
- Uyumluluk sorunlarÄ±

---

## Teknik Detaylar

### Registry DeÄŸiÅŸiklikleri
```
HKEY_CURRENT_USER\Software\...
  ValueName (REG_DWORD): OldValue â†’ NewValue
```

### Servis DeÄŸiÅŸiklikleri
| Servis | Orijinal | Yeni |
|--------|----------|------|
| ServiceName | Automatic | Disabled |

### ZamanlanmÄ±ÅŸ GÃ¶revler
| GÃ¶rev Yolu | Durum |
|------------|-------|
| \Microsoft\Windows\... | Disabled |

---

## DoÄŸrulama AdÄ±mlarÄ±

1. Registry Editor ile deÄŸerleri kontrol edin
2. `services.msc` ile servis durumunu kontrol edin
3. Task Scheduler ile gÃ¶rev durumunu kontrol edin

---

## Geri Alma ProsedÃ¼rÃ¼

Tweak otomatik olarak geri alÄ±nabilir. Manuel geri alma iÃ§in:

1. AdÄ±m 1
2. AdÄ±m 2

---

## Referanslar

- [Microsoft Docs: Ä°lgili Sayfa](https://docs.microsoft.com/...)
- [Windows Security Baseline](https://docs.microsoft.com/...)
