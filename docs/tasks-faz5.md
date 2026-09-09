# Faz 5 — Görev Listesi

> Kaynak: phases.md → `"Faz 5 — Asenkron Bilet Üretimi (RabbitMQ + Worker)"`
> Durum: Tamamlandı

## Mimari ve Veri Yapısı

- [x] `TicketFlow.Contracts` paylaşımlı proje oluştur: `TicketReservedEvent` ve `TicketIssuedEvent` (bkz. phases.md → Faz 5 → Mimari ve Veri Yapısı)
- [x] MassTransit + RabbitMQ paketlerini `ReservationService` (publisher) ve `TicketWorker` (consumer) projelerine ekle (bkz. phases.md → Faz 5 → Mimari ve Veri Yapısı)
- [x] In-memory idempotency seti (işlenmiş reservationId'ler) `TicketWorker`'da kur (bkz. phases.md → Faz 5 → Mimari ve Veri Yapısı)

## Kodlama Süreci — Backend

- [x] Faz 4'te TODO bırakılan yeri tamamla: Reservation kaydı `Reserved` → `TicketReservedEvent` publish et (bkz. phases.md → Faz 5 → Kodlama Süreci — Backend)
- [x] `TicketWorker`'da `IConsumer<TicketReservedEvent>` yaz: idempotency → QR üret → PDF simüle → email simüle → `TicketIssuedEvent` publish et (bkz. phases.md → Faz 5 → Kodlama Süreci — Backend)
- [x] Her iki servis için MassTransit + RabbitMQ konfigürasyonu (`appsettings.json` + `Program.cs`) (bkz. phases.md → Faz 5 → Kodlama Süreci — Backend)

## Kodlama Süreci — Frontend (Opsiyonel)

- [x] (Opsiyonel) ''Rezervasyonlarım'' ekranına polling ekle: `setInterval` + `getMyReservations()` (Şimdilik atlandı) (bkz. phases.md → Faz 5 → Kodlama Süreci — Frontend)

## Test Senaryoları

- [x] Unit test — Aynı `reservationId` ile consumer iki kez çağrıldığında idempotency: ikinci bilet üretilmemeli (bkz. phases.md → Faz 5 → Test Senaryoları)
- [x] Integration test — MassTransit in-memory harness ile: `TicketReservedEvent` → consumer → `TicketIssuedEvent` yayınlandı (bkz. phases.md → Faz 5 → Test Senaryoları)

## Faz Kontrol Listesi (Definition of Done)

- [x] Rezervasyon sonrası Worker log'larında ''bilet üretildi'' mesajı görünüyor mu? (bkz. phases.md → Faz 5 → ✅ Kontrol Listesi)
- [x] Worker kasıtlı çökertilip tekrar ayağa kaldırıldığında kuyrukta bekleyen mesaj kaybolmadı mı? (bkz. phases.md → Faz 5 → ✅ Kontrol Listesi)
