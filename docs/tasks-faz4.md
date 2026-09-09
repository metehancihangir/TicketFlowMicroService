# Faz 4 — Görev Listesi

> Kaynak: phases.md → `"Faz 4 — Rezervasyon + Simüle Ödeme"`
> Durum: Tamamlandı

## Ekran Bileşenleri

- [x] Etkinlik listesine ''Rezerve Et'' butonu ve loading/success/error state'i (bkz. phases.md → Faz 4 → Ekran Bileşenleri)
- [x] Rezervasyon durum göstergesi: ''İşleniyor...'' → sonuç mesajı (bkz. phases.md → Faz 4 → Ekran Bileşenleri)
- [x] ''Rezervasyonlarım'' liste ekranı (bkz. phases.md → Faz 4 → Ekran Bileşenleri)

## Mimari ve Veri Yapısı

- [x] `ReservationDb` için EF Core `DbContext` (bkz. phases.md → Faz 4 → Mimari ve Veri Yapısı)
- [x] `Reservation` entity'si: Id, UserId, EventId, SeatCount, Status (enum), PaymentStatus (enum), CreatedAt (bkz. phases.md → Faz 4 → Mimari ve Veri Yapısı)
- [x] Migration oluşturuldu (InitialCreate) (bkz. phases.md → Faz 4 → Mimari ve Veri Yapısı)
- [x] Event Service'teki `PUT /api/events/{id}/reserve-seat` atomik decrement ile yeniden yazıldı (bkz. phases.md → Faz 4 → Mimari ve Veri Yapısı)
- [x] `PUT /api/events/{id}/return-seat` kompanzasyon endpoint'i eklendi (bkz. phases.md → Faz 4 → Mimari ve Veri Yapısı)
- [x] `IHttpClientFactory` named client ''EventService'' ile konfigüre edildi (bkz. phases.md → Faz 4 → Mimari ve Veri Yapısı)

## Kodlama Süreci — Backend

- [x] `IPaymentSimulator` + `PaymentSimulator` (Task.Delay 500ms, %5 Failed) (bkz. phases.md → Faz 4 → Kodlama Süreci — Backend)
- [x] `POST /api/reservations`: JWT userId, Event Service çağrısı, ödeme simülasyonu, kayıt (bkz. phases.md → Faz 4 → Kodlama Süreci — Backend)
- [x] Ödeme Failed → kompanzasyon: koltuk geri iade (return-seat) (bkz. phases.md → Faz 4 → Kodlama Süreci — Backend)
- [x] `GET /api/reservations/me`: JWT userId'den kullanıcının rezervasyonları (bkz. phases.md → Faz 4 → Kodlama Süreci — Backend)
- [x] `ReservationService/Program.cs`'e EF Core, JWT, HttpClient kayıtları (bkz. phases.md → Faz 4 → Kodlama Süreci — Backend)

## Kodlama Süreci — Frontend

- [x] Angular `ReservationService`: `createReservation()`, `getMyReservations()` (bkz. phases.md → Faz 4 → Kodlama Süreci — Frontend)
- [x] Rezervasyon durum state'i: `'idle' | 'loading' | 'success' | 'error'` per event (bkz. phases.md → Faz 4 → Kodlama Süreci — Frontend)

## Test Senaryoları

- [x] Integration — userId JWT'den okunuyor (body'den değil) → 201 ✓ (bkz. phases.md → Faz 4 → Test Senaryoları)
- [x] Integration — PaymentFailed → koltuk geri iade edildi (return-seat kompanzasyon) → ✓ (bkz. phases.md → Faz 4 → Test Senaryoları)
- [x] Integration — Race condition: 2 paralel istek, 1 koltuk → biri 201, biri 409 → ✓ (bkz. phases.md → Faz 4 → Test Senaryoları)

## Faz Kontrol Listesi (Definition of Done)

- [x] Paralel rezervasyon testi geçiyor mu? → ✓ (bkz. phases.md → Faz 4 → ✅ Kontrol Listesi)
- [x] Ödeme başarısız olduğunda koltuk telafi ediliyor mu? → ✓ (bkz. phases.md → Faz 4 → ✅ Kontrol Listesi)
- [x] Angular'da rezervasyon durumu ekranda doğru yansıyor mu? → ✓ (bkz. phases.md → Faz 4 → ✅ Kontrol Listesi)
