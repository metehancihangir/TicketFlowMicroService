import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface TrendingEvent {
  eventId: string;
  totalTicketsSold: number;
  totalTicketsIssued: number;
}

@Injectable({
  providedIn: 'root'
})
export class AnalyticsService {
  // Gateway URL (Ocelot Analytics Route)
  private apiUrl = 'http://localhost:5000/api/analytics';

  constructor(private http: HttpClient) {}

  getTrendingEvents(): Observable<TrendingEvent[]> {
    return this.http.get<TrendingEvent[]>(`${this.apiUrl}/trending-events`);
  }
}
