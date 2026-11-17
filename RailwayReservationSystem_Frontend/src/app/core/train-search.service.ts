import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface TrainSearchResult {
  trainId: number;
  trainNumber: string;
  trainName: string;
  source: string;
  destination: string;
  totalSeats: number;
  scheduleId: number;
  departureTime: string;
  arrivalTime: string;
  arrivalNextDay: boolean;
  startDistanceKm?: number;
  endDistanceKm?: number;
  totalDistanceKm?: number;
}

@Injectable({ providedIn: 'root' })
export class TrainSearchService {
  private readonly http = inject(HttpClient);

  search(source: string, destination: string, date: string): Observable<TrainSearchResult[]> {
    const params = new HttpParams()
      .set('source', source)
      .set('destination', destination)
      .set('date', date);

    return this.http.get<TrainSearchResult[]>('/api/trains/search', { params });
  }
}
