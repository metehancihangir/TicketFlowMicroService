# Faz 1 — Görev Listesi

> Kaynak: phases.md → `"Faz 1 — Kimlik Doğrulama (Giriş / Kayıt Ekranı)"`
> Durum: Tamamlandı

## Ekran Bileşenleri

- [x] Angular projesinde kayıt formu bileşeni oluştur: `email`, `password` input + submit butonu (bkz. phases.md → Faz 1 → Ekran Bileşenleri)
- [x] Angular projesinde giriş formu bileşeni oluştur: `email`, `password` input + submit butonu (bkz. phases.md → Faz 1 → Ekran Bileşenleri)
- [x] Hata/başarı mesajı için metinsel alan ekle (bkz. phases.md → Faz 1 → Ekran Bileşenleri)

## Mimari ve Veri Yapısı

- [x] `AuthDb` için EF Core `DbContext` oluştur (`AuthDbContext`) (bkz. phases.md → Faz 1 → Mimari ve Veri Yapısı)
- [x] `User` entity'sini requirements.md §2.1 şemasıyla tanımla: Id (Guid PK), Email (unique, indexed), PasswordHash, Role (enum), CreatedAt (bkz. phases.md → Faz 1 → Mimari ve Veri Yapısı)
- [x] `Role` enum'unu tanımla: `Admin`, `User` (bkz. phases.md → Faz 1 → Mimari ve Veri Yapısı)
- [x] İlk migration oluştur: `dotnet ef migrations add InitialCreate` (bkz. phases.md → Faz 1 → Mimari ve Veri Yapısı)
- [x] Migration'ı `AuthDb`'ye uygula — Program.cs startup sırasında otomatik (bkz. phases.md → Faz 1 → Mimari ve Veri Yapısı)
- [x] `BCrypt.Net-Next` NuGet paketini `TicketFlow.AuthService`'e ekle (bkz. phases.md → Faz 1 → Mimari ve Veri Yapısı)
- [x] `Microsoft.AspNetCore.Authentication.JwtBearer` paketini ekle (bkz. phases.md → Faz 1 → Mimari ve Veri Yapısı)
- [x] `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL` paketlerini ekle (bkz. phases.md → Faz 1 → Mimari ve Veri Yapısı)

## Kodlama Süreci — Backend

- [x] `ITokenService` arayüzü ve `TokenService` implementasyonunu oluştur (bkz. phases.md → Faz 1 → Kodlama Süreci — Backend)
- [x] `POST /api/auth/register` endpoint'ini yaz (bkz. phases.md → Faz 1 → Kodlama Süreci — Backend)
- [x] `POST /api/auth/login` endpoint'ini yaz (bkz. phases.md → Faz 1 → Kodlama Süreci — Backend)
- [x] `AuthController` oluştur (bkz. phases.md → Faz 1 → Kodlama Süreci — Backend)
- [x] `Program.cs`'e EF Core, JWT auth ve TokenService servis kayıtlarını ekle (bkz. phases.md → Faz 1 → Kodlama Süreci — Backend)
- [x] JWT doğrulama middleware'ini Event/Reservation servislerine ekle (bkz. phases.md → Faz 1 → Kodlama Süreci — Backend)

## Kodlama Süreci — Frontend (Angular)

- [x] Angular uygulaması oluştur: `ticketflow-frontend` (bkz. phases.md → Faz 1 → Kodlama Süreci — Frontend)
- [x] `AuthService` (Angular `@Injectable`) oluştur (bkz. phases.md → Faz 1 → Kodlama Süreci — Frontend)
- [x] Token ve login durumunu `BehaviorSubject<string | null>` ile tut (bkz. phases.md → Faz 1 → Kodlama Süreci — Frontend)
- [x] Login başarılı olduğunda token'ı `localStorage`'a yaz (bkz. phases.md → Faz 1 → Kodlama Süreci — Frontend)
- [x] `HttpInterceptor` oluştur: token varsa `Authorization: Bearer <token>` header'ı ekle (bkz. phases.md → Faz 1 → Kodlama Süreci — Frontend)

## Test Senaryoları

- [x] Unit test — aynı email ile iki kez register denendiğinde `409` döndürdüğünü doğrula (bkz. phases.md → Faz 1 → Test Senaryoları)
- [x] Unit test — yanlış şifreyle login denemesinin `401` döndürdüğünü doğrula (bkz. phases.md → Faz 1 → Test Senaryoları)
- [x] Unit test — üretilen JWT'nin doğru claim'leri içerdiğini doğrula (bkz. phases.md → Faz 1 → Test Senaryoları)
- [x] Integration test — register → login → `200` aldığını doğrula (bkz. phases.md → Faz 1 → Test Senaryoları)

## Faz Kontrol Listesi (Definition of Done)

- [x] Register/login akışı Postman'de uçtan uca çalışıyor mu? (bkz. phases.md → Faz 1 → ✅ Kontrol Listesi)
- [x] Angular'dan login yapıp token `localStorage`'a yazılıyor mu? (bkz. phases.md → Faz 1 → ✅ Kontrol Listesi)
- [x] Interceptor token'ı otomatik ekliyor mu? (bkz. phases.md → Faz 1 → ✅ Kontrol Listesi)
