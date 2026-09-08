# Faz 0 — Görev Listesi

> Kaynak: phases.md → `"Faz 0 — Proje İskeleti ve Altyapı"`
> Durum: Tamamlandı

## Mimari ve Veri Yapısı

- [x] Çözüm yapısını kur: `TicketFlow.Gateway` .NET 10 Web API projesi oluştur (bkz. phases.md → Faz 0 → Mimari ve Veri Yapısı)
- [x] Çözüm yapısını kur: `TicketFlow.AuthService` .NET 10 Web API projesi oluştur (bkz. phases.md → Faz 0 → Mimari ve Veri Yapısı)
- [x] Çözüm yapısını kur: `TicketFlow.EventService` .NET 10 Web API projesi oluştur (bkz. phases.md → Faz 0 → Mimari ve Veri Yapısı)
- [x] Çözüm yapısını kur: `TicketFlow.ReservationService` .NET 10 Web API projesi oluştur (bkz. phases.md → Faz 0 → Mimari ve Veri Yapısı)
- [x] Çözüm yapısını kur: `TicketFlow.TicketWorker` .NET 10 Worker Service projesi oluştur (bkz. phases.md → Faz 0 → Mimari ve Veri Yapısı)
- [x] Çözüm yapısını kur: `TicketFlow.AnalyticsService` .NET 10 Web API projesi oluştur (bkz. phases.md → Faz 0 → Mimari ve Veri Yapısı)
- [x] Kök dizinde `TicketFlow.sln` solution dosyası oluştur ve tüm projeleri ekle (bkz. phases.md → Faz 0 → Mimari ve Veri Yapısı)
- [x] `docker-compose.yml` dosyasını kök dizinde oluştur: `auth-db` (PostgreSQL), `event-db` (PostgreSQL), `reservation-db` (PostgreSQL) container'larını tanımla (bkz. phases.md → Faz 0 → docker-compose.yml)
- [x] `docker-compose.yml`'e `analytics-db` (MongoDB) container'ını ekle (bkz. phases.md → Faz 0 → docker-compose.yml)
- [x] `docker-compose.yml`'e `rabbitmq` (rabbitmq:3-management, port 15672) container'ını ekle (bkz. phases.md → Faz 0 → docker-compose.yml)
- [x] `docker-compose.yml`'e `auth-service`, `event-service`, `reservation-service`, `ticket-worker`, `analytics-service` mikroservis tanımlarını ekle (bkz. phases.md → Faz 0 → docker-compose.yml)
- [x] `docker-compose.yml`'e `gateway` servisini ekle (tek dış port: `5000:8080`) (bkz. phases.md → Faz 0 → docker-compose.yml)
- [x] Her backend projesine `appsettings.Development.json` üzerinden connection string'lerini tanımla — hardcoded bağlantı yok (bkz. phases.md → Faz 0 → Mimari ve Veri Yapısı)
- [x] RabbitMQ ve MassTransit paketlerini henüz hiçbir servise bağlama; sadece container'ın ayakta olduğunu doğrulayacak şekilde bırak (bkz. phases.md → Faz 0 → Mimari ve Veri Yapısı)

## Kodlama Süreci

- [x] `TicketFlow.AuthService`'te boş bir `GET /health` endpoint'i oluştur (sadece `200 OK` dönsün) (bkz. phases.md → Faz 0 → Kodlama Süreci)
- [x] `TicketFlow.EventService`'te boş bir `GET /health` endpoint'i oluştur (bkz. phases.md → Faz 0 → Kodlama Süreci)
- [x] `TicketFlow.ReservationService`'te boş bir `GET /health` endpoint'i oluştur (bkz. phases.md → Faz 0 → Kodlama Süreci)
- [x] `TicketFlow.TicketWorker`'da sağlık kontrolü için temel Worker Service yapısını doğrula (HTTP endpoint'i yok) (bkz. phases.md → Faz 0 → Kodlama Süreci)
- [x] `TicketFlow.AnalyticsService`'te boş bir `GET /health` endpoint'i oluştur (bkz. phases.md → Faz 0 → Kodlama Süreci)
- [x] `TicketFlow.Gateway`'de boş bir `GET /health` endpoint'i oluştur (bkz. phases.md → Faz 0 → Kodlama Süreci)
- [x] Kök dizine `.gitignore` dosyası ekle (standart .NET gitignore) (bkz. phases.md → Faz 0 → Kodlama Süreci)
- [x] Kök dizine `.editorconfig` dosyası ekle (bkz. phases.md → Faz 0 → Kodlama Süreci)

## Her Servis İçin Dockerfile

- [x] `TicketFlow.Gateway/Dockerfile` oluştur (phases.md'deki şablona göre) (bkz. phases.md → Faz 0 → Her Servis İçin Dockerfile)
- [x] `TicketFlow.AuthService/Dockerfile` oluştur (bkz. phases.md → Faz 0 → Her Servis İçin Dockerfile)
- [x] `TicketFlow.EventService/Dockerfile` oluştur (bkz. phases.md → Faz 0 → Her Servis İçin Dockerfile)
- [x] `TicketFlow.ReservationService/Dockerfile` oluştur (bkz. phases.md → Faz 0 → Her Servis İçin Dockerfile)
- [x] `TicketFlow.TicketWorker/Dockerfile` oluştur (EXPOSE satırı yok, Worker Service şablonu) (bkz. phases.md → Faz 0 → Her Servis İçin Dockerfile)
- [x] `TicketFlow.AnalyticsService/Dockerfile` oluştur (bkz. phases.md → Faz 0 → Her Servis İçin Dockerfile)

## Test Senaryoları

- [x] Manuel doğrulama: `docker compose up --build` komutu çalıştırıldıktan sonra tüm container'ların (PostgreSQL x3, MongoDB, RabbitMQ, 6 servis) `healthy`/`running` durumda olduğunu doğrula (bkz. phases.md → Faz 0 → Test)
- [x] Manuel doğrulama: `docker compose ps` çıktısında hiçbir servisin `Exited` durumda olmadığını doğrula (bkz. phases.md → Faz 0 → Test)

## Faz Kontrol Listesi (Definition of Done)

- [x] `docker compose up --build` tek komutla tüm altyapıyı **ve** 6 servisi (Gateway dahil) ayağa kaldırıyor mu? (bkz. phases.md → Faz 0 → Kontrol Listesi)
- [x] Her boş servis projesi `/health` endpoint'ine `200` dönüyor mu? (bkz. phases.md → Faz 0 → Kontrol Listesi)
- [x] RabbitMQ management UI (`localhost:15672`) açılıyor mu? (bkz. phases.md → Faz 0 → Kontrol Listesi)
- [x] `docker compose ps` çıktısında tüm servisler `running`/`healthy`, hiçbiri `Exited` değil mi? (bkz. phases.md → Faz 0 → Kontrol Listesi)
- [x] Sadece `gateway` host makineden erişilebiliyor (`localhost:5000`), diğer servislerin portu dışarı açık değil mi? (bkz. phases.md → Faz 0 → Kontrol Listesi)
