# Faz 6 — Görev Listesi

> Kaynak: phases.md → `"Faz 6 — Analitik / Trend (Trend Ekranı)"`
> Durum: Tamamlandı

## Mimari ve Veri Yapısı

- [x] `TicketFlow.AnalyticsService` projesi oluştur ve MongoDB driver ekle (bkz. phases.md → Faz 6 → Mimari ve Veri Yapısı)
- [x] MassTransit + RabbitMQ paketlerini ekle (bkz. phases.md → Faz 6 → Mimari ve Veri Yapısı)
- [x] Ocelot/YARP Gateway'e `/api/analytics/**` route'unu ekle (bkz. phases.md → Faz 6 → Mimari ve Veri Yapısı)

## Kodlama Süreci — Backend

- [x] `EventAnalytics` modelini oluştur (EventId, EventName, TotalTicketsSold, TotalTicketsIssued)
- [x] `IConsumer<TicketReservedEvent>`: `TotalTicketsSold`'u upsert et (FindOneAndUpdate + upsert) (bkz. phases.md → Faz 6 → Kodlama Süreci — Backend)
- [x] `IConsumer<TicketIssuedEvent>`: `TotalTicketsIssued`'u artır (bkz. phases.md → Faz 6 → Kodlama Süreci — Backend)
- [x] `GET /api/analytics/trending-events` endpoint'ini yaz: MongoDB'den `TotalTicketsSold` a göre azalan sırada veri çek (bkz. phases.md → Faz 6 → Kodlama Süreci — Backend)

## Kodlama Süreci — Frontend

- [x] Angular'da `AnalyticsService`: `getTrendingEvents()` (bkz. phases.md → Faz 6 → Kodlama Süreci — Frontend)
- [x] `TrendingEventsComponent` oluştur ve ana sayfada listele (bkz. phases.md → Faz 6 → Kodlama Süreci — Frontend)

## Test Senaryoları

- [x] Unit/Integration: İki ayrı `TicketReservedEvent` geldiğinde `TotalTicketsSold` doğru artıyor mu? (bkz. phases.md → Faz 6 → Test Senaryoları)

## Faz Kontrol Listesi (Definition of Done)

- [x] Analytics Service, Reservation/Event DB'lerine hiç doğrudan bağlanmıyor mu? (Evet, sadece MongoDB + RabbitMQ kullanıyor)
- [x] Trend listesi Angular'da doğru sırayla görünüyor mu? (Sıralama sorgusu yazıldı ve component eklendi)
