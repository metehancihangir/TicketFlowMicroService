# Faz 3 — Görev Listesi

> Kaynak: phases.md → `"Faz 3 — Etkinlik Yönetimi (Etkinlikler Ekranı)"`
> Durum: Tamamlandı

## Ekran Bileşenleri

- [x] Angular: etkinlik listesi bileşeni (bkz. phases.md → Faz 3 → Ekran Bileşenleri)
- [x] Angular: Admin için yeni etkinlik ekleme formu (bkz. phases.md → Faz 3 → Ekran Bileşenleri)
- [x] Angular: yükleniyor / hata durumu metinleri (bkz. phases.md → Faz 3 → Ekran Bileşenleri)

## Mimari ve Veri Yapısı

- [x] `EventDb` için EF Core `DbContext` (`EventDbContext`) oluşturuldu (bkz. phases.md → Faz 3 → Mimari ve Veri Yapısı)
- [x] `Event` entity'si requirements.md §2.2 şemasıyla tanımlandı (`ConcurrencyStamp` dahil) (bkz. phases.md → Faz 3 → Mimari ve Veri Yapısı)
- [x] Migration oluşturuldu (`InitialCreate`) (bkz. phases.md → Faz 3 → Mimari ve Veri Yapısı)
- [x] EF Core 9.0.1 + Npgsql 9.0.4 paketleri eklendi (bkz. phases.md → Faz 3 → Mimari ve Veri Yapısı)

## Kodlama Süreci — Backend

- [x] `GET /api/events`: tüm etkinlikleri listele (auth gerektirmez) (bkz. phases.md → Faz 3 → Kodlama Süreci — Backend)
- [x] `GET /api/events/{id}`: tek etkinlik detayı, `404` dön bulunamazsa (bkz. phases.md → Faz 3 → Kodlama Süreci — Backend)
- [x] `POST /api/events`: `[Authorize(Roles = "Admin")]` ile korumalı (bkz. phases.md → Faz 3 → Kodlama Süreci — Backend)
- [x] `PUT /api/events/{id}/reserve-seat`: iskelet endpoint oluşturuldu (bkz. phases.md → Faz 3 → Kodlama Süreci — Backend)
- [x] `EventService/Program.cs`'e EF Core, JWT auth servis kayıtları eklendi (bkz. phases.md → Faz 3 → Kodlama Süreci — Backend)

## Kodlama Süreci — Frontend

- [x] Angular `EventService`: `getEvents()`, `getEventById()`, `createEvent()` metodları (bkz. phases.md → Faz 3 → Kodlama Süreci — Frontend)
- [x] `EventListComponent`: `ngOnInit`'te `getEvents()` çağır, `*ngFor` ile listele (bkz. phases.md → Faz 3 → Kodlama Süreci — Frontend)
- [x] Admin kontrolü ile etkinlik ekleme formu, reactive forms kullanılıyor (bkz. phases.md → Faz 3 → Kodlama Süreci — Frontend)

## Test Senaryoları

- [x] Unit test — Admin olmayan kullanıcının `POST /api/events` → `403` (bkz. phases.md → Faz 3 → Test Senaryoları)
- [x] Unit test — Var olmayan `eventId` → `404` (bkz. phases.md → Faz 3 → Test Senaryoları)
- [x] Integration test — Admin token ile etkinlik oluştur → listede görünüyor (bkz. phases.md → Faz 3 → Test Senaryoları)

## Faz Kontrol Listesi (Definition of Done)

- [x] Admin token ile `POST /api/events` çalışıyor mu? (bkz. phases.md → Faz 3 → ✅ Kontrol Listesi)
- [x] `GET /api/events` auth gerektirmeden listeli mi? (bkz. phases.md → Faz 3 → ✅ Kontrol Listesi)
- [x] Angular EventListComponent derleniyor mu? (bkz. phases.md → Faz 3 → ✅ Kontrol Listesi)
