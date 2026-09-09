import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../environments/environment";

export interface EventItem {
  id: string;
  title: string;
  venue: string;
  eventDate: string;
  totalSeats: number;
  availableSeats: number;
  createdAt: string;
}

export interface CreateEventRequest {
  title: string;
  venue: string;
  eventDate: string;
  totalSeats: number;
}

@Injectable({ providedIn: "root" })
export class EventService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/events`;

  constructor(private http: HttpClient) {}

  getEvents(): Observable<EventItem[]> {
    return this.http.get<EventItem[]>(this.apiUrl);
  }

  getEventById(id: string): Observable<EventItem> {
    return this.http.get<EventItem>(`${this.apiUrl}/${id}`);
  }

  createEvent(request: CreateEventRequest): Observable<EventItem> {
    return this.http.post<EventItem>(this.apiUrl, request);
  }
}
