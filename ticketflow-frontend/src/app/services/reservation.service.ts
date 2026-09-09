import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../environments/environment";

export interface Reservation {
  id: string;
  userId: string;
  eventId: string;
  seatCount: number;
  status: string;
  paymentStatus: string;
  createdAt: string;
}

export interface CreateReservationRequest {
  eventId: string;
  seatCount: number;
}

@Injectable({ providedIn: "root" })
export class ReservationService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/reservations`;

  constructor(private http: HttpClient) {}

  createReservation(request: CreateReservationRequest): Observable<Reservation> {
    return this.http.post<Reservation>(this.apiUrl, request);
  }

  getMyReservations(): Observable<Reservation[]> {
    return this.http.get<Reservation[]>(`${this.apiUrl}/me`);
  }
}
