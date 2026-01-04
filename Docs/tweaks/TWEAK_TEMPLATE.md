# Tweak Dokümantasyon Şablonu

Bu şablon, yeni tweak ekleme veya mevcut tweak'leri belgeleme için kullanılır.

---

## Tweak Bilgi Kartı

```yaml
id: tweak.category.name
name: İnsan Okunabilir İsim
description: |
  Tweak'in ne yaptığının açıklaması.
  Bir cümlede özet.
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

## Detaylı Açıklama

### Ne Yapar?
Tweak'in teknik açıklaması. Hangi registry anahtarlarını değiştiriyor, hangi servisleri durduruyor, vb.

### Neden Kullanılır?
Kullanım senaryoları ve faydaları.

### Potansiyel Yan Etkiler
- Liste halinde olası yan etkiler
- Hangi uygulamalar etkilenebilir
- Uyumluluk sorunları

---

## Teknik Detaylar

### Registry Değişiklikleri
```
HKEY_CURRENT_USER\Software\...
  ValueName (REG_DWORD): OldValue → NewValue
```

### Servis Değişiklikleri
| Servis | Orijinal | Yeni |
|--------|----------|------|
| ServiceName | Automatic | Disabled |

### Zamanlanmış Görevler
| Görev Yolu | Durum |
|------------|-------|
| \Microsoft\Windows\... | Disabled |

---

## Doğrulama Adımları

1. Registry Editor ile değerleri kontrol edin
2. `services.msc` ile servis durumunu kontrol edin
3. Task Scheduler ile görev durumunu kontrol edin

---

## Geri Alma Prosedürü

Tweak otomatik olarak geri alınabilir. Manuel geri alma için:

1. Adım 1
2. Adım 2

---

## Referanslar

- [Microsoft Docs: İlgili Sayfa](https://docs.microsoft.com/...)
- [Windows Security Baseline](https://docs.microsoft.com/...)
