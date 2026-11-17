import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface TrainRecord {
  trainId: number;
  trainNumber: string;
  trainName: string;
  source: string;
  destination: string;
  totalSeats: number;
  totalDistanceKm?: number;
  intermediateStations?: RouteStationRecord[];
}

export interface RouteStationRecord {
  stationName: string;
  distanceFromOriginKm: number;
}

export interface ScheduleRecord {
  scheduleId: number;
  trainId: number;
  departureTime: string;
  arrivalTime: string;
  arrivalNextDay: boolean;
  daysOfOperation: string;
}

export interface FareRecord {
  fareId: number;
  trainId: number;
  classType: string;
  amount: number;
}

export interface SeatRecord {
  seatId: number;
  trainId: number;
  scheduleId: number;
  seatNumber: string;
  classType: string;
  isAvailable: boolean;
  startDistanceKm?: number | null;
  endDistanceKm?: number | null;
}

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private readonly http = inject(HttpClient);

  getTrains(): Observable<TrainRecord[]> {
    return this.http.get<TrainRecord[]>('/api/trains');
  }

  getTrain(trainId: number): Observable<TrainRecord> {
    return this.http.get<TrainRecord>(`/api/trains/${trainId}`);
  }

  addTrain(train: Omit<TrainRecord, 'trainId'>): Observable<TrainRecord> {
    return this.http.post<TrainRecord>('/api/trains', train);
  }

  updateTrain(trainId: number, train: Omit<TrainRecord, 'trainId'>): Observable<TrainRecord> {
    return this.http.put<TrainRecord>(`/api/trains/${trainId}`, train);
  }

  deleteTrain(trainId: number): Observable<void> {
    return this.http.delete<void>(`/api/trains/${trainId}`);
  }

  getSchedules(trainId: number): Observable<ScheduleRecord[]> {
    return this.http.get<ScheduleRecord[]>(`/api/trains/${trainId}/schedules`);
  }

  getSchedule(scheduleId: number): Observable<ScheduleRecord> {
    return this.http.get<ScheduleRecord>(`/api/schedules/${scheduleId}`);
  }

  addSchedule(trainId: number, schedule: Omit<ScheduleRecord, 'scheduleId' | 'trainId'>): Observable<ScheduleRecord> {
    return this.http.post<ScheduleRecord>(`/api/trains/${trainId}/schedules`, schedule);
  }

  updateSchedule(scheduleId: number, schedule: Omit<ScheduleRecord, 'scheduleId' | 'trainId'>): Observable<ScheduleRecord> {
    return this.http.put<ScheduleRecord>(`/api/schedules/${scheduleId}`, schedule);
  }

  deleteSchedule(scheduleId: number): Observable<void> {
    return this.http.delete<void>(`/api/schedules/${scheduleId}`);
  }

  checkScheduleAvailability(trainId: number, scheduleId: number, journeyDate: string): Observable<boolean> {
    return this.http.get<boolean>(`/api/trains/${trainId}/schedules/${scheduleId}/availability`, {
      params: { journeyDate }
    });
  }

  getFares(trainId: number): Observable<FareRecord[]> {
    return this.http.get<FareRecord[]>(`/api/trains/${trainId}/fare`);
  }

  getFare(fareId: number): Observable<FareRecord> {
    return this.http.get<FareRecord>(`/api/fares/${fareId}`);
  }

  addFare(trainId: number, fare: { classType: string; amount: number }): Observable<unknown> {
    return this.http.post(`/api/trains/${trainId}/fare`, fare);
  }

  updateFare(fareId: number, fare: { classType: string; amount: number }): Observable<FareRecord> {
    return this.http.put<FareRecord>(`/api/fares/${fareId}`, fare);
  }

  deleteFare(fareId: number): Observable<void> {
    return this.http.delete<void>(`/api/fares/${fareId}`);
  }

  getSeats(trainId: number, scheduleId: number, journeyDate: string, startDistanceKm?: number, endDistanceKm?: number): Observable<SeatRecord[]> {
    const params: Record<string, string | number> = { scheduleId, journeyDate };
    if (startDistanceKm !== undefined && endDistanceKm !== undefined) {
      params['startDistanceKm'] = startDistanceKm;
      params['endDistanceKm'] = endDistanceKm;
    }
    return this.http.get<SeatRecord[]>(`/api/trains/${trainId}/seats`, { params });
  }

  getSeat(seatId: number): Observable<SeatRecord> {
    return this.http.get<SeatRecord>(`/api/seats/${seatId}`);
  }

  addSeat(trainId: number, scheduleId: number, seat: { seatNumber: string; classType: string; startDistanceKm: number | null; endDistanceKm: number | null }): Observable<unknown> {
    return this.http.post(`/api/trains/${trainId}/schedules/${scheduleId}/seats`, seat);
  }

  updateSeat(seatId: number, seat: { seatNumber: string; classType: string; startDistanceKm: number | null; endDistanceKm: number | null }): Observable<SeatRecord> {
    return this.http.put<SeatRecord>(`/api/seats/${seatId}`, seat);
  }

  deleteSeat(seatId: number): Observable<void> {
    return this.http.delete<void>(`/api/seats/${seatId}`);
  }
}
