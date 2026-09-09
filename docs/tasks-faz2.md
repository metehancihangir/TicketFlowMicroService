# Faz 2 — Görev Listesi

> Kaynak: phases.md → `"Faz 2 — API Gateway Entegrasyonu"`
> Durum: Tamamlandı

## Mimari ve Veri Yapısı

- [x] `TicketFlow.Gateway` projesine YARP (`Yarp.ReverseProxy 2.3.0`) NuGet paketini ekle (bkz. phases.md → Faz 2 → Mimari ve Veri Yapısı)
- [x] `appsettings.json`'da YARP route ve cluster tanımlarını yap: `/api/auth/**` → Auth, `/api/events/**` → Event, `/api/reservations/**` → Reservation, `/api/analytics/**` → Analytics (bkz. phases.md → Faz 2 → Mimari ve Veri Yapısı)
- [x] CORS politikasını sadece Gateway'de tanımla (Angular origin: localhost:4200); iç servisler CORS tanımlamaz (bkz. phases.md → Faz 2 → Mimari ve Veri Yapısı)

## Kodlama Süreci — Backend

- [x] `TicketFlow.Gateway/Program.cs`'i YARP middleware ile güncelle (bkz. phases.md → Faz 2 → Kodlama Süreci — Backend)
- [x] `docker-compose.yml`'deki Gateway ortam değişkenleri YARP config ile uyumlu (bkz. phases.md → Faz 2 → Kodlama Süreci — Backend)
- [x] İç servisler (auth, event, reservation, analytics) docker-compose'da dışarıya port açmıyor (bkz. phases.md → Faz 2 → Kodlama Süreci — Backend)
- [x] `docker-compose.yml`'e Jwt ortam değişkenleri eklendi (auth, event, reservation servislerine) (bkz. phases.md → Faz 2 → Kodlama Süreci — Backend)

## Kodlama Süreci — Frontend

- [x] Angular `apiBaseUrl`'ü `http://localhost:5000` olarak ayarlandı (bkz. phases.md → Faz 2 → Kodlama Süreci — Frontend)

## Test Senaryoları

- [x] Integration test — `GET /health` → 200 OK (bkz. phases.md → Faz 2 → Test Senaryoları)
- [x] Integration test — `GET /api/unknown/resource` → 404 Not Found (bkz. phases.md → Faz 2 → Test Senaryoları)
- [x] Integration test — `POST /api/auth/login` → 502 (route eşleşti, upstream yönlendirildi) (bkz. phases.md → Faz 2 → Test Senaryoları)

## Faz Kontrol Listesi (Definition of Done)

- [x] Angular artık sadece `:5000` portuna istek atıyor mu? (bkz. phases.md → Faz 2 → ✅ Kontrol Listesi)
- [x] Auth Service'in kendi portu dışarıya kapalı mı? (bkz. phases.md → Faz 2 → ✅ Kontrol Listesi)
