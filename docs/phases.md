# TicketFlow — Fazlı Kodlama Yol Haritası (phases.md)

> Bu doküman `requirements.md`'yi temel alır. Her faz, önceki fazın üzerine inşa edilir — bir faz bitmeden bir sonrakine geçme. Amaç sadece "çalışan kod" değil, **neden** o şekilde yazıldığını anlamaktır.
>
> **Not (varsayımlar):** requirements.md'de frontend teknolojisi **Angular** olarak karara bağlanmıştı ve UI/UX tasarımı kapsam dışı bırakılmıştı. Bu doküman o kararlara sadık kalır: "UI/UX Tasarımı" yerine **"Ekran Bileşenleri"** başlığı kullanılır (sadece işlevsel bileşen listesi, görsel tasarım yok) ve state management **Angular (RxJS + Service pattern)** ile ele alınır.

---

## Genel İlerleme Mantığı

```
Faz 0: Altyapı  →  Faz 1: Auth  →  Faz 2: Gateway  →  Faz 3: Etkinlikler
   →  Faz 4: Rezervasyon (+ ödeme simülasyonu)  →  Faz 5: Asenkron Bilet Üretimi
   →  Faz 6: Analitik/Trend  →  Faz 7: Sertleştirme (rate limit, test tamamlama)
```

Her faz sonunda **"Bu fazı bitirdin mi?"** kontrol listesi var — bir sonraki faza geçmeden önce hepsini işaretleyebilmelisin.

---

## Faz 0 — Proje İskeleti ve Altyapı

*Ekran yok — bu faz görünmez temeldir ama en kritik fazdır. Burası atlanırsa sonraki her faz sancılı olur.*

### Mimari ve Veri Yapısı
1. Çözüm (solution) yapısını kur: her mikroservis ayrı bir .NET 10 Web API projesi olacak şekilde (`TicketFlow.Gateway`, `TicketFlow.AuthService`, `TicketFlow.EventService`, `TicketFlow.ReservationService`, `TicketFlow.TicketWorker`, `TicketFlow.AnalyticsService`).
2. `docker-compose.yml` dosyasını oluştur: PostgreSQL (×3, her transactional servis için ayrı container veya ayrı DB adıyla tek container), MongoDB (Analytics için), RabbitMQ (management plugin'i açık, `:15672` UI erişimi ile).
3. Her backend projesine `appsettings.Development.json` üzerinden connection string'leri tanımla — **hardcoded bağlantı yok**.
4. RabbitMQ ve MassTransit paketlerini henüz hiçbir servise bağlama; sadece container'ın ayakta olduğunu doğrula.

### Kodlama Süreci
- Her servis projesinde boş bir `GET /health` endpoint'i oluştur (sadece `200 OK` dönsün). Bu, Docker Compose'un servislerin ayakta olup olmadığını doğrulamana yardımcı olur.
- `.gitignore`, `.editorconfig` gibi standart proje dosyalarını ekle.

### Her Servis İçin Dockerfile

Her .NET servisi (Gateway dahil) için proje kök dizininde **aynı kalıpta** bir `Dockerfile` oluştur. Aşağıdaki şablonu her servis için `PROJECT_NAME` kısmını değiştirerek kullan:

```dockerfile
# Dockerfile (örnek: TicketFlow.AuthService/Dockerfile)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "TicketFlow.AuthService.csproj"
RUN dotnet publish "TicketFlow.AuthService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TicketFlow.AuthService.dll"]
```

Bu şablonu şu projelere uygula (her birinde `.csproj` ve `.dll` adını ilgili servise göre değiştir):
- `TicketFlow.Gateway`
- `TicketFlow.AuthService`
- `TicketFlow.EventService`
- `TicketFlow.ReservationService`
- `TicketFlow.TicketWorker` *(HTTP endpoint'i yok, sadece `EXPOSE` satırını kaldırabilirsin — Worker Service şablonu (`dotnet new worker`) kullandıysan `ENTRYPOINT` aynı mantıkta kalır)*
- `TicketFlow.AnalyticsService`

### `docker-compose.yml` — Tüm Servisler ve Altyapı

Aşağıdaki iskeleti kök dizinde `docker-compose.yml` olarak oluştur. Bu, her mikroservisi, her veritabanını ve RabbitMQ'yu tek komutla ayağa kaldırır:

```yaml
version: "3.9"

services:
  # ---------- Altyapı ----------
  auth-db:
    image: postgres:16
    environment:
      POSTGRES_DB: AuthDb
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    volumes:
      - auth-db-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5

  event-db:
    image: postgres:16
    environment:
      POSTGRES_DB: EventDb
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    volumes:
      - event-db-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5

  reservation-db:
    image: postgres:16
    environment:
      POSTGRES_DB: ReservationDb
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    volumes:
      - reservation-db-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5

  analytics-db:
    image: mongo:7
    volumes:
      - analytics-db-data:/data/db
    healthcheck:
      test: ["CMD", "mongosh", "--eval", "db.adminCommand('ping')"]
      interval: 5s
      timeout: 5s
      retries: 5

  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "15672:15672"   # Management UI
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "-q", "ping"]
      interval: 5s
      timeout: 5s
      retries: 5

  # ---------- Mikroservisler ----------
  auth-service:
    build:
      context: ./TicketFlow.AuthService
      dockerfile: Dockerfile
    environment:
      ConnectionStrings__AuthDb: "Host=auth-db;Database=AuthDb;Username=postgres;Password=postgres"
    depends_on:
      auth-db:
        condition: service_healthy

  event-service:
    build:
      context: ./TicketFlow.EventService
      dockerfile: Dockerfile
    environment:
      ConnectionStrings__EventDb: "Host=event-db;Database=EventDb;Username=postgres;Password=postgres"
    depends_on:
      event-db:
        condition: service_healthy

  reservation-service:
    build:
      context: ./TicketFlow.ReservationService
      dockerfile: Dockerfile
    environment:
      ConnectionStrings__ReservationDb: "Host=reservation-db;Database=ReservationDb;Username=postgres;Password=postgres"
      Services__EventService: "http://event-service:8080"
      RabbitMq__Host: "rabbitmq"
    depends_on:
      reservation-db:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy

  ticket-worker:
    build:
      context: ./TicketFlow.TicketWorker
      dockerfile: Dockerfile
    environment:
      RabbitMq__Host: "rabbitmq"
    depends_on:
      rabbitmq:
        condition: service_healthy

  analytics-service:
    build:
      context: ./TicketFlow.AnalyticsService
      dockerfile: Dockerfile
    environment:
      ConnectionStrings__AnalyticsDb: "mongodb://analytics-db:27017"
      RabbitMq__Host: "rabbitmq"
    depends_on:
      analytics-db:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy

  # ---------- Gateway (dışa açılan tek kapı) ----------
  gateway:
    build:
      context: ./TicketFlow.Gateway
      dockerfile: Dockerfile
    ports:
      - "5000:8080"
    environment:
      ReverseProxy__Clusters__auth-cluster__Destinations__d1__Address: "http://auth-service:8080"
      ReverseProxy__Clusters__event-cluster__Destinations__d1__Address: "http://event-service:8080"
      ReverseProxy__Clusters__reservation-cluster__Destinations__d1__Address: "http://reservation-service:8080"
      ReverseProxy__Clusters__analytics-cluster__Destinations__d1__Address: "http://analytics-service:8080"
    depends_on:
      - auth-service
      - event-service
      - reservation-service
      - analytics-service

volumes:
  auth-db-data:
  event-db-data:
  reservation-db-data:
  analytics-db-data:
```

**Dikkat edilmesi gerekenler:**
- Her servis **kendi Postgres container'ına** bağlanır (`Database-per-Service` ilkesi burada somutlaşıyor — tek bir Postgres container'ı içinde birden fazla DB açmak yerine bilinçli olarak ayrı container'lar kullandık, böylece ileride her birini bağımsız ölçekleyebilir/yedekleyebilirsin).
- `reservation-service`'in `event-service`'e HTTP ile ulaşabilmesi için Docker'ın dahili DNS'i sayesinde servis adını (`http://event-service:8080`) doğrudan kullanabilirsin — IP adresi yazmana gerek yok.
- `depends_on` + `condition: service_healthy` kombinasyonu, bir servisin veritabanı **gerçekten hazır olmadan** ayağa kalkmasını engeller (aksi halde ilk migration denemesi başarısız olabilir).
- Sadece `gateway` dışarıya port açıyor (`5000:8080`) — diğer tüm servislerin portu Docker network'ü içinde kalıyor, bu da FR-6.2 ve NFR-3'ün pratikte nasıl uygulandığını gösteriyor.
- `RabbitMq__Host`, `ConnectionStrings__*` gibi environment variable isimleri, .NET'in "configuration override" kuralına göre `appsettings.json`'daki nested key'leri `__` (çift alt çizgi) ile eziyor — bu yüzden `appsettings.json`'daki key isimleriyle burada yazdıkların birebir eşleşmeli.

### Test
- Bu fazda otomatik test yazılmaz. Manuel doğrulama: `docker compose up --build` sonrası tüm container'ların (Postgres ×3, Mongo, RabbitMQ, 6 servis) `healthy`/`running` durumda olduğunu ve `docker compose ps` çıktısında hiçbir servisin `Exited` durumda olmadığını kontrol et.

### ✅ Faz 0 Kontrol Listesi
- [ ] `docker compose up --build` tek komutla tüm altyapıyı **ve** 6 servisi (Gateway dahil) ayağa kaldırıyor mu?
- [ ] Her boş servis projesi `/health` endpoint'ine 200 dönüyor mu?
- [ ] RabbitMQ management UI (`localhost:15672`) açılıyor mu?
- [ ] `docker compose ps` çıktısında tüm servisler `running`/`healthy`, hiçbiri `Exited` değil mi?
- [ ] Sadece `gateway` host makineden erişilebiliyor (`localhost:5000`), diğer servislerin portu dışarı açık değil mi?

---

## Faz 1 — Kimlik Doğrulama (Giriş / Kayıt Ekranı)

*İlgili requirements: FR-1, §2.1 (AuthDb), Auth Service endpoint tablosu*

### Ekran Bileşenleri
- Kayıt formu: `email`, `password` input + submit butonu.
- Giriş formu: `email`, `password` input + submit butonu.
- Hata/başarı mesajı için tek bir metinsel alan (örn. "Giriş başarısız" / "Kayıt başarılı, giriş yapabilirsiniz").
- Görsel tasarım yok — düz HTML form elemanları yeterli.

### Mimari ve Veri Yapısı
1. `AuthDb` için EF Core `DbContext` oluştur, `User` entity'sini requirements.md §2.1'deki şemayla birebir tanımla.
2. İlk migration'ı oluştur ve `AuthDb`'ye uygula (`dotnet ef migrations add InitialCreate`).
3. BCrypt.Net-Next paketini ekle (şifre hash'leme için).
4. JWT üretimi için `Microsoft.AspNetCore.Authentication.JwtBearer` paketini ekle; imzalama anahtarını `appsettings.json`'da tut (gerçek projede secret manager kullanılır, burada demo amaçlı config yeterli).

### Kodlama Süreci — Backend
1. `POST /api/auth/register`: gelen `email`'in DB'de var olup olmadığını kontrol et → varsa `409`. Yoksa şifreyi BCrypt ile hash'le, `User` kaydını oluştur, `201` dön.
2. `POST /api/auth/login`: email ile kullanıcıyı bul → bulunamazsa `401`. Şifreyi `BCrypt.Verify` ile doğrula → yanlışsa `401`. Doğruysa JWT üret (`sub`, `email`, `role` claim'leriyle) ve `expiresAt` ile birlikte dön.
3. JWT üretim mantığını ayrı bir `ITokenService` sınıfına çıkar — ileride başka servislerde token doğrulama konfigürasyonunu tekrar kullanacaksın (aynı signing key).

### Kodlama Süreci — Frontend (Angular)
1. `AuthService` (Angular service, `@Injectable`) oluştur: `register()`, `login()`, `logout()`, `getToken()`, `isLoggedIn()` metodlarıyla.
2. Token'ı ve login durumunu bir `BehaviorSubject<string | null>` ile tut — component'ler bu observable'a subscribe olarak login durumunu reaktif izlesin.
3. Login başarılı olduğunda token'ı `localStorage`'a yaz ve `BehaviorSubject`'i güncelle.
4. `HttpInterceptor` oluştur: her giden isteğe, token varsa `Authorization: Bearer <token>` header'ı ekle. Bu interceptor'ı Faz 1'de kur, çünkü Faz 3'ten itibaren tüm servislere lazım olacak.

### Test Senaryoları
**Unit:**
- Aynı email ile iki kez register denendiğinde ikinci denemenin reddedildiğini doğrula.
- Yanlış şifreyle login denemesinin `401` döndüğünü doğrula.
- Üretilen JWT'nin doğru claim'leri (`sub`, `email`, `role`) içerdiğini doğrula.

**Integration:**
- `WebApplicationFactory` ile gerçek (test) DB'ye register → login → dönen token ile korumalı bir endpoint'e (henüz yoksa geçici bir `[Authorize]` test endpoint'i ile) istek atıp `200` aldığını doğrula.

### ✅ Faz 1 Kontrol Listesi
- [ ] Register/login akışı Postman'de uçtan uca çalışıyor mu?
- [ ] Angular'dan login yapıp token `localStorage`'a yazılıyor mu?
- [ ] Interceptor token'ı otomatik ekliyor mu (Network tab'de kontrol et)?

---

## Faz 2 — API Gateway Entegrasyonu

*İlgili requirements: FR-6*

*Ekran yok — ama bu fazdan sonra Angular tek bir base URL (`localhost:5000`) kullanmaya başlayacak.*

### Mimari ve Veri Yapısı
1. `TicketFlow.Gateway` projesine YARP paketini ekle.
2. `appsettings.json`'da route ve cluster tanımlarını yap: `/api/auth/**` → Auth Service'in iç adresi (Docker Compose network'ünde servis adıyla, örn. `http://auth-service:8080`).
3. CORS politikasını **sadece Gateway'de** tanımla (Angular'ın çalıştığı origin'e izin ver). Auth Service'te CORS config'i varsa kaldır.

### Kodlama Süreci — Backend
1. `gateway` servisi zaten Faz 0'daki `docker-compose.yml`'de tanımlıydı (tek dış port: `5000:8080`) — bu fazda YARP'ın `appsettings.json`/environment üzerinden route ve cluster konfigürasyonunu gerçek route mantığıyla doldur (Faz 0'daki `ReverseProxy__Clusters__*` environment değişkenleri bunun için hazırlanmıştı).
2. Auth Service'in kendi portunu host makineye açan bir `ports:` satırı varsa (geliştirme kolaylığı için eklemiş olabilirsin) kaldır — artık sadece Gateway üzerinden erişilmeli.

### Kodlama Süreci — Frontend
1. Angular'daki `environment.ts` dosyasındaki `apiBaseUrl`'ü `http://localhost:5000` olarak güncelle. Artık her servise ayrı ayrı değil, tek base URL'e istek atılıyor.

### Test Senaryoları
**Integration:**
- Gateway üzerinden `/api/auth/login` isteğinin doğru şekilde Auth Service'e yönlendirildiğini doğrula (Gateway ayakta, Auth Service ayakta, response `200`).
- Gateway'in doğrudan olmayan bir path'e (`/api/unknown/**`) `404` döndürdüğünü doğrula.

### ✅ Faz 2 Kontrol Listesi
- [ ] Angular artık sadece `:5000` portuna istek atıyor mu?
- [ ] Auth Service'in kendi portu dışarıya kapalı mı (sadece Gateway erişebiliyor mu)?

---

## Faz 3 — Etkinlik Yönetimi (Etkinlikler Ekranı)

*İlgili requirements: FR-2, §2.2 (EventDb)*

### Ekran Bileşenleri
- Etkinlik listesi: başlık, mekan, tarih, kalan koltuk sayısını gösteren basit bir tablo/liste.
- (Admin rolü için) yeni etkinlik ekleme formu: başlık, mekan, tarih, toplam koltuk input'ları.
- Liste yüklenirken "Yükleniyor..." metni, hata durumunda "Etkinlikler alınamadı" metni.

### Mimari ve Veri Yapısı
1. `EventDb` için EF Core `DbContext` ve `Event` entity'sini requirements.md §2.2'deki şemayla oluştur (`ConcurrencyStamp` alanını unutma — bu Faz 4'te kritik olacak).
2. Migration oluştur ve uygula.
3. Gateway'e `/api/events/**` route'unu ekle.

### Kodlama Süreci — Backend
1. `GET /api/events`: tüm etkinlikleri (auth gerektirmeden) listele.
2. `GET /api/events/{id}`: tek etkinlik detayını dön, bulunamazsa `404`.
3. `POST /api/events`: `[Authorize(Roles = "Admin")]` ile koru. Rol kontrolü JWT `role` claim'i üzerinden yapılır — Auth Service'e tekrar istek atılmaz (stateless doğrulama, NFR-3).
4. `PUT /api/events/{id}/reserve-seat`: bu endpoint'i **şimdilik iskelet olarak** oluştur (gerçek atomik SQL mantığını Faz 4'te, Reservation Service ile birlikte test ederek yazacaksın — çünkü tek başına test etmek anlamsız, gerçek senaryo iki servisin birlikte çalışmasıdır).

### Kodlama Süreci — Frontend
1. `EventService` (Angular) oluştur: `getEvents()`, `getEventById()`, `createEvent()` metodlarıyla, `HttpClient` kullanarak.
2. `EventListComponent`: `ngOnInit`'te `getEvents()` çağırır, sonucu bir `events$` observable veya basit bir `events` array'inde tutar, `*ngFor` ile render eder.
3. Admin ise etkinlik ekleme formunu göster (basit bir `AuthService.isAdmin()` kontrolü ile) — reactive forms kullan.

### Test Senaryoları
**Unit:**
- Admin olmayan bir kullanıcının `POST /api/events` çağrısının `403` döndüğünü doğrula.
- Var olmayan bir `eventId` ile `GET /api/events/{id}` çağrısının `404` döndüğünü doğrula.

**Integration:**
- Admin token'ı ile etkinlik oluştur → `GET /api/events` listesinde göründüğünü doğrula.

### ✅ Faz 3 Kontrol Listesi
- [ ] Angular etkinlik listesini Gateway üzerinden çekip ekranda gösteriyor mu?
- [ ] Admin olmayan kullanıcı etkinlik oluşturma denediğinde düzgün hata alıyor mu?

---

## Faz 4 — Rezervasyon + Simüle Ödeme (Rezervasyon Ekranı)

*İlgili requirements: FR-3, §2.3 (ReservationDb) — bu, projenin mimari açıdan en öğretici fazıdır (race condition, servisler arası senkron çağrı, atomik SQL).*

### Ekran Bileşenleri
- Etkinlik listesindeki her satıra "Rezerve Et" butonu ve koltuk sayısı input'u.
- Rezervasyon sonucu için durum göstergesi: "İşleniyor..." → "Ödeme simüle ediliyor..." → "Rezervasyon başarılı" / "Koltuk kalmadı" / "Ödeme başarısız".
- Kullanıcının kendi rezervasyonlarını gördüğü basit bir liste ekranı.

### Mimari ve Veri Yapısı
1. `ReservationDb` için `DbContext` ve `Reservation` entity'sini requirements.md §2.3'teki şemayla oluştur (`Status` ve `PaymentStatus` enum'larına dikkat).
2. Migration oluştur ve uygula.
3. **Kritik adım:** Event Service'teki `PUT /api/events/{id}/reserve-seat` endpoint'inin gerçek SQL'ini şimdi yaz:
   ```sql
   UPDATE Events
   SET AvailableSeats = AvailableSeats - @requestedSeats
   WHERE Id = @eventId AND AvailableSeats >= @requestedSeats;
   ```
   Etkilenen satır sayısı 0 ise `409 Conflict` dön. Bunu EF Core ile `ExecuteUpdateAsync` (EF Core 7+) veya ham SQL (`ExecuteSqlRawAsync`) ile yazabilirsin — ikisini de dene, farkını gör.
4. Reservation Service'ten Event Service'e `HttpClient` ile (Docker network içi adresle) senkron çağrı yapılandırması ekle (`IHttpClientFactory` ile named client öner).

### Kodlama Süreci — Backend
1. `POST /api/reservations` akışı:
   - JWT'den `userId`'yi oku (`User.FindFirst("sub")`), **body'den asla okuma**.
   - Event Service'e `PUT /api/events/{eventId}/reserve-seat` çağrısı yap.
   - `409` dönerse: rezervasyon oluşturma, kullanıcıya `409` ilet.
   - `200` dönerse: **simüle ödeme adımını** çalıştır — bir `IPaymentSimulator` servisi yaz, `Task.Delay(500)` ile yapay gecikme ekle, `PaymentStatus.Completed` dön (opsiyonel: %5 ihtimalle `Failed` dönecek bir rastgelelik ekle, test amaçlı).
   - Ödeme `Completed` ise: `Reservation` kaydını `Status: Reserved` ile kaydet, henüz RabbitMQ'ya bağlanmadıysan bu adımı **Faz 5'e kadar TODO olarak bırak** (yorum satırıyla işaretle).
   - Ödeme `Failed` ise: `Reservation` kaydını `Status: PaymentFailed` ile kaydet. **Önemli:** bu durumda Event Service'te düşürdüğün koltuğu geri artırman gerekir (telafi işlemi / compensating transaction) — bu, dağıtık sistemlerde neden "distributed transaction" yerine "saga" pattern'ine ihtiyaç duyulduğunu göreceğin yer.
2. `GET /api/reservations/me`: JWT'den `userId`'yi oku, sadece o kullanıcının rezervasyonlarını dön.

### Kodlama Süreci — Frontend
1. `ReservationService` (Angular): `createReservation()`, `getMyReservations()`.
2. Rezervasyon butonuna tıklayınca: buton disable edilir, "İşleniyor..." gösterilir, response'a göre başarı/hata mesajı state'e yazılır (basit bir `status: 'idle' | 'loading' | 'success' | 'error'` alanı yeterli, karmaşık bir state management kütüphanesi gerekmez).

### Test Senaryoları
**Unit:**
- Aynı anda (paralel) iki istek son 1 koltuğu almaya çalıştığında sadece birinin başarılı olduğunu doğrula (race condition testi — `Task.WhenAll` ile iki paralel çağrı simüle et).
- `PaymentStatus.Failed` senaryosunda koltuğun Event Service'e geri iade edildiğini doğrula.
- `userId`'nin body'den değil JWT'den okunduğunu doğrula (body'de farklı bir `userId` gönderip yine de token sahibinin ID'sinin kullanıldığını kontrol et).

**Integration:**
- Uçtan uca: login → etkinlik oluştur (1 koltuk) → iki farklı kullanıcı aynı anda rezerve etmeye çalışsın → birinin `201`, diğerinin `409` aldığını doğrula.

### ✅ Faz 4 Kontrol Listesi
- [ ] Paralel rezervasyon testi (race condition) yazıldı ve geçiyor mu?
- [ ] Ödeme başarısız olduğunda koltuk telafi (rollback) ediliyor mu?
- [ ] Angular'da rezervasyon durumu (loading/success/error) ekranda doğru yansıyor mu?

---

## Faz 5 — Asenkron Bilet Üretimi (RabbitMQ + Worker)

*İlgili requirements: FR-4 — bu fazda ekran değişmez, ama Faz 4'teki rezervasyon ekranına "biletiniz hazırlanıyor" bilgisini ekleyeceksin.*

### Mimari ve Veri Yapısı
1. MassTransit'i Reservation Service (publisher) ve yeni `TicketFlow.TicketWorker` projesine (consumer) ekle, RabbitMQ'ya bağlan.
2. `TicketReservedEvent` ve `TicketIssuedEvent` contract'larını requirements.md §4 ve project04.md §7'deki şemayla ayrı bir **paylaşılan contracts projesi/paketi** olarak tanımla (her iki tarafın da aynı sınıfı kullanması için — kopya kod yerine referans).
3. Worker projesinde kalıcı veri katmanı yok (stateless); ama idempotency kontrolü için basit bir in-memory veya Redis tabanlı "işlenmiş reservationId" seti tutman gerekir (FR-4.6).

### Kodlama Süreci — Backend
1. Faz 4'te TODO bıraktığın yeri tamamla: `Reservation` kaydı `Reserved` olarak kaydedildikten **sonra** `TicketReservedEvent`'i publish et.
2. Worker'da bir `IConsumer<TicketReservedEvent>` yaz:
   - `reservationId` daha önce işlendiyse mesajı sessizce ACK'le (idempotency).
   - QR kodu üret (basit bir GUID tabanlı string yeterli, gerçek QR image üretimi gerekmez).
   - PDF üretimini simüle et (`Console.WriteLine` veya log ile "PDF üretildi: {ticketId}").
   - E-posta gönderimini simüle et (log ile).
   - `TicketIssuedEvent`'i publish et.
3. Analytics Service'i henüz bağlamadıysan, bu event'leri şimdilik "dinleyen kimse yok" durumunda bırakabilirsin — Faz 6'da tüketilecek.

### Kodlama Süreci — Frontend
- Bu fazda zorunlu bir değişiklik yok. İstersen "Rezervasyonlarım" ekranına, arka planda bilet üretiminin tamamlandığını göstermek için birkaç saniye sonra durumu yeniden çeken basit bir polling (`setInterval` + `getMyReservations()`) ekleyebilirsin — WebSocket/SignalR bu kapsamda hedeflenmiyor.

### Test Senaryoları
**Unit:**
- Aynı `reservationId` ile consumer iki kez tetiklendiğinde ikinci bilet üretiminin engellendiğini doğrula (idempotency).

**Integration:**
- Reservation Service'ten gerçek bir `TicketReservedEvent` publish et, Worker'ın onu tükettiğini ve `TicketIssuedEvent`'i yayınladığını doğrula (test container'da gerçek RabbitMQ ile, veya MassTransit'in in-memory test harness'ı ile).

### ✅ Faz 5 Kontrol Listesi
- [ ] Rezervasyon sonrası Worker log'larında "bilet üretildi" mesajını görüyor musun?
- [ ] Worker'ı kasıtlı çökertip (container durdurup) tekrar ayağa kaldırdığında, kuyrukta bekleyen mesajın kaybolmadığını doğruladın mı?

---

## Faz 6 — Analitik / Trend (Trend Ekranı)

*İlgili requirements: FR-5, §2.4 (AnalyticsDb)*

### Ekran Bileşenleri
- Ana sayfada (veya Etkinlikler ekranının üstünde) "Trend Olan Etkinlikler" başlıklı küçük bir liste: etkinlik adı + satılan bilet sayısı.

### Mimari ve Veri Yapısı
1. `TicketFlow.AnalyticsService` projesine MongoDB driver'ını ekle, `EventAnalytics` koleksiyonunu requirements.md §2.4'teki şemayla tanımla.
2. MassTransit consumer'larını ekle: `IConsumer<TicketReservedEvent>` ve `IConsumer<TicketIssuedEvent>`.
3. Gateway'e `/api/analytics/**` route'unu ekle.

### Kodlama Süreci — Backend
1. `TicketReservedEvent` geldiğinde: ilgili `EventAnalytics` dokümanını `TotalTicketsSold` alanını artıracak şekilde upsert et (yoksa oluştur, varsa güncelle — MongoDB'nin `FindOneAndUpdate` + `upsert: true` özelliğini kullan).
2. `TicketIssuedEvent` geldiğinde: `TotalTicketsIssued` alanını artır.
3. `GET /api/analytics/trending-events`: `TotalTicketsSold`'a göre azalan sırada listele, sadece bu koleksiyonu sorgula — **EventDb veya ReservationDb'ye asla sorgu atma** (FR-5.4, CQRS ayrımının özü budur).

### Kodlama Süreci — Frontend
1. `AnalyticsService` (Angular): `getTrendingEvents()`.
2. `TrendingEventsComponent`: sayfa yüklendiğinde çeker, basit bir liste olarak render eder.

### Test Senaryoları
**Unit:**
- İki ayrı `TicketReservedEvent` aynı `eventId` için geldiğinde `TotalTicketsSold`'un doğru şekilde 2 kez arttığını doğrula.

**Integration:**
- Uçtan uca: rezervasyon yap → bilet üretilsin → birkaç saniye sonra `trending-events` endpoint'inde o etkinliğin sayacının arttığını doğrula (event-driven olduğu için testte küçük bir bekleme/retry mantığı gerekebilir — bu, eventual consistency kavramını pratikte görmen için iyi bir fırsat).

### ✅ Faz 6 Kontrol Listesi
- [ ] Analytics Service, Reservation/Event DB'lerine hiç doğrudan bağlanmıyor mu (sadece MongoDB + RabbitMQ)?
- [ ] Trend listesi Angular'da doğru sırayla görünüyor mu?

---

## Faz 7 — Sertleştirme (Rate Limiting, Test Tamamlama, Son Kontroller)

*Ekran yok — bu faz projeyi "demo"dan "gösterilebilir bir portfolyo projesi"ne taşır.*

### Mimari ve Veri Yapısı
1. Gateway'e `Microsoft.AspNetCore.RateLimiting` middleware'ini ekle: genel IP bazlı limit + `/api/reservations` için daha sıkı bir policy (bkz. requirements.md açık soru #1 — bir sayı seç, örn. dakikada 10, ve bunu dokümana geri yaz).
2. Her serviste Serilog ile structured logging kur (NFR-8) — en azından console sink yeterli.
3. Docker Compose'a `healthcheck` directive'leri ekle (requirements.md açık soru #3'ü burada kapat).

### Kodlama Süreci
- Eksik kalan Unit/Integration testlerini tamamla — her fazın "Test Senaryoları" bölümünü tek tek gözden geçirip hiçbirinin atlanmadığından emin ol.
- Swagger/OpenAPI dokümantasyonunu her serviste aç, Postman koleksiyonunu dışa aktar.

### Test
- Rate limiting'in gerçekten çalıştığını doğrulayan bir Integration test yaz: aynı IP'den limiti aşan sayıda istek at, `429 Too Many Requests` aldığını doğrula.

### ✅ Faz 7 Kontrol Listesi
- [ ] `POST /api/reservations`'a hızlı art arda istek atınca `429` alıyor musun?
- [ ] `docker compose up` sonrası tüm servisler health-check'ten geçiyor mu?
- [ ] Her faz için yazman gereken testlerin tümü yeşil mi?

---

## Sonraki Adım

Bu planı bitirdiğinde elinde: 6 bağımsız mikroservis, bir Gateway, event-driven bir arka plan işleme hattı, CQRS tabanlı bir analitik katmanı ve uçtan uca çalışan (tasarımsız ama işlevsel) bir Angular istemcisi olacak — yani requirements.md'de tanımlanan sistemin tamamı.

İstersen bir sonraki adımda, bu fazlardan birini (örneğin Faz 4'ü) senin yazacağın koda dönüştürmen için satır satır bir "implementasyon prompt'u" hazırlayabilirim — tıpkı diğer projelerinde yaptığımız gibi.
