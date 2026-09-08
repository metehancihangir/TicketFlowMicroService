# TicketFlow — Dağıtık Etkinlik & Bilet Satış Sistemi

TicketFlow, yüksek trafikli konser, festival, tiyatro ve konferans gibi etkinliklerin bilet satış süreçlerini kesintisiz, güvenli ve ölçeklenebilir bir biçimde yönetmek üzere tasarlanmış modern, dağıtık (microservice) tabanlı bir biletleme platformudur.

---

## 1. Genel Bakış ve Çözülen Problem

Geleneksel monolitik bilet satış sistemleri, özellikle popüler etkinliklerin biletleri satışa çıktığı anda eşzamanlı binlerce kullanıcının akınına uğrar. Bu durum şu temel sorunları doğurur:
* **Yarış Durumu (Race Condition) & Çifte Satış (Overselling):** Son kalan koltukların aynı milisaniyede birden fazla kullanıcıya satılması.
* **Tek Hata Noktası (Single Point of Failure):** PDF oluşturma veya e-posta gönderimi gibi ağır I/O işlemlerinin ana sistemi kilitlemesi veya çökertmesi.
* **Veritabanı Kilitlenmesi (Database Bottleneck):** Ana sayfa arama/trend sorguları ile rezervasyon/satış işlemlerinin aynı veritabanı kaynakları için yarışması.
* **Karmaşık İstemci Yönetimi:** İstemcilerin doğrudan iç servislerin IP/port adreslerini bilmesi ve her serviste ayrı CORS/güvenlik konfigürasyonlarının gerekmesi.

**TicketFlow**, bu sorunları mikroservis mimarisinin temel prensipleri olan **API Gateway (YARP)**, **Database-per-Service**, **Stateless Authentication (JWT)**, **Asenkron Kuyruk Yönetimi (RabbitMQ)** ve **CQRS tabanlı Read-side ayrımı** ile çözer.

---

## 2. Kullanıcılara ve İşletmeye Sağladığı Değer

| Paydaş | Sağlanan Değer |
| :--- | :--- |
| **Son Kullanıcı (Bilet Alıcı)** | Sıra beklemeden, anlık geri bildirimle hızlı bilet rezervasyonu; PDF ve QR biletinin gecikmeksizin arka planda üretilip iletilmesi. |
| **Etkinlik Organizatörü / Admin** | Koltuk kapasitesinin milisaniyelik doğrulukla yönetilmesi, çifte satış riskinin sıfıra inmesi ve popüler etkinliklerin anlık analitiği. |
| **Geliştirici & DevOps Ekibi** | Birbirinden bağımsız ölçeklenebilen (örneğin satış anında sadece Reservation ve Event servislerini büyütme) ve izole hata alanlarına sahip modüler yapı. |

---

## 3. Temel Özellikler (Key Features)

* **Tek Giriş Kapısı (YARP API Gateway):** Dış dünyaya sadece tek bir port açılır; yönlendirme, ters proxy, CORS ve global rate limiting merkezi gateway üzerinden yönetilir.
* **Mikroservis Mimarisi (5 Bağımsız Servis):** Her servisin kendi veritabanı, sorumluluğu ve bağımsız yaşam döngüsü bulunur.
* **Stateless JWT Doğrulaması:** Kimlik doğrulama merkezi bir darboğaz oluşturmaz; servisler token imzasını CPU seviyesinde yerel olarak doğrular.
* **Atomik Koltuk Rezervasyonu:** Race condition'ı önleyen seviyeli veritabanı kilitleri ve atomik SQL sorguları ile sıfır hatalı stok düşümü.
* **Event-Driven Arka Plan İşleme (RabbitMQ):** PDF ve QR kod oluşturma gibi ağır işlemler ana HTTP akışını bekletmez; RabbitMQ üzerinden asenkron işletilir.
* **CQRS Mantığında Analitik & Popülerlik:** Raporlama ve arama sorguları ana transactional veritabanını yormaz; özel projeksiyon veritabanından saniyeler içinde beslenir.
* **Yalın Test/Demo Arayüzü (Angular SPA):** Tasarım yükünden arındırılmış, mimari akışı uçtan uca doğrulamaya odaklı işlevsel istemci katmanı.
* **Konteyner Desteği:** Tüm servisler, gateway, kuyruk ve veritabanları Docker & Docker Compose ile tek komutla ayağa kaldırılabilir.

---

## 4. Sistem Mimarisi ve Akış Şeması

```
                    [ İstemci Katmanı (Angular SPA) ]
                                    │
                                    ▼ (HTTP / JSON - Tek Port: :5000)
                    ┌───────────────────────────────┐
                    │      API Gateway (YARP)       │
                    │  - Routing & Reverse Proxy    │
                    │  - Centralized CORS & Logging │
                    └───────┬───────┬───────┬───────┘
                            │       │       │
       ┌────────────────────┘       │       └────────────────────┐
       ▼                            ▼                            ▼
/api/auth/**                  /api/events/**             /api/reservations/**
┌──────────────┐             ┌──────────────┐            ┌──────────────────┐
│ Auth Service │             │Event Service │            │Reservation Service│
│ (JWT Issuer) │             │ (EventDb)    │            │ (ReservationDb)  │
└──────────────┘             └──────┬───────┘            └────────┬─────────┘
                                    ▲                             │
                                    │ (Senkron HTTP Koltuk Düşümü)│
                                    └─────────────────────────────┤
                                                                  │ (TicketReservedEvent)
                                                                  ▼
                                                      ┌───────────────────────┐
                                                      │  RabbitMQ Message     │
                                                      │       Broker          │
                                                      └───────────┬───────────┘
                                                                  │
                                           ┌──────────────────────┴──────────────────────┐
                                           ▼                                             ▼
                              ┌─────────────────────────┐                   ┌─────────────────────────┐
                              │ Ticket Delivery Worker  │                   │   Analytics Service     │
                              │ (PDF & QR Simülasyonu)  │                   │      (AnalyticsDb)      │
                              └────────────┬────────────┘                   └─────────────▲───────────┘
                                           │ (TicketIssuedEvent)                          │
                                           └──────────────────────────────────────────────┘
                                                                                          │
                                             /api/analytics/** ───────────────────────────┘
                                             (YARP üzerinden doğrudan Analytics Service'e)
```

---

## 5. Servisler ve Sorumluluklar

### 5.1. API Gateway (YARP — Yet Another Reverse Proxy)
* **Görevi:** İstemci ile iç mikroservisler arasında tek giriş noktası (Single Entry Point) olarak görev yapar.
* **Yönlendirme Kuralları:**
  * `/api/auth/**` ➔ `Auth Service`
  * `/api/events/**` ➔ `Event Service`
  * `/api/reservations/**` ➔ `Reservation Service`
  * `/api/analytics/**` ➔ `Analytics Service`
* **Katkıları:** İç servislerin portlarını dış dünyaya kapatır, CORS politikalarını merkezileştirir ve Angular uygulamasının tek bir base URL (`http://localhost:5000`) ile çalışmasını sağlar.

### 5.2. Auth Service (Kimlik ve Yetkilendirme)
* **Görevi:** Kullanıcı kaydı, şifre hash'leme (BCrypt), kullanıcı girişi ve imzalı JWT (JSON Web Token) üretimi.
* **Roller:** `Admin`, `User`.
* **Veritabanı:** `AuthDb` (Id, Email, PasswordHash, Role, CreatedAt).
* **Temel Endpoint'ler:**
  * `POST /api/auth/register` — Yeni kullanıcı oluşturur.
  * `POST /api/auth/login` — Doğrulama sonrası Claims (`sub/userId`, `email`, `role`) içeren JWT döner.

### 5.3. Event Service (Etkinlik ve Kontenjan Yönetimi)
* **Görevi:** Etkinliklerin tanımlanması, mekan bilgileri, tarih ve kalan kontenjanın takibi.
* **Veritabanı:** `EventDb` (Id, Title, Venue, Date, TotalSeats, AvailableSeats, ConcurrencyStamp).
* **Temel Endpoint'ler:**
  * `GET /api/events` — Tüm aktif etkinlikleri listeler (Public).
  * `GET /api/events/{id}` — Etkinlik detayını döner.
  * `POST /api/events` — Yeni etkinlik ekler (`Role: Admin`).
  * `PUT /api/events/{id}/reserve-seat` — Koltuk kontenjanını atomik olarak düşürür (Reservation Service çağırır).

### 5.4. Reservation Service (Rezervasyon Motoru)
* **Görevi:** Bilet satın alma/ayırma sürecini başlatan ve koordine eden ana orkestrasyon noktasıdır.
* **Veritabanı:** `ReservationDb` (Id, UserId, EventId, SeatCount, Status, CreatedAt).
* **İş Akışı:**
  1. İstemci `POST /api/reservations` çağrısı yapar (`Authorization: Bearer <token>` ile).
  2. Kullanıcı ID'si gövdeden değil, doğrulanmış JWT içindeki `sub` claim'inden okunur.
  3. `Event Service`'e senkron HTTP çağrısı yapılarak istenen koltuk adedi atomik şekilde bloke edilir.
  4. Başarılı olursa rezervasyon `Status: Reserved` olarak kaydedilir.
  5. RabbitMQ üzerinde `TicketReservedEvent` mesajı yayınlanır (publish).

### 5.5. Ticket Delivery Worker (Arka Plan Üretim Servisi)
* **Görevi:** HTTP arayüzü olmayan, arka planda RabbitMQ dinleyen asenkron bir worker servistir.
* **İş Akışı:**
  1. Kuyruktan `TicketReservedEvent` mesajını tüketir (consume).
  2. Bilete özel benzersiz QR/Barkod kodu ve doğrulama anahtarı üretir.
  3. Sanal PDF biletini render eder ve depolama/konsol simülasyonunu tamamlar.
  4. E-posta bildirim adımını simüle eder.
  5. İşlem bittiğinde kuyruğa `TicketIssuedEvent` mesajını fırlatır.

### 5.6. Analytics & Search Service (İstatistik ve Trend Analitiği)
* **Görevi:** Ana sistemin transactional veri tabanını raporlama yükünden kurtaran Read-side projeksiyon servisidir (CQRS prensibi).
* **Veritabanı:** `AnalyticsDb` (Hafif Document DB / MongoDB veya optimize edilmiş ayrı tablo).
* **İş Akışı:**
  1. Kuyruktan `TicketReservedEvent` ve `TicketIssuedEvent` mesajlarını dinler.
  2. Her etkinlik için satılan bilet sayısını ve hızını anlık artırır.
  3. `GET /api/analytics/trending-events` endpoint'i ile ana sayfaya en popüler etkinlikleri sıfır join ve sıfır hesaplama maliyetiyle döner.

---

## 6. Frontend Katmanı (Angular Demo SPA)

Frontend katmanı, görsel tasarım karmaşasından ziyade dağıtık mimari akışını uçtan uca test etmek ve doğrulamak amacıyla yalın (minimal) bir Angular uygulaması olarak kurgulanır:

* **Odak Noktası:** Tasarım/CSS detayları yerine HTTP Interceptor ile JWT yönetimi, Gateway yönlendirmeleri ve asenkron işlem durumlarının ekrana yansıtılması.
* **Temel Ekranlar:**
  1. **Giriş / Kayıt (Auth View):** Basit e-posta ve şifre formları; alınan JWT token'ı `localStorage` üzerinde saklar ve istek header'larına `Bearer <token>` olarak ekler.
  2. **Etkinlikler & Popülerler (Events & Trending View):** Gateway üzerinden gelen etkinlikleri ve anlık trend olan etkinlik listesini gösterir.
  3. **Rezervasyon & Biletleme (Reservation View):** Etkinlik üzerinden tek tıkla `POST /api/reservations` çağrısı başlatır; dönen rezervasyon kodunu ve arka plan kuyruk durumunu ekranda simüle eder.

---

## 7. Event Sözleşmeleri (Event Contracts)

RabbitMQ üzerinde servisler arası taşınan mesaj modelleri:

### TicketReservedEvent
```json
{
  "reservationId": "f9a2b8e4-1234-4c5d-9abc-1234567890ab",
  "userId": "usr-8821",
  "userEmail": "user@example.com",
  "eventId": "evt-5501",
  "seatCount": 2,
  "reservedAt": "2026-09-08T20:45:00Z"
}
```

### TicketIssuedEvent
```json
{
  "ticketId": "tck-9904",
  "reservationId": "f9a2b8e4-1234-4c5d-9abc-1234567890ab",
  "eventId": "evt-5501",
  "qrCode": "TICKETFLOW-EVT5501-9904",
  "issuedAt": "2026-09-08T20:45:03Z"
}
```

---

## 8. Teknik Güvenlik ve Dayanıklılık Prensipleri

1. **Race Condition Önleme:**
   `Event Service` üzerinde koltuk sayısı güncellenirken koşullu atomik SQL kullanılır:
   ```sql
   UPDATE Events
   SET AvailableSeats = AvailableSeats - @requestedSeats
   WHERE Id = @eventId AND AvailableSeats >= @requestedSeats;
   ```
   Eğer etkilenen satır sayısı 0 ise işlem doğrudan `409 Conflict (Koltuk Kalmadı)` hatasıyla reddedilir.
2. **Stateless Yetkilendirme:**
   Her istekte Auth Service'e gitmek yerine, servisler asimetrik/simetrik anahtarla token geçerliliğini ve rolünü bağımsız olarak doğrular.
3. **Idempotency & Hata Toleransı:**
   Worker servisi herhangi bir sebeple çökerse mesaj kuyrukta güvenle bekler (ACK mekanizması). Tekrar tüketim durumunda aynı rezervasyona mükerrer bilet basılmaması için `reservationId` tekillik kontrolü yapılır.

---

## 9. Teknoloji Yığını

* **API Gateway:** YARP (Yet Another Reverse Proxy) / .NET
* **Backend Servisleri:** C# / .NET 8 veya .NET 9 Web API
* **Asenkron Mesajlaşma:** RabbitMQ & MassTransit
* **Frontend:** Angular (Minimal SPA, HttpInterceptor, Reactive Forms)
* **Veritabanları:**
  * `AuthDb`, `EventDb`, `ReservationDb`: PostgreSQL veya MySQL / SQL Server
  * `AnalyticsDb`: MongoDB veya optimize edilmiş SQLite / Redis
* **Konteynerleştirme & Orkestrasyon:** Docker, Docker Compose
* **Test & Belgelendirme:** Swagger / OpenAPI, Postman, xUnit