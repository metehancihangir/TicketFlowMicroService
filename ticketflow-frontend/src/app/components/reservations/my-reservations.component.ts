import { Component, OnInit } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ReservationService, Reservation } from "../../services/reservation.service";

@Component({
  selector: "app-my-reservations",
  standalone: true,
  imports: [CommonModule],
  template: `
    <div>
      <h2>Rezervasyonlarım</h2>
      <p *ngIf="loading">Yükleniyor...</p>
      <p *ngIf="error">Rezervasyonlar alınamadı.</p>

      <table *ngIf="!loading && !error">
        <thead>
          <tr>
            <th>Etkinlik ID</th>
            <th>Koltuk</th>
            <th>Durum</th>
            <th>Ödeme</th>
            <th>Tarih</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let r of reservations">
            <td>{{ r.eventId }}</td>
            <td>{{ r.seatCount }}</td>
            <td>{{ r.status }}</td>
            <td>{{ r.paymentStatus }}</td>
            <td>{{ r.createdAt | date:"dd/MM/yyyy HH:mm" }}</td>
          </tr>
          <tr *ngIf="reservations.length === 0">
            <td colspan="5">Henüz rezervasyon yok.</td>
          </tr>
        </tbody>
      </table>
    </div>
  `
})
export class MyReservationsComponent implements OnInit {
  reservations: Reservation[] = [];
  loading = true;
  error = false;

  constructor(private reservationService: ReservationService) {}

  ngOnInit(): void {
    this.reservationService.getMyReservations().subscribe({
      next: (data) => {
        this.reservations = data;
        this.loading = false;
      },
      error: () => {
        this.error = true;
        this.loading = false;
      }
    });
  }
}
