import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AnalyticsService, TrendingEvent } from '../../services/analytics.service';
import { EventService } from '../../services/event.service'; // Etkinlik isimlerini çekmek için

@Component({
  selector: 'app-trending-events',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card shadow-sm mb-4">
      <div class="card-header bg-warning text-dark">
        <h5 class="card-title mb-0">🔥 Trend Olan Etkinlikler</h5>
      </div>
      <div class="card-body p-0">
        <ul class="list-group list-group-flush" *ngIf="trendingEvents.length > 0; else noTrends">
          <li class="list-group-item d-flex justify-content-between align-items-center" *ngFor="let item of trendingEvents">
            <span>
              <strong>{{ getEventName(item.eventId) }}</strong>
            </span>
            <span class="badge bg-danger rounded-pill">{{ item.totalTicketsSold }} Bilet Satıldı</span>
          </li>
        </ul>
        <ng-template #noTrends>
          <div class="p-3 text-muted text-center">Henüz trend olan bir etkinlik yok.</div>
        </ng-template>
      </div>
    </div>
  `
})
export class TrendingEventsComponent implements OnInit {
  trendingEvents: TrendingEvent[] = [];
  eventNames: { [key: string]: string } = {};

  constructor(
    private analyticsService: AnalyticsService,
    private eventService: EventService
  ) {}

  ngOnInit(): void {
    this.analyticsService.getTrendingEvents().subscribe({
      next: (data) => {
        this.trendingEvents = data;
        // İsimleri getirebilmek için EventService'den etkinlikleri çekiyoruz
        this.eventService.getEvents().subscribe(events => {
          events.forEach(e => {
            this.eventNames[e.id] = e.name;
          });
        });
      },
      error: (err) => console.error('Trend etkinlikler yüklenemedi', err)
    });
  }

  getEventName(eventId: string): string {
    return this.eventNames[eventId] || 'Yükleniyor...';
  }
}
