import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AdminApiService } from './admin-api.service';
import { AuthService } from './auth.service';
import { BookingService } from './booking.service';

describe('Railway API contracts', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  function complete(method: string, url: string, body?: unknown, response: object | string | number | boolean | ArrayBuffer | Blob | null = []) {
    const request = http.expectOne({ method, url });
    if (body !== undefined) expect(request.request.body).toEqual(body);
    request.flush(response);
  }

  it('uses every train, schedule, fare, and seat route with backend payloads', () => {
    const api = TestBed.inject(AdminApiService);
    const train = { trainNumber: '7001', trainName: 'Coastal Express', source: 'Mangaluru', destination: 'Bengaluru', totalSeats: 200, totalDistanceKm: 300, intermediateStations: [{ stationName: 'Udupi', distanceFromOriginKm: 60 }] };
    const schedule = { departureTime: '08:00:00', arrivalTime: '12:00:00', arrivalNextDay: false, daysOfOperation: 'Mon Wed Fri' };
    const fare = { classType: 'SL', amount: 500 };
    const seat = { seatNumber: 'A1', classType: 'SL', startDistanceKm: 0, endDistanceKm: 60 };

    api.getTrains().subscribe();
    complete('GET', '/api/trains');
    api.getTrain(7).subscribe();
    complete('GET', '/api/trains/7');
    api.addTrain(train).subscribe();
    complete('POST', '/api/trains', train);
    api.updateTrain(7, train).subscribe();
    complete('PUT', '/api/trains/7', train);
    api.deleteTrain(7).subscribe();
    complete('DELETE', '/api/trains/7');

    api.getSchedules(7).subscribe();
    complete('GET', '/api/trains/7/schedules');
    api.getSchedule(8).subscribe();
    complete('GET', '/api/schedules/8');
    api.addSchedule(7, schedule).subscribe();
    complete('POST', '/api/trains/7/schedules', schedule);
    api.updateSchedule(8, schedule).subscribe();
    complete('PUT', '/api/schedules/8', schedule);
    api.deleteSchedule(8).subscribe();
    complete('DELETE', '/api/schedules/8');
    api.checkScheduleAvailability(7, 8, '2026-10-02').subscribe();
    complete('GET', '/api/trains/7/schedules/8/availability?journeyDate=2026-10-02', undefined, true);

    api.getFares(7).subscribe();
    complete('GET', '/api/trains/7/fare');
    api.getFare(9).subscribe();
    complete('GET', '/api/fares/9');
    api.addFare(7, fare).subscribe();
    complete('POST', '/api/trains/7/fare', fare);
    api.updateFare(9, fare).subscribe();
    complete('PUT', '/api/fares/9', fare);
    api.deleteFare(9).subscribe();
    complete('DELETE', '/api/fares/9');

    api.getSeats(7, 8, '2026-10-02').subscribe();
    complete('GET', '/api/trains/7/seats?scheduleId=8&journeyDate=2026-10-02');
    api.getSeat(10).subscribe();
    complete('GET', '/api/seats/10');
    api.addSeat(7, 8, seat).subscribe();
    complete('POST', '/api/trains/7/schedules/8/seats', seat);
    api.updateSeat(10, seat).subscribe();
    complete('PUT', '/api/seats/10', seat);
    api.deleteSeat(10).subscribe();
    complete('DELETE', '/api/seats/10');
  });

  it('sends passenger registration and login requests and reads the JWT role', () => {
    const auth = TestBed.inject(AuthService);
    const details = { fullName: 'Casey Passenger', email: 'casey@example.com', phone: '5551234', password: 'Secret123' };
    auth.register(details).subscribe();
    complete('POST', '/api/auth/register', details, { userId: 12, fullName: details.fullName, email: details.email, phone: details.phone, roleName: 'Passenger' });

    auth.login({ email: details.email, password: details.password }).subscribe();
    const token = `header.${btoa(JSON.stringify({ nameid: '12', email: details.email, role: 'Passenger' }))}.signature`;
    complete('POST', '/api/auth/login', { email: details.email, password: details.password }, { token });
    expect(auth.claims()).toEqual({ userId: 12, email: details.email, role: 'Passenger' });
    auth.getUserDetails(12).subscribe((profile) => auth.profile.set(profile));
    complete('GET', '/api/users/12', undefined, {
      userId: 12,
      fullName: details.fullName,
      email: details.email,
      phone: details.phone,
      roleName: 'Passenger',
      createdAt: '2026-09-30T00:00:00Z'
    });
    expect(auth.profile()?.fullName).toBe(details.fullName);
    auth.logout();
    expect(auth.token()).toBeNull();
    expect(auth.profile()).toBeNull();
  });

  it('covers booking history, creation, PNR, cancellation, and ticket download routes', () => {
    const bookings = TestBed.inject(BookingService);
    const request = {
      trainId: 7,
      scheduleId: 8,
      journeyDate: '2026-10-02',
      source: 'Mangaluru',
      destination: 'Udupi',
      startDistanceKm: 0,
      endDistanceKm: 60,
      classType: 'SL',
      paymentId: 'internal-payment-1',
      passengers: [{ name: 'Casey Passenger', age: 31, gender: 'Other' }]
    };
    const response = { bookingId: 11, pnr: 'PNR123', userId: 12, passengers: [] };

    bookings.getForPassenger(12).subscribe();
    complete('GET', '/api/Bookings/user/12');
    const paymentOrderRequest = { trainId: 7, scheduleId: 8, journeyDate: '2026-10-02', classType: 'SL', passengerCount: 1, source: 'Mangaluru', destination: 'Udupi' };
    bookings.createRazorpayOrder(paymentOrderRequest).subscribe();
    complete('POST', '/api/payments/razorpay/orders', paymentOrderRequest, {
      paymentId: 'internal-payment-1', orderId: 'order_123', keyId: 'rzp_test_public', amount: 10000, currency: 'INR'
    });
    const verificationRequest = {
      paymentId: 'internal-payment-1',
      orderId: 'order_123',
      razorpayPaymentId: 'pay_123',
      razorpaySignature: 'signature'
    };
    bookings.verifyRazorpayPayment(verificationRequest).subscribe();
    complete('POST', '/api/payments/razorpay/verify', verificationRequest, {
      paymentId: 'internal-payment-1', status: 'Captured', amount: 100
    });
    bookings.create(request).subscribe();
    complete('POST', '/api/Bookings', request, response);
    bookings.getById(11).subscribe();
    complete('GET', '/api/Bookings/11', undefined, response);
    bookings.getByPnr('PNR123').subscribe();
    complete('GET', '/api/Bookings/pnr/PNR123', undefined, {
      pnr: 'PNR123',
      journeyDate: '2026-10-02',
      status: 'Confirmed'
    });
    bookings.cancel(11).subscribe();
    complete('DELETE', '/api/Bookings/11', undefined, { bookingId: 11, status: 'Cancelled', refundStatus: 'Refunded' });
    bookings.downloadTicket(11).subscribe();
    const ticket = http.expectOne({ method: 'GET', url: '/api/Bookings/ticket/11/download' });
    ticket.flush(new Blob(['ticket'], { type: 'application/pdf' }));
  });
});
