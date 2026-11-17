import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface PassengerBooking {
  bookingId: number;
  pnr: string;
  userId: number;
  trainId: number;
  scheduleId: number;
  classType: string;
  journeyDate: string;
  status: string;
  numberOfTickets: number;
  totalFare: number;
  source?: string;
  destination?: string;
  passengers: BookingPassenger[];
}

export interface BookingPassenger {
  passengerId: number;
  name: string;
  age: number;
  gender: string;
  seatId: number;
}

export interface CreateBookingRequest {
  trainId: number;
  scheduleId: number;
  journeyDate: string;
  source: string;
  destination: string;
  startDistanceKm: number;
  endDistanceKm: number;
  classType: string;
  paymentId: string;
  passengers: Array<{ name: string; age: number; gender: string }>;
}

export interface PublicBookingStatus {
  pnr: string;
  journeyDate: string;
  status: string;
}

export interface RazorpayOrderRequest {
  trainId: number;
  scheduleId: number;
  journeyDate: string;
  classType: string;
  passengerCount: number;
  source: string;
  destination: string;
}

export interface RazorpayOrderResponse {
  paymentId: string;
  orderId: string;
  keyId: string;
  amount: number;
  currency: string;
}

export interface VerifyRazorpayPaymentRequest {
  paymentId: string;
  orderId: string;
  razorpayPaymentId: string;
  razorpaySignature: string;
}

export interface VerifiedPaymentResponse {
  paymentId: string;
  status: string;
  amount: number;
}

export interface BookingCancellationResponse {
  bookingId: number;
  status: string;
  refundStatus: string;
}

@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly http = inject(HttpClient);

  getForPassenger(userId: number): Observable<PassengerBooking[]> {
    return this.http.get<PassengerBooking[]>(`/api/Bookings/user/${userId}`);
  }

  create(request: CreateBookingRequest): Observable<PassengerBooking> {
    return this.http.post<PassengerBooking>('/api/Bookings', request);
  }

  createRazorpayOrder(request: RazorpayOrderRequest): Observable<RazorpayOrderResponse> {
    return this.http.post<RazorpayOrderResponse>('/api/payments/razorpay/orders', request);
  }

  verifyRazorpayPayment(request: VerifyRazorpayPaymentRequest): Observable<VerifiedPaymentResponse> {
    return this.http.post<VerifiedPaymentResponse>('/api/payments/razorpay/verify', request);
  }

  getById(bookingId: number): Observable<PassengerBooking> {
    return this.http.get<PassengerBooking>(`/api/Bookings/${bookingId}`);
  }

  getByPnr(pnr: string): Observable<PublicBookingStatus> {
    return this.http.get<PublicBookingStatus>(`/api/Bookings/pnr/${encodeURIComponent(pnr)}`);
  }

  cancel(bookingId: number): Observable<BookingCancellationResponse> {
    return this.http.delete<BookingCancellationResponse>(`/api/Bookings/${bookingId}`);
  }

  downloadTicket(bookingId: number): Observable<Blob> {
    return this.http.get(`/api/Bookings/ticket/${bookingId}/download`, { responseType: 'blob' });
  }
}
