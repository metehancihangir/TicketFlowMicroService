# TicketFlow — Requirements Document (Gereksinimler Dokümanı)

> Bu doküman, `project04.md` içinde tanımlanan genel mimariyi uygulanabilir, detaylı teknik gereksinimlere dönüştürür. Amaç: her servisin veri modelini, API sözleşmesini, teknoloji seçimini ve kapsam sınırlarını netleştirmek.

---

## 0. Kapsam Notları (Scope Notes)

Aşağıdaki kararlar proje sahibiyle netleştirilmiştir ve doküman boyunca bağlayıcıdır:

| Konu | Karar |
| :--- | :--- |
| **Veritabanı Stratejisi** | Database-per-service korunur; her servis kendi motorunu bağımsız seçebilir (aşağıda servis bazlı önerilir). Tek bir DB motoruna zorlanmaz. |
| **.NET Versiyonu** | **.NET 10** — tüm backend servisleri ve Gateway bu sürüm üzerinde geliştirilir. |
| **Test Kapsamı** | **Unit + Integration test** seviyesi yeterlidir. E2E test altyapısı bu proje kapsamı **dışındadır**. |
| **Ödeme (Payment)** | Ayrı bir Payment mikroservisi **eklenmez**. Reservation Service içine gömülü, **simüle edilmiş** bir ödeme onay adımı eklenir (gerçek gateway entegrasyonu yok). |
| **JWT Stratejisi** | Sadece kısa ömürlü **access token** kullanılır. Refresh token mekanizması bu sürümde **kapsam dışıdır**. |
| **Deployment** | Sadece **local Docker Compose** hedeflenir. Cloud (Azure/AWS) deployment bu sürümün kapsamı dışındadır. |
| **Frontend Tasarımı** | **Görsel/UX tasarımı yapılmayacaktır.** Angular SPA, yalnızca akışı uçtan uca doğrulamak için işlevsel (functional) bir test/demo istemcisidir. Component library, tema, responsive tasarım, marka kimliği gibi konular kapsam dışıdır — sadece formlar, listeler ve durum göstergeleri (loading/success/error) yeterlidir. |

---

## 1. Fonksiyonel Gereksinimler (Functional Requirements)

### FR-1: Kimlik Doğrulama (Auth Service)
- FR-1.1: Kullanıcılar e-posta + şifre ile kayıt olabilmelidir.
- FR-1.2: Şifreler BCrypt ile hash'lenerek saklanmalıdır (plaintext saklama yasaktır).
- FR-1.3: Kullanıcılar giriş yaptığında imzalı bir JWT access token almalıdır.
- FR-1.4: Token içinde en az `sub` (userId), `email`, `role` claim'leri bulunmalıdır.
- FR-1.5: Sistem `Admin` ve `User` olmak üzere iki rolü desteklemelidir.
- FR-1.6: Access token ömrü kısa tutulmalıdır (öneri: 30-60 dakika). Süresi dolan token ile yapılan isteklerde `401 Unauthorized` dönülmelidir.

### FR-2: Etkinlik Yönetimi (Event Service)
- FR-2.1: Adminler yeni etkinlik oluşturabilmelidir (başlık, mekan, tarih, toplam koltuk sayısı).
- FR-2.2: Herkes (auth gerekmeksizin) aktif etkinlikleri listeleyebilmelidir.
- FR-2.3: Etkinlik detayında kalan koltuk sayısı (`AvailableSeats`) görünmelidir.
- FR-2.4: Koltuk düşürme işlemi atomik olmalı; eşzamanlı isteklerde negatif koltuk sayısı **asla** oluşmamalıdır.
- FR-2.5: Koltuk yetersizse `409 Conflict` dönülmelidir.

### FR-3: Rezervasyon (Reservation Service)
- FR-3.1: Kimliği doğrulanmış kullanıcı, bir etkinlik için rezervasyon talebinde bulunabilmelidir.
- FR-3.2: `UserId` istek gövdesinden değil, JWT `sub` claim'inden okunmalıdır (istemci tarafından manipüle edilemez).
- FR-3.3: Rezervasyon öncesi Event Service'e senkron HTTP çağrısı yapılarak koltuk bloke edilmelidir.
- FR-3.4: Koltuk bloke işlemi başarılıysa, **simüle edilmiş ödeme adımı** tetiklenmelidir (bkz. FR-3.6).
- FR-3.5: Koltuk bloke başarısızsa rezervasyon oluşturulmamalı ve kullanıcıya anlamlı hata dönülmelidir.
- FR-3.6: **Simüle Ödeme:** Gerçek bir ödeme sağlayıcısına gidilmez; sistem içinde `PaymentStatus = Completed` olarak anında işaretlenen mock bir adım çalışır (yapay gecikme eklenebilir, örn. 500ms, gerçekçilik için). Bu adım başarısız senaryo simülasyonu da destekleyebilir (örn. %5 ihtimalle `PaymentStatus = Failed` — opsiyonel, test amaçlı).
- FR-3.7: Ödeme başarılıysa rezervasyon `Status: Reserved` olarak kaydedilmeli ve `TicketReservedEvent` RabbitMQ'ya yayınlanmalıdır.
- FR-3.8: Kullanıcı kendi rezervasyonlarını listeleyebilmelidir.

### FR-4: Bilet Üretimi (Ticket Delivery Worker)
- FR-4.1: Worker, `TicketReservedEvent` mesajını dinlemeli ve tüketmelidir.
- FR-4.2: Her bilet için benzersiz QR/barkod kodu üretilmelidir.
- FR-4.3: PDF bilet üretimi simüle edilmelidir (gerçek dosya render zorunlu değil; konsola/log'a yazdırma veya sahte dosya yolu üretimi yeterlidir).
- FR-4.4: E-posta gönderimi simüle edilmelidir (gerçek SMTP entegrasyonu gerekmez).
- FR-4.5: İşlem tamamlandığında `TicketIssuedEvent` yayınlanmalıdır.
- FR-4.6: Aynı `reservationId` için mükerrer bilet üretimi engellenmelidir (idempotency kontrolü).

### FR-5: Analitik (Analytics Service)
- FR-5.1: Servis, `TicketReservedEvent` ve `TicketIssuedEvent` mesajlarını dinlemelidir.
- FR-5.2: Her etkinlik için toplam satılan bilet sayısı ve satış hızı (örn. son 1 saatteki satış adedi) tutulmalıdır.
- FR-5.3: `GET /api/analytics/trending-events` endpoint'i en popüler etkinlikleri (satış adedine göre sıralı) dönmelidir.
- FR-5.4: Bu servis, transactional veritabanlarına (EventDb, ReservationDb) doğrudan sorgu **atmamalıdır** — sadece kendi projeksiyon veritabanını kullanmalıdır (CQRS Read-side ayrımı).

### FR-6: API Gateway
- FR-6.1: Tüm dış trafik tek bir port üzerinden (örn. `:5000`) karşılanmalıdır.
- FR-6.2: `/api/auth/**`, `/api/events/**`, `/api/reservations/**`, `/api/analytics/**` yolları ilgili servislere yönlendirilmelidir.
- FR-6.3: CORS politikası merkezi olarak Gateway'de tanımlanmalıdır; iç servislerde ayrı CORS konfigürasyonu olmamalıdır.
- FR-6.4: Global rate limiting uygulanmalıdır (öneri: IP bazlı, örn. dakikada 100 istek/IP — özellikle `POST /api/reservations` gibi kritik endpoint'lerde daha sıkı limit, örn. dakikada 10 istek/IP).

---

## 2. Servis Bazlı Veri Modelleri (Data Models)

### 2.1 Auth Service — `AuthDb`
**Önerilen DB:** PostgreSQL (alternatif: MySQL / SQL Server)

```
User
├── Id            : Guid (PK)
├── Email          : string (unique, indexed)
├── PasswordHash   : string
├── Role           : enum (Admin, User)
├── CreatedAt      : DateTime (UTC)
```

### 2.2 Event Service — `EventDb`
**Önerilen DB:** PostgreSQL (alternatif: MySQL / SQL Server)

```
Event
├── Id                : Guid (PK)
├── Title             : string
├── Venue             : string
├── Date              : DateTime (UTC)
├── TotalSeats        : int
├── AvailableSeats    : int
├── ConcurrencyStamp  : rowversion / concurrency token
├── CreatedAt         : DateTime (UTC)
├── CreatedByUserId   : Guid (FK - Admin referansı, cross-service, sadece Id saklanır)
```

### 2.3 Reservation Service — `ReservationDb`
**Önerilen DB:** PostgreSQL (alternatif: MySQL / SQL Server)

```
Reservation
├── Id              : Guid (PK)
├── UserId          : Guid (JWT'den okunur, cross-service referans)
├── EventId         : Guid (cross-service referans)
├── SeatCount       : int
├── Status          : enum (Pending, Reserved, PaymentFailed, Cancelled)
├── PaymentStatus   : enum (Pending, Completed, Failed)   // simüle ödeme
├── CreatedAt       : DateTime (UTC)
├── UpdatedAt       : DateTime (UTC)
```

### 2.4 Analytics Service — `AnalyticsDb`
**Önerilen DB:** MongoDB (alternatif: Redis — yüksek hız gerekiyorsa; SQLite — basit demo için)

```
EventAnalytics (Document)
├── EventId              : string (PK)
├── TotalTicketsSold     : int
├── TotalTicketsIssued   : int
├── LastSaleAt           : DateTime (UTC)
├── SalesVelocity        : object { last1h: int, last24h: int }
```

> Not: Ticket Delivery Worker kendi kalıcı veri katmanına ihtiyaç duymaz (stateless worker); ürettiği QR/PDF verisi event mesajı içinde taşınır veya geçici olarak loglanır.

---

## 3. API Endpoint Sözleşmeleri (Detaylı)

### Auth Service
| Method | Path | Auth | Request Body | Response |
| :--- | :--- | :--- | :--- | :--- |
| POST | `/api/auth/register` | Yok | `{ email, password }` | `201 { userId, email }` |
| POST | `/api/auth/login` | Yok | `{ email, password }` | `200 { token, expiresAt }` |

### Event Service
| Method | Path | Auth | Request Body | Response |
| :--- | :--- | :--- | :--- | :--- |
| GET | `/api/events` | Yok | — | `200 [ { id, title, venue, date, availableSeats } ]` |
| GET | `/api/events/{id}` | Yok | — | `200 { id, title, venue, date, totalSeats, availableSeats } ` |
| POST | `/api/events` | Admin | `{ title, venue, date, totalSeats }` | `201 { id }` |
| PUT | `/api/events/{id}/reserve-seat` | Internal (Reservation Service→Event Service) | `{ seatCount }` | `200 OK` / `409 Conflict` |

### Reservation Service
| Method | Path | Auth | Request Body | Response |
| :--- | :--- | :--- | :--- | :--- |
| POST | `/api/reservations` | User | `{ eventId, seatCount }` | `201 { reservationId, status, paymentStatus }` / `409 Conflict` |
| GET | `/api/reservations/me` | User | — | `200 [ { reservationId, eventId, status, createdAt } ]` |

### Analytics Service
| Method | Path | Auth | Request Body | Response |
| :--- | :--- | :--- | :--- | :--- |
| GET | `/api/analytics/trending-events` | Yok | — | `200 [ { eventId, totalTicketsSold, salesVelocity } ]` |

---

## 4. Event Sözleşmeleri (RabbitMQ / MassTransit)

`project04.md` içindeki `TicketReservedEvent` ve `TicketIssuedEvent` şemaları aynen korunur (bkz. orijinal doküman §7). Ek olarak:

### PaymentSimulationResult (internal, Reservation Service içi — kuyruğa çıkmaz)
```json
{
  "reservationId": "f9a2b8e4-...",
  "success": true,
  "simulatedAt": "2026-09-08T20:45:00Z"
}
```
> Bu event RabbitMQ'ya yayınlanmaz; Reservation Service'in kendi içindeki senkron/async metod çağrısıdır. Sadece dokümantasyon amaçlı burada listelenmiştir.

---

## 5. Teknoloji Yığını (Finalize Edilmiş)

| Katman | Teknoloji |
| :--- | :--- |
| Backend Framework | **.NET 10** Web API |
| API Gateway | YARP (.NET 10 üzerinde) |
| Mesajlaşma | RabbitMQ + MassTransit |
| Auth/Event/Reservation DB | PostgreSQL (alternatif: MySQL, SQL Server — servis bazında değişebilir) |
| Analytics DB | MongoDB (alternatif: Redis, SQLite) |
| Frontend | Angular (minimal, işlevsel — **UI/UX tasarımı yapılmayacak**) |
| Konteynerleştirme | Docker & Docker Compose (**sadece local**, cloud deployment kapsam dışı) |
| Test | xUnit (Unit) + WebApplicationFactory / Testcontainers (Integration) |
| Dokümantasyon | Swagger / OpenAPI, Postman Collection |

---

## 6. Frontend Gereksinimleri (Netleştirilmiş)

> **Önemli:** Bu proje bir UI/UX tasarım çalışması değildir. Angular SPA sadece dağıtık mimarinin uçtan uca çalıştığını göstermek için asgari, işlevsel bir test istemcisidir.

- FE-1: Herhangi bir component library (Material, Bootstrap vb.) veya özel tema **zorunlu değildir**; düz HTML form elemanları yeterlidir.
- FE-2: Responsive tasarım, marka kimliği, renk paleti gibi konular kapsam dışıdır.
- FE-3: Gerekli 3 ekran: Giriş/Kayıt, Etkinlikler & Trend Listesi, Rezervasyon.
- FE-4: HTTP Interceptor ile JWT'nin her isteğe `Authorization: Bearer <token>` olarak eklenmesi zorunludur.
- FE-5: Asenkron işlem durumları (loading / success / error) ekranda **metinsel olarak** gösterilmelidir — animasyon veya görsel efekt gerekmez.
- FE-6: Token `localStorage`'da saklanır (demo amaçlı; production-grade güvenlik pratiği bu kapsamda hedeflenmemektedir).

---

## 7. Non-Fonksiyonel Gereksinimler (NFR)

| ID | Gereksinim |
| :--- | :--- |
| NFR-1 | Koltuk rezervasyonunda race condition sıfır toleranslı olmalıdır (atomik SQL ile garanti edilir). |
| NFR-2 | Servisler birbirinden bağımsız deploy edilebilir olmalıdır (Docker Compose ile). |
| NFR-3 | Auth doğrulama her serviste local olarak (Auth Service'e gitmeden) yapılmalıdır — stateless JWT. |
| NFR-4 | Worker servisi hata durumunda mesajı kaybetmemeli; RabbitMQ ACK mekanizması kullanılmalıdır. |
| NFR-5 | Analytics Service, transactional DB'ler üzerinde yük oluşturmamalıdır (sadece event-driven besleme). |
| NFR-6 | Gateway seviyesinde rate limiting aktif olmalıdır. |
| NFR-7 | Test coverage hedefi: kritik iş mantığı (koltuk düşürme, rezervasyon, ödeme simülasyonu) için Unit + Integration test zorunludur. |
| NFR-8 | Loglama: her serviste yapılandırılmış (structured) loglama önerilir (öneri: Serilog). |

---

## 8. Kapsam Dışı Bırakılanlar (Out of Scope)

- Gerçek ödeme gateway entegrasyonu (Stripe, Iyzico vb.)
- Refresh token / token yenileme mekanizması
- Cloud deployment (Azure/AWS/GCP)
- E2E test otomasyonu
- Frontend UI/UX tasarımı, responsive tasarım, tema/marka çalışması
- Gerçek e-posta gönderimi (SMTP entegrasyonu)
- Gerçek PDF/QR dosya depolama (S3, Blob Storage vb.)
- Çoklu dil desteği (i18n)
- Bilet iptali / iade akışı (ileride ayrı bir faz olarak değerlendirilebilir)

---

## 9. Açık Sorular (Sonraki Görüşmede Netleştirilecek)

- [ ] Rate limiting için kesin sayısal değerler (dakika başına istek limiti) netleştirilecek mi, yoksa varsayılan öneriler mi kullanılacak?
- [ ] Ödeme simülasyonunda başarısız senaryo (%X ihtimalle `PaymentStatus: Failed`) test amaçlı eklensin mi, yoksa her zaman başarılı mı dönsün?
- [ ] Docker Compose dosyasında servis health-check'leri (`healthcheck` directive) detaylandırılacak mı?
