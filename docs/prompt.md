# TicketFlow — Ajan Çalışma Protokolü (prompt.md)

> **Bu dosya, IDE içindeki kodlama asistanına (Claude Code veya benzeri) verilecek sabit bir sistem talimatıdır.** Asistan, proje bitene kadar bu protokolü **harfiyen ve sapmadan** her fazda tekrar eder. Adımların sırası değiştirilemez, atlanamaz ve birleştirilemez. Her adım, bir öncekinin çıktısına bağımlıdır.
>
> **Referans dosyalar (asistan her döngüde bunları okumalıdır):**
> - `phases.md` — fazların tanımı, kapsamı ve her fazın "✅ Kontrol Listesi" (Definition of Done kaynağı)
> - `requirements.md` — fonksiyonel/mimari gereksinimlerin tek doğruluk kaynağı (source of truth)
> - `docs/tasks-faz[N].md` — o anki fazın görev listesi (bu protokolün 1. adımında üretilir)

---

## 0. Genel Kurallar (Her Adımda Geçerli)

1. **Faz atlanamaz.** Faz N tamamlanmadan (yani `docs/tasks-faz[N].md` içindeki tüm maddeler `[x]` olmadan ve `phases.md`'deki ilgili "✅ Kontrol Listesi" karşılanmadan) Faz N+1'e geçilmez.
2. **Sıra sabittir:** Branch oluştur → Görev listesi çıkar → Implement et → Commit & Push → PR aç & Merge et → `main`'e dön → Bir sonraki faza geç.
3. **Her commit'ten önce ilgili testler çalıştırılmalı ve geçmelidir.** Test kırmızıyken commit atılmaz, PR açılmaz.
4. **Görev listesindeki bir madde, ancak karşılık gelen kod gerçekten çalışır ve test edilmiş durumdaysa `[x]` olarak işaretlenir.** Sadece "yazıldı" diye işaretlenmez — çalıştığı doğrulanmalıdır.
5. Asistan, her adımın başında hangi adımda olduğunu ve hangi fazı işlediğini açıkça belirtmelidir (örn. "Faz 2 — Adım 3: Commit & Push").
6. Belirsiz veya `phases.md`/`requirements.md` ile çelişen bir durumla karşılaşılırsa, asistan **varsayım yapıp devam etmek yerine** kullanıcıya sorar ve onay bekler.

---

## 1. Döngü — Faz Başına Uygulanacak 6 Adım

Aşağıdaki `[FAZ]` ifadesi, o anki faz numarasıyla değiştirilir (0, 1, 2, ...). Asistan, hangi fazda olduğunu şu şekilde tespit eder: `docs/` klasöründeki en yüksek numaralı `tasks-faz[N].md` dosyasına bakar; o dosyadaki tüm maddeler `[x]` ise ve ilgili branch `main`'e merge edilmişse bir sonraki faza (`N+1`) geçilir. Hiç `tasks-faz*.md` yoksa Faz 0'dan başlanır.

### Adım 0 — Faz Branch'ini Oluştur

```bash
git checkout main
git pull
git checkout -b phase-[FAZ]
```

**Kural:** Branch adı daima `phase-[FAZ]` formatında olmalıdır (örn. `phase-0`, `phase-4`). Branch, güncel `main`'den türetilmelidir — bu yüzden `checkout -b` öncesi mutlaka `main` üzerinde `pull` yapılır.

---

### Adım 1 — Görev Listesini Oluştur (`docs/tasks-faz[FAZ].md`)

`phases.md` dosyasındaki **ilgili faz bölümü** (`## Faz [FAZ] — ...`) baştan sona incelenir. Bu bölümdeki her alt başlık (*Ekran Bileşenleri*, *Mimari ve Veri Yapısı*, *Kodlama Süreci — Backend*, *Kodlama Süreci — Frontend*, *Test Senaryoları*, *✅ Kontrol Listesi*) tek tek görev maddelerine bölünür.

**Zorunlu format kuralları:**
- Her görev, numaralandırılmış bir onay kutusu (`- [ ]`) satırıdır.
- Her görev maddesinin sonunda, `phases.md`'deki karşılık gelen bölüme referans **parantez içinde** belirtilir. Format: `(bkz. phases.md → Faz [FAZ] → [Bölüm Adı])`.
- Görevler, `phases.md`'deki sıralamayla birebir aynı sırada olmalıdır (önce Mimari/Veri Yapısı, sonra Backend, sonra Frontend, sonra Test — asistan bu sırayı kendiliğinden değiştirmez).
- Tek bir `phases.md` maddesi, gerekiyorsa birden fazla alt göreve bölünebilir (örn. "EF Core DbContext oluştur" ve "Migration oluştur ve uygula" ayrı iki madde olabilir), ancak hiçbir madde atlanamaz veya birleştirilip özetlenemez.
- Dosyanın en başında şu üstbilgi bulunur:

```markdown
# Faz [FAZ] — Görev Listesi

> Kaynak: phases.md → "Faz [FAZ] — [Faz Başlığı]"
> Durum: Devam Ediyor

## Mimari ve Veri Yapısı
- [ ] ... (bkz. phases.md → Faz [FAZ] → Mimari ve Veri Yapısı)

## Kodlama Süreci — Backend
- [ ] ...

## Kodlama Süreci — Frontend
- [ ] ...

## Test Senaryoları
- [ ] ...

## Faz Kontrol Listesi (Definition of Done)
- [ ] ... (bkz. phases.md → Faz [FAZ] → ✅ Kontrol Listesi)
```

Bu dosya oluşturulduktan sonra asistan, listeyi kullanıcıya gösterip **Adım 2'ye geçmeden önce onay ister** — çünkü görev kırılımının doğruluğu, sonraki tüm implementasyonun temelidir.

---

### Adım 2 — Görevleri Implement Et ve İşaretle

1. `docs/tasks-faz[FAZ].md` dosyasındaki görevler **yukarıdan aşağıya, sırayla** tek tek uygulanır. Bir görev bitmeden diğerine geçilmez.
2. Bir görev tamamlandığında:
   - İlgili kod yazılır/değiştirilir.
   - Varsa ilgili test(ler) çalıştırılır ve geçtiği doğrulanır.
   - Ancak bu ikisi de sağlandıktan sonra `docs/tasks-faz[FAZ].md` dosyasında ilgili satır `- [ ]` yerine `- [x]` olarak güncellenir.
3. "Test Senaryoları" başlığı altındaki her madde, gerçek bir Unit veya Integration test dosyası olarak yazılmalıdır — sözde/yorum satırı olarak bırakılamaz.
4. "Faz Kontrol Listesi (Definition of Done)" bölümündeki maddeler **en son** işaretlenir; bunlar, fazın bütünsel olarak (tüm servisler birlikte ayakta) doğrulandığını gösterir.
5. Bu adımın sonunda `docs/tasks-faz[FAZ].md` içindeki **tüm** maddeler `[x]` olmalıdır. Aksi halde bir sonraki adıma geçilmez.

---

### Adım 3 — Commit ve Push

```bash
git add .
git commit -m "[COMMIT MESAJI]"
git push -u origin phase-[FAZ]
```

**Commit mesajı kuralları:**
- İngilizce, kısa (50 karakter civarı), emir kipinde (imperative mood) yazılır. Örnek kalıp: `<tip>: <kısa açıklama>`.
- `<tip>` şu değerlerden biri olmalı: `feat`, `fix`, `test`, `chore`, `docs`, `infra`.
- Örnekler:
  - Faz 0 için: `infra: scaffold microservices and docker-compose setup`
  - Faz 1 için: `feat: implement auth service register/login with jwt`
  - Faz 4 için: `feat: add reservation flow with atomic seat locking and payment simulation`
- Mesaj, o fazda **gerçekte yapılanı** özetlemelidir — `phases.md`'deki faz başlığının birebir kopyası olmamalıdır.
- Eğer faz kapsamı büyükse (örn. Faz 4), asistan tek commit yerine görev grupları bazında (Mimari → Backend → Frontend → Test) birden fazla anlamlı commit atabilir; bu durumda her commit kendi kapsamını doğru yansıtan ayrı bir mesaja sahip olmalıdır. Bu durumda `push` sadece en son commit'ten sonra bir kez yapılır.

---

### Adım 4 — Pull Request Aç ve Merge Et

```bash
gh pr create --base main --head phase-[FAZ] \
  --title "Phase [FAZ]: [FAZ BAŞLIĞI]" \
  --body "Closes tasks in docs/tasks-faz[FAZ].md. See phases.md → Faz [FAZ] for scope."

gh pr merge --merge
```

**Kurallar:**
- PR başlığı formatı sabittir: `Phase [FAZ]: [phases.md'deki faz başlığı]` (örn. `Phase 4: Rezervasyon + Simüle Ödeme`).
- PR açıklamasında mutlaka `docs/tasks-faz[FAZ].md` dosyasına ve `phases.md`'deki ilgili bölüme referans verilir.
- **Pull request (PR) açma butonuna basıldıktan sonra, sayfa 10 saniye beklenmeli ve yenilenmelidir (F5). Ancak sayfa yenilendikten sonra Merge pull request butonuna basılmalıdır.**
- Merge stratejisi **merge commit** (`--merge`) olarak sabittir; `--squash` veya `--rebase` kullanılmaz — böylece her fazın commit geçmişi PR üzerinden okunabilir kalır.
- `gh pr merge` komutu, GitHub CLI'ın kimlik doğrulaması (`gh auth login`) daha önce yapılmış olmasını gerektirir; asistan bunu varsaymadan önce `gh auth status` ile kontrol edebilir.

---

### Adım 5 — `main`'e Dön ve Güncelle

```bash
git checkout main
git pull
```

Bu adım, bir sonraki fazın **güncel `main` üzerinden** başlamasını garanti eder.

---

### Adım 6 — Döngüyü Tekrarla

`[FAZ]` değeri bir artırılır ve **Adım 0'a geri dönülür.** `phases.md` içinde tanımlı son faz (Faz 7 — Sertleştirme) tamamlanıp merge edildiğinde döngü sona erer ve asistan projenin tamamlandığını raporlar.

---

## 2. Faz Geçişi Öncesi Zorunlu Doğrulama

Bir fazdan diğerine geçmeden önce asistan şu üç şeyi teyit eder:

- [ ] `docs/tasks-faz[FAZ].md` içindeki **tüm** maddeler `[x]`.
- [ ] `phases.md`'deki ilgili fazın "✅ Kontrol Listesi" bölümündeki her madde fiilen doğrulanmış (manuel test veya otomatik test ile).
- [ ] `phase-[FAZ]` branch'i `main`'e merge edilmiş ve yerel `main` güncellenmiş.

Bu üçünden biri eksikse, asistan bir sonraki faza **geçmez**, eksik kalan adıma geri döner ve kullanıcıyı bilgilendirir.

---

## 3. Hata / İstisna Durumları

| Durum | Asistanın Davranışı |
| :--- | :--- |
| Test kırmızı çıkarsa | Commit atılmaz. Önce hata giderilir, test tekrar çalıştırılır. |
| `phases.md` ile mevcut kod arasında çelişki bulunursa | Kullanıcıya sorulur; asistan kendiliğinden `phases.md`'yi değiştirmez. |
| Bir görev, o fazın kapsamında olmayan bir değişikliği gerektiriyor gibi görünüyorsa | Görev genişletilmez; kullanıcıya bildirilir, gerekirse `phases.md`/`requirements.md` güncellemesi ayrı bir onay konusu olarak açılır. |
| `gh pr merge` başarısız olursa (örn. conflict) | Önce `main` ile `phase-[FAZ]` arasında konflikt manuel çözülür, testler tekrar koşulur, sonra merge tekrar denenir. |
| Kullanıcı bir fazı manuel olarak durdurup değişiklik isterse | Asistan mevcut görev listesini günceller, ancak `[x]` işaretli tamamlanmış maddeleri kullanıcı onayı olmadan tekrar `[ ]`'a çevirmez. |

---

## 4. Özet Akış Şeması

```
main (güncel)
   │
   ▼
git checkout -b phase-N          [Adım 0]
   │
   ▼
docs/tasks-fazN.md oluştur        [Adım 1]  ← phases.md'den türetilir, kullanıcı onayı beklenir
   │
   ▼
görevleri implement et + [x] işaretle   [Adım 2]  ← her görev sonrası test
   │
   ▼
git add . && commit && push       [Adım 3]
   │
   ▼
gh pr create && gh pr merge       [Adım 4]
   │
   ▼
git checkout main && git pull     [Adım 5]
   │
   ▼
N = N + 1  →  Adım 0'a dön        [Adım 6]
```
