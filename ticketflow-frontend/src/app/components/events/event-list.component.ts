import { Component, OnInit } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from "@angular/forms";
import { EventService, EventItem } from "../../services/event.service";
import { AuthService } from "../../services/auth.service";
import { ReservationService } from "../../services/reservation.service";

type ReserveStatus = "idle" | "loading" | "success" | "error";

interface ReserveState {
  status: ReserveStatus;
  message: string;
}

@Component({
  selector: "app-event-list",
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div>
      <h2>Etkinlikler</h2>

      <p *ngIf="loading">Yükleniyor...</p>
      <p *ngIf="error">Etkinlikler alınamadı.</p>

      <table *ngIf="!loading && !error">
        <thead>
          <tr>
            <th>Başlık</th>
            <th>Mekan</th>
            <th>Tarih</th>
            <th>Kalan Koltuk</th>
            <th>İşlem</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let event of events">
            <td>{{ event.title }}</td>
            <td>{{ event.venue }}</td>
            <td>{{ event.eventDate | date:'dd/MM/yyyy HH:mm' }}</td>
            <td>{{ event.availableSeats }} / {{ event.totalSeats }}</td>
            <td>
              <button
                [disabled]="reserveStates[event.id]?.status === 'loading' || event.availableSeats === 0"
                (click)="onReserve(event)">
                {{ reserveStates[event.id]?.status === 'loading' ? 'İşleniyor...' : 'Rezerve Et' }}
              </button>
              <span *ngIf="reserveStates[event.id]?.status === 'success'" style="color: green; margin-left: 8px;">
                ✓ {{ reserveStates[event.id]?.message }}
              </span>
              <span *ngIf="reserveStates[event.id]?.status === 'error'" style="color: red; margin-left: 8px;">
                ✗ {{ reserveStates[event.id]?.message }}
              </span>
            </td>
          </tr>
          <tr *ngIf="events.length === 0">
            <td colspan="5">Henüz etkinlik yok.</td>
          </tr>
        </tbody>
      </table>

      <!-- Admin: Etkinlik Ekleme Formu -->
      <div *ngIf="authService.isAdmin()">
        <h3>Yeni Etkinlik Ekle</h3>
        <form [formGroup]="createForm" (ngSubmit)="onCreate()">
          <div>
            <label>Başlık:</label>
            <input formControlName="title" type="text" />
          </div>
          <div>
            <label>Mekan:</label>
            <input formControlName="venue" type="text" />
          </div>
          <div>
            <label>Tarih:</label>
            <input formControlName="eventDate" type="datetime-local" />
          </div>
          <div>
            <label>Toplam Koltuk:</label>
            <input formControlName="totalSeats" type="number" />
          </div>
          <button type="submit" [disabled]="createForm.invalid">Ekle</button>
        </form>
        <p *ngIf="createMessage">{{ createMessage }}</p>
      </div>
    </div>
  `
})
export class EventListComponent implements OnInit {
  events: EventItem[] = [];
  loading = true;
  error = false;
  createMessage = "";
  createForm: FormGroup;
  reserveStates: { [eventId: string]: ReserveState } = {};

  constructor(
    public authService: AuthService,
    private eventService: EventService,
    private reservationService: ReservationService,
    private fb: FormBuilder
  ) {
    this.createForm = this.fb.group({
      title: ["", Validators.required],
      venue: ["", Validators.required],
      eventDate: ["", Validators.required],
      totalSeats: [100, [Validators.required, Validators.min(1)]]
    });
  }

  ngOnInit(): void {
    this.eventService.getEvents().subscribe({
      next: (data) => {
        this.events = data;
        this.loading = false;
      },
      error: () => {
        this.error = true;
        this.loading = false;
      }
    });
  }

  onReserve(event: EventItem): void {
    this.reserveStates[event.id] = { status: "loading", message: "Ödeme simüle ediliyor..." };

    this.reservationService.createReservation({ eventId: event.id, seatCount: 1 }).subscribe({
      next: () => {
        this.reserveStates[event.id] = { status: "success", message: "Rezervasyon başarılı!" };
        event.availableSeats -= 1;
      },
      error: (err) => {
        const msg = err.status === 409
          ? "Koltuk kalmadı."
          : err.status === 422
          ? "Ödeme başarısız."
          : "Bir hata oluştu.";
        this.reserveStates[event.id] = { status: "error", message: msg };
      }
    });
  }

  onCreate(): void {
    if (this.createForm.invalid) return;
    const val = this.createForm.value;
    this.eventService.createEvent({
      title: val.title,
      venue: val.venue,
      eventDate: new Date(val.eventDate).toISOString(),
      totalSeats: val.totalSeats
    }).subscribe({
      next: (ev) => {
        this.events.push(ev);
        this.createForm.reset({ totalSeats: 100 });
        this.createMessage = `"${ev.title}" etkinliği eklendi.`;
      },
      error: () => {
        this.createMessage = "Etkinlik eklenemedi.";
      }
    });
  }
}
